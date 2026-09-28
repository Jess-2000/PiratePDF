using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackFlag.Engine.Fonts.Sanitization;

namespace BlackFlag.Engine.Fonts.Platform.Tests;

public sealed partial class PdfFileSystemExternalFontProviderTests
{
    [Theory]
    [InlineData("enumeration")]
    [InlineData("before-sanitize")]
    [InlineData("after-sanitize")]
    [InlineData("between-candidates")]
    [InlineData("before-publication")]
    public void CancelledBuildPublishesNothingAndCanRetry(string boundary)
    {
        string[] paths = WriteOrderedCandidates(2);
        using var cancellation = new CancellationTokenSource();
        bool cancel = true;
        var sanitizer = new CallbackSanitizer((call, token, after) =>
        {
            if (!cancel) return;
            if ((boundary == "before-sanitize" && !after) ||
                (boundary == "after-sanitize" && after) ||
                (boundary == "before-publication" && call == 2 && after))
                cancellation.Cancel();
        });
        int candidates = 0;
        var provider = new CallbackProvider(paths, sanitizer, _ =>
        {
            candidates++;
            if (cancel && (boundary == "enumeration" ||
                (boundary == "between-candidates" && candidates == 2)))
                cancellation.Cancel();
        });

        var failure = Assert.Throws<OperationCanceledException>(() => provider.GetFaces(cancellation.Token));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        cancel = false;
        // A newly added face distinguishes a full retry from partial or cached publication.
        File.WriteAllBytes(Path.Combine(paths[0], "Additional.ttf"), BuildFont());
        Assert.Equal(3, provider.GetFaces().Count);
        AssertFilesReleased(paths);
    }

    [Fact]
    public void SanitizerCancellationIsNotCached()
    {
        string[] paths = WriteOrderedCandidates(1);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var sanitizer = new CallbackSanitizer((call, _, after) =>
        {
            if (call == 1 && !after) throw new OperationCanceledException(cancellation.Token);
        });
        var provider = new CallbackProvider(paths, sanitizer);
        Assert.Equal(cancellation.Token,
            Assert.Throws<OperationCanceledException>(() => provider.GetFaces()).CancellationToken);
        Assert.Single(provider.GetFaces());
        Assert.Equal(2, sanitizer.Calls);
        AssertFilesReleased(paths);
    }

    [Fact]
    public void WarmPreCancelledCallPreservesCompletedSnapshot()
    {
        string[] paths = WriteOrderedCandidates(1);
        var sanitizer = new CallbackSanitizer();
        var provider = new CallbackProvider(paths, sanitizer);
        var faces = provider.GetFaces();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => provider.GetFaces(cancellation.Token));
        Assert.Equal(faces, provider.GetFaces());
        Assert.Equal(1, sanitizer.Calls);
    }

    [Fact]
    public async Task ConcurrentCallersShareOneCompleteBuild()
    {
        string[] paths = WriteOrderedCandidates(2);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var sanitizer = new CallbackSanitizer((call, _, after) =>
        {
            if (call != 1 || after) return;
            entered.Set();
            Assert.True(release.Wait(TimeSpan.FromSeconds(20)));
        });
        var provider = new CallbackProvider(paths, sanitizer);
        Task<IReadOnlyList<PdfExternalFontFace>> owner = Task.Run(() => provider.GetFaces());
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(20)));
            Task<IReadOnlyList<PdfExternalFontFace>> waiter = Task.Run(() => provider.GetFaces());
            release.Set();
            var results = await Task.WhenAll(owner, waiter).WaitAsync(TimeSpan.FromSeconds(20));
            Assert.Equal(2, sanitizer.Calls);
            Assert.Equal(2, results[0].Count);
            Assert.Equal(results[0], results[1]);
        }
        finally
        {
            release.Set();
            await owner.WaitAsync(TimeSpan.FromSeconds(20));
        }
    }

    private string[] WriteOrderedCandidates(int count)
    {
        string[] paths = new string[count];
        for (int index = 0; index < count; index++)
        {
            paths[index] = Path.Combine(directory, index.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Directory.CreateDirectory(paths[index]);
            File.WriteAllBytes(Path.Combine(paths[index], "Candidate.ttf"), BuildFont());
        }
        return paths;
    }

    private static void AssertFilesReleased(IEnumerable<string> paths)
    {
        foreach (string path in paths.SelectMany(Directory.EnumerateFiles))
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.True(stream.CanRead);
        }
    }

    private sealed class CallbackProvider(
        IEnumerable<string> paths,
        IPdfExternalFontSanitizer sanitizer,
        Action<string> candidate = null)
        : PdfFileSystemExternalFontProvider("responsiveness-tests", paths, sanitizer)
    {
        protected override bool IsSupportedFontFile(string path)
        {
            candidate?.Invoke(path);
            return base.IsSupportedFontFile(path);
        }
    }

    private sealed class CallbackSanitizer(Action<int, CancellationToken, bool> callback = null)
        : IPdfExternalFontSanitizer
    {
        public int Calls { get; private set; }

        public PdfSanitizedExternalFontProgram Sanitize(Stream untrustedFontProgram,
            PdfExternalFontSanitizationOptions options = null, CancellationToken cancellationToken = default)
        {
            Calls++;
            callback?.Invoke(Calls, cancellationToken, false);
            var program = new PdfOpenTypeFontSanitizer().Sanitize(untrustedFontProgram, options, cancellationToken);
            callback?.Invoke(Calls, cancellationToken, true);
            return program;
        }
    }
}
