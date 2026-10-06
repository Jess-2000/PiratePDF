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
}