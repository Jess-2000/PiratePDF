using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlackFlag.Engine.Services.Security.Trust;

namespace BlackFlag.Engine.Services.Tests.Security.Trust;

public sealed class DocumentTrustEvaluatorTests
{
    private const string PolicyVersion = "BF-1049-v1";

    [Fact]
    public void MissingTrustRequiresFullInspection()
    {
        using var stream = Document("piratepdf");
        var evaluator = new DocumentTrustEvaluator();

        DocumentTrustEvaluation result =
            DocumentTrustEvaluator.Evaluate(
                stream,
                null,
                PolicyVersion);

        Assert.Equal(
            DocumentTrustDecision.FullInspectionRequired,
            result.Decision);
        Assert.Equal(64, result.Sha256Fingerprint.Length);
    }

    [Fact]
    public void MatchingFingerprintAndPolicyUseFastPath()
    {
        using var stream = Document("piratepdf");
        var evaluator = new DocumentTrustEvaluator();

        DocumentTrustEvaluation first =
            DocumentTrustEvaluator.Evaluate(
                stream,
                null,
                PolicyVersion);

        var record =
            new DocumentTrustRecord(
                first.Sha256Fingerprint,
                PolicyVersion,
                DateTimeOffset.UnixEpoch);

        DocumentTrustEvaluation result =
            DocumentTrustEvaluator.Evaluate(
                stream,
                record,
                PolicyVersion);

        Assert.True(result.CanUseTrustedFastPath);
    }

    [Fact]
    public void ChangedBytesSilentlyRequireFullInspection()
    {
        var evaluator = new DocumentTrustEvaluator();

        using var original = Document("piratepdf");
        DocumentTrustEvaluation first =
            DocumentTrustEvaluator.Evaluate(
                original,
                null,
                PolicyVersion);

        var record =
            new DocumentTrustRecord(
                first.Sha256Fingerprint,
                PolicyVersion,
                DateTimeOffset.UnixEpoch);

        using var changed = Document("piratepdf changed");

        DocumentTrustEvaluation result =
            DocumentTrustEvaluator.Evaluate(
                changed,
                record,
                PolicyVersion);

        Assert.False(result.CanUseTrustedFastPath);
    }

    [Fact]
    public void ChangedPolicySilentlyRequiresFullInspection()
    {
        using var stream = Document("piratepdf");
        var evaluator = new DocumentTrustEvaluator();

        DocumentTrustEvaluation first =
            DocumentTrustEvaluator.Evaluate(
                stream,
                null,
                PolicyVersion);

        var record =
            new DocumentTrustRecord(
                first.Sha256Fingerprint,
                PolicyVersion,
                DateTimeOffset.UnixEpoch);

        DocumentTrustEvaluation result =
            DocumentTrustEvaluator.Evaluate(
                stream,
                record,
                "BF-1049-v2");

        Assert.False(result.CanUseTrustedFastPath);
    }

    [Fact]
    public void ProducerMetadataCannotCreateTrust()
    {
        using var stream = Document("piratepdf");
        var evaluator = new DocumentTrustEvaluator();

        DocumentTrustEvaluation result =
            DocumentTrustEvaluator.Evaluate(
                stream,
                null,
                PolicyVersion,
                new DocumentTrustEvidence(
                    Producer: "PiratePDF"));

        Assert.False(result.CanUseTrustedFastPath);
    }

    [Fact]
    public void WriterMismatchInvalidatesOtherwiseCurrentTrust()
    {
        using var stream = Document("piratepdf");
        var evaluator = new DocumentTrustEvaluator();

        DocumentTrustEvaluation first =
            DocumentTrustEvaluator.Evaluate(
                stream,
                null,
                PolicyVersion);

        var record =
            new DocumentTrustRecord(
                first.Sha256Fingerprint,
                PolicyVersion,
                DateTimeOffset.UnixEpoch,
                "PiratePDF");

        DocumentTrustEvaluation result =
            DocumentTrustEvaluator.Evaluate(
                stream,
                record,
                PolicyVersion,
                new DocumentTrustEvidence(
                    WriterIdentity: "OtherWriter"));

        Assert.False(result.CanUseTrustedFastPath);
    }

    [Fact]
    public void EvaluationRestoresStreamPosition()
    {
        using var stream = Document("piratepdf");
        stream.Position = 3;

        var evaluator = new DocumentTrustEvaluator();

        _ = DocumentTrustEvaluator.Evaluate(
            stream,
            null,
            PolicyVersion);

        Assert.Equal(3, stream.Position);
    }

    [Fact]
    public async Task CancellationIsObservedBeforeHashing()
    {
        using var stream = Document("piratepdf");
        var evaluator = new DocumentTrustEvaluator();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => Task.FromResult(
                DocumentTrustEvaluator.Evaluate(
                    stream,
                    null,
                    PolicyVersion,
                    cancellationToken: cancellation.Token)));
    }

    [Fact]
    public void NonSeekableStreamIsRejected()
    {
        using var inner = Document("piratepdf");
        using var stream = new NonSeekableReadStream(inner);
        var evaluator = new DocumentTrustEvaluator();

        Assert.Throws<ArgumentException>(
            () => DocumentTrustEvaluator.Evaluate(
                stream,
                null,
                PolicyVersion));
    }

    private static MemoryStream Document(string text) =>
        new(Encoding.UTF8.GetBytes(text));

    private sealed class NonSeekableReadStream : Stream
    {
        private readonly Stream inner;

        public NonSeekableReadStream(Stream inner) =>
            this.inner = inner;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();

        public override int Read(
            byte[] buffer,
            int offset,
            int count) =>
            inner.Read(buffer, offset, count);

        public override long Seek(
            long offset,
            SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(
            byte[] buffer,
            int offset,
            int count) =>
            throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
