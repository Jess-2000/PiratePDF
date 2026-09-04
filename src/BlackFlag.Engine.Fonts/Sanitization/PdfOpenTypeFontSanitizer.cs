using System.Buffers.Binary;
using System.Text;

namespace BlackFlag.Engine.Fonts.Sanitization;

/// <summary>
/// Fail-closed sanitizer for standalone TrueType/OpenType SFNT font programs.
/// It validates bounded structure and required tables, then reserializes a
/// canonical compact SFNT so downstream code never consumes provider bytes.
/// </summary>
public sealed class PdfOpenTypeFontSanitizer : IPdfExternalFontSanitizer
{
    private const uint TrueTypeScalar = 0x00010000;
    private const uint OpenTypeCffScalar = 0x4F54544F; // OTTO
    private const uint TrueTypeCollection = 0x74746366; // ttcf
    private const uint Woff = 0x774F4646;
    private const uint Woff2 = 0x774F4632;
    private const uint HeadMagic = 0x5F0F3CF5;
    private const uint ChecksumMagic = 0xB1B0AFBA;

    public PdfSanitizedExternalFontProgram Sanitize(
        Stream untrustedFontProgram,
        PdfExternalFontSanitizationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(untrustedFontProgram);
        options ??= new PdfExternalFontSanitizationOptions();
        options.Validate();

        if (!untrustedFontProgram.CanRead)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.UnreadableInput,
                "The external font stream is not readable.");
        }

        byte[] source = ReadBounded(
            untrustedFontProgram,
            options.MaximumInputBytes,
            cancellationToken);

        if (source.Length < 12)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedHeader,
                "The external font is too short to contain an SFNT header.");
        }

        uint scalar = ReadUInt32(source, 0);
        if (scalar is TrueTypeCollection or Woff or Woff2)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.UnsupportedContainer,
                "Font collections and WOFF/WOFF2 containers are not accepted by the TTF/OTF sanitizer.");
        }

        PdfExternalFontTechnology technology =
            scalar switch
            {
                TrueTypeScalar => PdfExternalFontTechnology.TrueType,
                OpenTypeCffScalar => PdfExternalFontTechnology.OpenType,
                _ => throw Reject(
                    PdfExternalFontSanitizationFailure.UnsupportedContainer,
                    "Only standalone TrueType and OpenType SFNT programs are accepted.")
            };

        ushort tableCount = ReadUInt16(source, 4);
        if (tableCount == 0)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedHeader,
                "The SFNT contains no tables.");
        }

        if (tableCount > options.MaximumTableCount)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.TooManyTables,
                $"The SFNT table count {tableCount} exceeds the configured limit.");
        }

        int directoryEnd =
            checked(12 + (tableCount * 16));

        if (directoryEnd > source.Length)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedHeader,
                "The SFNT table directory is truncated.");
        }

        ValidateSearchFields(source, tableCount);

        var records = new List<TableRecord>(tableCount);
        var tags = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < tableCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int recordOffset = 12 + (index * 16);
            string tag = ReadTag(source, recordOffset);

            if (!tags.Add(tag))
            {
                throw Reject(
                    PdfExternalFontSanitizationFailure.DuplicateTable,
                    $"The SFNT contains duplicate table '{tag}'.");
            }

            uint checksum = ReadUInt32(source, recordOffset + 4);
            uint tableOffset = ReadUInt32(source, recordOffset + 8);
            uint tableLength = ReadUInt32(source, recordOffset + 12);

            if (tableLength > options.MaximumSingleTableBytes)
            {
                throw Reject(
                    PdfExternalFontSanitizationFailure.ResourceLimitExceeded,
                    $"Table '{tag}' exceeds the configured single-table size limit.");
            }

            if ((tableOffset & 3) != 0 ||
                tableOffset < directoryEnd ||
                tableOffset > source.Length ||
                tableLength > source.Length - tableOffset)
            {
                throw Reject(
                    PdfExternalFontSanitizationFailure.InvalidTableRange,
                    $"Table '{tag}' has an invalid or unaligned source range.");
            }

            records.Add(
                new TableRecord(
                    tag,
                    checksum,
                    checked((int)tableOffset),
                    checked((int)tableLength)));
        }

        ValidateNoOverlap(records);
        ValidateChecksums(source, records);
        ValidateRequiredStructure(source, records, technology, options);

        byte[] sanitized =
            ReserializeCanonical(
                source,
                records,
                scalar);

        return new PdfSanitizedExternalFontProgram(
            sanitized,
            technology,
            records
                .Select(static record => record.Tag)
                .OrderBy(static tag => tag, StringComparer.Ordinal));
    }

    private static byte[] ReadBounded(
        Stream input,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        using var destination =
            new MemoryStream(
                Math.Min(maximumBytes, 1024 * 1024));

        byte[] buffer = new byte[81920];
        int total = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int read = input.Read(
                buffer,
                0,
                Math.Min(buffer.Length, maximumBytes - total + 1));

            if (read == 0)
            {
                return destination.ToArray();
            }

            total = checked(total + read);

            if (total > maximumBytes)
            {
                throw Reject(
                    PdfExternalFontSanitizationFailure.InputTooLarge,
                    $"The external font exceeds the configured {maximumBytes}-byte input limit.");
            }

            destination.Write(buffer, 0, read);
        }
    }

    private static void ValidateSearchFields(
        ReadOnlySpan<byte> source,
        ushort tableCount)
    {
        ushort searchRange = ReadUInt16(source, 6);
        ushort entrySelector = ReadUInt16(source, 8);
        ushort rangeShift = ReadUInt16(source, 10);

        int power = 1;
        int selector = 0;
        while ((power << 1) <= tableCount)
        {
            power <<= 1;
            selector++;
        }

        ushort expectedSearchRange =
            checked((ushort)(power * 16));
        ushort expectedRangeShift =
            checked((ushort)((tableCount * 16) - expectedSearchRange));

        if (searchRange != expectedSearchRange ||
            entrySelector != selector ||
            rangeShift != expectedRangeShift)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedHeader,
                "The SFNT searchRange/entrySelector/rangeShift fields are inconsistent with the table count.");
        }
    }

    private static string ReadTag(
        ReadOnlySpan<byte> source,
        int offset)
    {
        ReadOnlySpan<byte> tagBytes =
            source.Slice(offset, 4);

        for (int index = 0; index < tagBytes.Length; index++)
        {
            if (tagBytes[index] is < 0x20 or > 0x7E)
            {
                throw Reject(
                    PdfExternalFontSanitizationFailure.MalformedHeader,
                    "The SFNT contains a non-printable table tag.");
            }
        }

        return Encoding.ASCII.GetString(tagBytes);
    }

    private static void ValidateNoOverlap(
        IReadOnlyList<TableRecord> records)
    {
        TableRecord[] ordered =
        [
            .. records
                .Where(static record => record.Length != 0)
                .OrderBy(static record => record.Offset)
        ];

        for (int index = 1; index < ordered.Length; index++)
        {
            long previousEnd =
                (long)ordered[index - 1].Offset +
                ordered[index - 1].Length;

            if (ordered[index].Offset < previousEnd)
            {
                throw Reject(
                    PdfExternalFontSanitizationFailure.OverlappingTables,
                    $"Tables '{ordered[index - 1].Tag}' and '{ordered[index].Tag}' overlap.");
            }
        }
    }

    private static void ValidateChecksums(
        ReadOnlySpan<byte> source,
        IReadOnlyList<TableRecord> records)
    {
        foreach (TableRecord record in records)
        {
            uint actual =
                ComputeTableChecksum(
                    source.Slice(record.Offset, record.Length),
                    zeroHeadAdjustment:
                        record.Tag == "head");

            if (actual != record.Checksum)
            {
                throw Reject(
                    PdfExternalFontSanitizationFailure.InvalidChecksum,
                    $"Table '{record.Tag}' checksum does not match its directory entry.");
            }
        }
    }

    private static void ValidateRequiredStructure(
        ReadOnlySpan<byte> source,
        IReadOnlyList<TableRecord> records,
        PdfExternalFontTechnology technology,
        PdfExternalFontSanitizationOptions options)
    {
        TableRecord head = Require(records, "head");
        TableRecord maxp = Require(records, "maxp");
        TableRecord cmap = Require(records, "cmap");
        TableRecord name = Require(records, "name");
        TableRecord hhea = Require(records, "hhea");
        TableRecord hmtx = Require(records, "hmtx");

        ValidateHead(source.Slice(head.Offset, head.Length));
        int glyphCount =
            ValidateMaxp(
                source.Slice(maxp.Offset, maxp.Length),
                options.MaximumGlyphCount);

        int hMetrics =
            ValidateHhea(
                source.Slice(hhea.Offset, hhea.Length),
                glyphCount);

        ValidateHmtx(
            source.Slice(hmtx.Offset, hmtx.Length),
            glyphCount,
            hMetrics);

        ValidateCmap(
            source.Slice(cmap.Offset, cmap.Length),
            options.MaximumCMapSubtableCount);

        ValidateName(
            source.Slice(name.Offset, name.Length),
            options.MaximumNameRecordCount);

        if (TryGet(records, "OS/2", out TableRecord os2))
        {
            ValidateOs2(
                source.Slice(os2.Offset, os2.Length));
        }

        if (technology == PdfExternalFontTechnology.TrueType)
        {
            TableRecord loca = Require(records, "loca");
            TableRecord glyf = Require(records, "glyf");

            ValidateLoca(
                source.Slice(head.Offset, head.Length),
                source.Slice(loca.Offset, loca.Length),
                glyf.Length,
                glyphCount);
        }
        else
        {
            if (!TryGet(records, "CFF ", out TableRecord cff) &&
                !TryGet(records, "CFF2", out cff))
            {
                throw Reject(
                    PdfExternalFontSanitizationFailure.MissingRequiredTable,
                    "An OTTO font must contain a CFF or CFF2 outline table.");
            }

            ValidateCffHeader(
                source.Slice(cff.Offset, cff.Length),
                cff.Tag);
        }
    }

    private static void ValidateHead(ReadOnlySpan<byte> table)
    {
        if (table.Length < 54 ||
            ReadUInt32(table, 12) != HeadMagic)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                "The head table is truncated or has an invalid magic number.");
        }

        short indexToLocFormat =
            ReadInt16(table, 50);

        if (indexToLocFormat is not 0 and not 1)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                "The head indexToLocFormat value is invalid.");
        }
    }

    private static int ValidateMaxp(
        ReadOnlySpan<byte> table,
        int maximumGlyphCount)
    {
        if (table.Length < 6)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                "The maxp table is truncated.");
        }

        int glyphCount = ReadUInt16(table, 4);

        if (glyphCount == 0 ||
            glyphCount > maximumGlyphCount)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.ResourceLimitExceeded,
                $"The maxp glyph count {glyphCount} is invalid or exceeds the configured limit.");
        }

        return glyphCount;
    }

    private static int ValidateHhea(
        ReadOnlySpan<byte> table,
        int glyphCount)
    {
        if (table.Length < 36)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                "The hhea table is truncated.");
        }

        int metricCount = ReadUInt16(table, 34);

        if (metricCount == 0 ||
            metricCount > glyphCount)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                "The hhea numberOfHMetrics value is invalid.");
        }

        return metricCount;
    }

    private static void ValidateHmtx(
        ReadOnlySpan<byte> table,
        int glyphCount,
        int metricCount)
    {
        int required =
            checked(
                (metricCount * 4) +
                ((glyphCount - metricCount) * 2));

        if (table.Length < required)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                "The hmtx table is shorter than the maxp/hhea metrics require.");
        }
    }

    private static void ValidateCmap(
        ReadOnlySpan<byte> table,
        int maximumSubtables)
    {
        if (table.Length < 4 ||
            ReadUInt16(table, 0) != 0)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                "The cmap table header is invalid.");
        }

        int count = ReadUInt16(table, 2);

        if (count == 0 ||
            count > maximumSubtables)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.ResourceLimitExceeded,
                "The cmap subtable count is invalid or exceeds the configured limit.");
        }

        int recordsEnd =
            checked(4 + (count * 8));

        if (recordsEnd > table.Length)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                "The cmap encoding-record array is truncated.");
        }

        for (int index = 0; index < count; index++)
        {
            int record = 4 + (index * 8);
            uint offset = ReadUInt32(table, record + 4);

            if (offset > table.Length - 2)
            {
                throw Reject(
                    PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                    "A cmap subtable offset lies outside the cmap table.");
            }

            ValidateCmapSubtable(
                table[(int)offset..]);
        }
    }

    private static void ValidateCmapSubtable(
        ReadOnlySpan<byte> subtable)
    {
        ushort format = ReadUInt16(subtable, 0);

        int declaredLength =
            format switch
            {
                0 or 2 or 4 or 6 =>
                    subtable.Length >= 4
                        ? ReadUInt16(subtable, 2)
                        : 0,

                8 or 10 or 12 or 13 =>
                    subtable.Length >= 8
                        ? checked((int)ReadUInt32(subtable, 4))
                        : 0,

                14 =>
                    subtable.Length >= 6
                        ? checked((int)ReadUInt32(subtable, 2))
                        : 0,

                _ =>
                    throw Reject(
                        PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                        $"Unsupported cmap format {format} is rejected by the sanitizer.")
            };

        if (declaredLength <= 0 ||
            declaredLength > subtable.Length)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                $"cmap format {format} declares an invalid length.");
        }
    }

    private static void ValidateName(
        ReadOnlySpan<byte> table,
        int maximumRecords)
    {
        if (table.Length < 6)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                "The name table is truncated.");
        }

        int count = ReadUInt16(table, 2);
        int stringOffset = ReadUInt16(table, 4);

        if (count > maximumRecords)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.ResourceLimitExceeded,
                "The name-record count exceeds the configured limit.");
        }

        int recordsEnd =
            checked(6 + (count * 12));

        if (recordsEnd > table.Length ||
            stringOffset < recordsEnd ||
            stringOffset > table.Length)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                "The name table record or string-storage boundary is invalid.");
        }

        for (int index = 0; index < count; index++)
        {
            int record = 6 + (index * 12);
            int length = ReadUInt16(table, record + 8);
            int offset = ReadUInt16(table, record + 10);
            int absolute =
                checked(stringOffset + offset);

            if (absolute > table.Length ||
                length > table.Length - absolute)
            {
                throw Reject(
                    PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                    "A name record points outside the name table.");
            }
        }
    }

    private static void ValidateOs2(ReadOnlySpan<byte> table)
    {
        if (table.Length < 78)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                "The OS/2 table is shorter than the minimum supported structure.");
        }
    }

    private static void ValidateLoca(
        ReadOnlySpan<byte> head,
        ReadOnlySpan<byte> loca,
        int glyfLength,
        int glyphCount)
    {
        short format = ReadInt16(head, 50);
        int entryCount = checked(glyphCount + 1);
        int entrySize = format == 0 ? 2 : 4;
        int required =
            checked(entryCount * entrySize);

        if (loca.Length < required)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                "The loca table is shorter than the maxp glyph count requires.");
        }

        uint previous = 0;

        for (int index = 0; index < entryCount; index++)
        {
            uint current =
                format == 0
                    ? checked(ReadUInt16(loca, index * 2) * 2u)
                    : ReadUInt32(loca, index * 4);

            if (current < previous ||
                current > glyfLength)
            {
                throw Reject(
                    PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                    "The loca table is non-monotonic or points outside glyf.");
            }

            previous = current;
        }
    }

    private static void ValidateCffHeader(
        ReadOnlySpan<byte> table,
        string tag)
    {
        if (tag == "CFF ")
        {
            if (table.Length < 4 ||
                table[2] < 4 ||
                table[2] > table.Length)
            {
                throw Reject(
                    PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                    "The CFF header is invalid.");
            }

            return;
        }

        if (table.Length < 5 ||
            table[2] < 5 ||
            table[2] > table.Length)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                "The CFF2 header is invalid.");
        }
    }

    private static byte[] ReserializeCanonical(
        ReadOnlySpan<byte> source,
        IReadOnlyList<TableRecord> records,
        uint scalar)
    {
        TableRecord[] ordered =
        [
            .. records.OrderBy(
                static record => record.Tag,
                StringComparer.Ordinal)
        ];

        int directoryLength =
            checked(12 + (ordered.Length * 16));
        int currentOffset =
            Align4(directoryLength);

        var outputRecords =
            new List<OutputRecord>(ordered.Length);

        foreach (TableRecord record in ordered)
        {
            currentOffset = Align4(currentOffset);
            outputRecords.Add(
                new OutputRecord(
                    record,
                    currentOffset));

            currentOffset =
                checked(
                    currentOffset +
                    Align4(record.Length));
        }

        byte[] output =
            new byte[currentOffset];

        WriteUInt32(output, 0, scalar);
        WriteUInt16(output, 4, checked((ushort)ordered.Length));
        WriteSearchFields(output, ordered.Length);

        for (int index = 0; index < outputRecords.Count; index++)
        {
            OutputRecord outputRecord = outputRecords[index];
            int recordOffset = 12 + (index * 16);

            Encoding.ASCII.GetBytes(outputRecord.Source.Tag)
                .CopyTo(
                    output.AsSpan(recordOffset, 4));

            source.Slice(
                    outputRecord.Source.Offset,
                    outputRecord.Source.Length)
                .CopyTo(
                    output.AsSpan(
                        outputRecord.Offset,
                        outputRecord.Source.Length));

            if (outputRecord.Source.Tag == "head")
            {
                output.AsSpan(
                        outputRecord.Offset + 8,
                        4)
                    .Clear();
            }

            uint checksum =
                ComputeTableChecksum(
                    output.AsSpan(
                        outputRecord.Offset,
                        outputRecord.Source.Length),
                    zeroHeadAdjustment:
                        outputRecord.Source.Tag == "head");

            WriteUInt32(output, recordOffset + 4, checksum);
            WriteUInt32(output, recordOffset + 8, checked((uint)outputRecord.Offset));
            WriteUInt32(output, recordOffset + 12, checked((uint)outputRecord.Source.Length));
        }

        OutputRecord head =
            outputRecords.Single(
                static record =>
                    record.Source.Tag == "head");

        uint whole =
            ComputeTableChecksum(
                output,
                zeroHeadAdjustment: false);

        uint adjustment =
            unchecked(ChecksumMagic - whole);

        WriteUInt32(
            output,
            head.Offset + 8,
            adjustment);

        return output;
    }

    private static void WriteSearchFields(
        Span<byte> output,
        int tableCount)
    {
        int power = 1;
        int selector = 0;

        while ((power << 1) <= tableCount)
        {
            power <<= 1;
            selector++;
        }

        int searchRange = power * 16;
        int rangeShift =
            (tableCount * 16) -
            searchRange;

        WriteUInt16(output, 6, checked((ushort)searchRange));
        WriteUInt16(output, 8, checked((ushort)selector));
        WriteUInt16(output, 10, checked((ushort)rangeShift));
    }

    private static uint ComputeTableChecksum(
        ReadOnlySpan<byte> data,
        bool zeroHeadAdjustment)
    {
        uint sum = 0;
        int paddedLength = Align4(data.Length);
        Span<byte> word = stackalloc byte[4];

        for (int offset = 0; offset < paddedLength; offset += 4)
        {
            word.Clear();

            if (offset < data.Length)
            {
                int copy =
                    Math.Min(4, data.Length - offset);

                data.Slice(offset, copy)
                    .CopyTo(word);
            }

            if (zeroHeadAdjustment &&
                offset == 8)
            {
                word.Clear();
            }

            sum =
                unchecked(
                    sum +
                    BinaryPrimitives.ReadUInt32BigEndian(word));
        }

        return sum;
    }

    private static TableRecord Require(
        IReadOnlyList<TableRecord> records,
        string tag)
    {
        if (TryGet(records, tag, out TableRecord record))
        {
            return record;
        }

        throw Reject(
            PdfExternalFontSanitizationFailure.MissingRequiredTable,
            $"Required font table '{tag}' is missing.");
    }

    private static bool TryGet(
        IReadOnlyList<TableRecord> records,
        string tag,
        out TableRecord record)
    {
        for (int index = 0; index < records.Count; index++)
        {
            if (records[index].Tag == tag)
            {
                record = records[index];
                return true;
            }
        }

        record = default;
        return false;
    }

    private static int Align4(int value) =>
        checked((value + 3) & ~3);

    private static ushort ReadUInt16(
        ReadOnlySpan<byte> data,
        int offset)
    {
        if (offset < 0 ||
            offset > data.Length - 2)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                "A font structure read exceeded its table boundary.");
        }

        return BinaryPrimitives.ReadUInt16BigEndian(
            data.Slice(offset, 2));
    }

    private static short ReadInt16(
        ReadOnlySpan<byte> data,
        int offset) =>
        unchecked((short)ReadUInt16(data, offset));

    private static uint ReadUInt32(
        ReadOnlySpan<byte> data,
        int offset)
    {
        if (offset < 0 ||
            offset > data.Length - 4)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                "A font structure read exceeded its table boundary.");
        }

        return BinaryPrimitives.ReadUInt32BigEndian(
            data.Slice(offset, 4));
    }

    private static void WriteUInt16(
        Span<byte> data,
        int offset,
        ushort value) =>
        BinaryPrimitives.WriteUInt16BigEndian(
            data.Slice(offset, 2),
            value);

    private static void WriteUInt32(
        Span<byte> data,
        int offset,
        uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(
            data.Slice(offset, 4),
            value);

    private static PdfExternalFontSanitizationException Reject(
        PdfExternalFontSanitizationFailure failure,
        string message) =>
        new(
            failure,
            message);

    private readonly record struct TableRecord(
        string Tag,
        uint Checksum,
        int Offset,
        int Length);

    private readonly record struct OutputRecord(
        TableRecord Source,
        int Offset);
}
