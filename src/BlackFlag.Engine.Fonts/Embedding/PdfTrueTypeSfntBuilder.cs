using System.Text;

namespace BlackFlag.Engine.Fonts.Embedding;

internal static class PdfTrueTypeSfntBuilder
{
    public static (byte[] Glyf, byte[] Loca) RebuildGlyphTables(
        ReadOnlySpan<byte> glyf,
        IReadOnlyList<uint> offsets,
        IReadOnlySet<int> selected,
        int locaFormat,
        int glyphCount)
    {
        using var output = new MemoryStream();
        var newOffsets = new uint[glyphCount + 1];

        for (int glyph = 0; glyph < glyphCount; glyph++)
        {
            newOffsets[glyph] =
                checked((uint)output.Length);

            if (!selected.Contains(glyph))
            {
                continue;
            }

            uint start = offsets[glyph];
            uint end = offsets[glyph + 1];

            if (end > start)
            {
                output.Write(
                    glyf.Slice(
                        checked((int)start),
                        checked((int)(end - start))));
            }

            while ((output.Length & 3) != 0)
            {
                output.WriteByte(0);
            }
        }

        newOffsets[glyphCount] =
            checked((uint)output.Length);

        byte[] loca =
            BuildLoca(
                newOffsets,
                locaFormat);

        return (output.ToArray(), loca);
    }

    public static byte[] Build(
        IReadOnlyDictionary<string, byte[]> tables,
        int maximumGeneratedBytes,
        string subsetKind)
    {
        KeyValuePair<string, byte[]>[] ordered =
        [
            .. tables.OrderBy(
                static pair => pair.Key,
                StringComparer.Ordinal)
        ];

        // Widen before alignment and addition. Every table length is an int;
        // the full sum fits in long even when it cannot fit in an array.
        long generatedLength = checked(12L + ordered.Length * 16L);
        var offsets = new Dictionary<string, long>(StringComparer.Ordinal);

        foreach (KeyValuePair<string, byte[]> table in ordered)
        {
            offsets[table.Key] = generatedLength;
            generatedLength = checked(
                generatedLength + ((table.Value.LongLength + 3L) & ~3L));
        }

        if (generatedLength > maximumGeneratedBytes)
        {
            throw new InvalidDataException(
                $"The generated {subsetKind} subset is {generatedLength} bytes, exceeding the configured {maximumGeneratedBytes}-byte limit.");
        }

        byte[] output = new byte[checked((int)generatedLength)];

        PdfTrueTypeBinary.W32(output, 0, 0x00010000);
        PdfTrueTypeBinary.W16(
            output,
            4,
            checked((ushort)ordered.Length));

        PdfTrueTypeBinary.WriteSearchFields(
            output,
            ordered.Length);

        for (int index = 0; index < ordered.Length; index++)
        {
            WriteTable(
                output,
                12 + index * 16,
                checked((int)offsets[ordered[index].Key]),
                ordered[index]);
        }

        int headOffset = checked((int)offsets["head"]);

        uint adjustment =
            unchecked(
                0xB1B0AFBAu -
                PdfTrueTypeBinary.Checksum(
                    output,
                    false));

        PdfTrueTypeBinary.W32(
            output,
            headOffset + 8,
            adjustment);

        return output;
    }

    private static byte[] BuildLoca(
        uint[] offsets,
        int locaFormat)
    {
        byte[] loca =
            locaFormat == 0
                ? new byte[checked(offsets.Length * 2)]
                : new byte[checked(offsets.Length * 4)];

        for (int index = 0; index < offsets.Length; index++)
        {
            if (locaFormat == 0)
            {
                if ((offsets[index] & 1) != 0 ||
                    offsets[index] / 2 > ushort.MaxValue)
                {
                    throw new NotSupportedException(
                        "The generated short loca table cannot represent the subset offsets.");
                }

                PdfTrueTypeBinary.W16(
                    loca,
                    index * 2,
                    checked((ushort)(offsets[index] / 2)));
            }
            else
            {
                PdfTrueTypeBinary.W32(
                    loca,
                    index * 4,
                    offsets[index]);
            }
        }

        return loca;
    }

    private static void WriteTable(
        Span<byte> output,
        int record,
        int offset,
        KeyValuePair<string, byte[]> table)
    {
        Encoding.ASCII.GetBytes(table.Key)
            .CopyTo(output.Slice(record, 4));

        table.Value.CopyTo(output[offset..]);

        if (table.Key == "head")
        {
            output.Slice(offset + 8, 4).Clear();
        }

        PdfTrueTypeBinary.W32(
            output,
            record + 4,
            PdfTrueTypeBinary.Checksum(
                output.Slice(
                    offset,
                    table.Value.Length),
                table.Key == "head"));

        PdfTrueTypeBinary.W32(
            output,
            record + 8,
            checked((uint)offset));

        PdfTrueTypeBinary.W32(
            output,
            record + 12,
            checked((uint)table.Value.Length));
    }
}
