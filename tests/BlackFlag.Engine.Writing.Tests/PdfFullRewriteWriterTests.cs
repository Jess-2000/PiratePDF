using System.IO;
using System.Text;

using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfFullRewriteWriterTests
{
    [Fact]
    public void WriteEmitsCompleteTraditionalPdfRevision()
    {
        PdfDocument document = CreateDocument();
        PdfWritePlan plan = PdfWritePlanner.Create(
            document,
            new PdfWriteOptions
            {
                CrossReferenceMode = PdfCrossReferenceWriteMode.TraditionalTable
            });

        byte[] bytes = PdfFullRewriteWriter.Write(plan);
        string text = Encoding.Latin1.GetString(bytes);

        Assert.StartsWith("%PDF-1.7\n%", text, StringComparison.Ordinal);
        Assert.Contains("1 0 obj\n", text, StringComparison.Ordinal);
        Assert.Contains("3 0 obj\n", text, StringComparison.Ordinal);
        Assert.Contains("xref\n0 4\n", text, StringComparison.Ordinal);
        Assert.Contains("0000000000 65535 f \n", text, StringComparison.Ordinal);
        Assert.Contains("0000000000 00000 f \n", text, StringComparison.Ordinal);
        Assert.Contains("trailer\n", text, StringComparison.Ordinal);
        Assert.Contains("/Root 1 0 R", text, StringComparison.Ordinal);
        Assert.Contains("/Size 4", text, StringComparison.Ordinal);
        Assert.EndsWith("%%EOF\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteCrossReferenceOffsetsPointAtIndirectObjectHeaders()
    {
        PdfDocument document = CreateDocument();
        byte[] bytes = PdfFullRewriteWriter.Write(PdfWritePlanner.Create(document));
        string text = Encoding.Latin1.GetString(bytes);

        long objectOneOffset = FindByteOffset(bytes, "1 0 obj\n");
        long objectThreeOffset = FindByteOffset(bytes, "3 0 obj\n");

        Assert.Contains($"{objectOneOffset:0000000000} 00000 n \n", text, StringComparison.Ordinal);
        Assert.Contains($"{objectThreeOffset:0000000000} 00000 n \n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteStartXrefPointsAtCrossReferenceTable()
    {
        byte[] bytes = PdfFullRewriteWriter.Write(
            PdfWritePlanner.Create(CreateDocument()));
        string text = Encoding.Latin1.GetString(bytes);
        long xrefOffset = FindByteOffset(bytes, "xref\n");

        Assert.Contains(
            $"startxref\n{xrefOffset}\n%%EOF\n",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void WritePreservesInformationReferenceAndDropsSourcePrevForFullRewrite()
    {
        PdfDocument document = CreateDocument();
        document.Trailer.SetInformationObject(new PdfObjectIdentifier(3));
        document.Trailer.SetPreviousCrossReferenceOffset(42);

        string text = Encoding.Latin1.GetString(
            PdfFullRewriteWriter.Write(PdfWritePlanner.Create(document)));

        Assert.Contains("/Info 3 0 R", text, StringComparison.Ordinal);
        Assert.DoesNotContain("/Prev", text, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteDoesNotCloseCallerOwnedDestination()
    {
        using var destination = new MemoryStream();

        PdfFullRewriteWriter.Write(
            destination,
            PdfWritePlanner.Create(CreateDocument()));

        destination.WriteByte(0x2A);
        Assert.Equal(0x2A, destination.ToArray()[^1]);
    }

    [Fact]
    public void WriteSupportsNonSeekableDestination()
    {
        using var storage = new MemoryStream();
        using var destination = new NonSeekableWriteStream(storage);

        PdfFullRewriteWriter.Write(
            destination,
            PdfWritePlanner.Create(CreateDocument()));

        Assert.NotEmpty(storage.ToArray());
    }

    [Fact]
    public void WriteRejectsIncrementalPlan()
    {
        var options = new PdfWriteOptions
        {
            Mode = PdfWriteMode.IncrementalUpdate
        };

        Assert.Throws<NotSupportedException>(
            () => PdfFullRewriteWriter.Write(
                PdfWritePlanner.Create(CreateDocument(), options)));
    }

    [Fact]
    public void WriteSupportsCrossReferenceStreamPlan()
    {
        var options = new PdfWriteOptions
        {
            CrossReferenceMode = PdfCrossReferenceWriteMode.CrossReferenceStream
        };

        string text = Encoding.Latin1.GetString(
            PdfFullRewriteWriter.Write(
                PdfWritePlanner.Create(CreateDocument(), options)));

        Assert.Contains("/Type /XRef", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\nxref\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain("trailer\n", text, StringComparison.Ordinal);
        Assert.EndsWith("%%EOF\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteRejectsEncryptedDocumentUntilEncryptionAwareWritingExists()
    {
        PdfDocument document = CreateDocument();
        document.Trailer.SetEncryptionObject(new PdfObjectIdentifier(3));

        Assert.Throws<NotSupportedException>(
            () => PdfFullRewriteWriter.Write(
                PdfWritePlanner.Create(document)));
    }

    [Fact]
    public void WriteRejectsObjectRemovedAfterPlanning()
    {
        PdfDocument document = CreateDocument();
        PdfWritePlan plan = PdfWritePlanner.Create(document);
        document.Objects.Remove(new PdfObjectIdentifier(3));

        Assert.Throws<InvalidOperationException>(
            () => PdfFullRewriteWriter.Write(plan));
    }

    [Fact]
    public void WriteRejectsMultipleActiveGenerationsForOneObjectNumber()
    {
        PdfDocument document = PdfDocument.CreateEmpty();
        document.Objects.Add(new PdfObjectIdentifier(1, 0), new PdfDictionary());
        document.Objects.Add(new PdfObjectIdentifier(1, 1), new PdfDictionary());
        document.Trailer.SetRootObject(new PdfObjectIdentifier(1, 1));

        Assert.Throws<InvalidOperationException>(
            () => PdfFullRewriteWriter.Write(
                PdfWritePlanner.Create(document)));
    }

    private static PdfDocument CreateDocument()
    {
        PdfDocument document = PdfDocument.CreateEmpty(PdfVersion.Pdf17);
        document.Objects.Add(
            new PdfObjectIdentifier(1),
            new PdfDictionary(
            [
                new KeyValuePair<PdfName, PdfObject>(
                    new PdfName(Encoding.ASCII.GetBytes("Type")),
                    new PdfName(Encoding.ASCII.GetBytes("Catalog")))
            ]));
        document.Objects.Add(
            new PdfObjectIdentifier(3),
            new PdfString(Encoding.ASCII.GetBytes("metadata")));
        document.Trailer.SetRootObject(new PdfObjectIdentifier(1));
        return document;
    }

    private static long FindByteOffset(byte[] bytes, string value)
    {
        byte[] target = Encoding.ASCII.GetBytes(value);

        for (int offset = 0; offset <= bytes.Length - target.Length; offset++)
        {
            if (bytes.AsSpan(offset, target.Length).SequenceEqual(target))
            {
                return offset;
            }
        }

        throw new Xunit.Sdk.XunitException($"Could not find '{value}' in serialized PDF bytes.");
    }

    private sealed class NonSeekableWriteStream : Stream
    {
        private readonly Stream destination;

        internal NonSeekableWriteStream(Stream destination)
        {
            this.destination = destination;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => destination.Flush();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => destination.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => destination.Write(buffer);
        public override void WriteByte(byte value) => destination.WriteByte(value);

        protected override void Dispose(bool disposing)
        {
            // Test wrapper does not own its destination.
            base.Dispose(disposing);
        }
    }
}
