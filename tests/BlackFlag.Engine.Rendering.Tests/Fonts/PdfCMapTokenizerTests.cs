using System.IO;
using System.Text;
using BlackFlag.Engine.Rendering.Fonts;

namespace BlackFlag.Engine.Rendering.Tests.Fonts;

public sealed class PdfCMapTokenizerTests
{
    [Theory]
    [InlineData(">")]
    [InlineData(">>")]
    [InlineData("<< /Registry (Adobe) >>")]
    public void TokenProductionCannotExceedSourceLength(string source)
    {
        var tokens = PdfCMapParser.Tokenize(source, Math.Max(1, source.Length));
        // Bounded even against the original defect: at most source.Length + 1 yields.
        using var iterator = tokens.GetEnumerator();
        for (int step = 0; step <= source.Length; step++)
        {
            bool moved;
            try
            {
                moved = iterator.MoveNext();
            }
            catch (InvalidDataException)
            {
                return;
            }

            if (!moved)
            {
                return;
            }

            Assert.NotEmpty(iterator.Current.Value);
        }

        Assert.Fail("Tokenizer produced more tokens than source characters.");
    }

    [Theory]
    [InlineData(">")]
    [InlineData(">>")]
    [InlineData("<0041")]
    [InlineData("<00<41>")]
    [InlineData("<< /A <00> >")]
    [InlineData("<< [ >> ]")]
    [InlineData("<<")]
    [InlineData("[")]
    [InlineData("]")]
    [InlineData(")")]
    [InlineData("(unterminated")]
    public void MalformedDelimitersRejectWithinFiniteEnumeration(string source)
    {
        Assert.Throws<InvalidDataException>(() =>
            PdfCMapParser.Tokenize(source, 32).Take(source.Length + 1).ToArray());
    }

    [Theory]
    [InlineData("<0041>", "<0041>")]
    [InlineData("<< /Registry (Adobe) >> /Name def", "<<|/Registry|(Adobe)|>>|/Name|def")]
    [InlineData("<</A<0041>>><0042>", "<<|/A|<0041>|>>|<0042>")]
    [InlineData("<<% comment >\r\n/A[<41><0041>]>>%end", "<<|/A|[|<41>|<0041>|]|>>")]
    [InlineData(" \t\r\n\f\0<<\t>>\n/WMode\0\r1", "<<|>>|/WMode|1")]
    [InlineData("<< /A (text > [ << % (nested) \\) ) >>", "<<|/A|(text > [ << % (nested) \\) )|>>")]
    [InlineData("/A/B 1%comment\noperator", "/A|/B|1|operator")]
    public void SupportedTokensConsumeTheirFullLexicalForm(string source, string expected)
    {
        var tokens = PdfCMapParser.Tokenize(source, 32).Take(source.Length + 1).ToArray();
        Assert.Equal(expected.Split('|'), tokens.Select(token => token.Value));
        int previous = 0;
        foreach (var (Value, End) in tokens)
        {
            Assert.InRange(End, previous + 1, source.Length);
            previous = End;
        }
    }

    [Fact]
    public void DelimiterCorpusAlwaysAdvancesTerminatesOrRejects()
    {
        const string alphabet = "<>[]()%/ A\n";
        foreach (char first in alphabet)
            foreach (char second in alphabet)
                foreach (char third in alphabet)
                {
                    string source = new([first, second, third]);
                    using var tokens = PdfCMapParser.Tokenize(source, 4).GetEnumerator();
                    int previous = 0;
                    for (int step = 0; step <= source.Length; step++)
                    {
                        bool moved;
                        try
                        {
                            moved = tokens.MoveNext();
                        }
                        catch (InvalidDataException)
                        {
                            break;
                        }
                        if (!moved)
                        {
                            break;
                        }
                        Assert.InRange(tokens.Current.End, previous + 1, source.Length);
                        Assert.NotEmpty(tokens.Current.Value);
                        previous = tokens.Current.End;
                    }
                }
    }

    [Fact]
    public void RepeatedClosingDelimitersRejectBeforeProducingAToken()
    {
        using var tokens = PdfCMapParser.Tokenize(new string('>', 4096), 8).GetEnumerator();
        Assert.Throws<InvalidDataException>(() => tokens.MoveNext());
    }

    [Fact]
    public void ExactTokenLimitSucceedsAndNextTokenRejectsBeforeYield()
    {
        using var tokens = PdfCMapParser.Tokenize("a b c d", 3).GetEnumerator();
        for (int index = 0; index < 3; index++)
        {
            Assert.True(tokens.MoveNext());
        }
        Assert.Throws<InvalidDataException>(() => tokens.MoveNext());
        Assert.Equal(3, PdfCMapParser.Tokenize("a b c % trailing comment", 3).Count());
    }

    [Fact]
    public void ParseEnforcesExactInputAndTokenBudgets()
    {
        byte[] source = Encoding.ASCII.GetBytes("/WMode 1");
        Assert.Equal(1, PdfCMapParser.Parse(source, source.Length, 2).WritingMode);
        Assert.Throws<InvalidDataException>(() => PdfCMapParser.Parse(source, source.Length - 1, 2));
        Assert.Throws<InvalidDataException>(() => PdfCMapParser.Parse(source, source.Length, 1));
    }
}
