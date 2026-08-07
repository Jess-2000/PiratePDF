namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>Represents an immutable PDF line dash pattern.</summary>
public sealed class PdfDashPattern
{
    private readonly double[] elements;

    /// <summary>Initializes a dash pattern.</summary>
    public PdfDashPattern(
        IEnumerable<double> elements,
        double phase)
    {
        ArgumentNullException.ThrowIfNull(elements);
        this.elements = elements.ToArray();
        Phase = phase;
    }

    /// <summary>Gets dash and gap lengths.</summary>
    public IReadOnlyList<double> Elements => elements;

    /// <summary>Gets the dash phase.</summary>
    public double Phase { get; }

    /// <summary>Gets the solid-line dash pattern.</summary>
    public static PdfDashPattern Solid { get; } = new(Array.Empty<double>(), 0);
}
