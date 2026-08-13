#nullable enable
using System.IO;
using System.Text;
using BlackFlag.Engine.IO.RandomAccess;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Syntax.Lexing;
using BlackFlag.Engine.Syntax.Parsing;

namespace BlackFlag.Engine.Parsing.Tests;

public sealed class PdfStreamParserTests
{
    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public void ParseIndirectObjectReturnsStreamForSupportedHeaderLineEnding(
        string lineEnding)
    {
        byte[] content =
            CreateStreamObject(
                objectNumber: 1,
                generationNumber: 0,
                data: "ABC"u8.ToArray(),
                streamLineEnding: lineEnding,
                dataLineEnding: "\n");

        PdfIndirectObject indirectObject =
            ParseIndirectObject(content);

        PdfStream stream =
            Assert.IsType<PdfStream>(
                indirectObject.Value);

        Assert.Equal(
            "ABC"u8.ToArray(),
            stream.Data.ToArray());

        Assert.Equal(
            3,
            Assert.IsType<PdfInteger>(
                stream.Dictionary[
                    new PdfName("Length")])
                .Value);
    }

    [Fact]
    public void ParseIndirectObjectReturnsEmptyStream()
    {
        byte[] content =
            CreateStreamObject(
                objectNumber: 2,
                generationNumber: 0,
                data: [],
                streamLineEnding: "\n",
                dataLineEnding: "\n");

        PdfStream stream =
            Assert.IsType<PdfStream>(
                ParseIndirectObject(content).Value);

        Assert.Empty(
            stream.Data.ToArray());
    }

    [Fact]
    public void ParseIndirectObjectPreservesBinaryStreamBytes()
    {
        byte[] expected =
        [
            0x00,
            0x01,
            0x0A,
            0x0D,
            0x7F,
            0x80,
            0xFE,
            0xFF
        ];

        byte[] content =
            CreateStreamObject(
                objectNumber: 3,
                generationNumber: 7,
                data: expected,
                streamLineEnding: "\r\n",
                dataLineEnding: "\r\n");

        PdfIndirectObject indirectObject =
            ParseIndirectObject(content);

        Assert.Equal(3, indirectObject.ObjectNumber);
        Assert.Equal(7, indirectObject.GenerationNumber);

        PdfStream stream =
            Assert.IsType<PdfStream>(
                indirectObject.Value);

        Assert.Equal(
            expected,
            stream.Data.ToArray());
    }

    [Fact]
    public void ParseUsesDeclaredLengthWhenPayloadContainsEndStreamText()
    {
        byte[] expected =
            Encoding.ASCII.GetBytes(
                "prefix endstream suffix");

        byte[] content =
            CreateStreamObject(
                objectNumber: 4,
                generationNumber: 0,
                data: expected,
                streamLineEnding: "\n",
                dataLineEnding: "\n");

        PdfStream stream =
            Assert.IsType<PdfStream>(
                ParseIndirectObject(content).Value);

        Assert.Equal(
            expected,
            stream.Data.ToArray());
    }

    [Fact]
    public void ParseConsumesStreamAndAllowsFollowingIndirectObject()
    {
        byte[] first =
            CreateStreamObject(
                objectNumber: 5,
                generationNumber: 0,
                data: "DATA"u8.ToArray(),
                streamLineEnding: "\n",
                dataLineEnding: "\n",
                trailingBytes: " "u8.ToArray());

        byte[] second =
            Encoding.ASCII.GetBytes(
                "6 0 obj true endobj");

        byte[] content =
            Combine(
                first,
                second);

        using TestRandomAccessSource source =
            CreateSource(content);

        PdfTokenReader reader =
            CreateReader(source);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        PdfIndirectObject firstObject =
            parser.ParseIndirectObject();

        PdfStream stream =
            Assert.IsType<PdfStream>(
                firstObject.Value);

        Assert.Equal(
            "DATA"u8.ToArray(),
            stream.Data.ToArray());

        PdfIndirectObject secondObject =
            parser.ParseIndirectObject();

        Assert.Equal(6, secondObject.ObjectNumber);
        Assert.True(
            Assert.IsType<PdfBoolean>(
                secondObject.Value)
                .Value);

        Assert.True(reader.EndOfFile);
    }

    [Fact]
    public void ParseRejectsMissingLength()
    {
        byte[] content =
            Encoding.ASCII.GetBytes(
                "1 0 obj << >> stream\nABC\nendstream endobj");

        Assert.Throws<FormatException>(
            () => ParseIndirectObject(content));
    }

    [Theory]
    [InlineData("/Length /Three")]
    [InlineData("/Length 3 0 R")]
    [InlineData("/Length true")]
    public void ParseRejectsNonDirectIntegerLength(
        string lengthEntry)
    {
        byte[] content =
            Encoding.ASCII.GetBytes(
                $"1 0 obj << {lengthEntry} >> stream\nABC\nendstream endobj");

        Assert.Throws<FormatException>(
            () => ParseIndirectObject(content));
    }

    [Fact]
    public void ParseRejectsNegativeLength()
    {
        byte[] content =
            Encoding.ASCII.GetBytes(
                "1 0 obj << /Length -1 >> stream\n\nendstream endobj");

        Assert.Throws<FormatException>(
            () => ParseIndirectObject(content));
    }

