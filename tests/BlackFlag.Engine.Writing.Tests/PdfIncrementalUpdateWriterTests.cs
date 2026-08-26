using System.IO;
using System.Text;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfIncrementalUpdateWriterTests
{
    private static readonly byte[] SourceBytes =
        Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj\n<<>>\nendobj\nxref\n0 2\n%%EOF\n");

    [Fact]
    public void WritePreservesSourceBytesAndSerializesOnlyChangedObjects()
    {
        PdfDocument document = CreateDocument();
        PdfIncrementalWritePlan plan = CreatePlan(
            document,
            [new PdfObjectIdentifier(3)]);

        byte[] bytes = PdfIncrementalUpdateWriter.Write(plan);
        string appended = Encoding.Latin1.GetString(bytes[SourceBytes.Length..]);

        Assert.True(bytes.AsSpan(0, SourceBytes.Length).SequenceEqual(SourceBytes));
        Assert.DoesNotContain("1 0 obj\n", appended, StringComparison.Ordinal);
        Assert.Contains("3 0 obj\n", appended, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteEmitsChangedObjectSubsectionsAndPreviousCrossReferenceOffset()
    {
        PdfDocument document = CreateDocument();
        PdfIncrementalWritePlan plan = CreatePlan(
            document,
            [new PdfObjectIdentifier(3)]);

        byte[] bytes = PdfIncrementalUpdateWriter.Write(plan);
        string text = Encoding.Latin1.GetString(bytes);
        long objectOffset = FindByteOffset(bytes, "3 0 obj\n", SourceBytes.Length);
        long xrefOffset = FindByteOffset(bytes, "xref\n3 1\n", SourceBytes.Length);

        Assert.Contains(
            $"xref\n3 1\n{objectOffset:0000000000} 00000 n \n",
            text,
            StringComparison.Ordinal);
        Assert.Contains("/Prev 31", text, StringComparison.Ordinal);
        Assert.Contains("/Root 1 0 R", text, StringComparison.Ordinal);
        Assert.Contains("/Size 4", text, StringComparison.Ordinal);
        Assert.Contains(
            $"startxref\n{xrefOffset}\n%%EOF\n",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void WritePreservesInformationReference()
    {
        PdfDocument document = CreateDocument();
        document.Trailer.SetInformationObject(new PdfObjectIdentifier(3));
        PdfIncrementalWritePlan plan = CreatePlan(
            document,
            [new PdfObjectIdentifier(3)]);

        string text = Encoding.Latin1.GetString(
            PdfIncrementalUpdateWriter.Write(plan));

        Assert.Contains("/Info 3 0 R", text, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteSupportsNonSeekableDestinationAndLeavesItOpen()
    {
        PdfIncrementalWritePlan plan = CreatePlan(
            CreateDocument(),
            [new PdfObjectIdentifier(3)]);
        using var storage = new MemoryStream();
        using var destination = new NonSeekableWriteStream(storage);

        PdfIncrementalUpdateWriter.Write(destination, plan);
        destination.WriteByte(0x2A);

        Assert.Equal(0x2A, storage.ToArray()[^1]);
    }

    [Fact]
    public void WriteSupportsIncrementalCrossReferenceStream()
    {
        PdfDocument document = CreateDocument();
        var options = new PdfWriteOptions
        {
            Mode = PdfWriteMode.IncrementalUpdate,
            CrossReferenceMode = PdfCrossReferenceWriteMode.CrossReferenceStream
        };
        PdfWritePlan writePlan = PdfWritePlanner.Create(document, options);
        var source = new PdfMemorySourceRevision(
            SourceBytes,
            31,
            PdfCrossReferenceWriteMode.TraditionalTable);
        PdfIncrementalWritePlan plan = PdfIncrementalWritePlanner.Create(
            writePlan,
            source,
            [new PdfObjectIdentifier(3)]);

        byte[] bytes = PdfIncrementalUpdateWriter.Write(plan);
        string appended = Encoding.Latin1.GetString(bytes[SourceBytes.Length..]);
        long crossReferenceOffset = FindByteOffset(
            bytes,
            "4 0 obj\n",
            SourceBytes.Length);

        Assert.Contains("3 0 obj\n", appended, StringComparison.Ordinal);
        Assert.Contains("4 0 obj\n", appended, StringComparison.Ordinal);
        Assert.Contains("/Type /XRef", appended, StringComparison.Ordinal);
        Assert.Contains("/Index [3 2]", appended, StringComparison.Ordinal);
        Assert.Contains("/Length 22", appended, StringComparison.Ordinal);
        Assert.Contains("/Prev 31", appended, StringComparison.Ordinal);
        Assert.Contains("/Root 1 0 R", appended, StringComparison.Ordinal);
        Assert.Contains("/Size 5", appended, StringComparison.Ordinal);
        Assert.DoesNotContain("trailer\n", appended, StringComparison.Ordinal);
        Assert.Contains(
            $"startxref\n{crossReferenceOffset}\n%%EOF\n",
            appended,
            StringComparison.Ordinal);
    }

    [Fact]
    public void WritePreserveSourceUsesIncrementalCrossReferenceStreamWhenSourceDoes()
    {
        PdfDocument document = CreateDocument();
        var options = new PdfWriteOptions
        {
            Mode = PdfWriteMode.IncrementalUpdate,
            CrossReferenceMode = PdfCrossReferenceWriteMode.PreserveSource
        };
        PdfWritePlan writePlan = PdfWritePlanner.Create(document, options);
        var source = new PdfMemorySourceRevision(
            SourceBytes,
            31,
            PdfCrossReferenceWriteMode.CrossReferenceStream);
        PdfIncrementalWritePlan plan = PdfIncrementalWritePlanner.Create(
            writePlan,
            source,
            [new PdfObjectIdentifier(3)]);

        string appended = Encoding.Latin1.GetString(
            PdfIncrementalUpdateWriter.Write(plan)[SourceBytes.Length..]);

        Assert.Equal(
            PdfCrossReferenceWriteMode.CrossReferenceStream,
            plan.CrossReferenceMode);
        Assert.Contains("/Type /XRef", appended, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteReservesCrossReferenceStreamAfterLateAllocatedObject()
    {
        PdfDocument document = CreateDocument();
        var options = new PdfWriteOptions
        {
            Mode = PdfWriteMode.IncrementalUpdate,
            CrossReferenceMode = PdfCrossReferenceWriteMode.CrossReferenceStream
        };
        PdfWritePlan writePlan = PdfWritePlanner.Create(document, options);
        var lateIdentifier = new PdfObjectIdentifier(
            writePlan.NextObjectNumber);
        document.Objects.Add(
            lateIdentifier,
            new PdfString(Encoding.ASCII.GetBytes("late object")));
        var source = new PdfMemorySourceRevision(
            SourceBytes,
            31,
            PdfCrossReferenceWriteMode.TraditionalTable);
        PdfIncrementalWritePlan plan = PdfIncrementalWritePlanner.Create(
            writePlan,
            source,
            [new PdfObjectIdentifier(3), lateIdentifier]);

        string appended = Encoding.Latin1.GetString(
            PdfIncrementalUpdateWriter.Write(plan)[SourceBytes.Length..]);
        int crossReferenceObjectNumber =
            checked(lateIdentifier.ObjectNumber + 1);

        Assert.Contains(
            $"{lateIdentifier.ObjectNumber} 0 obj\n",
            appended,
            StringComparison.Ordinal);
        Assert.Contains(
            $"{crossReferenceObjectNumber} 0 obj\n",
            appended,
            StringComparison.Ordinal);
        Assert.Contains(
            $"/Size {crossReferenceObjectNumber + 1}",
            appended,
            StringComparison.Ordinal);
    }

    [Fact]
    public void WriteRejectsChangedObjectRemovedAfterPlanning()
    {
        PdfDocument document = CreateDocument();
        PdfIncrementalWritePlan plan = CreatePlan(
            document,
            [new PdfObjectIdentifier(3)]);
        document.Objects.Remove(new PdfObjectIdentifier(3));

        Assert.Throws<InvalidOperationException>(
            () => PdfIncrementalUpdateWriter.Write(plan));
    }

    private static PdfIncrementalWritePlan CreatePlan(
        PdfDocument document,
        PdfObjectIdentifier[] changed)
    {
        var options = new PdfWriteOptions
        {
            Mode = PdfWriteMode.IncrementalUpdate,
            CrossReferenceMode = PdfCrossReferenceWriteMode.TraditionalTable
        };
        PdfWritePlan writePlan = PdfWritePlanner.Create(document, options);
        var source = new PdfMemorySourceRevision(
            SourceBytes,
            31,
            PdfCrossReferenceWriteMode.TraditionalTable);

        return PdfIncrementalWritePlanner.Create(
            writePlan,
            source,
            changed);
    }

    private static PdfDocument CreateDocument()
    {
        PdfDocument document = PdfDocument.CreateEmpty(PdfVersion.Pdf17);
        document.Objects.Add(
            new PdfObjectIdentifier(1),
            new PdfDictionary());
        document.Objects.Add(
            new PdfObjectIdentifier(3),
            new PdfString(Encoding.ASCII.GetBytes("updated")));
        document.Trailer.SetRootObject(new PdfObjectIdentifier(1));
        return document;
    }

    private static long FindByteOffset(
        byte[] bytes,
        string value,
        int start)
    {
        byte[] target = Encoding.ASCII.GetBytes(value);

        for (int offset = start; offset <= bytes.Length - target.Length; offset++)
        {
            if (bytes.AsSpan(offset, target.Length).SequenceEqual(target))
            {
                return offset;
            }
        }

        throw new Xunit.Sdk.XunitException(
            $"Could not find '{value}' in serialized PDF bytes.");
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
            base.Dispose(disposing);
        }
    }
}
