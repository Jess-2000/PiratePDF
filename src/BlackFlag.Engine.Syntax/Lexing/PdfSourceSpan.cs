namespace BlackFlag.Engine.Syntax.Lexing;

/// <summary>
/// Identifies a half-open range of bytes within PDF source data.
/// </summary>
/// <remarks>
/// The start position is inclusive and the end position is exclusive.
/// </remarks>
public readonly record struct PdfSourceSpan
{
    /// <summary>
    /// Initializes a new source span.
    /// </summary>
    /// <param name="start">
    /// The inclusive starting position.
    /// </param>
    /// <param name="end">
    /// The exclusive ending position.
    /// </param>
    public PdfSourceSpan(
        PdfSourcePosition start,
        PdfSourcePosition end)
    {
        if (end.Offset < start.Offset)
        {
            throw new ArgumentException(
                "The end position cannot precede the start position.",
                nameof(end));
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets the inclusive starting position.
    /// </summary>
    public PdfSourcePosition Start { get; }

    /// <summary>
    /// Gets the exclusive ending position.
    /// </summary>
    public PdfSourcePosition End { get; }

    /// <summary>
    /// Gets the number of bytes covered by the span.
    /// </summary>
    public long Length =>
        End.Offset - Start.Offset;

    /// <summary>
    /// Gets a value indicating whether the span contains no bytes.
    /// </summary>
    public bool IsEmpty =>
        Length == 0;

    /// <summary>
    /// Creates an empty source span at the specified position.
    /// </summary>
    /// <param name="position">
    /// The position at which the empty span begins and ends.
    /// </param>
    /// <returns>
    /// An empty source span.
    /// </returns>
    public static PdfSourceSpan EmptyAt(
        PdfSourcePosition position)
    {
        return new PdfSourceSpan(
            position,
            position);
    }
}
