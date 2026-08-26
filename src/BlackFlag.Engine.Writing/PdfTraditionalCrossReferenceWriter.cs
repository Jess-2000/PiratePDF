using System.Globalization;
using System.Text;

using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>Writes a traditional PDF cross-reference table.</summary>
internal static class PdfTraditionalCrossReferenceWriter
{
    private const long MaximumTraditionalOffset = 9_999_999_999;

    internal static void Write(
        Stream destination,
        IReadOnlyDictionary<PdfObjectIdentifier, long> offsets,
        int highestObjectNumber)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(offsets);
        ArgumentOutOfRangeException.ThrowIfNegative(highestObjectNumber);

        var byObjectNumber = BuildObjectNumberMap(offsets);

        WriteAscii(destination, "xref\n");
        WriteAscii(
            destination,
            string.Create(
                CultureInfo.InvariantCulture,
                $"0 {highestObjectNumber + 1}\n"));
        WriteAscii(destination, "0000000000 65535 f \n");

        for (int objectNumber = 1; objectNumber <= highestObjectNumber; objectNumber++)
        {
            if (!byObjectNumber.TryGetValue(objectNumber, out var entry))
            {
                WriteAscii(destination, "0000000000 00000 f \n");
                continue;
            }

            if (entry.Offset > MaximumTraditionalOffset)
            {
                throw new InvalidOperationException(
                    "A traditional cross-reference table cannot represent an object offset greater than 9,999,999,999 bytes.");
            }

            WriteAscii(
                destination,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{entry.Offset:0000000000} {entry.Identifier.GenerationNumber:00000} n \n"));
        }
    }

    private static Dictionary<int, CrossReferenceEntry> BuildObjectNumberMap(
        IReadOnlyDictionary<PdfObjectIdentifier, long> offsets)
    {
        var result = new Dictionary<int, CrossReferenceEntry>();

        foreach (var pair in offsets)
        {
            if (!result.TryAdd(
                    pair.Key.ObjectNumber,
                    new CrossReferenceEntry(pair.Key, pair.Value)))
            {
                throw new InvalidOperationException(
                    $"A traditional cross-reference table cannot represent multiple active generations for object number {pair.Key.ObjectNumber}.");
            }
        }

        return result;
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
