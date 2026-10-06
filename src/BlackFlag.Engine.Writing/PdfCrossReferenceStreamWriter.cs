using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing;

/// <summary>Builds and writes a PDF cross-reference stream for a full rewrite.</summary>
internal static class PdfCrossReferenceStreamWriter
{
    private const int EntryWidth = 11;
    private static readonly PdfName XRefName = PdfName.FromLatin1("XRef");
    private static readonly PdfName WName = PdfName.FromLatin1("W");

    internal static void Write(
        Stream destination,
        PdfWritePlan plan,
        IReadOnlyDictionary<PdfObjectIdentifier, long> offsets,
        PdfObjectIdentifier crossReferenceIdentifier,
        long crossReferenceOffset)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(offsets);
        ArgumentOutOfRangeException.ThrowIfNegative(crossReferenceOffset);

        if (crossReferenceIdentifier.GenerationNumber != 0)
        {
            throw new InvalidOperationException(
                "A newly allocated cross-reference stream must use generation zero.");
        }

        Dictionary<int, CrossReferenceEntry> entries = BuildEntries(offsets);
        int size = checked(crossReferenceIdentifier.ObjectNumber + 1);
        byte[] data = BuildData(
            entries,
            crossReferenceIdentifier,
            crossReferenceOffset,
            size);
        PdfStream stream = CreateStream(plan, size, data);

        PdfIndirectObjectSerializer.Write(
            destination,
            crossReferenceIdentifier,
            stream);
    }

    private static Dictionary<int, CrossReferenceEntry> BuildEntries(
        IReadOnlyDictionary<PdfObjectIdentifier, long> offsets)
    {
        var result = new Dictionary<int, CrossReferenceEntry>();

        foreach (var pair in offsets)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(pair.Value);

            if (!result.TryAdd(
                    pair.Key.ObjectNumber,
                    new CrossReferenceEntry(
                        pair.Value,
                        pair.Key.GenerationNumber)))
            {
                throw new InvalidOperationException(
                    $"A cross-reference stream cannot describe multiple active generations for object number {pair.Key.ObjectNumber}.");
            }
        }

        return result;
    }

    private static byte[] BuildData(
        IReadOnlyDictionary<int, CrossReferenceEntry> entries,
        PdfObjectIdentifier crossReferenceIdentifier,
        long crossReferenceOffset,
        int size)
    {
        byte[] data = new byte[checked(size * EntryWidth)];

        WriteEntry(data, 0, 0, 0, 65535);

        for (int objectNumber = 1; objectNumber < size; objectNumber++)
        {
            if (objectNumber == crossReferenceIdentifier.ObjectNumber)
            {
                WriteEntry(data, objectNumber, 1, crossReferenceOffset, 0);
                continue;
            }

            if (entries.TryGetValue(objectNumber, out CrossReferenceEntry entry))
            {
                WriteEntry(
                    data,
                    objectNumber,
                    1,
                    entry.Offset,
                    entry.GenerationNumber);
                continue;
            }

            WriteEntry(data, objectNumber, 0, 0, 0);
        }

        return data;
    }

    private static void WriteEntry(
        byte[] data,
        int objectNumber,
        byte type,
        long fieldTwo,
        int fieldThree)
    {
        int offset = checked(objectNumber * EntryWidth);
        data[offset] = type;
        WriteUnsignedBigEndian(data.AsSpan(offset + 1, 8), checked((ulong)fieldTwo));
        WriteUnsignedBigEndian(data.AsSpan(offset + 9, 2), checked((ulong)fieldThree));
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

    private static PdfStream CreateStream(
        PdfWritePlan plan,
        int size,
        byte[] data)
    {
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
            { PdfNames.Root, new PdfIndirectReference(plan.RootObject) }
        };

        if (plan.InformationObject is { } informationObject)
        {
            dictionary.Add(PdfNames.Info, new PdfIndirectReference(informationObject));
        }

        return new PdfStream(dictionary, data);
    }

    private readonly record struct CrossReferenceEntry(
        long Offset,
        int GenerationNumber);
}
