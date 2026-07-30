namespace BlackFlag.Engine.Syntax.Lexing;

/// <summary>
/// Identifies the lexical category of a token read from PDF syntax.
/// </summary>
public enum PdfTokenKind
{
    /// <summary>
    /// Indicates that no token kind has been assigned.
    /// </summary>
    None = 0,

    /// <summary>
    /// Marks the logical end of the input source.
    /// </summary>
    EndOfFile,

    /// <summary>
    /// Represents a signed or unsigned integer value.
    /// </summary>
    IntegerNumber,

    /// <summary>
    /// Represents a signed or unsigned real value.
    /// </summary>
    RealNumber,

    /// <summary>
    /// Represents a PDF name beginning with a solidus.
    /// </summary>
    Name,

    /// <summary>
    /// Represents a parenthesis-delimited literal string.
    /// </summary>
    LiteralString,

    /// <summary>
    /// Represents an angle-bracket-delimited hexadecimal string.
    /// </summary>
    HexadecimalString,

    /// <summary>
    /// Represents a general PDF keyword or content-stream operator.
    /// </summary>
    Keyword,

    /// <summary>
    /// Represents an opening array delimiter.
    /// </summary>
    ArrayStart,

    /// <summary>
    /// Represents a closing array delimiter.
    /// </summary>
    ArrayEnd,

    /// <summary>
    /// Represents an opening dictionary delimiter.
    /// </summary>
    DictionaryStart,

    /// <summary>
    /// Represents a closing dictionary delimiter.
    /// </summary>
    DictionaryEnd
}