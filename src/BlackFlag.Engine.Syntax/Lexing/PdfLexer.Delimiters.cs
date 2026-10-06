namespace BlackFlag.Engine.Syntax.Lexing;

public sealed partial class PdfLexer
{
    private PdfToken ReadSingleByteToken(
        PdfTokenKind kind)
    {
        PdfSourcePosition start =
            Position;

        ConsumeByte();

        return CreateToken(
            kind,
            start);
    }

    private PdfToken ReadDictionaryEnd()
    {
        PdfSourcePosition start =
            Position;

        ConsumeExpectedByte(
            (byte)'>');

        if (
            !cursor.TryPeekByte(out byte value) ||
            value != (byte)'>')
        {
            throw new PdfLexerException(
                "A single closing angle bracket is not a valid token",
                start);
        }

        ConsumeByte();

        return CreateToken(
            PdfTokenKind.DictionaryEnd,
            start);
    }
}