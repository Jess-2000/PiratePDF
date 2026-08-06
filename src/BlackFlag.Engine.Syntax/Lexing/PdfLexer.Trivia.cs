namespace BlackFlag.Engine.Syntax.Lexing;

public sealed partial class PdfLexer
{
    private void SkipTrivia()
    {
        while (true)
        {
            SkipWhiteSpace();

            if (
                !cursor.TryPeekByte(out byte value) ||
                value != (byte)'%')
            {
                return;
            }

            SkipComment();
        }
    }

    private void SkipWhiteSpace()
    {
        while (
            cursor.TryPeekByte(out byte value) &&
            PdfCharacterClassifier.IsWhiteSpace(value))
        {
            ConsumeByte();
        }
    }

    private void SkipComment()
    {
        ConsumeExpectedByte(
            (byte)'%');

        while (
            cursor.TryPeekByte(out byte value) &&
            !PdfCharacterClassifier.IsEndOfLine(value))
        {
            ConsumeByte();
        }
    }
}
