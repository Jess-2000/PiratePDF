using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing;

/// <summary>Writes a sparse cross-reference stream for an incremental PDF revision.</summary>
internal static class PdfIncrementalCrossReferenceStreamWriter
{
    private const int EntryWidth = 11;
    private static readonly PdfName XRefName = PdfName.FromLatin1("XRef");
    private static readonly PdfName WName = PdfName.FromLatin1("W");
    private static readonly PdfName IndexName = PdfName.FromLatin1("Index");

    internal static void Write(
        Stream destination,
        PdfIncrementalWritePlan plan,
        IReadOnlyDictionary<PdfObjectIdentifier, long> offsets,
        PdfObjectIdentifier crossReferenceIdentifier,
        long crossReferenceOffset,
        int size)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(offsets);
        ArgumentOutOfRangeException.ThrowIfNegative(crossReferenceOffset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        if (crossReferenceIdentifier.GenerationNumber != 0)
        {
            throw new InvalidOperationException(
                "A newly allocated cross-reference stream must use generation zero.");
        }

        CrossReferenceEntry[] entries = BuildEntries(
            offsets,
            crossReferenceIdentifier,
            crossReferenceOffset);
        CrossReferenceRange[] ranges = BuildRanges(entries);
        byte[] data = BuildData(entries);
        PdfStream stream = CreateStream(plan, size, ranges, data);

        PdfIndirectObjectSerializer.Write(
            destination,
            crossReferenceIdentifier,
            stream);
    }

    private static CrossReferenceEntry[] BuildEntries(
        IReadOnlyDictionary<PdfObjectIdentifier, long> offsets,
        PdfObjectIdentifier crossReferenceIdentifier,
        long crossReferenceOffset)
    {
        var byObjectNumber = new Dictionary<int, CrossReferenceEntry>();

        foreach (var pair in offsets)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(pair.Value);

            if (!byObjectNumber.TryAdd(
                    pair.Key.ObjectNumber,
                    new CrossReferenceEntry(
                        pair.Key.ObjectNumber,
                        pair.Value,
                        pair.Key.GenerationNumber)))
            {
                throw new InvalidOperationException(
                    $"An incremental cross-reference stream cannot describe multiple active generations for object number {pair.Key.ObjectNumber}.");
            }
        }

        if (!byObjectNumber.TryAdd(
                crossReferenceIdentifier.ObjectNumber,
                new CrossReferenceEntry(
                    crossReferenceIdentifier.ObjectNumber,
                    crossReferenceOffset,
                    crossReferenceIdentifier.GenerationNumber)))
        {
            throw new InvalidOperationException(
                "The allocated cross-reference stream object number collides with a changed object.");
        }

        return [.. byObjectNumber.Values.OrderBy(entry => entry.ObjectNumber)];
    }

    private static CrossReferenceRange[] BuildRanges(
        CrossReferenceEntry[] entries)
    {
        var ranges = new List<CrossReferenceRange>();

        for (int start = 0; start < entries.Length;)
        {
            int end = start + 1;

            while (end < entries.Length &&
                entries[end].ObjectNumber == entries[end - 1].ObjectNumber + 1)
            {
                end++;
            }

            ranges.Add(
                new CrossReferenceRange(
                    entries[start].ObjectNumber,
                    end - start));
            start = end;
        }

        return [.. ranges];
    }

    private static byte[] BuildData(
        CrossReferenceEntry[] entries)
    {
        byte[] data = new byte[checked(entries.Length * EntryWidth)];

        for (int index = 0; index < entries.Length; index++)
        {
            CrossReferenceEntry entry = entries[index];
            int offset = checked(index * EntryWidth);
            data[offset] = 1;
            WriteUnsignedBigEndian(
                data.AsSpan(offset + 1, 8),
                checked((ulong)entry.Offset));
            WriteUnsignedBigEndian(
                data.AsSpan(offset + 9, 2),
                checked((ulong)entry.GenerationNumber));
        }

        return data;
    }

    private static PdfStream CreateStream(
        PdfIncrementalWritePlan plan,
        int size,
        CrossReferenceRange[] ranges,
        byte[] data)
    {
        var indexValues = new List<PdfObject>(ranges.Length * 2);

        foreach (CrossReferenceRange range in ranges)
        {
            indexValues.Add(new PdfInteger(range.FirstObjectNumber));
            indexValues.Add(new PdfInteger(range.Count));
        }

        var dictionary = new PdfDictionary
        {
            { PdfNames.Type, XRefName },
            { PdfNames.Size, new PdfInteger(size) },
            {
                WName,
                new PdfArray(
                [
                    new PdfInteger(1),
                    new PdfInteger(8),
                    new PdfInteger(2)
                ])
            },
            { IndexName, new PdfArray(indexValues) },
            { PdfNames.Root, new PdfIndirectReference(plan.WritePlan.RootObject) },
            {
                PdfNames.Previous,
                new PdfInteger(plan.SourceRevision.CrossReferenceOffset)
            }
        };

        if (plan.WritePlan.InformationObject is { } informationObject)
        {
            dictionary.Add(
                PdfNames.Info,
                new PdfIndirectReference(informationObject));
        }

        return new PdfStream(dictionary, data);
    }

    private static void WriteUnsignedBigEndian(
        Span<byte> destination,
        ulong value)
    {
        for (int index = destination.Length - 1; index >= 0; index--)
        {
            destination[index] = (byte)(value & 0xFF);
            value >>= 8;
        }

        if (value != 0)
        {
            throw new InvalidOperationException(
                "The cross-reference value does not fit the configured field width.");
        }
    }

    private readonly record struct CrossReferenceEntry(
        int ObjectNumber,
        long Offset,
        int GenerationNumber);

    private readonly record struct CrossReferenceRange(
        int FirstObjectNumber,
        int Count);
}
