namespace BlackFlag.Engine.Core.Objects;

/// <summary>
/// Represents a PDF Boolean object.
/// </summary>
public sealed record PdfBoolean : PdfObject
{
    private PdfBoolean(bool value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the shared true value.
    /// </summary>
    public static PdfBoolean True { get; } = new(true);

    /// <summary>
    /// Gets the shared false value.
    /// </summary>
    public static PdfBoolean False { get; } = new(false);

    /// <summary>
    /// Gets the underlying Boolean value.
    /// </summary>
    public bool Value { get; }

    /// <inheritdoc />
    public override PdfObjectKind Kind => PdfObjectKind.Boolean;

    /// <summary>
    /// Gets the shared PDF Boolean instance for a value.
    /// </summary>
    public static PdfBoolean FromValue(bool value)
    {
        return value ? True : False;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value ? "true" : "false";
    }
}
