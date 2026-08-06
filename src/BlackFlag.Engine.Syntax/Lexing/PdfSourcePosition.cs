namespace BlackFlag.Engine.Syntax.Lexing;

/// <summary>
/// Identifies an exact byte and text position within PDF source data.
/// </summary>
public readonly record struct PdfSourcePosition
{
    /// <summary>
    /// Initializes a new source position.
    /// </summary>
    /// <param name="offset">
    /// The zero-based byte offset from the beginning of the source.
    /// </param>
    /// <param name="line">
    /// The one-based source line number.
    /// </param>
    /// <param name="column">
    /// The one-based source column number.
    /// </param>
    public PdfSourcePosition(
        long offset,
        int line,
        int column)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "The source offset cannot be negative.");
        }

        if (line < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(line),
                line,
                "The source line must be at least one.");
        }

        if (column < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(column),
                column,
                "The source column must be at least one.");
        }

        Offset = offset;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Gets the zero-based byte offset from the beginning of the source.
    /// </summary>
    public long Offset { get; }

    /// <summary>
    /// Gets the one-based source line number.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the one-based source column number.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Gets the position at the beginning of a source.
    /// </summary>
    public static PdfSourcePosition Start { get; } =
        new(
            offset: 0,
            line: 1,
            column: 1);
}
