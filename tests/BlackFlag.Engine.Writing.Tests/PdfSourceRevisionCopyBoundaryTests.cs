#nullable enable
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfSourceRevisionCopyBoundaryTests
{
    private const int BufferSize = 64 * 1024;

    [Theory]
    [InlineData(1)]
    [InlineData(BufferSize - 1)]
    [InlineData(BufferSize)]
    [InlineData(BufferSize + 1)]
    [InlineData(3 * BufferSize)]
    [InlineData((3 * BufferSize) + 1)]
    public void RepeatedHostileAndOrdinaryCopiesPreserveEveryByte(int length)
    {
        byte[] original = CreateBytes(length);
        byte[] input = original.ToArray();
        var revision = new PdfMemorySourceRevision(input, length - 1, PdfCrossReferenceWriteMode.CrossReferenceStream);
        input.AsSpan().Fill(0xFF);

        for (int attempt = 0; attempt < 3; attempt++)
        {
            using var hostile = new CallbackDestination();
            revision.CopyTo(hostile);
            Assert.Equal(original, hostile.Bytes);
            int fullChunks = length / BufferSize;
            int remainder = length % BufferSize;
            Assert.Equal(fullChunks + (remainder == 0 ? 0 : 1), hostile.Counts.Count);
            for (int index = 0; index < fullChunks; index++)
            {
                Assert.Equal(BufferSize, hostile.Counts[index]);
            }

            if (remainder != 0)
            {
                Assert.Equal(remainder, hostile.Counts[^1]);
            }

            AssertSourceUnchanged(revision, original);
            Assert.Equal(length - 1, revision.CrossReferenceOffset);
            Assert.Equal(PdfCrossReferenceWriteMode.CrossReferenceStream, revision.CrossReferenceMode);
            // CopyTo must leave this nonseekable, caller-owned destination open.
            hostile.Write([42], 0, 1);
            Assert.Equal(42, hostile.Bytes[^1]);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CallbackFailurePropagatesSameExceptionAndPreservesRevision(int throwOnWrite)
    {
        byte[] original = CreateBytes((2 * BufferSize) + 1);
        var revision = new PdfMemorySourceRevision(original, 17, PdfCrossReferenceWriteMode.TraditionalTable);
        var failure = new IOException("Destination rejected a source chunk.");
        using var destination = new CallbackDestination(throwOnWrite, failure);

        IOException actual = Assert.Throws<IOException>(() => revision.CopyTo(destination));

        Assert.Same(failure, actual);
        Assert.Equal(throwOnWrite, destination.Counts.Count);
        // This destination copies before throwing; arbitrary destination output is not rolled back.
        int written = Math.Min(throwOnWrite * BufferSize, original.Length);
        Assert.Equal(original[..written], destination.Bytes);
        AssertSourceUnchanged(revision, original);
        Assert.Equal(17, revision.CrossReferenceOffset);
        Assert.Equal(PdfCrossReferenceWriteMode.TraditionalTable, revision.CrossReferenceMode);
        destination.Write([42], 0, 1);
        Assert.Equal(42, destination.Bytes[^1]);
    }

    [Fact]
    public void EmptySourceAndInvalidDestinationsRetainValidationContracts()
    {
        ArgumentException empty = Assert.Throws<ArgumentException>(() =>
            new PdfMemorySourceRevision([], 0, PdfCrossReferenceWriteMode.TraditionalTable));
        Assert.Equal("bytes", empty.ParamName);
        Assert.StartsWith("An original PDF source revision cannot be empty.", empty.Message);
        var revision = new PdfMemorySourceRevision([1], 0, PdfCrossReferenceWriteMode.TraditionalTable);
        Assert.Equal("destination", Assert.Throws<ArgumentNullException>(() => revision.CopyTo(null!)).ParamName);
        using var readOnly = new MemoryStream([1], writable: false);
        ArgumentException unwritable = Assert.Throws<ArgumentException>(() => revision.CopyTo(readOnly));
        Assert.Equal("destination", unwritable.ParamName);
        Assert.StartsWith("The source-revision destination must be writable.", unwritable.Message);
        AssertSourceUnchanged(revision, [1]);
    }

    private static byte[] CreateBytes(int length)
    {
        var bytes = new byte[length];
        for (int index = 0; index < length; index++)
        {
            bytes[index] = (byte)((index + (index / BufferSize)) % 251);
        }

        return bytes;
    }

    private static void AssertSourceUnchanged(PdfMemorySourceRevision revision, byte[] original)
    {
        using var ordinary = new MemoryStream();
        revision.CopyTo(ordinary);
        byte[] copied = ordinary.ToArray();
        Assert.Equal(original, copied);
        Assert.Equal(SHA256.HashData(original), SHA256.HashData(copied));
        Assert.Equal(original.Length, revision.Length);
    }

    // Nonseekable destination; the span mutation is confined to its synchronous lifetime.
    private sealed class CallbackDestination(int throwOnWrite = 0, IOException? failure = null) : Stream
    {
        private readonly MemoryStream storage = new();

        public byte[] Bytes => storage.ToArray();
        public List<int> Counts { get; } = [];
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => storage.CanWrite;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => storage.Flush();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => storage.Write(buffer, offset, count);

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            storage.Write(buffer);
            Counts.Add(buffer.Length);
            if (!buffer.IsEmpty)
            {
                MemoryMarshal.GetReference(buffer) ^= 0xFF;
            }

            if (Counts.Count == throwOnWrite)
            {
                throw failure!;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                storage.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
