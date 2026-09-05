using System.Buffers.Binary;
using System.Text;
using BlackFlag.Engine.Fonts.Sanitization;

namespace BlackFlag.Engine.Fonts.Embedding;

/// <summary>
/// Creates a conservative glyph-outline subset from a sanitized TrueType
/// program while retaining original glyph IDs.
/// </summary>
public sealed class PdfTrueTypeGlyphSubsetter
{
    private readonly PdfOpenTypeFontSanitizer sanitizer;

    public PdfTrueTypeGlyphSubsetter()
        : this(new PdfOpenTypeFontSanitizer())
    {
    }

    public PdfTrueTypeGlyphSubsetter(
        PdfOpenTypeFontSanitizer sanitizer)
    {
        ArgumentNullException.ThrowIfNull(sanitizer);
        this.sanitizer = sanitizer;
    }

    public PdfTrueTypeSubsetResult CreateSubset(
        PdfSanitizedExternalFontProgram program,
        string requiredText,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(requiredText);

        if (program.Technology != PdfExternalFontTechnology.TrueType)
        {
            throw new NotSupportedException(
                "BF-1054K subsetting supports sanitized TrueType glyf/loca fonts only.");
        }

        // Keep the font data in an array rather than a span: the table-copy
        // projection below captures source in a lambda, which ref-like spans
        // cannot be captured by.
        byte[] source = program.Bytes.ToArray();
        Dictionary<string, Table> tables = ReadTables(source);

        Table head = Require(tables, "head");
        Table maxp = Require(tables, "maxp");
        Table cmap = Require(tables, "cmap");
        Table loca = Require(tables, "loca");
        Table glyf = Require(tables, "glyf");

        int glyphCount = U16(Slice(source, maxp), 4);
        int locaFormat = I16(Slice(source, head), 50);

        if (locaFormat is not 0 and not 1)
        {
            throw new InvalidDataException(
                "The sanitized TrueType font has an unsupported loca format.");
        }

        uint[] offsets =
            ReadLoca(
                Slice(source, loca),
                glyphCount,
                locaFormat,
                glyf.Length);

        Func<int, int> glyphMapper =
            CreateGlyphMapper(
                Slice(source, cmap));

        var selectedGlyphs =
            new HashSet<int> { 0 };

        var unicodeByCode =
            new Dictionary<byte, int>();

        foreach (Rune rune in requiredText.EnumerateRunes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryEncodeWinAnsi(
                    rune.Value,
                    out byte code))
            {
                throw new NotSupportedException(
                    $"Unicode scalar U+{rune.Value:X} is outside the BF-1054K WinAnsi subset scope.");
            }

            int glyph = glyphMapper(rune.Value);

            if (glyph <= 0 ||
                glyph >= glyphCount)
            {
                throw new NotSupportedException(
                    $"The selected TrueType font has no usable glyph for U+{rune.Value:X}.");
            }

            EnsureSimpleGlyph(
                Slice(source, glyf),
                offsets,
                glyph);

            _ = selectedGlyphs.Add(glyph);
            unicodeByCode[code] = rune.Value;
        }

        Dictionary<string, byte[]> rebuilt =
            tables.ToDictionary(
                static pair => pair.Key,
                pair =>
                    Slice(source, pair.Value).ToArray(),
                StringComparer.Ordinal);

        (byte[] newGlyf, byte[] newLoca) =
            RebuildGlyphTables(
                Slice(source, glyf),
                offsets,
                selectedGlyphs,
                locaFormat,
                glyphCount);

        rebuilt["glyf"] = newGlyf;
        rebuilt["loca"] = newLoca;
        rebuilt["cmap"] =
            BuildCmap(
                unicodeByCode,
                glyphMapper);

        byte[] sfnt =
            BuildSfnt(
                rebuilt);

        using var stream =
            new MemoryStream(
                sfnt,
                writable: false);

        PdfSanitizedExternalFontProgram sanitized =
            sanitizer.Sanitize(
                stream,
                cancellationToken:
                    cancellationToken);

