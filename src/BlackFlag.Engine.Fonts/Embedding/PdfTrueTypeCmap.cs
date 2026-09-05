namespace BlackFlag.Engine.Fonts.Embedding;

internal static class PdfTrueTypeCmap
{
    public static Func<int, int> CreateGlyphMapper(
        ReadOnlySpan<byte> cmap)
    {
        int count = PdfTrueTypeBinary.U16(cmap, 2);
        Func<int, int>? format12 = null;
        Func<int, int>? format4 = null;
        Func<int, int>? format0 = null;

        for (int index = 0; index < count; index++)
        {
            int record = checked(4 + index * 8);
            int offset =
                checked(
                    (int)PdfTrueTypeBinary.U32(
                        cmap,
                        record + 4));

            ReadOnlySpan<byte> subtable = cmap[offset..];
            int format = PdfTrueTypeBinary.U16(subtable, 0);

            if (format == 12 && format12 is null)
            {
                format12 = CreateFormat12Mapper(subtable);
            }
            else if (format == 4 && format4 is null)
            {
                format4 = CreateFormat4Mapper(subtable);
            }
            else if (format == 0 && format0 is null)
            {
                format0 = CreateFormat0Mapper(subtable);
            }
        }

        return format12 ??
            format4 ??
            format0 ??
            throw new NotSupportedException(
                "The sanitized TrueType font has no supported Unicode cmap format (0, 4, or 12).");
    }

    public static byte[] BuildSubsetCmap(
        IReadOnlyDictionary<byte, int> unicodeByCode,
        Func<int, int> glyphMapper)
    {
        KeyValuePair<byte, int>[] mappings =
        [
            .. unicodeByCode.OrderBy(static pair => pair.Value)
        ];

        int segmentCount = mappings.Length + 1;
        int segCountX2 = segmentCount * 2;

        int power = 1;
        int selector = 0;

        while ((power << 1) <= segmentCount)
        {
            power <<= 1;
            selector++;
        }

        int searchRange = power * 2;
        int rangeShift = segCountX2 - searchRange;
        int format4Length = 16 + segmentCount * 8;

        byte[] format4 = new byte[format4Length];

        PdfTrueTypeBinary.W16(format4, 0, 4);
        PdfTrueTypeBinary.W16(
            format4,
            2,
            checked((ushort)format4Length));
        PdfTrueTypeBinary.W16(format4, 4, 0);
        PdfTrueTypeBinary.W16(
            format4,
            6,
            checked((ushort)segCountX2));
        PdfTrueTypeBinary.W16(
            format4,
            8,
            checked((ushort)searchRange));
        PdfTrueTypeBinary.W16(
            format4,
            10,
            checked((ushort)selector));
        PdfTrueTypeBinary.W16(
            format4,
            12,
            checked((ushort)rangeShift));

        int endCodes = 14;
        int reservedPad = endCodes + segmentCount * 2;
        int startCodes = reservedPad + 2;
        int deltas = startCodes + segmentCount * 2;
        int ranges = deltas + segmentCount * 2;

        for (int index = 0; index < mappings.Length; index++)
        {
            int unicode = mappings[index].Value;
            int glyph = glyphMapper(unicode);

            if (unicode > ushort.MaxValue ||
                glyph <= 0 ||
                glyph > ushort.MaxValue)
            {
                throw new NotSupportedException(
                    "BF-1054K WinAnsi cmap generation requires BMP Unicode and 16-bit glyph IDs.");
            }

            PdfTrueTypeBinary.W16(
                format4,
                endCodes + index * 2,
                checked((ushort)unicode));

            PdfTrueTypeBinary.W16(
                format4,
                startCodes + index * 2,
                checked((ushort)unicode));

            PdfTrueTypeBinary.W16(
                format4,
                deltas + index * 2,
                unchecked((ushort)(glyph - unicode)));

            PdfTrueTypeBinary.W16(
                format4,
                ranges + index * 2,
                0);
        }

        int sentinel = mappings.Length;

        PdfTrueTypeBinary.W16(
            format4,
            endCodes + sentinel * 2,
            0xFFFF);

        PdfTrueTypeBinary.W16(format4, reservedPad, 0);

        PdfTrueTypeBinary.W16(
            format4,
            startCodes + sentinel * 2,
            0xFFFF);

        PdfTrueTypeBinary.W16(
            format4,
            deltas + sentinel * 2,
            1);

        PdfTrueTypeBinary.W16(
            format4,
            ranges + sentinel * 2,
            0);

        byte[] cmap =
            new byte[
                checked(12 + format4.Length)];

        PdfTrueTypeBinary.W16(cmap, 0, 0);
        PdfTrueTypeBinary.W16(cmap, 2, 1);
        PdfTrueTypeBinary.W16(cmap, 4, 3);
        PdfTrueTypeBinary.W16(cmap, 6, 1);
        PdfTrueTypeBinary.W32(cmap, 8, 12);

        format4.CopyTo(cmap.AsSpan(12));

        return cmap;
    }

