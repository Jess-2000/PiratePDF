namespace BlackFlag.Engine.Syntax.Lexing;

/// <summary>
/// Represents an error encountered while reading PDF lexical syntax.
/// </summary>
public sealed class PdfLexerException : Exception
{
    /// <summary>
    /// Initializes a new lexer exception.
    /// </summary>
    /// <param name="message">
    /// A description of the lexical error.
    /// </param>
    /// <param name="position">
    /// The source position at which the error was detected.
    /// </param>
    public PdfLexerException(
        string message,
        PdfSourcePosition position)
        : base(CreateMessage(message, position))
    {
        Position = position;
    }

    /// <summary>
    /// Initializes a new lexer exception with an underlying exception.
    /// </summary>
    /// <param name="message">
    /// A description of the lexical error.
    /// </param>
    /// <param name="position">
    /// The source position at which the error was detected.
    /// </param>
    /// <param name="innerException">
    /// The exception that caused the lexical operation to fail.
    /// </param>
    public PdfLexerException(
        string message,
        PdfSourcePosition position,
        Exception innerException)
        : base(
            CreateMessage(message, position),
            innerException)
    {
        ArgumentNullException.ThrowIfNull(
            innerException);

        Position = position;
    }

    /// <summary>
    /// Gets the source position at which the error was detected.
    /// </summary>
    public PdfSourcePosition Position { get; }

    private static string CreateMessage(
        string message,
        PdfSourcePosition position)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            message);

        return string.Concat(
            message,
            " Offset: ",
            position.Offset,
            ", line: ",
            position.Line,
            ", column: ",
            position.Column,
            ".");
    }
}