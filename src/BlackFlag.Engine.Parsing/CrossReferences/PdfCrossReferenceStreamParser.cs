using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Parsing.CrossReferences;

/// <summary>
/// Validates and decodes a PDF cross-reference stream.
/// </summary>
public sealed class PdfCrossReferenceStreamParser
{
    private static readonly PdfName TypeKey = new("Type");
    private static readonly PdfName XRefName = new("XRef");
    private static readonly PdfName SizeKey = new("Size");
    private static readonly PdfName WidthsKey = new("W");
    private static readonly PdfName IndexKey = new("Index");
    private static readonly PdfName FilterKey = new("Filter");
    private static readonly PdfName DecodeParametersKey =
        new("DecodeParms");

    private readonly IPdfFilterPipeline filterPipeline;
    private readonly PdfFilterDecodeLimits decodeLimits;

    /// <summary>
    /// Initializes a cross-reference stream parser.
    /// </summary>
    public PdfCrossReferenceStreamParser(
        IPdfFilterPipeline filterPipeline,
        PdfFilterDecodeLimits decodeLimits)
    {
        ArgumentNullException.ThrowIfNull(filterPipeline);
        ArgumentNullException.ThrowIfNull(decodeLimits);

        this.filterPipeline = filterPipeline;
        this.decodeLimits = decodeLimits;
    }

    /// <summary>
    /// Decodes and validates a cross-reference stream.
    /// </summary>
    public PdfCrossReference Parse(
        long offset,
        PdfStream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset, nameof(offset));

        ArgumentNullException.ThrowIfNull(stream);

        PdfDictionary dictionary =
            stream.Dictionary;

        ValidateType(dictionary);

        int size =
            ReadRequiredNonNegativeInteger(
                dictionary,
                SizeKey,
                allowZero: false);

        int[] widths =
            ReadWidths(dictionary);

        (IReadOnlyList<PdfName> filters,
            IReadOnlyList<PdfDictionary?>? decodeParameters) =
                ReadFilters(dictionary);

        ReadOnlyMemory<byte> decoded =
            filterPipeline.Decode(
                stream.Data,
                filters,
                decodeParameters,
                decodeLimits,
                cancellationToken);

        List<(int FirstObjectNumber, int Count)> ranges =
            ReadIndexRanges(
                dictionary,
                size);

        int entryWidth =
            checked(
                widths[0] +
                widths[1] +
                widths[2]);

        long expectedLength = 0;

        foreach (var range in ranges)
        {
            expectedLength =
                checked(
                    expectedLength +
                    ((long)range.Count * entryWidth));
        }

        if (decoded.Length != expectedLength)
        {
            throw new FormatException(
                $"The decoded cross-reference stream contains {decoded.Length} "
                + $"bytes, but {expectedLength} bytes were required.");
        }

        var sections =
            new List<PdfCrossReferenceStreamSection>(
                ranges.Count);

        int byteOffset = 0;

        foreach ((int firstObjectNumber, int count) in ranges)
        {
            var entries =
                new PdfCrossReferenceStreamEntry[count];

            for (int index = 0; index < count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ulong typeField =
                    widths[0] == 0
                        ? 1
                        : ReadUnsigned(
                            decoded.Span,
                            ref byteOffset,
                            widths[0]);

                ulong field2 =
                    ReadUnsigned(
                        decoded.Span,
                        ref byteOffset,
                        widths[1]);

                ulong field3 =
                    ReadUnsigned(
                        decoded.Span,
                        ref byteOffset,
                        widths[2]);

                int objectNumber =
                    checked(
                        firstObjectNumber +
                        index);

                entries[index] =
                    CreateEntry(
                        objectNumber,
                        typeField,
                        field2,
                        field3);
            }

            sections.Add(
                new PdfCrossReferenceStreamSection(
                    firstObjectNumber,
                    entries));
        }

