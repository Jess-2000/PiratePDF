namespace BlackFlag.Engine.Syntax.Lexing;

public sealed partial class PdfLexer
{
    private static bool IsHexadecimalDigit(
        byte value)
    {
        return
            value is >= (byte)'0' and <= (byte)'9' ||
            value is >= (byte)'A' and <= (byte)'F' ||
            value is >= (byte)'a' and <= (byte)'f';
    }

    private static int GetHexadecimalDigitValue(
        byte value)
    {
        if (value is >= (byte)'0' and <= (byte)'9')
        {
            return value - (byte)'0';
        }

        if (value is >= (byte)'A' and <= (byte)'F')
        {
            return value - (byte)'A' + 10;
        }

        if (value is >= (byte)'a' and <= (byte)'f')
        {
            return value - (byte)'a' + 10;
        }

        throw new ArgumentOutOfRangeException(
            nameof(value),
            value,
            "The supplied byte is not a hexadecimal digit.");
    }
}
