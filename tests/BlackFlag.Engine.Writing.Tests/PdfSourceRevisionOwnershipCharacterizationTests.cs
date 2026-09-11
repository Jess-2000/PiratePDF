using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing.Tests;

// Source-revision ownership regressions converted from POST-P0-014 characterization.
// The separate parser-stream bridge control remains characterization of POST-P0-013.
public sealed class PdfSourceRevisionOwnershipCharacterizationTests
{
    [Fact]
    public void ExplicitDocumentStreamCopiesIsolateOnlyBytesCapturedBeforeParserMutation()
    {
        var parsed = new BlackFlag.Engine.Objects.PdfStream(
            Objects.PdfDictionary.Empty, "original"u8.ToArray());
        var before = new PdfStream(new PdfDictionary(), parsed.Data.Span);
        byte[] serializedBefore = PdfObjectSerializer.Serialize(before);
        MemoryMarshal.AsMemory(parsed.Data).Span[0] = (byte)'X';
        Assert.Equal("original"u8.ToArray(), before.ToByteArray());
        Assert.Equal(serializedBefore, PdfObjectSerializer.Serialize(before));
        var after = new PdfStream(new PdfDictionary(), parsed.Data.Span);
        Assert.Contains("Xriginal", Encoding.Latin1.GetString(PdfObjectSerializer.Serialize(after)), StringComparison.Ordinal);
        Assert.Equal("Xriginal"u8.ToArray(), after.ToByteArray());
    }

    [Fact]
    public void ArrayFallbackExposesOnlyAnIndependentCopyDuringCallback()
    {
        byte[] input = "source revision"u8.ToArray();
        var revision = new PdfMemorySourceRevision(input, 0, PdfCrossReferenceWriteMode.TraditionalTable);
        input[0] = (byte)'X';
        using var destination = new ArrayDestination();
        revision.CopyTo(destination);
        Assert.True(destination.Mutated);
        Assert.Equal("source revision"u8.ToArray(), Copy(revision));
    }

    [Fact]
    public void SpanCallbackCannotMutateRetainedSourceOrChangeLaterCopies()
    {
        var revision = new PdfMemorySourceRevision("source revision"u8, 0, PdfCrossReferenceWriteMode.TraditionalTable);
        byte[] original = Copy(revision);
        using var destination = new MutatingDestination();
        revision.CopyTo(destination);
        Assert.True(destination.Mutated);
        Assert.Equal(original, destination.ToArray());
        Assert.Equal(original, Copy(revision));
        Assert.Equal(SHA256.HashData(original), SHA256.HashData(Copy(revision)));
        Assert.Equal(original.Length, revision.Length);
        Assert.Equal(0, revision.CrossReferenceOffset);
        Assert.Equal(PdfCrossReferenceWriteMode.TraditionalTable, revision.CrossReferenceMode);
    }

    [Theory]
    [InlineData(PdfCrossReferenceWriteMode.TraditionalTable)]
    [InlineData(PdfCrossReferenceWriteMode.CrossReferenceStream)]
    public void IncrementalDestinationMutationCannotCorruptTheNextSave(PdfCrossReferenceWriteMode mode)
    {
        _ = CreateIncrementalAfterHostileCopy(mode);
    }

    // Shared deterministic fixture for supplemental actual-parser reopen validation.
    public static (byte[] Original, byte[] Saved, long OriginalXref) CreateIncrementalAfterHostileCopy(
        PdfCrossReferenceWriteMode mode)
    {
        var document = PdfDocument.CreateEmpty(PdfVersion.Pdf17);
        var rootId = new PdfObjectIdentifier(1);
        var changedId = new PdfObjectIdentifier(3, 7);
        document.Objects.Add(rootId, new PdfDictionary());
        // Keep the original revision large enough to cross multiple isolation chunks.
        document.Objects.Add(new PdfObjectIdentifier(2),
            new PdfString(Encoding.ASCII.GetBytes(new string('p', (2 * 64 * 1024) + 1))));
        document.Objects.Add(changedId, new PdfString("original"u8));
        document.Trailer.SetRootObject(rootId);
        byte[] original = PdfFullRewriteWriter.Write(PdfWritePlanner.Create(document,
            new PdfWriteOptions { CrossReferenceMode = mode }));
        string text = Encoding.Latin1.GetString(original);
        long xrefOffset = long.Parse(text[(text.LastIndexOf("startxref\n", StringComparison.Ordinal) + 10)..]
            .Split('\n')[0], System.Globalization.CultureInfo.InvariantCulture);
        var revision = new PdfMemorySourceRevision(original, xrefOffset, mode);
        document.Objects.Set(changedId, new PdfString("updated"u8));
        var options = new PdfWriteOptions { Mode = PdfWriteMode.IncrementalUpdate, CrossReferenceMode = mode };
        var plan = PdfIncrementalWritePlanner.Create(PdfWritePlanner.Create(document, options), revision, [changedId]);
        Assert.Same(revision, plan.SourceRevision);
        using var destination = new MutatingDestination(original.Length);
        PdfIncrementalUpdateWriter.Write(destination, plan);
        Assert.True(destination.Mutated);
        Assert.Equal(original, destination.ToArray()[..original.Length]);
        byte[] secondSave = PdfIncrementalUpdateWriter.Write(plan);
        Assert.Equal(original, secondSave[..original.Length]);
        Assert.Equal(SHA256.HashData(original), SHA256.HashData(secondSave[..original.Length]));
        Assert.Equal(destination.ToArray(), secondSave);
        Assert.Equal(secondSave, PdfIncrementalUpdateWriter.Write(plan));
        Assert.Equal(original, Copy(revision));
        Assert.Equal(original.Length, revision.Length);
        Assert.Equal(xrefOffset, revision.CrossReferenceOffset);
        Assert.Equal(mode, revision.CrossReferenceMode);
        string appended = Encoding.Latin1.GetString(secondSave[original.Length..]);
        Assert.Contains($"/Prev {xrefOffset}", appended, StringComparison.Ordinal);
        long appendedXref = long.Parse(appended[(appended.LastIndexOf("startxref\n", StringComparison.Ordinal) + 10)..]
            .Split('\n')[0], System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(appendedXref > original.Length);
        string xrefStart = Encoding.Latin1.GetString(secondSave[(int)appendedXref..]);
        Assert.StartsWith(mode == PdfCrossReferenceWriteMode.TraditionalTable ? "xref\n" : "4 0 obj\n", xrefStart);
        return (original, secondSave, xrefOffset);
    }

    private static byte[] Copy(PdfMemorySourceRevision revision)
    {
        using var stream = new MemoryStream();
        revision.CopyTo(stream);
        return stream.ToArray();
    }

    private sealed class MutatingDestination(int sourceLength = int.MaxValue) : MemoryStream
    {
        private int remainingSourceBytes = sourceLength;

        public bool Mutated { get; private set; }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            base.Write(buffer);
            if (remainingSourceBytes > 0 && !buffer.IsEmpty)
            {
                // GetReference returns a writable managed ref even for ReadOnlySpan.
                // No ref is retained beyond this synchronous callback.
                MemoryMarshal.GetReference(buffer) = (byte)'X';
                Mutated = true;
                remainingSourceBytes -= Math.Min(remainingSourceBytes, buffer.Length);
            }
        }
    }

    private sealed class ArrayDestination : Stream
    {
        public bool Mutated { get; private set; }
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count)
        {
            // Stream's fallback may return this temporary buffer to a pool after
            // the callback. Mutate only while its lifetime is valid.
            buffer.AsSpan(offset, count).Fill(0xFF);
            Mutated = true;
        }
    }
}
