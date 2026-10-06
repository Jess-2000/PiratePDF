using System.Globalization;
using System.Text;
using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>Writes changed-object subsections for a traditional incremental cross-reference table.</summary>
internal static class PdfIncrementalTraditionalCrossReferenceWriter
{
    private const long MaximumTraditionalOffset = 9_999_999_999;

    internal static void Write(
        Stream destination,
        IReadOnlyDictionary<PdfObjectIdentifier, long> offsets)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(offsets);

        CrossReferenceEntry[] entries = BuildEntries(offsets);

        WriteAscii(destination, "xref\n");

        for (int index = 0; index < entries.Length;)
        {
            int end = FindContiguousEnd(entries, index);
            WriteSubsection(destination, entries, index, end);
            index = end;
        }
    }

    private static CrossReferenceEntry[] BuildEntries(
        IReadOnlyDictionary<PdfObjectIdentifier, long> offsets)
    {
        var byObjectNumber = new Dictionary<int, CrossReferenceEntry>();

        foreach (var pair in offsets)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(pair.Value);

            if (pair.Value > MaximumTraditionalOffset)
            {
                throw new InvalidOperationException(
                    "A traditional cross-reference table cannot represent an object offset greater than 9,999,999,999 bytes.");
            }

            if (!byObjectNumber.TryAdd(
                    pair.Key.ObjectNumber,
                    new CrossReferenceEntry(pair.Key, pair.Value)))
            {
                throw new InvalidOperationException(
                    $"An incremental traditional cross-reference table cannot describe multiple active generations for object number {pair.Key.ObjectNumber}.");
            }
        }

        return [.. byObjectNumber.Values.OrderBy(entry => entry.Identifier.ObjectNumber)];
    }

    private static int FindContiguousEnd(
        CrossReferenceEntry[] entries,
        int start)
    {
        int end = start + 1;

        while (end < entries.Length &&
            entries[end].Identifier.ObjectNumber ==
            entries[end - 1].Identifier.ObjectNumber + 1)
        {
            end++;
        }

        return end;
    }

    private static void WriteSubsection(
        Stream destination,
        CrossReferenceEntry[] entries,
        int start,
        int end)
    {
        int firstObjectNumber = entries[start].Identifier.ObjectNumber;
        int count = end - start;

        WriteAscii(
            destination,
            string.Create(
                CultureInfo.InvariantCulture,
                $"{firstObjectNumber} {count}\n"));

        for (int index = start; index < end; index++)
        {
            CrossReferenceEntry entry = entries[index];
            WriteAscii(
                destination,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{entry.Offset:0000000000} {entry.Identifier.GenerationNumber:00000} n \n"));
        }
    }

    private static void WriteAscii(Stream destination, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        destination.Write(bytes);
    }

    private readonly record struct CrossReferenceEntry(
        PdfObjectIdentifier Identifier,
        long Offset);
}
