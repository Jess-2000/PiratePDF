using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlackFlag.Engine.IO.RandomAccess;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Interpretation;
using BlackFlag.Engine.Rendering.Resources;
using BlackFlag.Engine.Syntax.Lexing;
using BlackFlag.Engine.Syntax.Parsing;
using G = BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Services.Tests.Text;

internal static class PdfDecorationSyntaxAssertions
{
    // Use the repository lexer so strings/comments cannot masquerade as operators.
    // This assertion adapter reads only numeric graphics operations in our tiny
    // fixtures. The acceptance harness also uses the full content/PDF parsers.
    internal static PdfDisplayPath[] ReadPaths(string content)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(content);
        using var source = new FixtureSource(bytes);
        var reader = new PdfTokenReader(new PdfLexer(new RandomAccessCursor(source),
            new PdfLexerOptions { AllowUnknownKeywords = true }));
        var numbers = new List<G.PdfObject>();
        var graphics = new List<PdfGraphicsOperation>();
        bool inText = false;
        var nesting = new Stack<string>();
        while (!reader.EndOfFile)
        {
            PdfToken token = reader.Read();
            string value = Encoding.ASCII.GetString(bytes, (int)token.Span.Start.Offset,
                (int)(token.Span.End.Offset - token.Span.Start.Offset));
            if (token.Kind != PdfTokenKind.Keyword)
            {
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                    numbers.Add(token.Kind == PdfTokenKind.IntegerNumber
                        ? new G.PdfInteger(long.Parse(value, CultureInfo.InvariantCulture)) : new G.PdfReal(number));
                continue;
            }
            if (value == "BT") { Assert.False(inText); inText = true; nesting.Push("BT"); }
            if (value == "ET") { Assert.True(inText); inText = false; Assert.Equal("BT", nesting.Pop()); }
            if (value is "BMC" or "BDC") nesting.Push("marked");
            if (value == "EMC") Assert.Equal("marked", nesting.Pop());
            if (value is "m" or "l" or "S" or "re" or "n" or "W" or "W*")
                Assert.False(inText, $"Path operator {value} occurred inside BT/ET.");
            if (value is "q" or "Q" or "cm" or "w" or "J" or "j" or
                "g" or "G" or "rg" or "RG" or "k" or "K" or
                "m" or "l" or "S" or "re" or "n" or "W" or "W*")
            {
                Assert.True(PdfGraphicsOperator.TryGet(value, out var op));
                Assert.Equal(op.MinimumOperandCount, numbers.Count);
                graphics.Add(new PdfGraphicsOperation(op, numbers,
                    new PdfContentSourceSpan(0, token.Span.Start.Offset, token.Span.End.Offset)));
            }
            numbers.Clear();
        }
        Assert.False(inText);
        Assert.Empty(nesting);
        return new PdfGraphicsInterpreter().Interpret(graphics, new PdfResourceScope((_, _) => null, null))
            .OfType<PdfDisplayPath>().ToArray();
    }

    internal static void Point(PdfPoint expected, PdfPoint actual)
    {
        Assert.Equal(expected.X, actual.X, 6);
        Assert.Equal(expected.Y, actual.Y, 6);
    }

    private sealed class FixtureSource(byte[] bytes) : BlackFlag.Engine.Abstractions.IO.IRandomAccessSource
    {
        public long Length => bytes.Length;
        public int Read(long offset, Span<byte> destination)
        {
            int count = Math.Min(bytes.Length - checked((int)offset), destination.Length);
            bytes.AsSpan((int)offset, count).CopyTo(destination);
            return count;
        }
        public ValueTask<int> ReadAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(Read(offset, destination.Span));
        }
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
