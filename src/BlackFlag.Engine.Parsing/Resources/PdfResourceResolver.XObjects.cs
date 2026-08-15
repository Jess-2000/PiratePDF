using System.Collections.Concurrent;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Parsing.Resources;

public sealed partial class PdfResourceResolver
{
    private PdfResolvedXObject CreateXObject(
        PdfResourceIdentity identity,
        PdfObject source,
        PdfResourceScope? parent)
    {
        if (source is not PdfStream stream)
        {
            throw CreateException(
                PdfResourceResolutionFailure.InvalidXObject,
                PdfResourceKind.XObject,
                resourceName: null,
                "An XObject resource must resolve to a PDF stream.");
        }

        string subtype = RequireName(stream.Dictionary, "Subtype", PdfResourceKind.XObject);
        return subtype switch
        {
            "Image" => CreateImageXObject(identity, stream),
            "Form" => CreateFormXObject(identity, stream, parent),
            _ => throw CreateException(
                PdfResourceResolutionFailure.UnsupportedResource,
                PdfResourceKind.XObject,
                resourceName: null,
                $"XObject subtype /{subtype} is not supported by BF-1046.")
        };
    }

    private PdfResolvedImageXObject CreateImageXObject(
        PdfResourceIdentity identity,
        PdfStream stream)
    {
        if (stream.Data.Length > options.MaximumImageEncodedBytes)
        {
            throw CreateException(
                PdfResourceResolutionFailure.LimitExceeded,
                PdfResourceKind.XObject,
                resourceName: null,
                $"Image stream size {stream.Data.Length} exceeds the configured maximum of "
                + $"{options.MaximumImageEncodedBytes} bytes.");
        }

        int width = RequirePositiveInteger(stream.Dictionary, "Width", PdfResourceKind.XObject);
        int height = RequirePositiveInteger(stream.Dictionary, "Height", PdfResourceKind.XObject);
        bool imageMask = GetOptionalBoolean(stream.Dictionary, "ImageMask") ?? false;
        int? bitsPerComponent = GetOptionalInteger(stream.Dictionary, "BitsPerComponent");
        if (imageMask && bitsPerComponent is null)
        {
            bitsPerComponent = 1;
        }

        if (!imageMask && bitsPerComponent is null)
        {
            throw CreateException(
                PdfResourceResolutionFailure.InvalidXObject,
                PdfResourceKind.XObject,
                resourceName: null,
                "An image XObject must define /BitsPerComponent unless /ImageMask is true.");
        }

        return new PdfResolvedImageXObject(
            identity,
            stream,
            width,
            height,
            bitsPerComponent,
            imageMask,
            GetOptionalBoolean(stream.Dictionary, "Interpolate") ?? false,
            GetOptionalRaw(stream.Dictionary, "ColorSpace"),
            GetOptionalRaw(stream.Dictionary, "Decode"),
            GetOptionalRaw(stream.Dictionary, "Mask"),
            GetOptionalRaw(stream.Dictionary, "SMask"),
            GetOptionalRaw(stream.Dictionary, "Filter"));
    }

    private PdfResolvedFormXObject CreateFormXObject(
        PdfResourceIdentity identity,
        PdfStream stream,
        PdfResourceScope? parent)
    {
        PdfArray boundingBox = ResolveRequiredArray(stream.Dictionary, "BBox", PdfResourceKind.XObject);
        PdfArray? matrix = ResolveOptionalArray(stream.Dictionary, "Matrix");
        PdfDictionary? group = ResolveOptionalDictionary(stream.Dictionary, "Group");
        PdfObject? resourcesObject = GetOptionalRaw(stream.Dictionary, "Resources");

        var resourceScope = new Lazy<PdfResourceScope?>(
            () =>
            {
                if (resourcesObject is null)
                {
                    return parent;
                }

                PdfObject resolved = Dereference(resourcesObject);
                if (resolved is not PdfDictionary formResources)
                {
                    throw CreateException(
                        PdfResourceResolutionFailure.WrongResourceType,
                        PdfResourceKind.XObject,
                        resourceName: null,
                        "A Form XObject /Resources entry must resolve to a dictionary.");
                }

                return CreateScope(formResources, parent);
            },
            LazyThreadSafetyMode.ExecutionAndPublication);

        return new PdfResolvedFormXObject(
            identity,
            stream,
            boundingBox,
            matrix,
            resourceScope,
            group);
    }

}
