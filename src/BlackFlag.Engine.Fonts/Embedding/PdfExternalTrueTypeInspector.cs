using System.Buffers.Binary;
using BlackFlag.Engine.Fonts.Sanitization;

namespace BlackFlag.Engine.Fonts.Embedding;

/// <summary>
/// Reads the bounded writer-facing metrics required for conservative full
/// embedding from a sanitized TrueType font.
/// </summary>
public static class PdfExternalTrueTypeInspector
{
    public static PdfExternalTrueTypeMetrics Inspect(
        PdfSanitizedExternalFontProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);

        if (program.Technology != PdfExternalFontTechnology.TrueType)
        {
            throw new NotSupportedException(
                "BF-1054J embeds sanitized TrueType-outline fonts only.");
        }

        ReadOnlySpan<byte> bytes = program.TrustedBytes;
        Dictionary<string, Table> tables = ReadTables(bytes);

        Table head = Require(tables, "head");
        Table hhea = Require(tables, "hhea");
        Table hmtx = Require(tables, "hmtx");
        Table maxp = Require(tables, "maxp");
        Table cmap = Require(tables, "cmap");
        Table os2 = Require(tables, "OS/2");

        ReadOnlySpan<byte> headBytes = Slice(bytes, head);
        ReadOnlySpan<byte> hheaBytes = Slice(bytes, hhea);
        ReadOnlySpan<byte> maxpBytes = Slice(bytes, maxp);
        ReadOnlySpan<byte> hmtxBytes = Slice(bytes, hmtx);
        ReadOnlySpan<byte> cmapBytes = Slice(bytes, cmap);
        ReadOnlySpan<byte> os2Bytes = Slice(bytes, os2);

        int unitsPerEm = U16(headBytes, 18);
        if (unitsPerEm is < 16 or > 16384)
        {
            throw new InvalidDataException(
                "The sanitized TrueType font has an invalid unitsPerEm value.");
        }

        int glyphCount = U16(maxpBytes, 4);
        int metricCount = U16(hheaBytes, 34);

        if (glyphCount <= 0 ||
            metricCount <= 0 ||
            metricCount > glyphCount)
        {
            throw new InvalidDataException(
                "The sanitized TrueType font has inconsistent glyph/metric counts.");
        }

        int[] advances = ReadAdvanceWidths(
            hmtxBytes,
            glyphCount,
            metricCount);

        Func<int, int> glyphMapper =
            CreateGlyphMapper(cmapBytes);

        int[] widths = new int[224];

        for (int code = 32; code <= 255; code++)
        {
            int unicode = WinAnsiToUnicode(code);
            int glyph = glyphMapper(unicode);

            if (glyph < 0 || glyph >= advances.Length)
            {
                glyph = 0;
            }

            widths[code - 32] =
                Scale(
                    advances[glyph],
                    unitsPerEm);
        }

