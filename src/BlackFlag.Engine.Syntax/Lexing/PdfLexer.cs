using BlackFlag.Engine.IO.RandomAccess;

namespace BlackFlag.Engine.Syntax.Lexing;

/// <summary>
/// Reads lexical tokens sequentially from PDF source data.
/// </summary>
/// <remarks>
/// The lexer owns its logical position but does not own or dispose the
/// supplied cursor. The cursor must not be repositioned externally while the
/// lexer is in use.
/// </remarks>
public sealed partial class PdfLexer : IPdfLexer
{
    private readonly IRandomAccessCursor cursor;

    private bool previousByteWasCarriageReturn;

    /// <summary>
    /// Initializes a new PDF lexer.
    /// </summary>
    /// <param name="cursor">
    /// The cursor from which PDF source bytes will be read.
    /// </param>
    /// <param name="options">
    /// Optional lexer configuration.
    /// </param>
    public PdfLexer(
        IRandomAccessCursor cursor,
        PdfLexerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(
            cursor);

        this.cursor =
            cursor;

        Options =
            options ??
            new PdfLexerOptions();

        Position =
            new PdfSourcePosition(
                cursor.Position,
                line: 1,
                column: 1);
    }

    /// <summary>
    /// Gets the options used by this lexer.
    /// </summary>
    public PdfLexerOptions Options { get; }

    /// <inheritdoc />
    public PdfSourcePosition Position { get; private set; }

    /// <inheritdoc />
    public PdfToken ReadToken()
    {
        SkipTrivia();

        PdfSourcePosition start =
            Position;

        if (!TryPeek(out byte value))
        {
            return PdfToken.EndOfFileAt(
                start);
        }

        return value switch
        {
            (byte)'/' =>
                ReadName(),

            (byte)'(' =>
                ReadLiteralString(),

            (byte)'[' =>
                ReadSingleByteToken(
                    PdfTokenKind.ArrayStart),

            (byte)']' =>
                ReadSingleByteToken(
                    PdfTokenKind.ArrayEnd),

            (byte)'<' =>
                ReadDictionaryOrHexadecimalString(),

            (byte)'>' =>
                ReadDictionaryEnd(),

            (byte)'+' or
            (byte)'-' or
            (byte)'.' =>
                ReadNumber(),

            _ when
                PdfCharacterClassifier.IsDecimalDigit(
                    value) =>
                    ReadNumber(),

            _ when
                IsKeywordStartByte(
                    value) =>
                    ReadKeyword(),

            _ =>
                throw CreateUnsupportedTokenException(
                    value,
                    start)
        };
    }
}