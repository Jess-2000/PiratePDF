using System.Text;
using BlackFlag.Engine.Syntax.Lexing;

namespace BlackFlag.Engine.Syntax.Tests.Lexing;

internal static class LexerTestFactory
{
    public static PdfLexer CreateLexer(
        string source)
    {
        return new PdfLexer(
            CreateCursor(
                source));
    }

    public static TestRandomAccessCursor CreateCursor(
        string source)
    {
        ArgumentNullException.ThrowIfNull(
            source);

        return new TestRandomAccessCursor(
            Encoding.ASCII.GetBytes(
                source));
    }
}