        return new PdfExternalTrueTypeMetrics(
            unitsPerEm,
            Scale(I16(hheaBytes, 4), unitsPerEm),
            Scale(I16(hheaBytes, 6), unitsPerEm),
            Scale(I16(headBytes, 36), unitsPerEm),
            Scale(I16(headBytes, 38), unitsPerEm),
            Scale(I16(headBytes, 40), unitsPerEm),
            Scale(I16(headBytes, 42), unitsPerEm),
            widths,
            ReadEmbeddingRights(os2Bytes));
    }

    private static Dictionary<string, Table> ReadTables(
        ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 12)
        {
            throw new InvalidDataException(
                "The sanitized TrueType font is truncated.");
        }

        int count = U16(bytes, 4);
        var result = new Dictionary<string, Table>(
            StringComparer.Ordinal);

        for (int index = 0; index < count; index++)
        {
            int record = checked(12 + (index * 16));

            if (record > bytes.Length - 16)
            {
                throw new InvalidDataException(
                    "The sanitized TrueType table directory is truncated.");
            }

            string tag =
                System.Text.Encoding.ASCII.GetString(
                    bytes.Slice(record, 4));

            int offset = checked((int)U32(bytes, record + 8));
            int length = checked((int)U32(bytes, record + 12));

            if (offset < 0 ||
                length < 0 ||
                offset > bytes.Length ||
                length > bytes.Length - offset ||
                !result.TryAdd(
                    tag,
                    new Table(offset, length)))
            {
                throw new InvalidDataException(
                    "The sanitized TrueType table directory is inconsistent.");
            }
        }

        return result;
    }

    private static int[] ReadAdvanceWidths(
        ReadOnlySpan<byte> hmtx,
        int glyphCount,
        int metricCount)
    {
        int required =
            checked(
                (metricCount * 4) +
                ((glyphCount - metricCount) * 2));

        if (hmtx.Length < required)
        {
            throw new InvalidDataException(
                "The sanitized hmtx table is shorter than required.");
        }

        int[] result = new int[glyphCount];
        int lastAdvance = 0;

        for (int index = 0; index < metricCount; index++)
        {
            lastAdvance = U16(hmtx, index * 4);
            result[index] = lastAdvance;
        }

        for (int index = metricCount; index < glyphCount; index++)
        {
            result[index] = lastAdvance;
        }

        return result;
    }

    private static Func<int, int> CreateGlyphMapper(
        ReadOnlySpan<byte> cmap)
    {
        if (cmap.Length < 4)
        {
            throw new InvalidDataException(
                "The sanitized cmap table is truncated.");
        }

        int count = U16(cmap, 2);

        // Prefer Unicode format 12, then format 4, then format 0.
        Func<int, int>? format4 = null;
        Func<int, int>? format0 = null;

        for (int index = 0; index < count; index++)
        {
            int record = checked(4 + (index * 8));
            int offset = checked((int)U32(cmap, record + 4));

            if (offset > cmap.Length - 2)
            {
                continue;
            }

            ReadOnlySpan<byte> subtable = cmap[offset..];
            int format = U16(subtable, 0);

            if (format == 12)
            {
                return CreateFormat12Mapper(subtable);
            }

            if (format == 4 && format4 is null)
            {
                format4 = CreateFormat4Mapper(subtable);
            }
            else if (format == 0 && format0 is null)
            {
                format0 = CreateFormat0Mapper(subtable);
            }
        }

        return format4 ??
            format0 ??
            throw new NotSupportedException(
                "The sanitized TrueType font has no cmap format supported by BF-1054J.");
    }

    private static Func<int, int> CreateFormat0Mapper(
        ReadOnlySpan<byte> table)
    {
        if (table.Length < 262)
        {
            throw new InvalidDataException(
                "The cmap format 0 subtable is truncated.");
        }

        byte[] glyphs =
            table.Slice(6, 256).ToArray();

        return unicode =>
            unicode is >= 0 and <= 255
                ? glyphs[unicode]
                : 0;
    }

    private static Func<int, int> CreateFormat4Mapper(
        ReadOnlySpan<byte> table)
    {
        if (table.Length < 16)
        {
            throw new InvalidDataException(
                "The cmap format 4 subtable is truncated.");
        }

        int length = U16(table, 2);
        if (length > table.Length)
        {
            throw new InvalidDataException(
                "The cmap format 4 length is invalid.");
        }

        byte[] data = table[..length].ToArray();
        int segCount = U16(data, 6) / 2;

        int endCodes = 14;
        int startCodes = checked(endCodes + (segCount * 2) + 2);
        int idDeltas = checked(startCodes + (segCount * 2));
        int idRanges = checked(idDeltas + (segCount * 2));

        if (idRanges + (segCount * 2) > data.Length)
        {
            throw new InvalidDataException(
                "The cmap format 4 arrays are truncated.");
        }

        return unicode =>
        {
            if (unicode is < 0 or > 0xFFFF)
            {
                return 0;
            }

            for (int segment = 0; segment < segCount; segment++)
            {
                int end = U16(data, endCodes + (segment * 2));
                if (unicode > end)
                {
                    continue;
                }

                int start = U16(data, startCodes + (segment * 2));
                if (unicode < start)
                {
                    return 0;
                }

                int delta = I16(data, idDeltas + (segment * 2));
                int range = U16(data, idRanges + (segment * 2));

                if (range == 0)
                {
                    return (unicode + delta) & 0xFFFF;
                }

                int rangeWord =
                    idRanges + (segment * 2);

                int glyphOffset =
                    checked(
                        rangeWord +
                        range +
                        ((unicode - start) * 2));

                if (glyphOffset > data.Length - 2)
                {
                    return 0;
                }

                int glyph = U16(data, glyphOffset);

                return glyph == 0
                    ? 0
                    : (glyph + delta) & 0xFFFF;
            }

            return 0;
        };
    }

    private static Func<int, int> CreateFormat12Mapper(
        ReadOnlySpan<byte> table)
    {
        if (table.Length < 16)
        {
            throw new InvalidDataException(
                "The cmap format 12 subtable is truncated.");
        }

        int length = checked((int)U32(table, 4));
        int groups = checked((int)U32(table, 12));

        if (length > table.Length ||
            16L + (groups * 12L) > length)
        {
            throw new InvalidDataException(
                "The cmap format 12 group array is invalid.");
        }

        byte[] data = table[..length].ToArray();

        return unicode =>
        {
            if (unicode < 0)
            {
                return 0;
            }

            uint scalar = checked((uint)unicode);

            for (int index = 0; index < groups; index++)
            {
                int offset = 16 + (index * 12);
                uint start = U32(data, offset);
                uint end = U32(data, offset + 4);

                if (scalar < start)
                {
                    return 0;
                }

                if (scalar <= end)
                {
                    uint firstGlyph = U32(data, offset + 8);
                    uint glyph =
                        checked(firstGlyph + (scalar - start));

                    return glyph <= int.MaxValue
                        ? (int)glyph
                        : 0;
                }
            }

            return 0;
        };
    }

    private static PdfExternalFontEmbeddingRights ReadEmbeddingRights(
        ReadOnlySpan<byte> os2)
    {
        if (os2.Length < 10)
        {
            return PdfExternalFontEmbeddingRights.Unknown;
        }

        int fsType = U16(os2, 8);

        if ((fsType & 0x0002) != 0 ||
            (fsType & 0x0200) != 0)
        {
            return PdfExternalFontEmbeddingRights.Restricted;
        }

        if ((fsType & 0x0100) != 0)
        {
            return PdfExternalFontEmbeddingRights.Allowed;
        }

        return PdfExternalFontEmbeddingRights.Allowed;
    }

    private static int WinAnsiToUnicode(int code) =>
        code switch
        {
            0x80 => 0x20AC,
            0x82 => 0x201A,
            0x83 => 0x0192,
            0x84 => 0x201E,
            0x85 => 0x2026,
            0x86 => 0x2020,
            0x87 => 0x2021,
            0x88 => 0x02C6,
            0x89 => 0x2030,
            0x8A => 0x0160,
            0x8B => 0x2039,
            0x8C => 0x0152,
            0x8E => 0x017D,
            0x91 => 0x2018,
            0x92 => 0x2019,
            0x93 => 0x201C,
            0x94 => 0x201D,
            0x95 => 0x2022,
            0x96 => 0x2013,
            0x97 => 0x2014,
            0x98 => 0x02DC,
            0x99 => 0x2122,
            0x9A => 0x0161,
            0x9B => 0x203A,
            0x9C => 0x0153,
            0x9E => 0x017E,
            0x9F => 0x0178,
            0x81 or 0x8D or 0x8F or 0x90 or 0x9D => 0,
            _ => code
        };

    private static int Scale(
        int value,
        int unitsPerEm) =>
        checked(
            (int)Math.Round(
                value * 1000.0 / unitsPerEm,
                MidpointRounding.AwayFromZero));

    private static Table Require(
        IReadOnlyDictionary<string, Table> tables,
        string tag) =>
        tables.TryGetValue(tag, out Table table)
            ? table
            : throw new InvalidDataException(
                $"Sanitized TrueType font is missing required writer table '{tag}'.");

    private static ReadOnlySpan<byte> Slice(
        ReadOnlySpan<byte> bytes,
        Table table) =>
        bytes.Slice(table.Offset, table.Length);

    private static int U16(
        ReadOnlySpan<byte> data,
        int offset)
    {
        if (offset < 0 || offset > data.Length - 2)
        {
            throw new InvalidDataException(
                "A sanitized TrueType metric read exceeded its table.");
        }

        return BinaryPrimitives.ReadUInt16BigEndian(
            data.Slice(offset, 2));
    }

    private static int I16(
        ReadOnlySpan<byte> data,
        int offset) =>
        unchecked((short)U16(data, offset));

    private static uint U32(
        ReadOnlySpan<byte> data,
        int offset)
    {
        if (offset < 0 || offset > data.Length - 4)
        {
            throw new InvalidDataException(
                "A sanitized TrueType metric read exceeded its table.");
        }

        return BinaryPrimitives.ReadUInt32BigEndian(
            data.Slice(offset, 4));
    }

    private readonly record struct Table(
        int Offset,
        int Length);
}
