using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class FindingMinimizationTests
{
    [Fact]
    public async Task SmallerOwnedCandidatePreservesClassWithinFourExecutions()
    {
        using var fixture = new FindingFixture();
        FindingResult original = await fixture.Retain();
        byte[] originalBytes = File.ReadAllBytes(fixture.FilePath("case-one"));
        await fixture.Retain("smaller", "F"u8.ToArray(), FindingFixture.Source() with { ParentSha256 = original.Metadata.Sha256 });
        FindingResult result = await FindingMinimization.CompareAsync(fixture.Store, "case-one", "smaller", FindingFixture.Supervisor());
        Assert.Equal(FindingExit.Success, result.ExitCode);
        Assert.Equal(FindingClass.UnexpectedManagedFailure, result.Classification);
        Assert.Equal(4, File.ReadAllLines(Path.Combine(fixture.Repository, "finding-executions.log")).Length);
        Assert.Equal(originalBytes, File.ReadAllBytes(fixture.FilePath("case-one")));
        Assert.All(Directory.EnumerateDirectories(fixture.RunPath), directory => Assert.Contains(Path.GetFileName(directory), new[] { "case-one", "smaller" }));
        Assert.Equal(4, FindingMinimization.MaximumExecutions);
        Assert.Equal(262144, FindingMinimization.MaximumProcessedBytes);
    }

    [Theory]
    [InlineData("rejected")]
    [InlineData("success")]
    [InlineData("larger")]
    [InlineData("unrelated")]
    [InlineData("wrong-parent")]
    [InlineData("uncharacterized")]
    public async Task ReductionCannotReplaceFailureWithDifferentMeaning(string fault)
    {
        using var fixture = new FindingFixture();
        FindingResult original = await fixture.Retain();
        FindingSource source = FindingFixture.Source() with { ParentSha256 = original.Metadata.Sha256 };
        byte[] bytes = "F"u8.ToArray();
        switch (fault)
        {
            case "rejected": bytes = "R"u8.ToArray(); break;
            case "success": bytes = "S"u8.ToArray(); break;
            case "larger": bytes = "FFFF"u8.ToArray(); break;
            case "unrelated": source = source with { Related = "OTHER-FINDING" }; break;
            case "wrong-parent": source = source with { ParentSha256 = new string('D', 64) }; break;
            case "uncharacterized": source = source with { Related = "" }; break;
        }
        await fixture.Retain("smaller", bytes, source);
        Assert.Equal(FindingExit.MinimizationRefused,
            (await FindingMinimization.CompareAsync(fixture.Store, "case-one", "smaller", FindingFixture.Supervisor())).ExitCode);
        Assert.False(Directory.Exists(Path.Combine(fixture.RunPath, "proposal")));
    }
}
