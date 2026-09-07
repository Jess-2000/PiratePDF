using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;

namespace BlackFlag.Engine.Rendering.Images;

/// <summary>
/// Decodes PDF Image XObjects and inline images without exposing backend types.
/// </summary>
public sealed class PdfImageDecoder
{
    private static readonly PdfName WidthKey = new("Width");
    private static readonly PdfName WKey = new("W");
    private static readonly PdfName HeightKey = new("Height");
    private static readonly PdfName HKey = new("H");
    private static readonly PdfName BitsKey = new("BitsPerComponent");
    private static readonly PdfName BpcKey = new("BPC");
    private static readonly PdfName ColorSpaceKey = new("ColorSpace");
    private static readonly PdfName CsKey = new("CS");
    private static readonly PdfName DecodeKey = new("Decode");
    private static readonly PdfName DKey = new("D");
    private static readonly PdfName FilterKey = new("Filter");
    private static readonly PdfName FKey = new("F");
    private static readonly PdfName DecodeParmsKey = new("DecodeParms");
    private static readonly PdfName DpKey = new("DP");
    private static readonly PdfName ImageMaskKey = new("ImageMask");
    private static readonly PdfName ImKey = new("IM");

    private static readonly double[] DefaultDecodePair = [0d, 1d];

    private readonly IPdfFilterPipeline pipeline;
    private readonly PdfFilterDecodeLimits filterLimits;
    private readonly Dictionary<string, IPdfImageCodec> codecs;

    /// <summary>Initializes a decoder with production filter registrations.</summary>
    public PdfImageDecoder(IEnumerable<IPdfImageCodec>? codecs = null)
        : this(
            new PdfFilterPipeline(PdfFilterRegistrations.CreateDefault()),
            new PdfFilterDecodeLimits(),
            codecs)
    {
    }

