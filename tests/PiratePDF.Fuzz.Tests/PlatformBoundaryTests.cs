using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class PlatformBoundaryTests
{
    [Fact]
    public async Task CorpusAdmissionReportsUnsupportedPlatformBeforeAnyUnixFileOpen()
    {
        using var fixture = new ReplayFixture();
        File.Delete(fixture.ManifestPath);
        CorpusValidationResult result = await CorpusValidator.ValidateAsync(fixture.Repository);
        Assert.Equal(OperatingSystem.IsWindows() ? CorpusFailure.MissingManifest : CorpusFailure.UnsupportedPlatform, result.Failure);
        Assert.Empty(result.Seeds);
    }

    [Fact]
    public async Task FindingStoreRetainsItsExplicitWindowsAdmissionBoundary()
    {
        using var fixture = new ReplayFixture();
        string[] args = ["reproduce-finding", "--repository-root", fixture.Repository, "--run", "owned-run", "--finding", "missing-case"];
        Assert.True(FindingCommand.IsValidInvocation(args));
        FindingResult result = await FindingCommand.ExecuteAsync(args);
        Assert.Equal(OperatingSystem.IsWindows() ? FindingExit.IntegrityFailure : FindingExit.InvalidInvocation, result.ExitCode);
        Assert.False(File.Exists(Path.Combine(fixture.Repository, "finding-executions.log")));
    }
}
