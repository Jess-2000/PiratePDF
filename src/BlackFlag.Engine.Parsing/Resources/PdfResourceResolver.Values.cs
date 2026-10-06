using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Parsing.Resources;

public sealed partial class PdfResourceResolver
{
    private PdfObject Dereference(PdfObject value)
    {
        if (value is not PdfIndirectReference reference)
        {
            return value;
        }

        return objectResolver.Resolve(
            new PdfObjectId(reference.ObjectNumber, reference.GenerationNumber));
    }

    private PdfDictionary? ResolveOptionalDictionary(PdfDictionary dictionary, string key)
    {
        PdfObject? value = GetOptionalRaw(dictionary, key);
        if (value is null)
        {
            return null;
        }

        return Dereference(value) as PdfDictionary
            ?? throw new InvalidOperationException($"/{key} must resolve to a dictionary.");
    }

    private PdfStream? ResolveOptionalStream(PdfDictionary dictionary, string key)
    {
        PdfObject? value = GetOptionalRaw(dictionary, key);
        if (value is null)
        {
            return null;
        }

        return Dereference(value) as PdfStream
            ?? throw new InvalidOperationException($"/{key} must resolve to a stream.");
    }

    private PdfArray? ResolveOptionalArray(PdfDictionary dictionary, string key)
    {
        PdfObject? value = GetOptionalRaw(dictionary, key);
        if (value is null)
        {
            return null;
        }

        return Dereference(value) as PdfArray
            ?? throw new InvalidOperationException($"/{key} must resolve to an array.");
    }

    private PdfArray ResolveRequiredArray(
        PdfDictionary dictionary,
        string key,
        PdfResourceKind kind) =>
        ResolveOptionalArray(dictionary, key)
        ?? throw CreateException(
            PdfResourceResolutionFailure.MalformedResource,
            kind,
            resourceName: null,
            $"Resource dictionary is missing required /{key} array.");

    private PdfStream? ResolveEmbeddedFontProgram(PdfDictionary? descriptor)
    {
        if (descriptor is null)
        {
            return null;
        }

        return ResolveOptionalStream(descriptor, "FontFile")
            ?? ResolveOptionalStream(descriptor, "FontFile2")
            ?? ResolveOptionalStream(descriptor, "FontFile3");
    }

    private static PdfDictionary RequireDictionary(PdfObject source, PdfResourceKind kind) =>
        source as PdfDictionary
        ?? throw CreateException(
            PdfResourceResolutionFailure.WrongResourceType,
            kind,
            resourceName: null,
            $"A {kind} resource must resolve to a PDF dictionary.");

    private static PdfDictionary GetDictionaryFromDictionaryOrStream(PdfObject source, PdfResourceKind kind) =>
        source switch
        {
            PdfDictionary dictionary => dictionary,
            PdfStream stream => stream.Dictionary,
            _ => throw CreateException(
                PdfResourceResolutionFailure.WrongResourceType,
                kind,
                resourceName: null,
                $"A {kind} resource must resolve to a dictionary or stream.")
        };

    private static string RequireName(PdfDictionary dictionary, string key, PdfResourceKind kind) =>
        GetOptionalName(dictionary, key)
        ?? throw CreateException(
            PdfResourceResolutionFailure.MalformedResource,
            kind,
            resourceName: null,
            $"Resource dictionary is missing required /{key} name.");

    private static string? GetOptionalName(PdfDictionary? dictionary, string key)
    {
        if (dictionary is null || !dictionary.TryGetValue(new PdfName(key), out PdfObject value))
        {
            return null;
        }

        return value is PdfName name
            ? name.Value
            : throw new InvalidOperationException($"/{key} must be a PDF name.");
    }

    private static string? GetOptionalString(PdfDictionary? dictionary, string key)
    {
        if (dictionary is null || !dictionary.TryGetValue(new PdfName(key), out PdfObject value))
        {
            return null;
        }

        return value is PdfLiteralString text
            ? text.Value
            : null;
    }

    private static PdfObject? GetOptionalRaw(PdfDictionary dictionary, string key) =>
        dictionary.TryGetValue(new PdfName(key), out PdfObject value)
            ? value
            : null;

    private static int RequirePositiveInteger(PdfDictionary dictionary, string key, PdfResourceKind kind)
    {
        int? value = GetOptionalInteger(dictionary, key);
        if (value is null || value <= 0)
        {
            throw CreateException(
                PdfResourceResolutionFailure.MalformedResource,
                kind,
                resourceName: null,
                $"/{key} must be a positive integer.");
        }

        return value.Value;
    }

    private static int? GetOptionalInteger(PdfDictionary dictionary, string key)
    {
        if (!dictionary.TryGetValue(new PdfName(key), out PdfObject value))
        {
            return null;
        }

        if (value is not PdfInteger integer || integer.Value is < int.MinValue or > int.MaxValue)
        {
            throw new InvalidOperationException($"/{key} must be a 32-bit PDF integer.");
        }

        return checked((int)integer.Value);
    }

    private static double? GetOptionalNumber(PdfDictionary dictionary, string key)
    {
        if (!dictionary.TryGetValue(new PdfName(key), out PdfObject value))
        {
            return null;
        }

        return value switch
        {
            PdfInteger integer => integer.Value,
            PdfReal real => real.Value,
            _ => throw new InvalidOperationException($"/{key} must be numeric.")
        };
    }

    private static bool? GetOptionalBoolean(PdfDictionary dictionary, string key)
    {
        if (!dictionary.TryGetValue(new PdfName(key), out PdfObject value))
        {
            return null;
        }

        return value is PdfBoolean boolean
            ? boolean.Value
            : throw new InvalidOperationException($"/{key} must be a PDF boolean.");
    }

}
