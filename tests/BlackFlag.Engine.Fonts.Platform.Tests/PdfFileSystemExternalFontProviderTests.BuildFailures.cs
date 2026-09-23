using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackFlag.Engine.Fonts.Platform.Windows;
using BlackFlag.Engine.Fonts.Sanitization;

namespace BlackFlag.Engine.Fonts.Platform.Tests;

public sealed partial class PdfFileSystemExternalFontProviderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallerCancellationDoesNotPoisonAnotherConcurrentCaller(bool cancelOwner)
    {
        string[] paths = WriteOrderedCandidates(2);
        using var ownerCancellation = new CancellationTokenSource();
        using var waiterCancellation = new CancellationTokenSource();
        using var entered = new ManualResetEventSlim();
        using var waiterStarted = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var sanitizer = new CallbackSanitizer((call, _, after) =>
        {
            if (call != 1 || after) return;
            entered.Set();
            Assert.True(release.Wait(TimeSpan.FromSeconds(20)));
        });
        var provider = new CallbackProvider(paths, sanitizer);
        IReadOnlyList<PdfExternalFontFace> ownerFaces = null;
        IReadOnlyList<PdfExternalFontFace> waiterFaces = null;
        Task<Exception> owner = Task.Run(() => Record.Exception(() => ownerFaces = provider.GetFaces(ownerCancellation.Token)));
        Task<Exception> waiter = null;
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(20)));
            waiter = Task.Run(() =>
            {
                waiterStarted.Set();
                return Record.Exception(() => waiterFaces = provider.GetFaces(waiterCancellation.Token));
            });
            Assert.True(waiterStarted.Wait(TimeSpan.FromSeconds(20)));
            if (cancelOwner)
            {
                ownerCancellation.Cancel();
                release.Set();
                var failure = Assert.IsType<OperationCanceledException>(await owner.WaitAsync(TimeSpan.FromSeconds(20)));
                Assert.Equal(ownerCancellation.Token, failure.CancellationToken);
                Assert.Null(await waiter.WaitAsync(TimeSpan.FromSeconds(20)));
                Assert.Equal(2, waiterFaces.Count);
                Assert.Equal(3, sanitizer.Calls);
            }
            else
            {
                waiterCancellation.Cancel();
                // The waiting caller must return while the owner is still held.
                var failure = Assert.IsType<OperationCanceledException>(await waiter.WaitAsync(TimeSpan.FromSeconds(20)));
                Assert.Equal(waiterCancellation.Token, failure.CancellationToken);
                Assert.False(owner.IsCompleted);
                release.Set();
                Assert.Null(await owner.WaitAsync(TimeSpan.FromSeconds(20)));
                Assert.Equal(2, ownerFaces.Count);
                Assert.Equal(2, sanitizer.Calls);
            }
            Assert.Equal(2, provider.GetFaces().Count);
            AssertFilesReleased(paths);
        }
        finally
        {
            release.Set();
            await owner.WaitAsync(TimeSpan.FromSeconds(20));
            if (waiter is not null) await waiter.WaitAsync(TimeSpan.FromSeconds(20));
        }
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("directory-disappears")]
    [InlineData("sharing")]
    [InlineData("unauthorized")]
    [InlineData("io")]
    [InlineData("invalid-data")]
    [InlineData("unsupported")]
    public void CandidateIoAndSupportedFailuresStillAllowLaterValidFonts(string failure)
    {
        string[] paths = WriteOrderedCandidates(2);
        FileStream exclusive = null;
        int candidates = 0;
        var sanitizer = new CallbackSanitizer((call, _, after) =>
        {
            if (call != 1 || after) return;
            switch (failure)
            {
                case "unauthorized": throw new UnauthorizedAccessException("Injected access denial");
                case "io": throw new IOException("Injected IO failure");
                case "invalid-data": throw new InvalidDataException("Injected invalid data");
                case "unsupported": throw new NotSupportedException("Injected unsupported font");
            }
        });
        var provider = new CallbackProvider(paths, sanitizer, path =>
        {
            if (++candidates != 1) return;
            switch (failure)
            {
                case "missing": File.Delete(path); break;
                case "directory-disappears": File.Delete(path); Directory.Delete(paths[0]); break;
                case "sharing": exclusive = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None); break;
            }
        });
        try
        {
            Assert.Single(provider.GetFaces());
            Assert.Single(provider.GetFaces());
        }
        finally
        {
            exclusive?.Dispose();
        }
        AssertFilesReleased(paths.Where(Directory.Exists));
    }

    [Fact]
    public void CancellationAfterMalformedCandidateIsNotOmitted()
    {
        string[] paths = WriteOrderedCandidates(2);
        using var cancellation = new CancellationTokenSource();
        var sanitizer = new CallbackSanitizer((call, _, after) =>
        {
            if (call != 1 || after) return;
            cancellation.Cancel();
            throw new PdfExternalFontSanitizationException(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable, "Injected malformed font");
        });
        var provider = new CallbackProvider(paths, sanitizer);
        Assert.Throws<OperationCanceledException>(() => provider.GetFaces(cancellation.Token));
        Assert.Equal(1, sanitizer.Calls);
        Assert.Equal(2, provider.GetFaces().Count);
        AssertFilesReleased(paths);
    }

    [Fact]
    public void AggregateReadFailureCannotBeTranslatedIntoMalformedOmission()
    {
        string[] paths = WriteOrderedCandidates(1);
        long length = BuildFont().Length;
        var provider = new PdfWindowsExternalFontProvider(paths, new RereadingSanitizer(),
            new PdfExternalFontCatalogLimits { MaximumSourceBytes = length });
        AssertLimit(provider, nameof(PdfExternalFontCatalogLimits.MaximumSourceBytes), length);
        AssertLimit(provider, nameof(PdfExternalFontCatalogLimits.MaximumSourceBytes), length);
        AssertFilesReleased(paths);
    }

    [Fact]
    public void CatalogMatchingPropagatesCancellationAndRetriesWithoutChangingRanking()
    {
        string[] paths = WriteOrderedCandidates(2);
        using var cancellation = new CancellationTokenSource();
        var sanitizer = new CallbackSanitizer((call, _, after) =>
        {
            if (call == 1 && after) cancellation.Cancel();
        });
        var provider = new PdfWindowsExternalFontProvider(paths, sanitizer);
        var catalog = new PdfExternalFontCatalog([provider]);
        var request = new PdfExternalFontMatchRequest("Test Family");
        Assert.Throws<OperationCanceledException>(() => catalog.Match(request, "A", cancellation.Token));
        var actual = catalog.Match(request, "A");
        var expected = new PdfExternalFontCatalog([new PdfWindowsExternalFontProvider(paths)]).Match(request, "A");
        Assert.Equal(2, actual.Count);
        for (int index = 0; index < expected.Count; index++)
            Assert.Equivalent(expected[index].Face, actual[index].Face, strict: true);
    }

    [Fact]
    public void RecursiveCatalogAccessStillFailsRatherThanRecursingOrPublishing()
    {
        string[] paths = WriteOrderedCandidates(1);
        CallbackProvider provider = null;
        var sanitizer = new CallbackSanitizer((_, _, _) => provider.GetFaces());
        provider = new CallbackProvider(paths, sanitizer);
        var failure = Assert.Throws<InvalidOperationException>(() => provider.GetFaces());
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => provider.GetFaces()));
        Assert.Equal(1, sanitizer.Calls);
        AssertFilesReleased(paths);
    }

    private sealed class RereadingSanitizer : IPdfExternalFontSanitizer
    {
        public PdfSanitizedExternalFontProgram Sanitize(Stream untrustedFontProgram,
            PdfExternalFontSanitizationOptions options = null, CancellationToken cancellationToken = default)
        {
            var result = new PdfOpenTypeFontSanitizer().Sanitize(untrustedFontProgram, options, cancellationToken);
            untrustedFontProgram.Position = 0;
            try
            {
                _ = untrustedFontProgram.ReadByte();
            }
            catch (PdfExternalFontCatalogLimitException)
            {
                throw new InvalidDataException("A custom sanitizer cannot hide catalog exhaustion.");
            }
            return result;
        }
    }
}
