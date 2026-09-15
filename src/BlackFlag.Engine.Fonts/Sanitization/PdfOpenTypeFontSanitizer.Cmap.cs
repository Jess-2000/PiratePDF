namespace BlackFlag.Engine.Fonts.Sanitization;

public sealed partial class PdfOpenTypeFontSanitizer
{
    private static void ValidateCmap(
        ReadOnlySpan<byte> table,
        int maximumSubtables,
        CancellationToken cancellationToken)
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
                table[(int)offset..], cancellationToken);
        }
    }

    private static void ValidateCmapSubtable(
        ReadOnlySpan<byte> subtable,
        CancellationToken cancellationToken)
    {
        ushort format = ReadUInt16(subtable, 0);

        uint declaredLength =
            format switch
            {
                0 or 2 or 4 or 6 =>
                    subtable.Length >= 4
                        ? ReadUInt16(subtable, 2)
                        : 0u,

                8 or 10 or 12 or 13 =>
                    subtable.Length >= 8
                        ? ReadUInt32(subtable, 4)
                        : 0u,

                14 =>
                    subtable.Length >= 6
                        ? ReadUInt32(subtable, 2)
                        : 0u,

                _ =>
                    throw Reject(
                        PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                        $"Unsupported cmap format {format} is rejected by the sanitizer.")
            };

        if (declaredLength == 0 ||
            declaredLength > subtable.Length)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                $"cmap format {format} declares an invalid length.");
        }

        // Only narrow after the declaration fits the enclosing managed span.
        // Nested records must fit the declared subtable, not trailing cmap data.
        ReadOnlySpan<byte> data = subtable[..(int)declaredLength];
        switch (format)
        {
            case 12:
            case 13:
                ValidateCmapRecords(data, 16, 12, 12, format);
                break;
            case 14:
                ValidateVariationSequences(data, cancellationToken);
                break;
        }
    }

    private static void ValidateCmapRecords(
        ReadOnlySpan<byte> data,
        int headerLength,
        int countOffset,
        int recordSize,
        int format)
    {
        if (data.Length < headerLength ||
            ReadUInt32(data, countOffset) > (data.Length - headerLength) / recordSize)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                $"cmap format {format} has a truncated header or record array.");
        }
    }

    private static void ValidateVariationSequences(
        ReadOnlySpan<byte> data,
        CancellationToken cancellationToken)
    {
        ValidateCmapRecords(data, 10, 6, 11, 14);
        int count = (int)ReadUInt32(data, 6);
        for (int index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int record = 10 + index * 11;
            ValidateVariationTable(data, ReadUInt32(data, record + 3), 4);
            ValidateVariationTable(data, ReadUInt32(data, record + 7), 5);
        }
    }

    private static void ValidateVariationTable(
        ReadOnlySpan<byte> data,
        uint offset,
        int recordSize)
    {
        if (offset == 0)
        {
            return;
        }

        if (offset > data.Length - 4)
        {
            throw Reject(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable,
                "cmap format 14 variation-table offset lies outside its subtable.");
        }

        ValidateCmapRecords(data[(int)offset..], 4, 0, recordSize, 14);
    }

}
