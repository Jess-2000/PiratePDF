using System.Collections.Concurrent;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Parsing.Resources;

public sealed partial class PdfResourceResolver
{
    private PdfResolvedColorSpace CreateColorSpace(
        PdfResourceIdentity identity,
        PdfObject source)
    {
        PdfObject resolved = Dereference(source);

        if (resolved is PdfName name)
        {
            return CreateNamedColorSpace(identity, resolved, name.Value);
        }

        if (resolved is not PdfArray array || array.Count == 0 || array[0] is not PdfName family)
        {
            throw CreateException(
                PdfResourceResolutionFailure.InvalidColorSpace,
                PdfResourceKind.ColorSpace,
                resourceName: null,
                "A color-space resource must be a name or a non-empty array beginning with a name.");
        }

        return family.Value switch
        {
            "CalGray" => CreateSimpleColorSpace(identity, resolved, PdfColorSpaceKind.CalGray, 1),
            "CalRGB" => CreateSimpleColorSpace(identity, resolved, PdfColorSpaceKind.CalRgb, 3),
            "Lab" => CreateSimpleColorSpace(identity, resolved, PdfColorSpaceKind.Lab, 3),
            "Indexed" => CreateSimpleColorSpace(identity, resolved, PdfColorSpaceKind.Indexed, 1),
            "Pattern" => CreateSimpleColorSpace(identity, resolved, PdfColorSpaceKind.Pattern, 0),
            "ICCBased" => CreateIccBasedColorSpace(identity, resolved, array),
            "Separation" => CreateSeparationColorSpace(identity, resolved, array),
            "DeviceN" => CreateDeviceNColorSpace(identity, resolved, array),
            _ => throw CreateException(
                PdfResourceResolutionFailure.UnsupportedResource,
                PdfResourceKind.ColorSpace,
                resourceName: null,
                $"Color-space family /{family.Value} is not supported by BF-1046.")
        };
    }

    private static PdfResolvedColorSpace CreateNamedColorSpace(
        PdfResourceIdentity identity,
        PdfObject source,
        string name) =>
        name switch
        {
            "DeviceGray" or "G" => CreateSimpleColorSpace(identity, source, PdfColorSpaceKind.DeviceGray, 1),
            "DeviceRGB" or "RGB" => CreateSimpleColorSpace(identity, source, PdfColorSpaceKind.DeviceRgb, 3),
            "DeviceCMYK" or "CMYK" => CreateSimpleColorSpace(identity, source, PdfColorSpaceKind.DeviceCmyk, 4),
            "Pattern" => CreateSimpleColorSpace(identity, source, PdfColorSpaceKind.Pattern, 0),
            _ => new PdfResolvedColorSpace(
                identity,
                source,
                PdfColorSpaceKind.Unknown,
                componentCount: 0,
                Array.Empty<string>(),
                iccProfile: null,
                alternateSpace: null,
                tintTransform: null)
        };

    private static PdfResolvedColorSpace CreateSimpleColorSpace(
        PdfResourceIdentity identity,
        PdfObject source,
        PdfColorSpaceKind kind,
        int components) =>
        new(
            identity,
            source,
            kind,
            components,
            Array.Empty<string>(),
            iccProfile: null,
            alternateSpace: null,
            tintTransform: null);

    private PdfResolvedColorSpace CreateIccBasedColorSpace(
        PdfResourceIdentity identity,
        PdfObject source,
        PdfArray array)
    {
        if (array.Count < 2)
        {
            throw InvalidColorSpace("An ICCBased color space requires an ICC profile stream.");
        }

        PdfObject profileObject = Dereference(array[1]);
        if (profileObject is not PdfStream profile)
        {
            throw InvalidColorSpace("An ICCBased profile must resolve to a stream.");
        }

        if (profile.Length > options.MaximumIccProfileBytes)
        {
            throw CreateException(
                PdfResourceResolutionFailure.LimitExceeded,
                PdfResourceKind.ColorSpace,
                resourceName: null,
                $"ICC profile size {profile.Length} exceeds the configured maximum of "
                + $"{options.MaximumIccProfileBytes} bytes.");
        }

        int components = RequirePositiveInteger(profile.Dictionary, "N", PdfResourceKind.ColorSpace);
        PdfObject? alternate = GetOptionalRaw(profile.Dictionary, "Alternate");
        return new PdfResolvedColorSpace(
            identity,
            source,
            PdfColorSpaceKind.IccBased,
            components,
            Array.Empty<string>(),
            profile,
            alternate,
            tintTransform: null);
    }

    private static PdfResolvedColorSpace CreateSeparationColorSpace(
        PdfResourceIdentity identity,
        PdfObject source,
        PdfArray array)
    {
        if (array.Count < 4 || array[1] is not PdfName colorant)
        {
            throw InvalidColorSpace("A Separation color space requires a colorant name, alternate space, and tint transform.");
        }

        return new PdfResolvedColorSpace(
            identity,
            source,
            PdfColorSpaceKind.Separation,
            componentCount: 1,
            new[] { colorant.Value },
            iccProfile: null,
            array[2],
            array[3]);
    }

    private static PdfResolvedColorSpace CreateDeviceNColorSpace(
        PdfResourceIdentity identity,
        PdfObject source,
        PdfArray array)
    {
        if (array.Count < 4 || array[1] is not PdfArray names)
        {
            throw InvalidColorSpace("A DeviceN color space requires a colorant-name array, alternate space, and tint transform.");
        }

        var colorants = new string[names.Count];
        for (int index = 0; index < names.Count; index++)
        {
            if (names[index] is not PdfName colorant)
            {
                throw InvalidColorSpace("Every DeviceN colorant must be a PDF name.");
            }

            colorants[index] = colorant.Value;
        }

        return new PdfResolvedColorSpace(
            identity,
            source,
            PdfColorSpaceKind.DeviceN,
            names.Count,
            colorants,
            iccProfile: null,
            array[2],
            array[3]);
    }

}
