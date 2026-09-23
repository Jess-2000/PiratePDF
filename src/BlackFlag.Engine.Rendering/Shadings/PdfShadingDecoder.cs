
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.Color;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Functions;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.Shadings;

/// <summary>Decodes axial and radial PDF shadings into sampled sRGB descriptors.</summary>
public sealed class PdfShadingDecoder
{
    private const int DefaultSampleCount = 33;

    private readonly PdfColorConverter colors = new();

    /// <summary>Decodes an axial or radial resolved shading.</summary>
    public PdfShadingDescriptor Decode(
        PdfResolvedShading shading,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shading);

        if (shading.ShadingType is not 2 and not 3)
        {
            throw new NotSupportedException(
                $"BF-1048F raster shading currently supports ShadingType 2 and 3; received {shading.ShadingType}.");
        }

        PdfDictionary dictionary = shading.Source switch
        {
            PdfDictionary value => value,
            PdfStream stream => stream.Dictionary,
            _ => throw new InvalidDataException(
                "A resolved shading source must be a dictionary or stream.")
        };

        double[] coords = ReadNumbers(
            RequireArray(dictionary, "Coords"));

        if ((shading.ShadingType == 2 && coords.Length != 4) ||
            (shading.ShadingType == 3 && coords.Length != 6))
        {
            throw new InvalidDataException(
                "The shading /Coords array has the wrong number of values.");
        }

        double[] domain = dictionary.TryGetValue(
            new PdfName("Domain"),
            out PdfObject domainObject)
            ? ReadNumbers(RequireArray(domainObject, "Domain"))
            : [0, 1];

        if (domain.Length != 2)
        {
            throw new InvalidDataException(
                "A shading /Domain must contain exactly two numbers.");
        }

        bool[] extend = ReadExtend(dictionary);
        PdfObject function = shading.Function
            ?? throw new InvalidDataException(
                "BF-1048F axial/radial shading requires a resolved /Function.");

        PdfColorValue prototype = CreateColorPrototype(shading.ColorSpace);
        var stops = new List<PdfGradientStop>(DefaultSampleCount);

        for (int index = 0; index < DefaultSampleCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            double fraction = index / (double)(DefaultSampleCount - 1);
            double t = domain[0] + fraction * (domain[1] - domain[0]);
            double[] components = PdfFunctionEvaluator.Evaluate(function, [t]);

            PdfColorValue value = new(
                prototype.ColorSpaceName,
                components,
                prototype.Resource);

            stops.Add(new PdfGradientStop(
                fraction,
                colors.Convert(value, PdfColorTarget.Srgb)));
        }

        return new PdfShadingDescriptor(
            shading.ShadingType,
            new PdfPoint(coords[0], coords[1]),
            new PdfPoint(coords[shading.ShadingType == 2 ? 2 : 3],
                        coords[shading.ShadingType == 2 ? 3 : 4]),
            shading.ShadingType == 3 ? coords[2] : 0,
            shading.ShadingType == 3 ? coords[5] : 0,
            extend[0],
            extend[1],
            stops);
    }

    private static PdfColorValue CreateColorPrototype(PdfObject? colorSpace)
    {
        return colorSpace switch
        {
            PdfName name when name.Value is "DeviceGray" or "G" =>
                new PdfColorValue("DeviceGray", [0]),
            PdfName name when name.Value is "DeviceRGB" or "RGB" =>
                new PdfColorValue("DeviceRGB", [0, 0, 0]),
            PdfName name when name.Value is "DeviceCMYK" or "CMYK" =>
                new PdfColorValue("DeviceCMYK", [0, 0, 0, 0]),
            _ => throw new NotSupportedException(
                "BF-1048F shading decoding currently requires a device color-space name.")
        };
    }

    private static bool[] ReadExtend(PdfDictionary dictionary)
    {
        if (!dictionary.TryGetValue(new PdfName("Extend"), out PdfObject value))
        {
            return [false, false];
        }

        PdfArray array = RequireArray(value, "Extend");
        if (array.Count != 2 ||
            array[0] is not PdfBoolean first ||
            array[1] is not PdfBoolean second)
        {
            throw new InvalidDataException(
                "A shading /Extend entry must contain two booleans.");
        }

        return [first.Value, second.Value];
    }

    private static PdfArray RequireArray(PdfDictionary dictionary, string key) =>
        dictionary.TryGetValue(new PdfName(key), out PdfObject value)
            ? RequireArray(value, key)
            : throw new InvalidDataException($"Shading /{key} is required.");

    private static PdfArray RequireArray(PdfObject value, string key) =>
        value as PdfArray
        ?? throw new InvalidDataException($"Shading /{key} must be an array.");

    private static double[] ReadNumbers(PdfArray array) =>
        array.Select(value => value switch
        {
            PdfInteger integer => (double)integer.Value,
            PdfReal real => real.Value,
            _ => throw new InvalidDataException(
                "A shading numeric array contains a non-number.")
        }).ToArray();
}
