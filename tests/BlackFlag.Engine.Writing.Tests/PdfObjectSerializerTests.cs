using System.IO;
using System.Text;

using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfObjectSerializerTests
{
    [Fact]
    public void SerializeWritesPrimitiveObjects()
    {
        Assert.Equal("null", SerializeText(PdfNull.Instance));
        Assert.Equal("true", SerializeText(new PdfBoolean(true)));
        Assert.Equal("-42", SerializeText(new PdfInteger(-42)));
        Assert.Equal("1.25", SerializeText(new PdfReal(1.25)));
        Assert.Equal("1.0", SerializeText(new PdfReal(1)));
        Assert.Equal("12 3 R", SerializeText(new PdfIndirectReference(12, 3)));
    }

    [Theory]
    [InlineData(1e20, "100000000000000000000.0")]
    [InlineData(1e-7, "0.0000001")]
    [InlineData(-2.5e3, "-2500.0")]
    public void SerializeExpandsRealExponentNotation(
        double value,
        string expected)
    {
        Assert.Equal(expected, SerializeText(new PdfReal(value)));
    }

    [Fact]
    public void SerializeEscapesNames()
    {
        var name = new PdfName(
            [(byte)'A', (byte)' ', (byte)'#', (byte)'/', 0xFF]);

        Assert.Equal("/A#20#23#2F#FF", SerializeText(name));
    }

    [Fact]
    public void SerializeEscapesLiteralStringsWithoutChangingSourceBytes()
    {
        var value = new PdfString(
            [(byte)'(', (byte)'A', (byte)'\\', (byte)'\n', 0x01, (byte)')']);

        Assert.Equal("(\\(A\\\\\\n\\001\\))", SerializeText(value));
    }

    [Fact]
    public void SerializeWritesHexadecimalStringsWithUppercaseHex()
    {
        var value = new PdfString(
            [0x00, 0xAF, 0x10],
            PdfStringFormat.Hexadecimal);

        Assert.Equal("<00AF10>", SerializeText(value));
    }

    [Fact]
    public void SerializeWritesArraysAndOrderedDictionariesDeterministically()
    {
        var dictionary = new PdfDictionary
        {
            { PdfName.FromLatin1("Zulu"), new PdfInteger(2) },
            { PdfName.FromLatin1("Alpha"), new PdfInteger(1) }
        };
        var array = new PdfArray([dictionary, new PdfBoolean(false)]);

        Assert.Equal(
            "[<< /Alpha 1 /Zulu 2 >> false]",
            SerializeText(array));
    }

    [Fact]
    public void SerializeStreamUsesActualEncodedLengthWithoutMutatingDictionary()
    {
        var dictionary = new PdfDictionary
        {
            { PdfNames.Length, new PdfInteger(999) },
            { PdfName.FromLatin1("Type"), PdfName.FromLatin1("Example") }
        };
        var stream = new PdfStream(dictionary, [1, 2, 3]);

        string serialized = SerializeText(stream);

        Assert.Equal(
            "<< /Length 3 /Type /Example >>\nstream\n\u0001\u0002\u0003\nendstream",
            serialized);
        Assert.Equal(999, stream.Dictionary.Get<PdfInteger>(PdfNames.Length).Value);
    }

    [Fact]
    public void SerializeRejectsDirectCompositeCycles()
    {
        var array = new PdfArray();
        array.Add(array);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => PdfObjectSerializer.Serialize(array));

        Assert.Contains("cycle", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WriteRejectsNonWritableDestination()
    {
        using var destination = new MemoryStream([1, 2, 3], writable: false);

        Assert.Throws<ArgumentException>(
            () => PdfObjectSerializer.Write(destination, PdfNull.Instance));
    }

    private static string SerializeText(PdfObject value)
    {
        return Encoding.Latin1.GetString(PdfObjectSerializer.Serialize(value));
    }
}
