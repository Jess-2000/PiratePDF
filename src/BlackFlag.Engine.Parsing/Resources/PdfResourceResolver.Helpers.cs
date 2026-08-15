using System.Collections.Concurrent;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Parsing.Resources;

public sealed partial class PdfResourceResolver
{
    private static PdfName GetCategoryKey(PdfResourceKind kind) =>
        kind switch
        {
            PdfResourceKind.Font => FontKey,
            PdfResourceKind.XObject => XObjectKey,
            PdfResourceKind.ColorSpace => ColorSpaceKey,
            PdfResourceKind.ExtGState => ExtGStateKey,
            PdfResourceKind.Pattern => PatternKey,
            PdfResourceKind.Shading => ShadingKey,
            PdfResourceKind.Properties => PropertiesKey,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported resource kind.")
        };

    private static PdfResourceResolutionException InvalidColorSpace(string message) =>
        CreateException(
            PdfResourceResolutionFailure.InvalidColorSpace,
            PdfResourceKind.ColorSpace,
            resourceName: null,
            message);

    private static PdfResourceResolutionException CreateException(
        PdfResourceResolutionFailure failure,
        PdfResourceKind kind,
        string? resourceName,
        string message,
        Exception? innerException = null) =>
        new(failure, kind, resourceName, message, innerException);

    private readonly record struct ResourceCacheKey(
        PdfResourceKind Kind,
        PdfObjectId ObjectId);

    private readonly record struct LocalResourceKey(
        PdfResourceKind Kind,
        string Name);

}
