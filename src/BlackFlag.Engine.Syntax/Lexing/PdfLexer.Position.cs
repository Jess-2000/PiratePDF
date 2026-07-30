namespace BlackFlag.Engine.Syntax.Lexing;

public sealed partial class PdfLexer
{
    private bool IsEndOfSource =>
        cursor.EndOfSource;

    private bool TryPeek(
        out byte value)
    {
        return cursor.TryPeekByte(
            out value);
    }

    private byte Peek()
    {
        return cursor.PeekByte();
    }

    private void ConsumeExpectedByte(
        byte expected)
    {
        if (
            IsEndOfSource ||
            Peek() != expected)
        {
            throw new InvalidOperationException(
                "The cursor is not positioned at the expected byte.");
        }

        ConsumeByte();
    }

    private byte ConsumeByte()
    {
        if (!cursor.TryReadByte(out byte value))
        {
            throw new PdfLexerException(
                "Unexpected end of source",
                Position);
        }

        AdvancePosition(
            value);

        return value;
    }

    private void AdvancePosition(
        byte value)
    {
        long nextOffset =
            Position.Offset + 1;

        if (value == (byte)'\r')
        {
            Position =
                new PdfSourcePosition(
                    nextOffset,
                    Position.Line + 1,
                    column: 1);

            previousByteWasCarriageReturn =
                true;

            return;
        }

        if (value == (byte)'\n')
        {
            if (previousByteWasCarriageReturn)
            {
                Position =
                    new PdfSourcePosition(
                        nextOffset,
                        Position.Line,
                        column: 1);
            }
            else
            {
                Position =
                    new PdfSourcePosition(
                        nextOffset,
                        Position.Line + 1,
                        column: 1);
            }

            previousByteWasCarriageReturn =
                false;

            return;
        }

        Position =
            new PdfSourcePosition(
                nextOffset,
                Position.Line,
                Position.Column + 1);

        previousByteWasCarriageReturn =
            false;
    }
}