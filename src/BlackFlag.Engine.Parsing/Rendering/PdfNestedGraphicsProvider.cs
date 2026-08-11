
using BlackFlag.Engine.Filters;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.ContentStreams;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Parsing.Resources;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Interpretation;
using BlackFlag.Engine.Rendering.Nested;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Parsing.Rendering;

/// <summary>
/// Parses and interprets Form, Type 1 pattern, and Type 3 CharProc streams for
/// the renderer-neutral nested-content contracts.
/// </summary>
public sealed class PdfNestedGraphicsProvider :
    IPdfNestedGraphicsProvider,
    IPdfFormOperationProvider
{
    private static readonly PdfName FilterKey = new("Filter");
    private static readonly PdfName DecodeParmsKey = new("DecodeParms");
    private static readonly PdfName ResourcesKey = new("Resources");
    private static readonly PdfName CharProcsKey = new("CharProcs");
    private static readonly PdfName EncodingKey = new("Encoding");
    private static readonly PdfName DifferencesKey = new("Differences");

    private readonly IPdfIndirectObjectResolver objects;
    private readonly PdfResourceResolver resources;
    private readonly PdfFilterPipeline filters;
    private readonly PdfFilterDecodeLimits limits;
    private readonly PdfContentStreamParser parser;

    /// <summary>Initializes a nested-content provider.</summary>
    public PdfNestedGraphicsProvider(
        IPdfIndirectObjectResolver objects,
        PdfResourceResolver resources)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(resources);

        this.objects = objects;
        this.resources = resources;
        filters = new PdfFilterPipeline(PdfFilterRegistrations.CreateDefault());
        limits = new PdfFilterDecodeLimits();
        parser = new PdfContentStreamParser();
    }

    /// <inheritdoc />
    public IEnumerable<PdfGraphicsOperation> GetOperations(
        PdfResolvedFormXObject form,
        PdfResourceScope scope)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(scope);

        return parser.Parse(Decode(form.Stream));
    }

    /// <inheritdoc />
    public PdfTilingPatternCell GetPatternCell(
        PdfResolvedPattern pattern,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        if (pattern.PatternType != 1 || pattern.Source is not PdfStream stream)
        {
            throw new NotSupportedException(
                "A tiling-pattern cell requires a PatternType 1 stream.");
        }

        PdfDictionary dictionary = stream.Dictionary;
        PdfDisplayBounds bounds = ReadBounds(RequireArray(dictionary, "BBox"));
        double xStep = RequireNumber(dictionary, "XStep");
        double yStep = RequireNumber(dictionary, "YStep");
        int paintType = RequireInteger(dictionary, "PaintType");
        int tilingType = RequireInteger(dictionary, "TilingType");
        PdfMatrix matrix = dictionary.TryGetValue(
            new PdfName("Matrix"),
            out PdfObject matrixValue)
            ? ReadMatrix(RequireArray(matrixValue, "Matrix"))
            : PdfMatrix.Identity;

        PdfResourceScope scope = CreateNestedScope(dictionary);
        IReadOnlyList<PdfGraphicsOperation> operations =
            parser.Parse(Decode(stream, cancellationToken));

        var interpreter = new PdfGraphicsInterpreter(
            formOperationProvider: this);

        PdfDisplayList content = interpreter.Interpret(operations, scope);

        return new PdfTilingPatternCell(
            content,
            bounds,
            xStep,
            yStep,
            matrix,
            paintType,
            tilingType);
    }

    /// <inheritdoc />
    public PdfType3GlyphProgram GetType3Glyph(
        PdfResolvedFont font,
        ReadOnlyMemory<byte> sourceCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(font);

        if (font.Technology != PdfFontTechnology.Type3)
        {
            throw new NotSupportedException(
                $"Font '{font.FaceName}' is not a Type 3 font.");
        }

        if (sourceCode.Length != 1)
        {
            throw new NotSupportedException(
                "BF-1048F Type 3 glyph lookup currently requires one-byte source codes.");
        }

        string glyphName = ResolveGlyphName(font.Dictionary, sourceCode.Span[0]);

        PdfDictionary charProcs = ResolveDictionary(
            Require(font.Dictionary, CharProcsKey, "Type 3 /CharProcs is required."));

        if (!charProcs.TryGetValue(new PdfName(glyphName), out PdfObject charProcObject))
        {
            throw new InvalidDataException(
                $"Type 3 CharProc /{glyphName} is missing.");
        }

        PdfObject resolved = Resolve(charProcObject);
        if (resolved is not PdfStream charProc)
        {
            throw new InvalidDataException(
                $"Type 3 CharProc /{glyphName} must resolve to a stream.");
        }

        PdfResourceScope scope = CreateNestedScope(font.Dictionary);
        IReadOnlyList<PdfGraphicsOperation> operations =
            parser.Parse(Decode(charProc, cancellationToken));

        var interpreter = new PdfGraphicsInterpreter(
            formOperationProvider: this);

        PdfDisplayList content = interpreter.Interpret(operations, scope);
        PdfMatrix fontMatrix = font.Dictionary.TryGetValue(
            new PdfName("FontMatrix"),
            out PdfObject matrixObject)
            ? ReadMatrix(RequireArray(matrixObject, "FontMatrix"))
            : new PdfMatrix(0.001, 0, 0, 0.001, 0, 0);

        return new PdfType3GlyphProgram(
            glyphName,
            content,
            fontMatrix);
    }

    private PdfResourceScope CreateNestedScope(PdfDictionary dictionary)
    {
        if (!dictionary.TryGetValue(ResourcesKey, out PdfObject resourceObject))
        {
            return resources.CreateScope(PdfDictionary.Empty);
        }

        return resources.CreateScope(ResolveDictionary(resourceObject));
    }

    private ReadOnlyMemory<byte> Decode(
        PdfStream stream,
        CancellationToken cancellationToken = default)
    {
        PdfName[] filterNames = ReadFilters(stream.Dictionary);
        PdfDictionary?[]? parameters =
            ReadDecodeParameters(stream.Dictionary, filterNames.Length);

        return filters.Decode(
            stream.Data,
            filterNames,
            parameters,
            limits,
            cancellationToken);
    }

    private PdfObject Resolve(PdfObject value) =>
        value is PdfIndirectReference reference
            ? objects.Resolve(new PdfObjectId(
                reference.ObjectNumber,
                reference.GenerationNumber))
            : value;

    private PdfDictionary ResolveDictionary(PdfObject value) =>
        Resolve(value) as PdfDictionary
        ?? throw new InvalidDataException(
            "A nested graphics dictionary reference did not resolve to a dictionary.");

    private static string ResolveGlyphName(
        PdfDictionary font,
        byte code)
    {
        if (!font.TryGetValue(EncodingKey, out PdfObject encoding) ||
            encoding is not PdfDictionary dictionary ||
            !dictionary.TryGetValue(DifferencesKey, out PdfObject differencesObject) ||
            differencesObject is not PdfArray differences)
        {
            return code switch
            {
                >= 32 and <= 126 => ((char)code).ToString(),
                _ => ".notdef"
            };
        }

        int current = -1;
        foreach (PdfObject item in differences)
        {
            switch (item)
            {
                case PdfInteger integer:
                    current = checked((int)integer.Value);
                    break;

                case PdfName name when current >= 0:
                    if (current == code)
                    {
                        return name.Value;
                    }

                    current++;
                    break;
            }
        }

        return ".notdef";
    }

    private static PdfDisplayBounds ReadBounds(PdfArray array)
    {
        if (array.Count != 4)
        {
            throw new InvalidDataException("Pattern /BBox must contain four numbers.");
        }

        return PdfDisplayBounds.FromCorners(
            new PdfPoint(ReadNumber(array[0]), ReadNumber(array[1])),
            new PdfPoint(ReadNumber(array[2]), ReadNumber(array[3])));
    }

    private static PdfMatrix ReadMatrix(PdfArray array)
    {
        if (array.Count != 6)
        {
            throw new InvalidDataException("PDF matrix must contain six numbers.");
        }

        return new PdfMatrix(
            ReadNumber(array[0]),
            ReadNumber(array[1]),
            ReadNumber(array[2]),
            ReadNumber(array[3]),
            ReadNumber(array[4]),
            ReadNumber(array[5]));
    }

    private static PdfArray RequireArray(
        PdfDictionary dictionary,
        string key) =>
        dictionary.TryGetValue(new PdfName(key), out PdfObject value)
            ? RequireArray(value, key)
            : throw new InvalidDataException($"/{key} is required.");

    private static PdfArray RequireArray(
        PdfObject value,
        string key) =>
        value as PdfArray
        ?? throw new InvalidDataException($"/{key} must be an array.");

    private static PdfObject Require(
        PdfDictionary dictionary,
        PdfName key,
        string message) =>
        dictionary.TryGetValue(key, out PdfObject value)
            ? value
            : throw new InvalidDataException(message);

    private static int RequireInteger(
        PdfDictionary dictionary,
        string key)
    {
        if (!dictionary.TryGetValue(new PdfName(key), out PdfObject value) ||
            value is not PdfInteger integer ||
            integer.Value is < int.MinValue or > int.MaxValue)
        {
            throw new InvalidDataException($"/{key} must be an integer.");
        }

        return (int)integer.Value;
    }

    private static double RequireNumber(
        PdfDictionary dictionary,
        string key) =>
        dictionary.TryGetValue(new PdfName(key), out PdfObject value)
            ? ReadNumber(value)
            : throw new InvalidDataException($"/{key} is required.");

    private static double ReadNumber(PdfObject value) => value switch
    {
        PdfInteger integer => integer.Value,
        PdfReal real => real.Value,
        _ => throw new InvalidDataException("Expected a numeric PDF object.")
    };

    private static PdfName[] ReadFilters(PdfDictionary dictionary)
    {
        if (!dictionary.TryGetValue(FilterKey, out PdfObject value))
        {
            return [];
        }

        return value switch
        {
            PdfName name => [name],
            PdfArray array => array.Select(item =>
                item as PdfName ??
                throw new InvalidDataException(
                    "Every nested-content /Filter entry must be a name.")).ToArray(),
            _ => throw new InvalidDataException(
                "Nested-content /Filter must be a name or array.")
        };
    }

    private static PdfDictionary?[]? ReadDecodeParameters(
        PdfDictionary dictionary,
        int filterCount)
    {
        if (!dictionary.TryGetValue(DecodeParmsKey, out PdfObject value))
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
                throw new InvalidDataException(
                    "A single /DecodeParms dictionary requires one filter.");
            }

            return [single];
        }

        if (value is not PdfArray array || array.Count != filterCount)
        {
            throw new InvalidDataException(
                "/DecodeParms must align with /Filter.");
        }

        return array.Select(item => item switch
        {
            PdfDictionary parameters => parameters,
            PdfNull => null,
            _ => throw new InvalidDataException(
                "/DecodeParms entries must be dictionaries or null.")
        }).ToArray();
    }
}