    public static bool TryEncodeWinAnsi(
        int unicode,
        out byte code)
    {
        if (unicode is >= 0x20 and <= 0x7E ||
            unicode is >= 0xA0 and <= 0xFF)
        {
            code = checked((byte)unicode);
            return true;
        }

        int mapped =
            unicode switch
            {
                0x20AC => 0x80,
                0x201A => 0x82,
                0x0192 => 0x83,
                0x201E => 0x84,
                0x2026 => 0x85,
                0x2020 => 0x86,
                0x2021 => 0x87,
                0x02C6 => 0x88,
                0x2030 => 0x89,
                0x0160 => 0x8A,
                0x2039 => 0x8B,
                0x0152 => 0x8C,
                0x017D => 0x8E,
                0x2018 => 0x91,
                0x2019 => 0x92,
                0x201C => 0x93,
                0x201D => 0x94,
                0x2022 => 0x95,
                0x2013 => 0x96,
                0x2014 => 0x97,
                0x02DC => 0x98,
                0x2122 => 0x99,
                0x0161 => 0x9A,
                0x203A => 0x9B,
                0x0153 => 0x9C,
                0x017E => 0x9E,
                0x0178 => 0x9F,
                _ => -1
            };

        if (mapped < 0)
        {
            code = 0;
            return false;
        }

        code = checked((byte)mapped);
        return true;
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
        int length = PdfTrueTypeBinary.U16(table, 2);

        if (length < 16 || length > table.Length)
        {
            throw new InvalidDataException(
                "The cmap format 4 subtable has an invalid length.");
        }

        byte[] data = table[..length].ToArray();
        int segCount = PdfTrueTypeBinary.U16(data, 6) / 2;

        int endCodes = 14;
        int startCodes = endCodes + segCount * 2 + 2;
        int deltas = startCodes + segCount * 2;
        int ranges = deltas + segCount * 2;

        return unicode =>
        {
            if (unicode is < 0 or > 0xFFFF)
            {
                return 0;
            }

            for (int segment = 0; segment < segCount; segment++)
            {
                int end =
                    PdfTrueTypeBinary.U16(
                        data,
                        endCodes + segment * 2);

                if (unicode > end)
                {
                    continue;
                }

                int start =
                    PdfTrueTypeBinary.U16(
                        data,
                        startCodes + segment * 2);

                if (unicode < start)
                {
                    return 0;
                }

                int delta =
                    PdfTrueTypeBinary.I16(
                        data,
                        deltas + segment * 2);

                int range =
                    PdfTrueTypeBinary.U16(
                        data,
                        ranges + segment * 2);

                if (range == 0)
                {
                    return (unicode + delta) & 0xFFFF;
                }

                int rangeWord = ranges + segment * 2;
                int glyphOffset =
                    checked(
                        rangeWord +
                        range +
                        (unicode - start) * 2);

                if (glyphOffset > data.Length - 2)
                {
                    return 0;
                }

                int glyph =
                    PdfTrueTypeBinary.U16(
                        data,
                        glyphOffset);

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

        int length =
            checked(
                (int)PdfTrueTypeBinary.U32(
                    table,
                    4));

        int groupCount =
            checked(
                (int)PdfTrueTypeBinary.U32(
                    table,
                    12));

        if (length > table.Length ||
            16L + groupCount * 12L > length)
        {
            throw new InvalidDataException(
                "The cmap format 12 group array is invalid.");
        }

        byte[] data =
            table[..length].ToArray();

        return unicode =>
        {
            if (unicode < 0)
            {
                return 0;
            }

            uint scalar =
                checked((uint)unicode);

            int low = 0;
            int high = groupCount - 1;

            while (low <= high)
            {
                int middle =
                    low +
                    ((high - low) / 2);

                int offset =
                    16 +
                    middle * 12;

                uint start =
                    PdfTrueTypeBinary.U32(
                        data,
                        offset);

                uint end =
                    PdfTrueTypeBinary.U32(
                        data,
                        offset + 4);

                if (scalar < start)
                {
                    high = middle - 1;
                    continue;
                }

                if (scalar > end)
                {
                    low = middle + 1;
                    continue;
                }

                uint firstGlyph =
                    PdfTrueTypeBinary.U32(
                        data,
                        offset + 8);

                uint glyph =
                    checked(
                        firstGlyph +
                        scalar -
                        start);

                return glyph <= int.MaxValue
                    ? checked((int)glyph)
                    : 0;
            }

            return 0;
        };
    }

}