        return new PdfCrossReference(
            offset,
            dictionary,
            sections);
    }

    private static PdfCrossReferenceStreamEntry CreateEntry(
        int objectNumber,
        ulong typeField,
        ulong field2,
        ulong field3)
    {
        return typeField switch
        {
            0 =>
                PdfCrossReferenceStreamEntry.CreateFree(
                    objectNumber,
                    ToInt64(
                        field2,
                        "next free object number"),
                    ToGeneration(
                        field3)),

            1 =>
                PdfCrossReferenceStreamEntry.CreateInUse(
                    objectNumber,
                    ToInt64(
                        field2,
                        "object offset"),
                    ToGeneration(
                        field3)),

            2 =>
                PdfCrossReferenceStreamEntry.CreateCompressed(
                    objectNumber,
                    ToInt64(
                        field2,
                        "object-stream number"),
                    ToInt32(
                        field3,
                        "object-stream index")),

            _ =>
                throw new FormatException(
                    $"Cross-reference stream entry {objectNumber} uses "
                    + $"unsupported type {typeField}.")
        };
    }

    private static void ValidateType(
        PdfDictionary dictionary)
    {
        if (
            !dictionary.TryGetValue(
                TypeKey,
                out PdfObject value) ||
            value is not PdfName name ||
            name != XRefName)
        {
            throw new FormatException(
                "A cross-reference stream dictionary must contain /Type /XRef.");
        }
    }

    private static int[] ReadWidths(
        PdfDictionary dictionary)
    {
        if (
            !dictionary.TryGetValue(
                WidthsKey,
                out PdfObject value) ||
            value is not PdfArray array ||
            array.Count != 3)
        {
            throw new FormatException(
                "A cross-reference stream /W entry must be an array of three integers.");
        }

        var widths =
            new int[3];

        for (int index = 0; index < widths.Length; index++)
        {
            if (
                array[index] is not PdfInteger integer ||
                integer.Value is < 0 or > 8)
            {
                throw new FormatException(
                    "Each cross-reference stream /W width must be an integer from 0 through 8.");
            }

            widths[index] =
                (int)integer.Value;
        }

        if (widths[1] == 0)
        {
            throw new FormatException(
                "The second cross-reference stream field width cannot be zero.");
        }

        return widths;
    }

    private static List<(int FirstObjectNumber, int Count)>
        ReadIndexRanges(
            PdfDictionary dictionary,
            int size)
    {
        if (!dictionary.TryGetValue(
                IndexKey,
                out PdfObject value))
        {
            return
            [
                (0, size)
            ];
        }

        if (
            value is not PdfArray array ||
            array.Count == 0 ||
            (array.Count & 1) != 0)
        {
            throw new FormatException(
                "A cross-reference stream /Index entry must contain integer pairs.");
        }

        var ranges =
            new List<(int FirstObjectNumber, int Count)>(
                array.Count / 2);

        var representedObjects =
            new HashSet<int>();

        for (int index = 0; index < array.Count; index += 2)
        {
            int first =
                ReadArrayInteger(
                    array,
                    index,
                    "/Index first object number",
                    allowZero: true);

            int count =
                ReadArrayInteger(
                    array,
                    index + 1,
                    "/Index count",
                    allowZero: false);

            int end =
                checked(
                    first +
                    count);

            if (end > size)
            {
                throw new FormatException(
                    "A cross-reference stream /Index range exceeds /Size.");
            }

            for (int objectNumber = first; objectNumber < end; objectNumber++)
            {
                if (!representedObjects.Add(objectNumber))
                {
                    throw new FormatException(
                        "Cross-reference stream /Index ranges cannot overlap.");
                }
            }

            ranges.Add(
                (first, count));
        }

        return ranges;
    }

    private static (
        IReadOnlyList<PdfName> Filters,
        IReadOnlyList<PdfDictionary?>? DecodeParameters)
        ReadFilters(
            PdfDictionary dictionary)
    {
        if (!dictionary.TryGetValue(
                FilterKey,
                out PdfObject filterValue))
        {
            return
            (
                Array.Empty<PdfName>(),
                null
            );
        }

        PdfName[] filters =
            filterValue switch
            {
                PdfName name =>
                    [name],

                PdfArray array =>
                    ReadFilterArray(array),

                _ =>
                    throw new FormatException(
                        "A stream /Filter entry must be a name or an array of names.")
            };

        if (!dictionary.TryGetValue(
                DecodeParametersKey,
                out PdfObject parametersValue))
        {
            return
            (
                filters,
                null
            );
        }

        PdfDictionary?[] parameters =
            parametersValue switch
            {
                PdfDictionary parametersDictionary =>
                    filters.Length == 1
                        ? [parametersDictionary]
                        : throw new FormatException(
                            "A single /DecodeParms dictionary requires one filter."),

                PdfNull =>
                    new PdfDictionary?[filters.Length],

                PdfArray array =>
                    ReadDecodeParametersArray(
                        array,
                        filters.Length),

                _ =>
                    throw new FormatException(
                        "A stream /DecodeParms entry must be a dictionary, null, or an array.")
            };

        return
        (
            filters,
            parameters
        );
    }

    private static PdfName[] ReadFilterArray(
        PdfArray array)
    {
        if (array.Count == 0)
        {
            throw new FormatException(
                "A stream /Filter array cannot be empty.");
        }

        var filters =
            new PdfName[array.Count];

        for (int index = 0; index < array.Count; index++)
        {
            filters[index] =
                array[index] as PdfName ??
                throw new FormatException(
                    "Every stream /Filter array entry must be a PDF name.");
        }

        return filters;
    }

    private static PdfDictionary?[] ReadDecodeParametersArray(
        PdfArray array,
        int filterCount)
    {
        if (array.Count != filterCount)
        {
            throw new FormatException(
                "A stream /DecodeParms array must align with the /Filter array.");
        }

        var parameters =
            new PdfDictionary?[array.Count];

        for (int index = 0; index < array.Count; index++)
        {
            parameters[index] =
                array[index] switch
                {
                    PdfDictionary dictionary =>
                        dictionary,

                    PdfNull =>
                        null,

                    _ =>
                        throw new FormatException(
                            "Every /DecodeParms array entry must be a dictionary or null.")
                };
        }

        return parameters;
    }

    private static int ReadRequiredNonNegativeInteger(
        PdfDictionary dictionary,
        PdfName key,
        bool allowZero)
    {
        if (
            !dictionary.TryGetValue(
                key,
                out PdfObject value) ||
            value is not PdfInteger integer)
        {
            throw new FormatException(
                $"A cross-reference stream /{key.Value} entry must be an integer.");
        }

        return ConvertInteger(
            integer.Value,
            $"/{key.Value}",
            allowZero);
    }

    private static int ReadArrayInteger(
        PdfArray array,
        int index,
        string description,
        bool allowZero)
    {
        if (array[index] is not PdfInteger integer)
        {
            throw new FormatException(
                $"{description} must be an integer.");
        }

        return ConvertInteger(
            integer.Value,
            description,
            allowZero);
    }

    private static int ConvertInteger(
        long value,
        string description,
        bool allowZero)
    {
        if (
            value < 0 ||
            (!allowZero && value == 0) ||
            value > int.MaxValue)
        {
            throw new FormatException(
                $"{description} is outside the supported range.");
        }

        return (int)value;
    }

    private static ulong ReadUnsigned(
        ReadOnlySpan<byte> data,
        ref int offset,
        int width)
    {
        ulong value = 0;

        for (int index = 0; index < width; index++)
        {
            value =
                (value << 8) |
                data[offset++];
        }

        return value;
    }

    private static long ToInt64(
        ulong value,
        string description)
    {
        if (value > long.MaxValue)
        {
            throw new FormatException(
                $"The decoded {description} exceeds Int64.MaxValue.");
        }

        return (long)value;
    }

    private static int ToInt32(
        ulong value,
        string description)
    {
        if (value > int.MaxValue)
        {
            throw new FormatException(
                $"The decoded {description} exceeds Int32.MaxValue.");
        }

        return (int)value;
    }

    private static int ToGeneration(
        ulong value)
    {
        if (value > 65535)
        {
            throw new FormatException(
                "A decoded cross-reference generation number exceeds 65535.");
        }

        return (int)value;
    }
}