        return new PdfTrueTypeSubsetResult(
            sanitized,
            unicodeByCode);
    }

    private static void EnsureSimpleGlyph(
        ReadOnlySpan<byte> glyf,
        uint[] offsets,
        int glyph)
    {
        uint start = offsets[glyph];
        uint end = offsets[glyph + 1];

        if (start == end)
        {
            throw new NotSupportedException(
                $"Selected glyph {glyph} has no outline data.");
        }

        ReadOnlySpan<byte> data =
            glyf.Slice(
                checked((int)start),
                checked((int)(end - start)));

        if (data.Length < 10)
        {
            throw new InvalidDataException(
                $"Selected glyph {glyph} is truncated.");
        }

        short contours =
            unchecked(
                (short)BinaryPrimitives.ReadUInt16BigEndian(
                    data[..2]));

        if (contours < 0)
        {
            throw new NotSupportedException(
                $"Selected glyph {glyph} is composite; composite-glyph closure is deferred beyond BF-1054K.");
        }
    }

    private static (byte[] Glyf, byte[] Loca) RebuildGlyphTables(
        ReadOnlySpan<byte> glyf,
        uint[] offsets,
        HashSet<int> selected,
        int locaFormat,
        int glyphCount)
    {
        using var output =
            new MemoryStream();

        uint[] newOffsets =
            new uint[glyphCount + 1];

        for (int glyph = 0;
            glyph < glyphCount;
            glyph++)
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
            locaFormat == 0
                ? new byte[
                    checked(
                        (glyphCount + 1) * 2)]
                : new byte[
                    checked(
                        (glyphCount + 1) * 4)];

        for (int index = 0;
            index < newOffsets.Length;
            index++)
        {
            if (locaFormat == 0)
            {
                if ((newOffsets[index] & 1) != 0 ||
                    (newOffsets[index] / 2) > ushort.MaxValue)
                {
                    throw new NotSupportedException(
                        "The generated short loca table cannot represent the subset offsets.");
                }

                BinaryPrimitives.WriteUInt16BigEndian(
                    loca.AsSpan(index * 2, 2),
                    checked((ushort)(newOffsets[index] / 2)));
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(
                    loca.AsSpan(index * 4, 4),
                    newOffsets[index]);
            }
        }

        return (
            output.ToArray(),
            loca);
    }

    private static byte[] BuildCmap(
        IReadOnlyDictionary<byte, int> unicodeByCode,
        Func<int, int> glyphMapper)
    {
        KeyValuePair<byte, int>[] mappings =
        [
            .. unicodeByCode
                .OrderBy(
                    static pair => pair.Value)
        ];

        int segmentCount =
            mappings.Length + 1;

        int segCountX2 =
            segmentCount * 2;

        int power = 1;
        int selector = 0;

        while ((power << 1) <= segmentCount)
        {
            power <<= 1;
            selector++;
        }

        int searchRange = power * 2;
        int rangeShift =
            segCountX2 -
            searchRange;

        int format4Length =
            16 +
            (segmentCount * 8);

        byte[] format4 =
            new byte[format4Length];

        W16(format4, 0, 4);
        W16(format4, 2, checked((ushort)format4Length));
        W16(format4, 4, 0);
        W16(format4, 6, checked((ushort)segCountX2));
        W16(format4, 8, checked((ushort)searchRange));
        W16(format4, 10, checked((ushort)selector));
        W16(format4, 12, checked((ushort)rangeShift));

        int endCodes = 14;
        int reservedPad =
            endCodes +
            (segmentCount * 2);
        int startCodes =
            reservedPad + 2;
        int deltas =
            startCodes +
            (segmentCount * 2);
        int ranges =
            deltas +
            (segmentCount * 2);

        for (int index = 0;
            index < mappings.Length;
            index++)
        {
            int unicode =
                mappings[index].Value;
            int glyph =
                glyphMapper(unicode);

            if (unicode > ushort.MaxValue ||
                glyph <= 0 ||
                glyph > ushort.MaxValue)
            {
                throw new NotSupportedException(
                    "BF-1054K WinAnsi cmap generation requires BMP Unicode and 16-bit glyph IDs.");
            }

            W16(format4, endCodes + index * 2, checked((ushort)unicode));
            W16(format4, startCodes + index * 2, checked((ushort)unicode));
            W16(
                format4,
                deltas + index * 2,
                unchecked(
                    (ushort)(glyph - unicode)));
            W16(format4, ranges + index * 2, 0);
        }

        int sentinel = mappings.Length;
        W16(format4, endCodes + sentinel * 2, 0xFFFF);
        W16(format4, reservedPad, 0);
        W16(format4, startCodes + sentinel * 2, 0xFFFF);
        W16(format4, deltas + sentinel * 2, 1);
        W16(format4, ranges + sentinel * 2, 0);

        byte[] cmap =
            new byte[
                checked(
                    12 +
                    format4.Length)];

        W16(cmap, 0, 0);
        W16(cmap, 2, 1);
        W16(cmap, 4, 3);
        W16(cmap, 6, 1);
        W32(cmap, 8, 12);

        format4.CopyTo(
            cmap.AsSpan(12));

        return cmap;
    }

    private static byte[] BuildSfnt(
        IReadOnlyDictionary<string, byte[]> tables)
    {
        KeyValuePair<string, byte[]>[] ordered =
        [
            .. tables.OrderBy(
                static pair => pair.Key,
                StringComparer.Ordinal)
        ];

        int directoryLength =
            checked(
                12 +
                (ordered.Length * 16));

        int cursor =
            Align4(directoryLength);

        var offsets =
            new Dictionary<string, int>(
                StringComparer.Ordinal);

        foreach (KeyValuePair<string, byte[]> table in ordered)
        {
            cursor =
                Align4(cursor);
            offsets[table.Key] = cursor;
            cursor =
                checked(
                    cursor +
                    Align4(table.Value.Length));
        }

        byte[] output =
            new byte[cursor];

        W32(output, 0, 0x00010000);
        W16(output, 4, checked((ushort)ordered.Length));
        WriteSearchFields(output, ordered.Length);

        for (int index = 0;
            index < ordered.Length;
            index++)
        {
            KeyValuePair<string, byte[]> table =
                ordered[index];

            int record =
                12 +
                (index * 16);
            int offset =
                offsets[table.Key];

            Encoding.ASCII.GetBytes(
                    table.Key)
                .CopyTo(
                    output.AsSpan(
                        record,
                        4));

            table.Value.CopyTo(
                output.AsSpan(
                    offset));

            if (table.Key == "head")
            {
                output.AsSpan(
                        offset + 8,
                        4)
                    .Clear();
            }

            W32(
                output,
                record + 4,
                Checksum(
                    output.AsSpan(
                        offset,
                        table.Value.Length),
                    table.Key == "head"));

            W32(
                output,
                record + 8,
                checked((uint)offset));

            W32(
                output,
                record + 12,
                checked((uint)table.Value.Length));
        }

        int headOffset =
            offsets["head"];

        uint adjustment =
            unchecked(
                0xB1B0AFBAu -
                Checksum(
                    output,
                    false));

        W32(
            output,
            headOffset + 8,
            adjustment);

        return output;
    }

    private static Dictionary<string, Table> ReadTables(
        ReadOnlySpan<byte> bytes)
    {
        int count = U16(bytes, 4);

        var result =
            new Dictionary<string, Table>(
                StringComparer.Ordinal);

        for (int index = 0;
            index < count;
            index++)
        {
            int record =
                checked(
                    12 +
                    (index * 16));

            string tag =
                Encoding.ASCII.GetString(
                    bytes.Slice(
                        record,
                        4));

            int offset =
                checked(
                    (int)U32(
                        bytes,
                        record + 8));

            int length =
                checked(
                    (int)U32(
                        bytes,
                        record + 12));

            if (!result.TryAdd(
                    tag,
                    new Table(
                        offset,
                        length)))
            {
                throw new InvalidDataException(
                    "The sanitized font contains duplicate table tags.");
            }
        }

        return result;
    }

    private static uint[] ReadLoca(
        ReadOnlySpan<byte> loca,
        int glyphCount,
        int format,
        int glyfLength)
    {
        int count =
            checked(
                glyphCount + 1);

        uint[] result =
            new uint[count];

        for (int index = 0;
            index < count;
            index++)
        {
            result[index] =
                format == 0
                    ? checked(
                        (uint)U16(
                            loca,
                            index * 2) *
                        2u)
                    : U32(
                        loca,
                        index * 4);

            if (result[index] > glyfLength ||
                (index > 0 &&
                result[index] <
                    result[index - 1]))
            {
                throw new InvalidDataException(
                    "The sanitized loca table is inconsistent.");
            }
        }

        return result;
    }

    private static Func<int, int> CreateGlyphMapper(
        ReadOnlySpan<byte> cmap)
    {
        int count =
            U16(
                cmap,
                2);

        Func<int, int>? format4 = null;
        Func<int, int>? format0 = null;

        for (int index = 0;
            index < count;
            index++)
        {
            int record =
                checked(
                    4 +
                    (index * 8));

            int offset =
                checked(
                    (int)U32(
                        cmap,
                        record + 4));

            ReadOnlySpan<byte> subtable =
                cmap[offset..];

            int format =
                U16(
                    subtable,
                    0);

            if (format == 4 &&
                format4 is null)
            {
                format4 =
                    CreateFormat4Mapper(
                        subtable);
            }
            else if (format == 0 &&
                    format0 is null)
            {
                format0 =
                    CreateFormat0Mapper(
                        subtable);
            }
        }

        return format4 ??
            format0 ??
            throw new NotSupportedException(
                "BF-1054K requires cmap format 0 or 4 for WinAnsi subsetting.");
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
            table.Slice(6, 256)
                .ToArray();

        return unicode =>
            unicode is >= 0 and <= 255
                ? glyphs[unicode]
                : 0;
    }

    private static Func<int, int> CreateFormat4Mapper(
        ReadOnlySpan<byte> table)
    {
        int length = U16(table, 2);

        if (length < 16 ||
            length > table.Length)
        {
            throw new InvalidDataException(
                "The cmap format 4 subtable has an invalid length.");
        }

        byte[] data =
            table[..length]
                .ToArray();

        int segCount =
            U16(data, 6) / 2;

        int endCodes = 14;
        int startCodes =
            endCodes +
            (segCount * 2) +
            2;
        int deltas =
            startCodes +
            (segCount * 2);
        int ranges =
            deltas +
            (segCount * 2);

        return unicode =>
        {
            if (unicode is < 0 or > 0xFFFF)
            {
                return 0;
            }

            for (int segment = 0;
                segment < segCount;
                segment++)
            {
                int end =
                    U16(
                        data,
                        endCodes +
                        segment * 2);

                if (unicode > end)
                {
                    continue;
                }

                int start =
                    U16(
                        data,
                        startCodes +
                        segment * 2);

                if (unicode < start)
                {
                    return 0;
                }

                int delta =
                    I16(
                        data,
                        deltas +
                        segment * 2);

                int range =
                    U16(
                        data,
                        ranges +
                        segment * 2);

                if (range == 0)
                {
                    return
                        (unicode + delta) &
                        0xFFFF;
                }

                int rangeWord =
                    ranges +
                    segment * 2;

                int glyphOffset =
                    checked(
                        rangeWord +
                        range +
                        ((unicode - start) * 2));

                if (glyphOffset >
                    data.Length - 2)
                {
                    return 0;
                }

                int glyph =
                    U16(
                        data,
                        glyphOffset);

                return glyph == 0
                    ? 0
                    : (glyph + delta) &
                    0xFFFF;
            }

            return 0;
        };
    }

    private static bool TryEncodeWinAnsi(
        int unicode,
        out byte code)
    {
        if (unicode is >= 0x20 and <= 0x7E ||
            unicode is >= 0xA0 and <= 0xFF)
        {
            code =
                checked((byte)unicode);
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

    private static Table Require(
        IReadOnlyDictionary<string, Table> tables,
        string tag) =>
        tables.TryGetValue(
            tag,
            out Table table)
            ? table
            : throw new InvalidDataException(
                $"The sanitized font is missing required table '{tag}'.");

    private static ReadOnlySpan<byte> Slice(
        ReadOnlySpan<byte> bytes,
        Table table) =>
        bytes.Slice(
            table.Offset,
            table.Length);

    private static int Align4(
        int value) =>
        checked(
            (value + 3) &
            ~3);

    private static int U16(
        ReadOnlySpan<byte> data,
        int offset)
    {
        if (offset < 0 ||
            offset > data.Length - 2)
        {
            throw new InvalidDataException(
                "A subsetter table read exceeded its boundary.");
        }

        return BinaryPrimitives.ReadUInt16BigEndian(
            data.Slice(offset, 2));
    }

    private static int I16(
        ReadOnlySpan<byte> data,
        int offset) =>
        unchecked(
            (short)U16(
                data,
                offset));

    private static uint U32(
        ReadOnlySpan<byte> data,
        int offset)
    {
        if (offset < 0 ||
            offset > data.Length - 4)
        {
            throw new InvalidDataException(
                "A subsetter table read exceeded its boundary.");
        }

        return BinaryPrimitives.ReadUInt32BigEndian(
            data.Slice(offset, 4));
    }

    private static void W16(
        Span<byte> data,
        int offset,
        ushort value) =>
        BinaryPrimitives.WriteUInt16BigEndian(
            data.Slice(offset, 2),
            value);

    private static void W32(
        Span<byte> data,
        int offset,
        uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(
            data.Slice(offset, 4),
            value);

    private static void WriteSearchFields(
        Span<byte> output,
        int count)
    {
        int power = 1;
        int selector = 0;

        while ((power << 1) <= count)
        {
            power <<= 1;
            selector++;
        }

        int searchRange =
            power * 16;

        W16(
            output,
            6,
            checked((ushort)searchRange));

        W16(
            output,
            8,
            checked((ushort)selector));

        W16(
            output,
            10,
            checked(
                (ushort)(
                    (count * 16) -
                    searchRange)));
    }

    private static uint Checksum(
        ReadOnlySpan<byte> data,
        bool zeroHead)
    {
        uint sum = 0;
        int padded =
            Align4(data.Length);

        Span<byte> word =
            stackalloc byte[4];

        for (int offset = 0;
            offset < padded;
            offset += 4)
        {
            word.Clear();

            if (offset < data.Length)
            {
                data.Slice(
                        offset,
                        Math.Min(
                            4,
                            data.Length - offset))
                    .CopyTo(word);
            }

            if (zeroHead &&
                offset == 8)
            {
                word.Clear();
            }

            sum =
                unchecked(
                    sum +
                    BinaryPrimitives
                        .ReadUInt32BigEndian(
                            word));
        }

        return sum;
    }

    private readonly record struct Table(
        int Offset,
        int Length);
}
