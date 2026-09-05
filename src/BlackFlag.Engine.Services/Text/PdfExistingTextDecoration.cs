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
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        ValidatePositive(advanceWidth, nameof(advanceWidth));

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
        Offset = offset;
        Thickness = thickness;
    }

    public PdfExistingTextDecorationKind Kind { get; }
    public double AdvanceWidth { get; }
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
