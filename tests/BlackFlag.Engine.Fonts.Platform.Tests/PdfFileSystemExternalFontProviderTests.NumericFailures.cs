using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BlackFlag.Engine.Fonts.Platform.Windows;
using BlackFlag.Engine.Fonts.Sanitization;

namespace BlackFlag.Engine.Fonts.Platform.Tests;

public sealed partial class PdfFileSystemExternalFontProviderTests
{
    [Fact]
    public void HostileLengthDoesNotPoisonMixedCatalog()
    {
        byte[] cmap = new byte[28];
        W16(cmap, 2, 1);
        W16(cmap, 4, 3);
        W16(cmap, 6, 10);
        W32(cmap, 8, 12);
        W16(cmap, 12, 12);
        W32(cmap, 16, uint.MaxValue);
        string badPath = Path.Combine(directory, "Bad.ttf");
        File.WriteAllBytes(badPath, BuildFont(cmap));
        File.WriteAllBytes(Path.Combine(directory, "Good.ttf"), BuildFont());
        var provider = new PdfWindowsExternalFontProvider([directory]);
        Exception first = Record.Exception(() => provider.GetFaces());
        File.Delete(badPath);
        Exception second = Record.Exception(() => provider.GetFaces());
        Assert.Null(first);
        Assert.Null(second);
        Assert.Single(provider.GetFaces());
    }

    [Theory]
    [InlineData("V")]
    [InlineData("B")]
    [InlineData("BV")]
    [InlineData("VB")]
    [InlineData("BBV")]
    [InlineData("UV")]
    [InlineData("VV")]
    public void CandidateFailureOrderDoesNotAffectCompleteSnapshot(string candidates)
    {
        string[] directories = new string[candidates.Length];
        for (int index = 0; index < candidates.Length; index++)
        {
            directories[index] = Path.Combine(directory, index.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Directory.CreateDirectory(directories[index]);
            byte[] bytes = candidates[index] switch
            {
                'V' => BuildFont(),
                'B' => BuildMalformedFont(),
                'U' => UnsupportedMappingFont(),
                _ => throw new ArgumentException(nameof(candidates))
            };
            if (candidates[index] == 'U')
            {
                using var source = new MemoryStream(bytes, writable: false);
                Assert.NotNull(new PdfOpenTypeFontSanitizer().Sanitize(source));
            }
            File.WriteAllBytes(Path.Combine(directories[index], "Candidate.ttf"), bytes);
        }

        // One candidate per explicitly ordered search directory avoids relying
        // on the filesystem's unspecified order within a directory.
        var provider = new PdfWindowsExternalFontProvider(directories.Concat(directories));
        var faces = provider.GetFaces();
        Assert.Equal(candidates.Count(value => value == 'V'), faces.Count);
        Assert.Equal(faces.Select(face => face.FaceId).Order(StringComparer.Ordinal), faces.Select(face => face.FaceId));
        foreach (PdfExternalFontFace face in faces)
        {
            Assert.Equal("Test Family", face.FamilyName);
            Assert.Equal("Test Family Regular", face.FaceName);
            Assert.Equal(400, face.Weight);
            Assert.True(provider.SupportsRune(face, new Rune('A')));
        }

        // Once built, the complete metadata snapshot is reused without retry.
        foreach (string candidateDirectory in directories)
        {
            File.Delete(Path.Combine(candidateDirectory, "Candidate.ttf"));
        }
        Assert.Equal(faces.Select(face => face.FaceId), provider.GetFaces().Select(face => face.FaceId));
    }

    [Theory]
    [InlineData("io", true)]
    [InlineData("domain", true)]
    [InlineData("unsupported", true)]
    [InlineData("cancellation", false)]
    [InlineData("programming", false)]
    [InlineData("memory", false)]
    public void ExistingFailureFilterRemainsNarrow(string kind, bool omitted)
    {
        File.WriteAllBytes(Path.Combine(directory, "Candidate.ttf"), BuildFont());
        Exception failure = kind switch
        {
            "io" => new IOException("Injected read failure"),
            "domain" => new PdfExternalFontSanitizationException(
                PdfExternalFontSanitizationFailure.MalformedRequiredTable, "Injected malformed table"),
            "unsupported" => new NotSupportedException("Injected unsupported candidate"),
            "cancellation" => new OperationCanceledException(),
            "programming" => new InvalidOperationException("Injected programming failure"),
            "memory" => new OutOfMemoryException("Injected exception; no excessive allocation"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var sanitizer = new CountingSanitizer(failure);
        var provider = new TestProvider(directory, sanitizer);
        if (omitted)
        {
            Assert.Empty(provider.GetFaces());
            Assert.Empty(provider.GetFaces());
        }
        else
        {
            Assert.Same(failure, Record.Exception(() => provider.GetFaces()));
            Assert.Same(failure, Record.Exception(() => provider.GetFaces()));
        }
        Assert.Equal(kind == "cancellation" ? 2 : 1, sanitizer.Calls);
    }

    [Fact]
    public void PreCancelledRequestDoesNotInitializeOrPoisonSnapshot()
    {
        File.WriteAllBytes(Path.Combine(directory, "Candidate.ttf"), BuildFont());
        var sanitizer = new CountingSanitizer();
        var provider = new TestProvider(directory, sanitizer);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var failure = Assert.Throws<OperationCanceledException>(() => provider.GetFaces(cancellation.Token));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(0, sanitizer.Calls);
        Assert.Single(provider.GetFaces());
        Assert.Equal(1, sanitizer.Calls);
    }

    private static byte[] BuildMalformedFont()
    {
        byte[] cmap = new byte[28];
        W16(cmap, 2, 1);
        W16(cmap, 4, 3);
        W16(cmap, 6, 10);
        W32(cmap, 8, 12);
        W16(cmap, 12, 12);
        W32(cmap, 16, uint.MaxValue);
        return BuildFont(cmap);
    }

    private static byte[] UnsupportedMappingFont()
    {
        // Format 13 is accepted structurally by the sanitizer, but the
        // existing metadata mapper supports only formats 0, 4 and 12.
        byte[] cmap = new byte[28];
        W16(cmap, 2, 1);
        W16(cmap, 6, 6);
        W32(cmap, 8, 12);
        W16(cmap, 12, 13);
        W32(cmap, 16, 16);
        return BuildFont(cmap);
    }

    private sealed class TestProvider(string path, IPdfExternalFontSanitizer sanitizer)
        : PdfFileSystemExternalFontProvider("numeric-tests", [path], sanitizer);

    private sealed class CountingSanitizer(Exception failure = null) : IPdfExternalFontSanitizer
    {
        public int Calls { get; private set; }

        public PdfSanitizedExternalFontProgram Sanitize(
            Stream untrustedFontProgram,
            PdfExternalFontSanitizationOptions options = null,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            if (failure is not null) throw failure;
            return new PdfOpenTypeFontSanitizer().Sanitize(untrustedFontProgram, options, cancellationToken);
        }
    }
}
