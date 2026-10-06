using System.IO;
using System.Text;

using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfIndirectObjectSerializerTests
{
    [Fact]
    public void SerializeWritesHeaderBodyAndTerminatorDeterministically()
    {
        byte[] bytes = PdfIndirectObjectSerializer.Serialize(
            new PdfObjectIdentifier(15, 2),
            new PdfString(
                [(byte)'O', (byte)'K'],
                PdfStringFormat.Hexadecimal));

        Assert.Equal(
            "15 2 obj\n<4F4B>\nendobj\n",
            Encoding.ASCII.GetString(bytes));
    }

    [Fact]
    public void WriteAppendsAtCurrentDestinationPosition()
    {
        using var destination = new MemoryStream();
        destination.Write(Encoding.ASCII.GetBytes("prefix\n"));

        PdfIndirectObjectSerializer.Write(
            destination,
            new PdfObjectIdentifier(3),
            new PdfInteger(7));

        Assert.Equal(
            "prefix\n3 0 obj\n7\nendobj\n",
            Encoding.ASCII.GetString(destination.ToArray()));
    }
}
