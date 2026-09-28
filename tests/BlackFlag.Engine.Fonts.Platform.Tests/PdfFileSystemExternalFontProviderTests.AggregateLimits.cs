using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BlackFlag.Engine.Fonts.Platform.MacOS;
using BlackFlag.Engine.Fonts.Platform.Windows;

namespace BlackFlag.Engine.Fonts.Platform.Tests;

public sealed partial class PdfFileSystemExternalFontProviderTests
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void CandidateBudgetCountsDistinctSupportedPathsIncludingMalformed(int count, bool malformed)
    {
        string[] paths = WriteOrderedCandidates(count);
        if (malformed) File.WriteAllBytes(Path.Combine(paths[0], "Candidate.ttf"), BuildMalformedFont());
        File.WriteAllText(Path.Combine(paths[0], "Ignored.txt"), "not a candidate");
        var limits = new PdfExternalFontCatalogLimits { MaximumCandidates = 2 };
        var sanitizer = new CallbackSanitizer();
        var provider = new PdfWindowsExternalFontProvider(paths.Concat(paths), sanitizer, limits);
        if (count <= 2)
        {
            Assert.Equal(count - (malformed ? 1 : 0), provider.GetFaces().Count);
            Assert.Equal(count, sanitizer.Calls);
        }
        else
        {
            AssertLimit(provider, nameof(limits.MaximumCandidates), 2);
            Assert.Equal(2, sanitizer.Calls);
            AssertLimit(provider, nameof(limits.MaximumCandidates), 2);
            Assert.Equal(4, sanitizer.Calls);
            File.Delete(Path.Combine(paths[2], "Candidate.ttf"));
            File.Move(Path.Combine(paths[1], "Candidate.ttf"), Path.Combine(paths[1], "Renamed.ttf"));
            AssertMatchesControl(provider, paths);
        }
        AssertFilesReleased(paths);
    }

    [Theory]
    [InlineData(1, false, false)]
    [InlineData(2, false, false)]
    [InlineData(3, false, false)]
    [InlineData(1, true, false)]
    [InlineData(2, true, false)]
    [InlineData(3, true, false)]
    [InlineData(1, false, true)]
    [InlineData(2, false, true)]
    [InlineData(3, false, true)]
    public void DirectoryEntryBudgetAlsoBoundsNonFonts(int count, bool nonFonts, bool malformed)
    {
        string[] paths = WriteOrderedCandidates(count);
        foreach (string path in paths)
        {
            if (nonFonts) File.Move(Path.Combine(path, "Candidate.ttf"), Path.Combine(path, "Ignored.txt"));
            if (malformed) File.WriteAllBytes(Path.Combine(path, "Candidate.ttf"), BuildMalformedFont());
        }
        var limits = new PdfExternalFontCatalogLimits { MaximumDirectoryEntries = 2 };
        var sanitizer = new CallbackSanitizer();
        var provider = new PdfWindowsExternalFontProvider(paths, sanitizer, limits);
        if (count <= 2) Assert.Equal(nonFonts || malformed ? 0 : count, provider.GetFaces().Count);
        else
        {
            AssertLimit(provider, nameof(limits.MaximumDirectoryEntries), 2);
            Assert.Equal(nonFonts ? 0 : 2, sanitizer.Calls);
            AssertLimit(provider, nameof(limits.MaximumDirectoryEntries), 2);
            File.Delete(Directory.GetFiles(paths[2]).Single());
            AssertMatchesControl(provider, paths);
        }
        AssertFilesReleased(paths);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void DirectoryEntryBudgetCountsSubdirectoriesWithoutRecursing(int count)
    {
        string[] paths = WriteOrderedCandidates(count);
        var provider = new PdfWindowsExternalFontProvider([directory], null,
            new PdfExternalFontCatalogLimits { MaximumDirectoryEntries = 2 });
        if (count <= 2) Assert.Empty(provider.GetFaces());
        else
        {
            AssertLimit(provider, nameof(PdfExternalFontCatalogLimits.MaximumDirectoryEntries), 2);
            File.Delete(Path.Combine(paths[2], "Candidate.ttf"));
            Directory.Delete(paths[2]);
            Assert.Empty(provider.GetFaces());
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void SearchDirectoryBudgetCountsSuppliedValuesBeforeDeduplication(int count)
    {
        var limits = new PdfExternalFontCatalogLimits { MaximumSearchDirectories = 2 };
        int enumerated = 0;
        IEnumerable<string> Directories()
        {
            while (true)
            {
                enumerated++;
                yield return enumerated == 1 ? directory : " ";
            }
        }

        if (count <= 2)
        {
            var provider = new PdfWindowsExternalFontProvider(Directories().Take(count), null, limits);
            Assert.Empty(provider.GetFaces());
        }
        else
        {
            var failure = Assert.Throws<PdfExternalFontCatalogLimitException>(() =>
                new PdfWindowsExternalFontProvider(Directories(), null, limits));
            Assert.Equal(nameof(limits.MaximumSearchDirectories), failure.LimitName);
        }
        Assert.Equal(count, enumerated);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void SourceByteBudgetAccumulatesAcrossValidAndMalformedFonts(int spareBytes, bool malformed)
    {
        string[] paths = WriteOrderedCandidates(2);
        if (malformed) File.WriteAllBytes(Path.Combine(paths[0], "Candidate.ttf"), BuildMalformedFont());
        long total = paths.Sum(path => new FileInfo(Path.Combine(path, "Candidate.ttf")).Length);
        var limits = new PdfExternalFontCatalogLimits { MaximumSourceBytes = total + spareBytes };
        var sanitizer = new CallbackSanitizer();
        var provider = new PdfWindowsExternalFontProvider(paths, sanitizer, limits);
        if (spareBytes >= 0) Assert.Equal(malformed ? 1 : 2, provider.GetFaces().Count);
        else
        {
            AssertLimit(provider, nameof(limits.MaximumSourceBytes), total - 1);
            Assert.Equal(1, sanitizer.Calls); // Second file rejected by length before sanitizer work.
            AssertFilesReleased(paths);
            AssertLimit(provider, nameof(limits.MaximumSourceBytes), total - 1);
            File.Delete(Path.Combine(paths[0], "Candidate.ttf"));
            File.Move(Path.Combine(paths[1], "Candidate.ttf"), Path.Combine(paths[1], "Renamed.ttf"));
            AssertMatchesControl(provider, paths);
        }
        AssertFilesReleased(paths);
    }

    [Fact]
    public void SingleOversizedSourceIsRejectedBeforeSanitization()
    {
        string[] paths = WriteOrderedCandidates(1);
        var sanitizer = new CallbackSanitizer();
        var provider = new PdfWindowsExternalFontProvider(paths, sanitizer,
            new PdfExternalFontCatalogLimits { MaximumSourceBytes = 1 });
        AssertLimit(provider, nameof(PdfExternalFontCatalogLimits.MaximumSourceBytes), 1);
        Assert.Equal(0, sanitizer.Calls);
        AssertFilesReleased(paths);
    }

    [Fact]
    public void EmptyAndTinyMalformedFilesConsumeActualBytesAndCandidateUnits()
    {
        string[] paths = WriteOrderedCandidates(2);
        File.WriteAllBytes(Path.Combine(paths[0], "Candidate.ttf"), []);
        File.WriteAllBytes(Path.Combine(paths[1], "Candidate.ttf"), [0]);
        var sanitizer = new CallbackSanitizer();
        var provider = new PdfWindowsExternalFontProvider(paths, sanitizer,
            new PdfExternalFontCatalogLimits { MaximumCandidates = 2, MaximumSourceBytes = 1 });
        Assert.Empty(provider.GetFaces());
        Assert.Equal(2, sanitizer.Calls);
        AssertFilesReleased(paths);
    }

    [Theory]
    [InlineData("directories", 0)]
    [InlineData("directories", -1)]
    [InlineData("entries", 0)]
    [InlineData("entries", -1)]
    [InlineData("candidates", 0)]
    [InlineData("candidates", -1)]
    [InlineData("bytes", 0)]
    [InlineData("bytes", -1)]
    public void LimitsRejectNonpositiveValuesBeforeDirectoryEnumeration(string kind, int value)
    {
        var limits = new PdfExternalFontCatalogLimits
        {
            MaximumSearchDirectories = kind == "directories" ? value : 64,
            MaximumDirectoryEntries = kind == "entries" ? value : 65_536,
            MaximumCandidates = kind == "candidates" ? value : 4_096,
            MaximumSourceBytes = kind == "bytes" ? value : 1L << 30
        };
        IEnumerable<string> UnexpectedEnumeration()
        {
            Assert.Fail("Options must be checked first.");
            yield return directory;
        }
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PdfWindowsExternalFontProvider(UnexpectedEnumeration(), null, limits));
    }

    [Theory]
    [InlineData("entries")]
    [InlineData("candidates")]
    [InlineData("bytes")]
    public void CancellationWinsAtNextBoundaryBeforeAggregateExhaustion(string kind)
    {
        string[] paths = WriteOrderedCandidates(2);
        using var cancellation = new CancellationTokenSource();
        var sanitizer = new CallbackSanitizer((call, _, after) =>
        {
            if (call == 1 && after) cancellation.Cancel();
        });
        var limits = new PdfExternalFontCatalogLimits
        {
            MaximumDirectoryEntries = kind == "entries" ? 1 : 65_536,
            MaximumCandidates = kind == "candidates" ? 1 : 4_096,
            MaximumSourceBytes = kind == "bytes" ? BuildFont().Length : 1L << 30
        };
        var provider = new PdfWindowsExternalFontProvider(paths, sanitizer, limits);
        Assert.Equal(cancellation.Token,
            Assert.Throws<OperationCanceledException>(() => provider.GetFaces(cancellation.Token)).CancellationToken);
        Assert.Equal(1, sanitizer.Calls);
        Assert.Throws<PdfExternalFontCatalogLimitException>(() => provider.GetFaces());
        AssertFilesReleased(paths);
    }

    [Fact]
    public void MacAdapterUsesTheSameExplicitLimitsAndMetadata()
    {
        string[] paths = WriteOrderedCandidates(2);
        var provider = new PdfMacOSExternalFontProvider(paths, null,
            new PdfExternalFontCatalogLimits { MaximumCandidates = 1 });
        AssertLimit(provider, nameof(PdfExternalFontCatalogLimits.MaximumCandidates), 1);
        File.Delete(Path.Combine(paths[1], "Candidate.ttf"));
        var face = Assert.Single(provider.GetFaces());
        Assert.Equal("Test Family", face.FamilyName);
        Assert.Equal(PdfMacOSExternalFontProvider.ProviderId, face.ProviderId);
    }

    private static void AssertLimit(PdfFileSystemExternalFontProvider provider, string name, long limit)
    {
        var failure = Assert.Throws<PdfExternalFontCatalogLimitException>(() => provider.GetFaces());
        Assert.Equal(name, failure.LimitName);
        Assert.Equal(limit, failure.Limit);
        Assert.Contains(name, failure.Message, StringComparison.Ordinal);
    }

    private static void AssertMatchesControl(PdfFileSystemExternalFontProvider provider, string[] paths)
    {
        var expected = new PdfWindowsExternalFontProvider(paths).GetFaces();
        var actual = provider.GetFaces();
        Assert.Equal(expected.Count, actual.Count);
        for (int index = 0; index < expected.Count; index++)
            Assert.Equivalent(expected[index], actual[index], strict: true);
        Assert.Equal(actual, provider.GetFaces());
    }
}