    [Fact]
    public void ParseRejectsLengthLargerThanInt32()
    {
        byte[] content =
            Encoding.ASCII.GetBytes(
                "1 0 obj << /Length 2147483648 >> stream\n\nendstream endobj");

        Assert.Throws<NotSupportedException>(
            () => ParseIndirectObject(content));
    }

    [Fact]
    public void ParseRejectsMissingLineEndingAfterStreamKeyword()
    {
        byte[] content =
            Encoding.ASCII.GetBytes(
                "1 0 obj << /Length 3 >> streamABC\nendstream endobj");

        Assert.Throws<PdfLexerException>(
            () => ParseIndirectObject(content));
    }

    [Fact]
    public void ParseRejectsTruncatedDeclaredData()
    {
        byte[] content =
            Encoding.ASCII.GetBytes(
                "1 0 obj << /Length 1000 >> stream\nABC");

        Assert.Throws<EndOfStreamException>(
            () => ParseIndirectObject(content));
    }

    [Fact]
    public void ParseRejectsMissingLineEndingBeforeEndStream()
    {
        byte[] content =
            Encoding.ASCII.GetBytes(
                "1 0 obj << /Length 3 >> stream\nABCendstream endobj");

        Assert.Throws<FormatException>(
            () => ParseIndirectObject(content));
    }

    [Fact]
    public void ParseRejectsMissingEndStreamKeyword()
    {
        byte[] content =
            Encoding.ASCII.GetBytes(
                "1 0 obj << /Length 3 >> stream\nABC\nendstreaX endobj");

        Assert.Throws<FormatException>(
            () => ParseIndirectObject(content));
    }

    [Fact]
    public void ParseRejectsEndStreamWithoutDelimiter()
    {
        byte[] content =
            Encoding.ASCII.GetBytes(
                "1 0 obj << /Length 3 >> stream\nABC\nendstreamX endobj");

        Assert.Throws<FormatException>(
            () => ParseIndirectObject(content));
    }

    [Fact]
    public void ParseRejectsMissingEndObjectAfterStream()
    {
        byte[] content =
            Encoding.ASCII.GetBytes(
                "1 0 obj << /Length 3 >> stream\nABC\nendstream");

        Assert.Throws<PdfUnexpectedTokenException>(
            () => ParseIndirectObject(content));
    }

    [Fact]
    public void ParsedStreamOwnsImmutableSnapshot()
    {
        byte[] content =
            CreateStreamObject(
                objectNumber: 7,
                generationNumber: 0,
                data: "COPY"u8.ToArray(),
                streamLineEnding: "\n",
                dataLineEnding: "\n");

        PdfStream stream =
            Assert.IsType<PdfStream>(
                ParseIndirectObject(content).Value);

        int payloadOffset =
            FindSequence(
                content,
                "COPY"u8);

        content[payloadOffset] =
            (byte)'X';

        Assert.Equal(
            "COPY"u8.ToArray(),
            stream.Data.ToArray());
    }

    private static PdfIndirectObject ParseIndirectObject(
        byte[] content)
    {
        using TestRandomAccessSource source =
            CreateSource(content);

        PdfTokenReader reader =
            CreateReader(source);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        return parser.ParseIndirectObject();
    }

    private static TestRandomAccessSource CreateSource(
        byte[] content)
    {
        return new TestRandomAccessSource(
            content);
    }

    private static PdfTokenReader CreateReader(
        TestRandomAccessSource source)
    {
        var cursor =
            new RandomAccessCursor(source);

        var lexer =
            new PdfLexer(cursor);

        return new PdfTokenReader(lexer);
    }

    private static byte[] CreateStreamObject(
        int objectNumber,
        int generationNumber,
        byte[] data,
        string streamLineEnding,
        string dataLineEnding,
        byte[]? trailingBytes = null)
    {
        byte[] prefix =
            Encoding.ASCII.GetBytes(
                $"{objectNumber} {generationNumber} obj "
                + $"<< /Length {data.Length} >> stream"
                + streamLineEnding);

        byte[] suffix =
            Encoding.ASCII.GetBytes(
                dataLineEnding
                + "endstream endobj");

        return Combine(
            prefix,
            data,
            suffix,
            trailingBytes ?? []);
    }

    private static byte[] Combine(
        params byte[][] parts)
    {
        int length =
            parts.Sum(
                static part => part.Length);

        byte[] result =
            GC.AllocateUninitializedArray<byte>(
                length);

        int offset = 0;

        foreach (byte[] part in parts)
        {
            part.CopyTo(
                result,
                offset);

            offset += part.Length;
        }

        return result;
    }

    private static int FindSequence(
        byte[] source,
        ReadOnlySpan<byte> sequence)
    {
        for (
            int index = 0;
            index <= source.Length - sequence.Length;
            index++)
        {
            if (source.AsSpan(
                    index,
                    sequence.Length)
                .SequenceEqual(sequence))
            {
                return index;
            }
        }

        throw new InvalidOperationException(
            "The requested byte sequence was not found.");
    }
}

