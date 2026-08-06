using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Parsing.ObjectStreams;

/// <summary>
/// Validates and decodes PDF <c>/ObjStm</c> streams.
/// </summary>
public sealed class PdfObjectStreamParser
{
    private static readonly PdfName TypeKey = new("Type");
    private static readonly PdfName ObjectStreamType = new("ObjStm");
    private static readonly PdfName CountKey = new("N");
    private static readonly PdfName FirstKey = new("First");
    private static readonly PdfName FilterKey = new("Filter");
    private static readonly PdfName DecodeParametersKey =
        new("DecodeParms");

    private readonly IPdfFilterPipeline filterPipeline;
    private readonly PdfFilterDecodeLimits decodeLimits;

    /// <summary>
    /// Initializes an object-stream parser.
    /// </summary>
    public PdfObjectStreamParser(
        IPdfFilterPipeline filterPipeline,
        PdfFilterDecodeLimits decodeLimits)
    {
        ArgumentNullException.ThrowIfNull(filterPipeline);
        ArgumentNullException.ThrowIfNull(decodeLimits);

        this.filterPipeline = filterPipeline;
        this.decodeLimits = decodeLimits;
    }

    /// <summary>
    /// Decodes and validates a PDF object stream.
    /// </summary>
    public PdfCompressedObjects Parse(
        PdfStream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        PdfDictionary dictionary =
            stream.Dictionary;

        ValidateType(dictionary);

        int count =
            ReadRequiredInteger(
                dictionary,
                CountKey,
                allowZero: false);

        int firstObjectOffset =
            ReadRequiredInteger(
                dictionary,
                FirstKey,
                allowZero: true);

        (
            IReadOnlyList<PdfName> filters,
            IReadOnlyList<PdfDictionary?>? decodeParameters
        ) =
            ReadFilters(dictionary);

        ReadOnlyMemory<byte> decoded =
            filterPipeline.Decode(
                stream.Data,
                filters,
                decodeParameters,
                decodeLimits,
                cancellationToken);

        if (firstObjectOffset > decoded.Length)
        {
            throw new FormatException(
                "The object stream /First value exceeds the decoded stream length.");
        }

        PdfCompressedObjectEntry[] entries =
            ParseHeader(
                decoded.Span[..firstObjectOffset],
                count,
                decoded.Length - firstObjectOffset,
                cancellationToken);

        return new PdfCompressedObjects(
            dictionary,
            decoded.Span,
            firstObjectOffset,
            entries);
    }

    private static PdfCompressedObjectEntry[] ParseHeader(
        ReadOnlySpan<byte> header,
        int count,
        int objectDataLength,
        CancellationToken cancellationToken)
    {
        var entries =
            new PdfCompressedObjectEntry[count];

        var objectNumbers =
            new HashSet<int>();

        int offset = 0;
        int previousRelativeOffset = -1;

        for (int index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int objectNumber =
                ReadHeaderInteger(
                    header,
                    ref offset,
                    "object number",
                    allowZero: false);

            int relativeOffset =
                ReadHeaderInteger(
                    header,
                    ref offset,
                    "relative object offset",
                    allowZero: true);

            if (!objectNumbers.Add(objectNumber))
            {
                throw new FormatException(
                    $"Object stream index contains duplicate object {objectNumber}.");
            }

            if (
                previousRelativeOffset >= 0 &&
                relativeOffset <= previousRelativeOffset)
            {
                throw new FormatException(
                    "Object stream relative offsets must be strictly increasing.");
            }

            if (relativeOffset >= objectDataLength)
            {
                throw new FormatException(
                    $"Object {objectNumber} begins outside the object-data region.");
            }

            entries[index] =
                new PdfCompressedObjectEntry(
                    objectNumber,
                    index,
                    relativeOffset);

            previousRelativeOffset =
                relativeOffset;
        }

        SkipWhiteSpace(
            header,
            ref offset);

        if (offset != header.Length)
        {
            throw new FormatException(
                "The object stream header contains more index data than /N permits.");
        }

        return entries;
    }

    private static int ReadHeaderInteger(
        ReadOnlySpan<byte> header,
        ref int offset,
        string description,
        bool allowZero)
    {
        SkipWhiteSpace(
            header,
            ref offset);

        if (
            offset >= header.Length ||
            header[offset] < (byte)'0' ||
            header[offset] > (byte)'9')
        {
            throw new FormatException(
                $"Object stream {description} is missing or invalid.");
        }

        int value = 0;

        while (
            offset < header.Length &&
            header[offset] >= (byte)'0' &&
            header[offset] <= (byte)'9')
        {
            int digit =
                header[offset] -
                (byte)'0';

            try
            {
                value =
                    checked(
                        (value * 10) +
                        digit);
            }
            catch (OverflowException exception)
            {
                throw new FormatException(
                    $"Object stream {description} exceeds Int32.MaxValue.",
                    exception);
            }

            offset++;
        }

        if (
            !allowZero &&
            value == 0)
        {
            throw new FormatException(
                $"Object stream {description} must be greater than zero.");
        }

        return value;
    }

    private static void SkipWhiteSpace(
        ReadOnlySpan<byte> data,
        ref int offset)
    {
        while (
            offset < data.Length &&
            IsWhiteSpace(data[offset]))
        {
            offset++;
        }
    }

    private static bool IsWhiteSpace(
        byte value)
    {
        return value is
            0x00 or
            0x09 or
            0x0A or
            0x0C or
            0x0D or
            0x20;
    }

    private static void ValidateType(
        PdfDictionary dictionary)
    {
        if (
            !dictionary.TryGetValue(
                TypeKey,
                out PdfObject value) ||
            value is not PdfName name ||
            name != ObjectStreamType)
        {
            throw new FormatException(
                "An object stream dictionary must contain /Type /ObjStm.");
        }
    }

    private static int ReadRequiredInteger(
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
                $"An object stream /{key.Value} entry must be an integer.");
        }

        if (
            integer.Value < 0 ||
            (!allowZero && integer.Value == 0) ||
            integer.Value > int.MaxValue)
        {
            throw new FormatException(
                $"The object stream /{key.Value} value is outside the supported range.");
        }

        return (int)integer.Value;
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
                        "An object stream /Filter entry must be a name or an array of names.")
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
                        "An object stream /DecodeParms entry must be a dictionary, null, or an array.")
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
                "An object stream /Filter array cannot be empty.");
        }

        var filters =
            new PdfName[array.Count];

        for (int index = 0; index < array.Count; index++)
        {
            filters[index] =
                array[index] as PdfName ??
                throw new FormatException(
                    "Every object stream /Filter array entry must be a PDF name.");
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
                "An object stream /DecodeParms array must align with /Filter.");
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
                            "Every /DecodeParms entry must be a dictionary or null.")
                };
        }

        return parameters;
    }
}

