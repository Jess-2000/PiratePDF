#nullable enable

using System.Threading;
using System.Threading.Tasks;
using BlackFlag.Engine.Abstractions.IO;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Recovery;

namespace BlackFlag.Engine.Parsing.Tests.Recovery;

public sealed class PdfCrossReferenceRecoveryServiceTests
{
    [Fact]
    public void RecoverBuildsSyntheticIndexWhenXrefIsMissing()
    {
        byte[] bytes = FixturePdf(
            includeXref: false,
            trailerSuffix: string.Empty);

        using var source = new MemorySource(bytes);
        PdfCrossReferenceRecoveryResult result =
            new PdfCrossReferenceRecoveryService(source).Recover();

        Assert.Equal(new PdfObjectId(1, 0), result.RootObjectId);
        Assert.Equal(PdfRecoveredEncryptionState.None, result.EncryptionState);
        Assert.True(result.LocationTable.TryGetLocation(
            new PdfObjectId(1, 0),
            out _));
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code == "OBJECT_RECOVERED");
    }

    [Fact]
    public void RecoverDoesNotAcceptFakeObjectHeaderInsideValidStream()
    {
        const string payload = "99 0 obj << /Type /Catalog >> endobj";
        byte[] bytes = FixturePdf(
            includeXref: false,
            trailerSuffix: string.Empty,
            streamPayload: payload);

        using var source = new MemorySource(bytes);
        PdfCrossReferenceRecoveryResult result =
            new PdfCrossReferenceRecoveryService(source).Recover();

        Assert.DoesNotContain(
            result.Objects,
            item => item.ObjectId.ObjectNumber == 99);
    }

    [Fact]
    public void RecoverInfersUniqueCatalogWithoutTrailer()
    {
        byte[] bytes = FixturePdf(
            includeXref: false,
            includeTrailer: false,
            trailerSuffix: string.Empty);

        using var source = new MemorySource(bytes);
        PdfCrossReferenceRecoveryResult result =
            new PdfCrossReferenceRecoveryService(source).Recover();

        Assert.Equal(new PdfObjectId(1, 0), result.RootObjectId);
        Assert.Equal(PdfRecoveryConfidence.Partial, result.Confidence);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code == "ROOT_INFERRED");
    }

    [Fact]
    public void StructurallyPresentStandardEncryptionRequiresAuthentication()
    {
        string owner = new('A', 32);
        string user = new('B', 32);

        string encryptionObject =
            $"4 0 obj\n<< /Filter /Standard /V 1 /R 2 /O ({owner}) "
            + $"/U ({user}) /P -4 >>\nendobj\n";

        byte[] bytes = FixturePdf(
            includeXref: false,
            trailerSuffix: " /Encrypt 4 0 R",
            extraObject: encryptionObject);

        using var source = new MemorySource(bytes);
        PdfCrossReferenceRecoveryResult result =
            new PdfCrossReferenceRecoveryService(source).Recover();

        Assert.Equal(
            PdfRecoveredEncryptionState.AuthenticationRequired,
            result.EncryptionState);
    }

    [Fact]
    public void DamagedEncryptionStopsAtDamagedClassification()
    {
        const string encryptionObject =
            "4 0 obj\n<< /Filter /Standard /R 2 >>\nendobj\n";

        byte[] bytes = FixturePdf(
            includeXref: false,
            trailerSuffix: " /Encrypt 4 0 R",
            extraObject: encryptionObject);

        using var source = new MemorySource(bytes);
        PdfCrossReferenceRecoveryResult result =
            new PdfCrossReferenceRecoveryService(source).Recover();

        Assert.Equal(
            PdfRecoveredEncryptionState.Damaged,
            result.EncryptionState);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code == "ENCRYPTION_DAMAGED");
    }

    private static byte[] FixturePdf(
        bool includeXref,
        string trailerSuffix,
        bool includeTrailer = true,
        string? streamPayload = null,
        string? extraObject = null)
    {
        string streamObject = streamPayload is null
            ? string.Empty
            : $"3 0 obj\n<< /Length {streamPayload.Length} >>\nstream\n"
            + $"{streamPayload}\nendstream\nendobj\n";

        string pdf =
            "%PDF-1.7\n"
            + "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"
            + "2 0 obj\n<< /Type /Pages /Count 0 /Kids [] >>\nendobj\n"
            + streamObject
            + (extraObject ?? string.Empty);

        if (includeXref)
        {
            pdf += "xref\n0 1\n0000000000 65535 f \n";
        }

        if (includeTrailer)
        {
            pdf += $"trailer\n<< /Size 5 /Root 1 0 R{trailerSuffix} >>\n";
        }

        pdf += "%%EOF\n";
        return System.Text.Encoding.ASCII.GetBytes(pdf);
    }

    private sealed class MemorySource : IRandomAccessSource
    {
        private readonly byte[] bytes;

        public MemorySource(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public long Length => bytes.LongLength;

        public int Read(long offset, Span<byte> destination)
        {
            if (offset < 0 || offset > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            int count = checked((int)Math.Min(destination.Length, Length - offset));
            bytes.AsSpan(checked((int)offset), count).CopyTo(destination);
            return count;
        }

        public ValueTask<int> ReadAsync(
            long offset,
            Memory<byte> destination,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(Read(offset, destination.Span));
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            throw new NotImplementedException();
        }

    }
}
