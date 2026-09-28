using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BlackFlag.Engine.Fonts.Sanitization;
using Xunit;

namespace BlackFlag.Engine.Fonts.Tests;

public sealed class PdfSanitizedExternalFontProgramOwnershipTests
{
    [Fact]
    public void SanitizationOwnsItsInputAndPreservesItsHash()
    {
        using var fixture = TestFontFactory.CreateMinimalTrueType();
        byte[] input = fixture.ToArray();
        using var source = new MemoryStream(input, writable: false);
        var program = new PdfOpenTypeFontSanitizer().Sanitize(source);
        byte[] expected = program.Bytes.ToArray();

        Array.Fill(input, (byte)0xFF);

        AssertTrusted(program, expected);
        Assert.True(source.CanRead);
    }

    [Fact]
    public void RetainedPublicMemoryCannotChangeLaterReadsOrReaders()
    {
        var program = CreateProgram();
        byte[] expected = program.Bytes.ToArray();
        ReadOnlyMemory<byte> retained = program.Bytes;
        using Stream reader = program.OpenRead();

        if (MemoryMarshal.TryGetArray(retained, out ArraySegment<byte> segment))
        {
            segment.Array![segment.Offset] ^= 0xFF;
        }
        AssertTrusted(program, expected);

        MemoryMarshal.AsMemory(retained).Span.Fill(0xFF);
        AssertTrusted(program, expected);
        Assert.Equal(expected, ReadAll(reader));
    }

    [Fact]
    public void ReadersPreserveIndependentPositionDisposalAndReadOnlyBehavior()
    {
        var program = CreateProgram();
        byte[] expected = program.Bytes.ToArray();
        using Stream first = program.OpenRead();
        using Stream second = program.OpenRead();
        Assert.Equal(0, first.Position);
        Assert.Equal(expected.Length, first.Length);
        Assert.True(first.CanRead);
        Assert.True(first.CanSeek);
        Assert.False(first.CanWrite);
        Assert.Equal(expected[0], first.ReadByte());
        Assert.Equal(1, first.Position);
        Assert.Equal(0, second.Position);
        Assert.Equal(expected.Length - 1, first.Seek(-1, SeekOrigin.End));
        Assert.Equal(expected[^1], first.ReadByte());
        Assert.Equal(-1, first.ReadByte());
        Assert.Throws<NotSupportedException>(() => second.WriteByte(1));
        Assert.Throws<NotSupportedException>(() => second.SetLength(0));

        if (first is MemoryStream memory)
        {
            if (memory.TryGetBuffer(out ArraySegment<byte> buffer))
            {
                buffer.Array![buffer.Offset] ^= 0xFF;
            }
            else
            {
                Assert.Throws<UnauthorizedAccessException>(() => memory.GetBuffer());
            }
            byte[] copy = memory.ToArray();
            Array.Fill(copy, (byte)0xFF);
        }

        first.Dispose();
        Assert.Equal(expected, ReadAll(second));
        AssertTrusted(program, expected);
    }

    [Theory]
    [InlineData("copy")]
    [InlineData("copy-async")]
    [InlineData("write-to")]
    public async Task StreamCopyDestinationCannotRetainTrustedBackingStorage(string operation)
    {
        var program = CreateProgram();
        byte[] expected = program.Bytes.ToArray();
        using Stream reader = program.OpenRead();
        using Stream retainedReader = program.OpenRead();
        using var destination = new RetainingDestination();
        if (operation == "copy-async")
        {
            await reader.CopyToAsync(destination);
        }
        else if (operation == "write-to" && reader is MemoryStream memory)
        {
            memory.WriteTo(destination);
        }
        else
        {
            reader.CopyTo(destination);
        }

        Assert.Equal(expected, destination.ToArray());
        destination.CorruptRetainedBuffer();
        Assert.Equal(expected, ReadAll(retainedReader));
        AssertTrusted(program, expected);
    }

    [Fact]
    public async Task CancelledReadPreservesPositionAndTrustedContents()
    {
        var program = CreateProgram();
        byte[] expected = program.Bytes.ToArray();
        using Stream reader = program.OpenRead();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await reader.ReadExactlyAsync(new byte[4], cancellation.Token));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(0, reader.Position);
        AssertTrusted(program, expected);
    }

    private static PdfSanitizedExternalFontProgram CreateProgram()
    {
        using var fixture = TestFontFactory.CreateMinimalTrueType();
        return new PdfOpenTypeFontSanitizer().Sanitize(fixture);
    }

    private static void AssertTrusted(PdfSanitizedExternalFontProgram program, byte[] expected)
    {
        byte[] actual = program.Bytes.ToArray();
        Assert.Equal(expected, actual);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(actual)), program.Sha256);
        using Stream reader = program.OpenRead();
        Assert.Equal(expected, ReadAll(reader));
    }

    private static byte[] ReadAll(Stream source)
    {
        using var output = new MemoryStream();
        source.CopyTo(output);
        return output.ToArray();
    }

    private sealed class RetainingDestination : MemoryStream
    {
        private ArraySegment<byte> retained;

        public override void Write(byte[] buffer, int offset, int count)
        {
            retained = new ArraySegment<byte>(buffer, offset, count);
            base.Write(buffer, offset, count);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _ = MemoryMarshal.TryGetArray(buffer, out retained);
            base.Write(buffer.Span);
            return ValueTask.CompletedTask;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        public void CorruptRetainedBuffer()
        {
            if (retained.Array is not null)
            {
                retained.Array.AsSpan(retained.Offset, retained.Count).Fill(0xFF);
            }
        }
    }
}
