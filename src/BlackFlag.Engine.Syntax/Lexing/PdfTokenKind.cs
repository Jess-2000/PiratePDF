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
    /// Represents a non-reserved PDF keyword or content-stream operator.
    /// </summary>
    Keyword,

    /// <summary>
    /// Represents the <c>true</c> keyword.
    /// </summary>
    BooleanTrue,

    /// <summary>
    /// Represents the <c>false</c> keyword.
    /// </summary>
    BooleanFalse,

    /// <summary>
    /// Represents the <c>null</c> keyword.
    /// </summary>
    Null,

    /// <summary>
    /// Represents the <c>obj</c> keyword.
    /// </summary>
    ObjectStart,

    /// <summary>
    /// Represents the <c>endobj</c> keyword.
    /// </summary>
    ObjectEnd,

    /// <summary>
    /// Represents the <c>R</c> indirect-reference marker.
    /// </summary>
    IndirectReferenceMarker,

    /// <summary>
    /// Represents the <c>stream</c> keyword.
    /// </summary>
    StreamStart,

    /// <summary>
    /// Represents the <c>endstream</c> keyword.
    /// </summary>
    StreamEnd,

    /// <summary>
    /// Represents the <c>xref</c> keyword.
    /// </summary>
    CrossReference,

    /// <summary>
    /// Represents the <c>trailer</c> keyword.
    /// </summary>
    Trailer,

    /// <summary>
    /// Represents the <c>startxref</c> keyword.
    /// </summary>
    StartCrossReference,

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

