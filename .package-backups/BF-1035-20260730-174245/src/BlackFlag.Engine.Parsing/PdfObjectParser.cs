using System.Collections.ObjectModel;
using BlackFlag.Engine.Abstractions.IO;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Syntax.Lexing;
using BlackFlag.Engine.Syntax.Parsing;

namespace BlackFlag.Engine.Parsing;

/// <summary>
/// Parses immutable PDF objects from a token stream and its underlying source.
/// </summary>
/// <remarks>
/// BF-1035 supports primitive PDF objects only. Collection, indirect-object,
/// and stream parsing are added by later packages.
/// </remarks>
public sealed partial class PdfObjectParser
{
    private static readonly ReadOnlyCollection<PdfTokenKind>
        PrimitiveTokenKinds =
            Array.AsReadOnly<PdfTokenKind>(
                [
                    PdfTokenKind.Null,
                    PdfTokenKind.BooleanTrue,
                    PdfTokenKind.BooleanFalse,
                    PdfTokenKind.IntegerNumber,
                    PdfTokenKind.RealNumber,
                    PdfTokenKind.Name,
                    PdfTokenKind.LiteralString,
                    PdfTokenKind.HexadecimalString
                ]);

    private readonly PdfTokenReader reader;
    private readonly IRandomAccessSource source;

    /// <summary>
    /// Initializes a primitive PDF object parser.
    /// </summary>
    /// <param name="reader">
    /// The token reader that supplies lexical tokens.
    /// </param>
    /// <param name="source">
    /// The source containing the bytes covered by each token span.
    /// </param>
    public PdfObjectParser(
        PdfTokenReader reader,
        IRandomAccessSource source)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(source);

        this.reader = reader;
        this.source = source;
    }

    /// <summary>
    /// Parses and consumes the next primitive PDF object.
    /// </summary>
    /// <returns>
    /// The parsed immutable PDF object.
    /// </returns>
    /// <exception cref="PdfUnexpectedTokenException">
    /// The next token is not a supported primitive-object token.
    /// </exception>
    /// <exception cref="EndOfStreamException">
    /// The source cannot provide all bytes covered by the token span.
    /// </exception>
    /// <exception cref="FormatException">
    /// A token's source bytes cannot be converted to the value represented by
    /// its lexical kind.
    /// </exception>
    public PdfObject Parse()
    {
        PdfToken token =
            reader.Peek();

        PdfObject value =
            token.Kind switch
            {
                PdfTokenKind.Null =>
                    ParseNull(),

                PdfTokenKind.BooleanTrue =>
                    ParseBoolean(value: true),

                PdfTokenKind.BooleanFalse =>
                    ParseBoolean(value: false),

                PdfTokenKind.IntegerNumber =>
                    ParseInteger(token),

                PdfTokenKind.RealNumber =>
                    ParseReal(token),

                PdfTokenKind.Name =>
                    ParseName(token),

                PdfTokenKind.LiteralString =>
                    ParseLiteralString(token),

                PdfTokenKind.HexadecimalString =>
                    ParseHexadecimalString(token),

                _ =>
                    throw new PdfUnexpectedTokenException(
                        PrimitiveTokenKinds,
                        token)
            };

        reader.Read();

        return value;
    }

    private static PdfNull ParseNull()
    {
        return PdfNull.Instance;
    }

    private static PdfBoolean ParseBoolean(
        bool value)
    {
        return new PdfBoolean(value);
    }
}
