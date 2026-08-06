namespace BlackFlag.Engine.Syntax.Lexing;

/// <summary>
/// Defines sequential lexical access to PDF source data.
/// </summary>
public interface IPdfLexer
{
    /// <summary>
    /// Gets the position at which the next token scan will begin.
    /// </summary>
    PdfSourcePosition Position { get; }

    /// <summary>
    /// Reads the next token from the source.
    /// </summary>
    /// <returns>
    /// The next token, or an end-of-file token when no source data remains.
    /// </returns>
    PdfToken ReadToken();
}
