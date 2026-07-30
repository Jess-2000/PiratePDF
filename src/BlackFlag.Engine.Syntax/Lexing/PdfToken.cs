namespace BlackFlag.Engine.Syntax.Lexing;

/// <summary>
/// Describes a lexical token and its location within PDF source data.
/// </summary>
/// <remarks>
/// The token stores a source span rather than copied token bytes. This keeps
/// the lexer hot path allocation-free and allows later parsing stages to read
/// token data only when required.
/// </remarks>
public readonly record struct PdfToken
{
    /// <summary>
    /// Initializes a new PDF token.
    /// </summary>
    /// <param name="kind">
    /// The token's lexical category.
    /// </param>
    /// <param name="span">
    /// The source range occupied by the token.
    /// </param>
    public PdfToken(
        PdfTokenKind kind,
        PdfSourceSpan span)
    {
        if (kind == PdfTokenKind.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "A token must have an assigned token kind.");
        }

        if (
            kind != PdfTokenKind.EndOfFile &&
            span.IsEmpty)
        {
            throw new ArgumentException(
                "Only an end-of-file token may have an empty source span.",
                nameof(span));
        }

        Kind = kind;
        Span = span;
    }

    /// <summary>
    /// Gets the token's lexical category.
    /// </summary>
    public PdfTokenKind Kind { get; }

    /// <summary>
    /// Gets the source range occupied by the token.
    /// </summary>
    public PdfSourceSpan Span { get; }

    /// <summary>
    /// Creates an end-of-file token at the specified position.
    /// </summary>
    /// <param name="position">
    /// The position immediately following the final source byte.
    /// </param>
    /// <returns>
    /// An end-of-file token.
    /// </returns>
    public static PdfToken EndOfFileAt(
        PdfSourcePosition position)
    {
        return new PdfToken(
            PdfTokenKind.EndOfFile,
            PdfSourceSpan.EmptyAt(position));
    }
}