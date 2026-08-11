
using BlackFlag.Engine.Rendering.DisplayList;

namespace BlackFlag.Engine.Rendering.Shadings;

/// <summary>Represents a renderer-neutral axial or radial PDF shading.</summary>
public sealed class PdfShadingDescriptor
{
    private readonly PdfGradientStop[] stops;

    /// <summary>Initializes a shading descriptor.</summary>
    public PdfShadingDescriptor(
        int shadingType,
        PdfPoint start,
        PdfPoint end,
        double startRadius,
        double endRadius,
        bool extendStart,
        bool extendEnd,
        IEnumerable<PdfGradientStop> stops)
    {
        if (shadingType is not 2 and not 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(shadingType),
                "BF-1048F canonical gradients support ShadingType 2 or 3.");
        }

        ArgumentNullException.ThrowIfNull(stops);

        ShadingType = shadingType;
        Start = start;
        End = end;
        StartRadius = startRadius;
        EndRadius = endRadius;
        ExtendStart = extendStart;
        ExtendEnd = extendEnd;
        this.stops = stops.ToArray();

        if (this.stops.Length < 2)
        {
            throw new ArgumentException(
                "A canonical shading requires at least two sampled color stops.",
                nameof(stops));
        }
    }

    /// <summary>Gets the PDF shading type.</summary>
    public int ShadingType { get; }

    /// <summary>Gets the start point.</summary>
    public PdfPoint Start { get; }

    /// <summary>Gets the end point.</summary>
    public PdfPoint End { get; }

    /// <summary>Gets the start radius for radial shadings.</summary>
    public double StartRadius { get; }

    /// <summary>Gets the end radius for radial shadings.</summary>
    public double EndRadius { get; }

    /// <summary>Gets whether color extends before the domain start.</summary>
    public bool ExtendStart { get; }

    /// <summary>Gets whether color extends after the domain end.</summary>
    public bool ExtendEnd { get; }

    /// <summary>Gets sampled sRGB stops.</summary>
    public IReadOnlyList<PdfGradientStop> Stops => stops;
}
