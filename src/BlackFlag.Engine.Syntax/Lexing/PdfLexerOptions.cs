namespace BlackFlag.Engine.Syntax.Lexing;

/// <summary>
/// Configures limits applied while reading PDF lexical tokens.
/// </summary>
public sealed class PdfLexerOptions
{
    /// <summary>
    /// The default maximum number of source bytes that may belong to one
    /// lexical token.
    /// </summary>
    public const int DefaultMaximumTokenLength = 16 * 1024 * 1024;

    private int maximumTokenLength =
        DefaultMaximumTokenLength;

    /// <summary>
    /// Gets or sets the maximum number of source bytes that may belong to one
    /// lexical token.
    /// </summary>
    /// <remarks>
    /// The limit protects the lexer from unbounded scanning when malformed
    /// input contains a missing string terminator or another unterminated
    /// token. It does not limit stream data, which is handled by the parser
    /// using the stream dictionary's length information.
    /// </remarks>
    public int MaximumTokenLength
    {
        get => maximumTokenLength;

        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "The maximum token length must be at least one byte.");
            }

            maximumTokenLength = value;
        }
    }
}