    /// <summary>Initializes a decoder with explicit dependencies.</summary>
    public PdfImageDecoder(
        IPdfFilterPipeline pipeline,
        PdfFilterDecodeLimits filterLimits,
        IEnumerable<IPdfImageCodec>? codecs = null)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(filterLimits);
        this.pipeline = pipeline;
        this.filterLimits = filterLimits;
        this.codecs = (codecs ?? [])
            .ToDictionary(codec => codec.FilterName, StringComparer.Ordinal);
    }

    /// <summary>Decodes one canonical image display item.</summary>
    public PdfImageDecodeResult Decode(
        PdfDisplayImage item,
        PdfImageDecodeLimits? limits = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        limits ??= PdfImageDecodeLimits.Default;
        limits.Validate();

        PdfDictionary dictionary;
        ReadOnlyMemory<byte> encoded;

        if (item.ImageResource is not null)
        {
            dictionary = item.ImageResource.Stream.Dictionary;
            encoded = item.ImageResource.Stream.Data;
        }
        else
        {
            PdfInlineImage inline = item.InlineImage
                ?? throw new InvalidOperationException("Image source is missing.");
            dictionary = inline.Dictionary;
            encoded = inline.EncodedBytes;
        }

        ValidateEncodedLength(encoded.Length, limits);

        int width = ReadPositiveInt(dictionary, WidthKey, WKey);
        int height = ReadPositiveInt(dictionary, HeightKey, HKey);
        ValidateDimensions(width, height, limits);

        PdfName[] filters = ReadFilters(dictionary);
        if (filters.Length > 0 &&
            codecs.TryGetValue(NormalizeFilterName(filters[^1].Value), out IPdfImageCodec? codec))
        {
            ReadOnlyMemory<byte> codecPayload = DecodePrefixFilters(
                encoded,
                filters,
                dictionary,
                cancellationToken);

            PdfDecodedImage decoded = codec is IBoundedPdfImageCodec boundedCodec
                ? boundedCodec.Decode(codecPayload, dictionary, limits, cancellationToken)
                : codec.Decode(codecPayload, dictionary, cancellationToken);

            ValidateDimensions(decoded.Width, decoded.Height, limits);
            return new PdfImageDecodeResult(decoded, []);
        }

        ReadOnlyMemory<byte> samples = pipeline.Decode(
            encoded,
            filters,
            ReadDecodeParameters(dictionary, filters.Length),
            filterLimits,
            cancellationToken);

        int bits = ReadOptionalPositiveInt(dictionary, BitsKey, BpcKey) ?? 8;
        bool imageMask = ReadBoolean(dictionary, ImageMaskKey, ImKey);
        string colorSpace = imageMask
            ? "DeviceGray"
            : ReadColorSpaceName(dictionary);

        int components = colorSpace switch
        {
            "DeviceGray" or "G" => 1,
            "DeviceRGB" or "RGB" => 3,
            "DeviceCMYK" or "CMYK" => 4,
            _ => throw new NotSupportedException(
                $"Raw image color space '{colorSpace}' is not yet supported by the BF-1048D sample decoder.")
        };

        double[] decode = ReadDecodeArray(dictionary, components, imageMask);
        byte[] rgba = DecodeRawSamples(
            samples.Span,
            width,
            height,
            components,
            bits,
            decode,
            imageMask,
            cancellationToken);

        return new PdfImageDecodeResult(
            new PdfDecodedImage(width, height, rgba),
            []);
    }

    private ReadOnlyMemory<byte> DecodePrefixFilters(
        ReadOnlyMemory<byte> encoded,
        PdfName[] filters,
        PdfDictionary dictionary,
        CancellationToken cancellationToken)
    {
        if (filters.Length == 1)
        {
            return encoded.ToArray();
        }

        PdfName[] prefix = filters[..^1];
        PdfDictionary?[]? parameters = ReadDecodeParameters(dictionary, filters.Length);
        PdfDictionary?[]? prefixParameters = parameters?[..^1];

        return pipeline.Decode(
            encoded,
            prefix,
            prefixParameters,
            filterLimits,
            cancellationToken);
    }

    private static byte[] DecodeRawSamples(
        ReadOnlySpan<byte> source,
        int width,
        int height,
        int components,
        int bits,
        double[] decode,
        bool imageMask,
        CancellationToken cancellationToken)
    {
        if (bits is not (1 or 2 or 4 or 8 or 16))
        {
            throw new NotSupportedException(
                $"BitsPerComponent={bits} is not supported.");
        }

        int samplesPerRow = checked(width * components);
        int rowBits = checked(samplesPerRow * bits);
        int rowBytes = checked((rowBits + 7) / 8);
        int required = checked(rowBytes * height);
        if (source.Length < required)
        {
            throw new InvalidDataException(
                $"Image sample stream contains {source.Length} bytes but at least {required} are required.");
        }

        byte[] rgba = new byte[checked(width * height * 4)];
        uint maximum = bits == 16 ? 65535u : (1u << bits) - 1u;
        Span<double> values = stackalloc double[4];

        for (int y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadOnlySpan<byte> row = source.Slice(y * rowBytes, rowBytes);

            for (int x = 0; x < width; x++)
            {
                for (int component = 0; component < components; component++)
                {
                    int sampleIndex = checked(x * components + component);
                    uint raw = ReadPackedSample(row, sampleIndex, bits);
                    double normalized = raw / (double)maximum;
                    values[component] = decode[component * 2] +
                        normalized * (decode[component * 2 + 1] - decode[component * 2]);
                }

                int destination = checked((y * width + x) * 4);
                if (imageMask)
                {
                    byte alpha = ToByte(values[0]);
                    rgba[destination] = 0;
                    rgba[destination + 1] = 0;
                    rgba[destination + 2] = 0;
                    rgba[destination + 3] = alpha;
                }
                else
                {
                    WriteColor(rgba, destination, values, components);
                }
            }
        }

        return rgba;
    }

    private static uint ReadPackedSample(
        ReadOnlySpan<byte> row,
        int sampleIndex,
        int bits)
    {
        if (bits == 8)
        {
            return row[sampleIndex];
        }

        if (bits == 16)
        {
            int offset = checked(sampleIndex * 2);
            return (uint)((row[offset] << 8) | row[offset + 1]);
        }

        int bitOffset = checked(sampleIndex * bits);
        int byteOffset = bitOffset / 8;
        int shift = 8 - bits - (bitOffset % 8);
        uint mask = (uint)((1 << bits) - 1);
        return (uint)(row[byteOffset] >> shift) & mask;
    }

    private static void WriteColor(
        byte[] rgba,
        int destination,
        ReadOnlySpan<double> values,
        int components)
    {
        switch (components)
        {
            case 1:
                byte gray = ToByte(values[0]);
                rgba[destination] = gray;
                rgba[destination + 1] = gray;
                rgba[destination + 2] = gray;
                break;

            case 3:
                rgba[destination] = ToByte(values[0]);
                rgba[destination + 1] = ToByte(values[1]);
                rgba[destination + 2] = ToByte(values[2]);
                break;

            case 4:
                double c = Clamp01(values[0]);
                double m = Clamp01(values[1]);
                double y = Clamp01(values[2]);
                double k = Clamp01(values[3]);
                rgba[destination] = ToByte((1 - c) * (1 - k));
                rgba[destination + 1] = ToByte((1 - m) * (1 - k));
                rgba[destination + 2] = ToByte((1 - y) * (1 - k));
                break;
        }

        rgba[destination + 3] = byte.MaxValue;
    }

    private static double[] ReadDecodeArray(
        PdfDictionary dictionary,
        int components,
        bool imageMask)
    {
        if (!TryGet(dictionary, DecodeKey, DKey, out PdfObject? value))
        {
            if (imageMask)
            {
                return [0, 1];
            }

            return [.. Enumerable.Range(0, components).SelectMany(_ => DefaultDecodePair)];
        }

        if (value is not PdfArray array || array.Count != components * 2)
        {
            throw new InvalidDataException("Image /Decode must contain two numbers per component.");
        }

        return [.. array.Select(ReadNumber)];
    }

    private static string ReadColorSpaceName(PdfDictionary dictionary)
    {
        if (!TryGet(dictionary, ColorSpaceKey, CsKey, out PdfObject? value))
        {
            throw new InvalidDataException("Image color space is missing.");
        }

        return value switch
        {
            PdfName name => name.Value,
            PdfArray array when array.Count > 0 && array[0] is PdfName name => name.Value,
            _ => throw new NotSupportedException("Complex image color-space resolution is deferred to the color pipeline.")
        };
    }

    private static PdfName[] ReadFilters(PdfDictionary dictionary)
    {
        if (!TryGet(dictionary, FilterKey, FKey, out PdfObject? value))
        {
            return [];
        }

        return value switch
        {
            PdfName name => [new PdfName(NormalizeFilterName(name.Value))],
            PdfArray array => array.Select(item =>
                item is PdfName name
                    ? new PdfName(NormalizeFilterName(name.Value))
                    : throw new InvalidDataException("Image /Filter entries must be names.")).ToArray(),
            _ => throw new InvalidDataException("Image /Filter must be a name or array.")
        };
    }

    private static string NormalizeFilterName(string name) => name switch
    {
        "AHx" => "ASCIIHexDecode",
        "A85" => "ASCII85Decode",
        "LZW" => "LZWDecode",
        "Fl" => "FlateDecode",
        "RL" => "RunLengthDecode",
        "CCF" => "CCITTFaxDecode",
        "DCT" => "DCTDecode",
        _ => name
    };

    private static PdfDictionary?[]? ReadDecodeParameters(
        PdfDictionary dictionary,
        int filterCount)
    {
        if (!TryGet(dictionary, DecodeParmsKey, DpKey, out PdfObject? value))
        {
            return null;
        }

        if (value is PdfNull)
        {
            return Enumerable.Repeat<PdfDictionary?>(null, filterCount).ToArray();
        }

        if (value is PdfDictionary single)
        {
            if (filterCount != 1)
            {
                throw new InvalidDataException("Single /DecodeParms requires one filter.");
            }

            return [single];
        }

        if (value is not PdfArray array || array.Count != filterCount)
        {
            throw new InvalidDataException("/DecodeParms must align with /Filter.");
        }

        return array.Select(item => item switch
        {
            PdfDictionary parameters => parameters,
            PdfNull => null,
            _ => throw new InvalidDataException("/DecodeParms entries must be dictionaries or null.")
        }).ToArray();
    }

    private static int ReadPositiveInt(
        PdfDictionary dictionary,
        PdfName longKey,
        PdfName shortKey) =>
        ReadOptionalPositiveInt(dictionary, longKey, shortKey)
        ?? throw new InvalidDataException($"Required image parameter /{longKey.Value} is missing.");

    private static int? ReadOptionalPositiveInt(
        PdfDictionary dictionary,
        PdfName longKey,
        PdfName shortKey)
    {
        if (!TryGet(dictionary, longKey, shortKey, out PdfObject? value))
        {
            return null;
        }

        if (value is not PdfInteger integer ||
            integer.Value <= 0 ||
            integer.Value > int.MaxValue)
        {
            throw new InvalidDataException($"/{longKey.Value} must be a positive integer.");
        }

        return (int)integer.Value;
    }

    private static bool ReadBoolean(
        PdfDictionary dictionary,
        PdfName longKey,
        PdfName shortKey) =>
        TryGet(dictionary, longKey, shortKey, out PdfObject? value) &&
        value is PdfBoolean boolean &&
        boolean.Value;

    private static bool TryGet(
        PdfDictionary dictionary,
        PdfName longKey,
        PdfName shortKey,
        out PdfObject? value)
    {
        if (dictionary.TryGetValue(longKey, out PdfObject found) ||
            dictionary.TryGetValue(shortKey, out found))
        {
            value = found;
            return true;
        }

        value = null;
        return false;
    }

    private static double ReadNumber(PdfObject value) => value switch
    {
        PdfInteger integer => integer.Value,
        PdfReal real => real.Value,
        _ => throw new InvalidDataException("Image numeric array contains a non-number.")
    };

    internal static void ValidateEncodedLength(int encodedLength, PdfImageDecodeLimits limits)
    {
        if (encodedLength > limits.MaximumEncodedBytes)
        {
            throw new InvalidDataException("Encoded image exceeds the configured byte limit.");
        }
    }

    internal static int ValidateDimensions(
        int width,
        int height,
        PdfImageDecodeLimits limits)
    {
        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException("Image dimensions must be positive integers.");
        }

        if (width > limits.MaximumDimension || height > limits.MaximumDimension)
        {
            throw new InvalidDataException("Image dimension exceeds the configured limit.");
        }

        if (checked((long)width * height) > limits.MaximumDecodedPixels)
        {
            throw new InvalidDataException("Decoded image exceeds the configured pixel limit.");
        }

        // PdfDecodedImage and the RGBA copy use int-sized arrays. Establish
        // their representable size before a backend can allocate native pixels.
        int byteCount;
        try
        {
            int pixelCount = checked(width * height);
            byteCount = checked(pixelCount * 4);
        }
        catch (OverflowException exception)
        {
            throw new InvalidDataException("Decoded image RGBA size exceeds the supported range.", exception);
        }

        if (byteCount > Array.MaxLength)
        {
            throw new InvalidDataException("Decoded image RGBA size exceeds the supported array length.");
        }

        return byteCount;
    }

    private static byte ToByte(double value) =>
        checked((byte)Math.Round(Clamp01(value) * byte.MaxValue));

    private static double Clamp01(double value) => Math.Clamp(value, 0, 1);
}
