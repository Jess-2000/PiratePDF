using BlackFlag.Engine.Syntax.Lexing;
using BlackFlag.Engine.Syntax.Parsing;
using BlackFlag.Engine.Syntax.Tests.Lexing;

namespace BlackFlag.Engine.Syntax.Tests.Parsing;

public sealed class PdfTokenReaderTests
{
    [Fact]
    public void ConstructorRejectsNullLexer()
    {
        Assert.Throws<ArgumentNullException>(
            () => new PdfTokenReader(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConstructorRejectsInvalidInitialCapacity(
        int initialCapacity)
    {
        PdfLexer lexer =
            LexerTestFactory.CreateLexer("null");

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfTokenReader(
                lexer,
                initialCapacity));
    }

    [Fact]
    public void CurrentThrowsBeforeFirstRead()
    {
        var reader = CreateReader("null");

        Assert.False(reader.HasCurrent);

        Assert.Throws<InvalidOperationException>(
            () => _ = reader.Current);
    }

    [Fact]
    public void PeekReturnsNextTokenWithoutConsumingIt()
    {
        var reader = CreateReader("null true");

        PdfToken first = reader.Peek();
        PdfToken second = reader.Peek();

        Assert.Equal(PdfTokenKind.Null, first.Kind);
        Assert.Equal(first, second);
        Assert.False(reader.HasCurrent);
        Assert.Equal(PdfSourcePosition.Start, reader.Position);
    }

    [Fact]
    public void PeekWithLookaheadReturnsRequestedToken()
    {
        var reader = CreateReader("null true false");

        Assert.Equal(PdfTokenKind.Null, reader.Peek(0).Kind);
        Assert.Equal(PdfTokenKind.BooleanTrue, reader.Peek(1).Kind);
        Assert.Equal(PdfTokenKind.BooleanFalse, reader.Peek(2).Kind);
        Assert.Equal(PdfTokenKind.Null, reader.Read().Kind);
    }

    [Fact]
    public void PeekRejectsNegativeLookahead()
    {
        var reader = CreateReader("null");

        Assert.Throws<ArgumentOutOfRangeException>(
            () => reader.Peek(-1));
    }

    [Fact]
    public void ReadConsumesTokensSequentially()
    {
        var reader = CreateReader("null true false");

        Assert.Equal(PdfTokenKind.Null, reader.Read().Kind);
        Assert.Equal(PdfTokenKind.BooleanTrue, reader.Read().Kind);
        Assert.Equal(PdfTokenKind.BooleanFalse, reader.Read().Kind);
        Assert.Equal(PdfTokenKind.EndOfFile, reader.Read().Kind);
    }

    [Fact]
    public void ReadUpdatesCurrentAndHasCurrent()
    {
        var reader = CreateReader("null true");

        PdfToken token = reader.Read();

        Assert.True(reader.HasCurrent);
        Assert.Equal(token, reader.Current);
    }

    [Fact]
    public void PositionTracksNextUnreadToken()
    {
        var reader = CreateReader("null true");

        Assert.Equal(0, reader.Position.Offset);

        reader.Read();

        Assert.Equal(5, reader.Position.Offset);
    }

    [Fact]
    public void EndOfFileIsFalseBeforeFinalToken()
    {
        var reader = CreateReader("null");

        Assert.False(reader.EndOfFile);
    }

    [Fact]
    public void EndOfFileIsTrueAtLogicalEnd()
    {
        var reader = CreateReader("null");

        reader.Read();

        Assert.True(reader.EndOfFile);
        Assert.Equal(4, reader.Position.Offset);
    }

    [Fact]
    public void EmptySourceBeginsAtEndOfFile()
    {
        var reader = CreateReader(string.Empty);

        Assert.True(reader.EndOfFile);
        Assert.Equal(PdfTokenKind.EndOfFile, reader.Peek().Kind);
        Assert.Equal(PdfSourcePosition.Start, reader.Position);
    }

    [Fact]
    public void EndOfFileTokenRemainsStableAcrossLookaheadAndReads()
    {
        var reader = CreateReader("null");

        reader.Read();

        PdfToken peeked = reader.Peek();
        PdfToken distant = reader.Peek(1000);
        PdfToken firstRead = reader.Read();
        PdfToken secondRead = reader.Read();

        Assert.Equal(peeked, distant);
        Assert.Equal(peeked, firstRead);
        Assert.Equal(firstRead, secondRead);
        Assert.Equal(firstRead, reader.Current);
    }

    [Fact]
    public void MatchConsumesMatchingToken()
    {
        var reader = CreateReader("null true");

        bool matched = reader.Match(PdfTokenKind.Null);

        Assert.True(matched);
        Assert.Equal(PdfTokenKind.Null, reader.Current.Kind);
        Assert.Equal(PdfTokenKind.BooleanTrue, reader.Peek().Kind);
    }

    [Fact]
    public void MatchDoesNotConsumeNonmatchingToken()
    {
        var reader = CreateReader("null true");

        bool matched = reader.Match(PdfTokenKind.BooleanTrue);

        Assert.False(matched);
        Assert.False(reader.HasCurrent);
        Assert.Equal(PdfTokenKind.Null, reader.Peek().Kind);
    }

    [Fact]
    public void MatchCanConsumeEndOfFile()
    {
        var reader = CreateReader(string.Empty);

        bool matched = reader.Match(PdfTokenKind.EndOfFile);

        Assert.True(matched);
        Assert.Equal(PdfTokenKind.EndOfFile, reader.Current.Kind);
        Assert.True(reader.EndOfFile);
    }

    [Fact]
    public void MatchRejectsUnassignedKind()
    {
        var reader = CreateReader("null");

        Assert.Throws<ArgumentOutOfRangeException>(
            () => reader.Match(PdfTokenKind.None));
    }

    [Fact]
    public void ExpectConsumesAndReturnsMatchingToken()
    {
        var reader = CreateReader("null true");

        PdfToken token = reader.Expect(PdfTokenKind.Null);

        Assert.Equal(PdfTokenKind.Null, token.Kind);
        Assert.Equal(token, reader.Current);
        Assert.Equal(PdfTokenKind.BooleanTrue, reader.Peek().Kind);
    }

    [Fact]
    public void ExpectThrowsWithoutConsumingUnexpectedToken()
    {
        var reader = CreateReader("null true");

        PdfUnexpectedTokenException exception =
            Assert.Throws<PdfUnexpectedTokenException>(
                () => reader.Expect(PdfTokenKind.BooleanTrue));

        Assert.Equal(PdfTokenKind.BooleanTrue, exception.ExpectedKind);
        Assert.Equal(PdfTokenKind.Null, exception.ActualToken.Kind);
        Assert.Equal(PdfSourcePosition.Start, exception.Position);
        Assert.False(reader.HasCurrent);
        Assert.Equal(PdfTokenKind.Null, reader.Peek().Kind);
    }

    [Fact]
    public void ExpectRejectsUnassignedKind()
    {
        var reader = CreateReader("null");

        Assert.Throws<ArgumentOutOfRangeException>(
            () => reader.Expect(PdfTokenKind.None));
    }

    [Fact]
    public void LargeLookaheadGrowsRingBufferWithoutReorderingTokens()
    {
        const int tokenCount = 64;
        string source = string.Join(' ', Enumerable.Repeat("null", tokenCount));
        var reader = CreateReader(source, initialCapacity: 1);

        Assert.Equal(
            PdfTokenKind.Null,
            reader.Peek(tokenCount - 1).Kind);

        Assert.Equal(
            PdfTokenKind.EndOfFile,
            reader.Peek(tokenCount).Kind);

        for (int index = 0; index < tokenCount; index++)
        {
            PdfToken token = reader.Read();

            Assert.Equal(PdfTokenKind.Null, token.Kind);
            Assert.Equal(index * 5, token.Span.Start.Offset);
        }

        Assert.True(reader.EndOfFile);
    }

    [Fact]
    public void WrappedRingBufferPreservesOrderWhenItGrows()
    {
        var reader = CreateReader(
            "null true false null true false null true",
            initialCapacity: 4);

        Assert.Equal(PdfTokenKind.Null, reader.Peek(3).Kind);
        Assert.Equal(PdfTokenKind.Null, reader.Read().Kind);
        Assert.Equal(PdfTokenKind.BooleanTrue, reader.Read().Kind);

        Assert.Equal(PdfTokenKind.BooleanTrue, reader.Peek(5).Kind);

        PdfTokenKind[] expectedKinds =
        [
            PdfTokenKind.BooleanFalse,
            PdfTokenKind.Null,
            PdfTokenKind.BooleanTrue,
            PdfTokenKind.BooleanFalse,
            PdfTokenKind.Null,
            PdfTokenKind.BooleanTrue
        ];

        foreach (PdfTokenKind expectedKind in expectedKinds)
        {
            Assert.Equal(expectedKind, reader.Read().Kind);
        }
    }

    private static PdfTokenReader CreateReader(
        string source,
        int initialCapacity = 8)
    {
        return new PdfTokenReader(
            LexerTestFactory.CreateLexer(source),
            initialCapacity);
    }
}
