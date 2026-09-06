namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Describes explicit text-space geometry for one local decoration.
/// Automatic advance measurement is intentionally deferred to BF-1054T.
/// </summary>
public sealed class PdfExistingTextDecoration
{
    public PdfExistingTextDecoration(
        PdfExistingTextDecorationKind kind,
        double advanceWidth,
        double? offset = null,
        double? thickness = null)
        : this(
            kind,
            advanceWidth,
            requiresMeasurement: false,
            offset,
            thickness)
    {
    }

    private PdfExistingTextDecoration(
        PdfExistingTextDecorationKind kind,
        double advanceWidth,
        bool requiresMeasurement,
        double? offset,
        double? thickness)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (!requiresMeasurement)
        {
            ValidatePositive(advanceWidth, nameof(advanceWidth));
        }

        if (offset.HasValue &&
            !double.IsFinite(offset.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (thickness.HasValue)
        {
            ValidatePositive(thickness.Value, nameof(thickness));
        }

        Kind = kind;
        AdvanceWidth = advanceWidth;
        RequiresMeasurement = requiresMeasurement;
        Offset = offset;
        Thickness = thickness;
    }

    public PdfExistingTextDecorationKind Kind { get; }
    public double AdvanceWidth { get; }
    public bool RequiresMeasurement { get; }

    public static PdfExistingTextDecoration Automatic(
        PdfExistingTextDecorationKind kind,
        double? offset = null,
        double? thickness = null) =>
        new(
            kind,
            0,
            requiresMeasurement: true,
            offset,
            thickness);

    internal PdfExistingTextDecoration ResolveAdvance(
        double advanceWidth) =>
        RequiresMeasurement
            ? new PdfExistingTextDecoration(
                Kind,
                advanceWidth,
                requiresMeasurement: false,
                Offset,
                Thickness)
            : this;
    public double? Offset { get; }
    public double? Thickness { get; }

    private static void ValidatePositive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                name,
                value,
                "Decoration measurements must be finite and positive.");
        }
    }
}
