using System.Collections.Concurrent;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Parsing.Resources;

public sealed partial class PdfResourceResolver
{
    private static PdfResolvedExtGState CreateExtGState(
        PdfResourceIdentity identity,
        PdfObject source)
    {
        PdfDictionary dictionary = RequireDictionary(source, PdfResourceKind.ExtGState);
        return new PdfResolvedExtGState(
            identity,
            dictionary,
            GetOptionalNumber(dictionary, "CA"),
            GetOptionalNumber(dictionary, "ca"),
            GetOptionalBoolean(dictionary, "OP"),
            GetOptionalBoolean(dictionary, "op"),
            GetOptionalInteger(dictionary, "OPM"),
            GetOptionalRaw(dictionary, "BM"),
            GetOptionalRaw(dictionary, "SMask"));
    }

    private static PdfResolvedPattern CreatePattern(
        PdfResourceIdentity identity,
        PdfObject source)
    {
        PdfDictionary dictionary = GetDictionaryFromDictionaryOrStream(source, PdfResourceKind.Pattern);
        int patternType = RequirePositiveInteger(dictionary, "PatternType", PdfResourceKind.Pattern);
        if (patternType is not 1 and not 2)
        {
            throw CreateException(
                PdfResourceResolutionFailure.UnsupportedResource,
                PdfResourceKind.Pattern,
                resourceName: null,
                $"PatternType {patternType} is not supported.");
        }

        return new PdfResolvedPattern(identity, source, patternType);
    }

    private static PdfResolvedShading CreateShading(
        PdfResourceIdentity identity,
        PdfObject source)
    {
        PdfDictionary dictionary = GetDictionaryFromDictionaryOrStream(source, PdfResourceKind.Shading);
        int shadingType = RequirePositiveInteger(dictionary, "ShadingType", PdfResourceKind.Shading);
        if (shadingType is < 1 or > 7)
        {
            throw CreateException(
                PdfResourceResolutionFailure.UnsupportedResource,
                PdfResourceKind.Shading,
                resourceName: null,
                $"ShadingType {shadingType} is outside the supported range from 1 through 7.");
        }

        return new PdfResolvedShading(
            identity,
            source,
            shadingType,
            GetOptionalRaw(dictionary, "ColorSpace"),
            GetOptionalRaw(dictionary, "Function"));
    }

    private static PdfResolvedProperties CreateProperties(
        PdfResourceIdentity identity,
        PdfObject source) =>
        new(identity, RequireDictionary(source, PdfResourceKind.Properties));

}
