using System.Text;

using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfCrossReferenceStreamWriterTests
{
    [Fact]
    public void WriteProducesCrossReferenceStreamAndNoTraditionalTrailer()
    {
        byte[] bytes = WriteCrossReferenceStream(CreateDocument());
        string text = Encoding.Latin1.GetString(bytes);

        Assert.Contains("4 0 obj\n", text, StringComparison.Ordinal);
        Assert.Contains("/Type /XRef", text, StringComparison.Ordinal);
        Assert.Contains("/Size 5", text, StringComparison.Ordinal);
        Assert.Contains("/W [1 8 2]", text, StringComparison.Ordinal);
        Assert.Contains("/Root 1 0 R", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\nxref\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain("trailer\n", text, StringComparison.Ordinal);
        Assert.EndsWith("%%EOF\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteStartXrefPointsAtCrossReferenceStreamObject()
    {
        byte[] bytes = WriteCrossReferenceStream(CreateDocument());
        string text = Encoding.Latin1.GetString(bytes);
        long offset = FindByteOffset(bytes, "4 0 obj\n");

        Assert.Contains(
            $"startxref\n{offset}\n%%EOF\n",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void WriteCrossReferenceStreamContainsEntriesForObjectsGapAndItself()
    {
        byte[] bytes = WriteCrossReferenceStream(CreateDocument());
        long objectOneOffset = FindByteOffset(bytes, "1 0 obj\n");
        long objectThreeOffset = FindByteOffset(bytes, "3 0 obj\n");
        long xrefOffset = FindByteOffset(bytes, "4 0 obj\n");
        byte[] streamData = GetStreamData(bytes, xrefOffset);

        Assert.Equal(55, streamData.Length);
        AssertEntry(streamData, 0, 0, 0, 65535);
        AssertEntry(streamData, 1, 1, objectOneOffset, 0);
        AssertEntry(streamData, 2, 0, 0, 0);
        AssertEntry(streamData, 3, 1, objectThreeOffset, 0);
        AssertEntry(streamData, 4, 1, xrefOffset, 0);
    }

    [Fact]
    public void WritePreservesInformationReferenceAndOmitsPrev()
    {
        PdfDocument document = CreateDocument();
        document.Trailer.SetInformationObject(new PdfObjectIdentifier(3));
        document.Trailer.SetPreviousCrossReferenceOffset(42);

        string text = Encoding.Latin1.GetString(
            WriteCrossReferenceStream(document));

        Assert.Contains("/Info 3 0 R", text, StringComparison.Ordinal);
        Assert.DoesNotContain("/Prev", text, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteRejectsCrossReferenceStreamBeforePdf15()
    {
        PdfDocument document = CreateDocument(PdfVersion.Pdf14);
        PdfWritePlan plan = PdfWritePlanner.Create(
            document,
            CreateOptions());

        Assert.Throws<NotSupportedException>(
            () => PdfFullRewriteWriter.Write(plan));
    }

    [Fact]
    public void WriteRejectsMultipleActiveGenerationsForOneObjectNumber()
    {
        PdfDocument document = PdfDocument.CreateEmpty(PdfVersion.Pdf17);
        document.Objects.Add(new PdfObjectIdentifier(1, 0), new PdfDictionary());
        document.Objects.Add(new PdfObjectIdentifier(1, 1), new PdfDictionary());
        document.Trailer.SetRootObject(new PdfObjectIdentifier(1, 1));
        PdfWritePlan plan = PdfWritePlanner.Create(document, CreateOptions());

        Assert.Throws<InvalidOperationException>(
            () => PdfFullRewriteWriter.Write(plan));
    }

    private static byte[] WriteCrossReferenceStream(PdfDocument document)
    {
        PdfWritePlan plan = PdfWritePlanner.Create(document, CreateOptions());
        return PdfFullRewriteWriter.Write(plan);
    }

    private static PdfWriteOptions CreateOptions()
    {
        return new PdfWriteOptions
        {
            CrossReferenceMode = PdfCrossReferenceWriteMode.CrossReferenceStream
        };
    }

    private static PdfDocument CreateDocument(PdfVersion? version = null)
    {
        PdfDocument document = PdfDocument.CreateEmpty(version ?? PdfVersion.Pdf17);
        document.Objects.Add(
            new PdfObjectIdentifier(1),
            new PdfDictionary(
            [
                new KeyValuePair<PdfName, PdfObject>(
                    PdfNames.Type,
                    PdfNames.Catalog)
            ]));
        document.Objects.Add(
            new PdfObjectIdentifier(3),
            new PdfString(Encoding.ASCII.GetBytes("metadata")));
        document.Trailer.SetRootObject(new PdfObjectIdentifier(1));
        return document;
    }

    private static byte[] GetStreamData(byte[] bytes, long objectOffset)
    {
        byte[] streamMarker = Encoding.ASCII.GetBytes("stream\n");
        byte[] endMarker = Encoding.ASCII.GetBytes("\nendstream");
        int start = FindSequence(bytes, streamMarker, checked((int)objectOffset)) + streamMarker.Length;
        int end = FindSequence(bytes, endMarker, start);
        return bytes[start..end];
    }

    private static void AssertEntry(
        byte[] data,
        int objectNumber,
        byte expectedType,
        long expectedFieldTwo,
        int expectedFieldThree)
    {
        const int entryWidth = 11;
        int offset = objectNumber * entryWidth;

        Assert.Equal(expectedType, data[offset]);
        Assert.Equal(
            checked((ulong)expectedFieldTwo),
            ReadUnsignedBigEndian(data.AsSpan(offset + 1, 8)));
        Assert.Equal(
            checked((ulong)expectedFieldThree),
            ReadUnsignedBigEndian(data.AsSpan(offset + 9, 2)));
    }

    private static ulong ReadUnsignedBigEndian(ReadOnlySpan<byte> source)
    {
        ulong value = 0;

        foreach (byte item in source)
        {
            value = (value << 8) | item;
        }

        return value;
    }

    private static long FindByteOffset(byte[] bytes, string value)
    {
        return FindSequence(bytes, Encoding.ASCII.GetBytes(value), 0);
    }

    private static int FindSequence(byte[] bytes, byte[] target, int start)
    {
        for (int offset = start; offset <= bytes.Length - target.Length; offset++)
        {
            if (bytes.AsSpan(offset, target.Length).SequenceEqual(target))
            {
                return offset;
            }
        }

        throw new Xunit.Sdk.XunitException(
            $"Could not find byte sequence '{Encoding.ASCII.GetString(target)}'.");
    }
}
