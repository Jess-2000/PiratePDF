using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class FindingReproductionTests
{
    [Theory]
    [InlineData("S", FindingClass.Success, FindingExit.Success)]
    [InlineData("R", FindingClass.ExpectedRejection, FindingExit.Success)]
    [InlineData("F", FindingClass.UnexpectedManagedFailure, FindingExit.ReproducedFailure)]
    [InlineData("A", FindingClass.AbnormalExit, FindingExit.ReproducedFailure)]
    [InlineData("P", FindingClass.WorkerProtocolFailure, FindingExit.ReproducedFailure)]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task TwoFreshWorkersAgreeWithoutRewritingOriginal(string input, FindingClass expected, FindingExit exit)
    {
        using var fixture = new FindingFixture();
        await fixture.Retain(bytes: Encoding.ASCII.GetBytes(input), source: FindingFixture.Source(expected));
        byte[] original = File.ReadAllBytes(fixture.FilePath("case-one", "metadata.json"));
        FindingResult result = await fixture.Reproduce();
        Assert.Equal(exit, result.ExitCode);
        Assert.Equal(expected, result.Classification);
        Assert.True(result.Agrees);
        Assert.True(result.MatchesDiscovery);
        Assert.Equal(2, result.Attempts.Count);
        Assert.Equal(new[] { 1, 2 }, result.Attempts.Select(attempt => attempt.Attempt));
        Assert.Equal(original, File.ReadAllBytes(fixture.FilePath("case-one", "metadata.json")));
        Assert.Equal(2, File.ReadAllLines(Path.Combine(fixture.Repository, "finding-executions.log")).Length);
        Assert.DoesNotContain("PRIVATE", result.Json, StringComparison.Ordinal);
        Assert.DoesNotContain(fixture.Repository, result.Json, StringComparison.Ordinal);
        Assert.InRange(Encoding.UTF8.GetByteCount(result.Json), 1, 1536);
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task AlternatingResultsAreNotReproducible()
    {
        using var fixture = new FindingFixture();
        await fixture.Retain(bytes: "I"u8.ToArray());
        FindingResult result = await fixture.Reproduce();
        Assert.Equal(FindingExit.NonReproducible, result.ExitCode);
        Assert.Equal(FindingClass.NonReproducible, result.Classification);
        Assert.False(result.Agrees);
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task StableDifferentOutcomeDoesNotConfirmDiscovery()
    {
        using var fixture = new FindingFixture();
        await fixture.Retain(bytes: "S"u8.ToArray());
        FindingResult result = await fixture.Reproduce();
        Assert.Equal(FindingExit.NonReproducible, result.ExitCode);
        Assert.Equal(FindingClass.Success, result.Classification);
        Assert.True(result.Agrees);
        Assert.False(result.MatchesDiscovery);
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task ChangedMetadataBetweenWorkersStopsNextLaunch()
    {
        using var fixture = new FindingFixture();
        await fixture.Retain(bytes: "M"u8.ToArray());
        FindingResult result = await fixture.Reproduce();
        Assert.Equal(FindingExit.IntegrityFailure, result.ExitCode);
        Assert.Single(result.Attempts);
        Assert.Single(File.ReadAllLines(Path.Combine(fixture.Repository, "finding-executions.log")));
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task ReproductionLedgerIsCreateNewAndBounded()
    {
        using var fixture = new FindingFixture();
        await fixture.Retain();
        FindingResult first = await fixture.Reproduce();
        byte[] evidence = File.ReadAllBytes(fixture.FilePath("case-one", "reproduction-01.json"));
        for (int index = 1; index < 4; index++) Assert.Equal(first.Json, (await fixture.Reproduce()).Json);
        Assert.Equal(evidence, File.ReadAllBytes(fixture.FilePath("case-one", "reproduction-01.json")));
        Assert.Equal(FindingExit.LimitExceeded, (await fixture.Reproduce()).ExitCode);
        Assert.Equal(8, File.ReadAllLines(Path.Combine(fixture.Repository, "finding-executions.log")).Length);
        Assert.Equal(6, Directory.EnumerateFiles(Path.GetDirectoryName(fixture.FilePath("case-one"))).Count());
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task ExistingMalformedEvidenceIsNeverOverwritten()
    {
        using var fixture = new FindingFixture();
        await fixture.Retain();
        string path = fixture.FilePath("case-one", "reproduction-01.json");
        File.WriteAllText(path, "original evidence");
        Assert.Equal(FindingExit.IntegrityFailure, (await fixture.Reproduce()).ExitCode);
        Assert.Equal("original evidence", File.ReadAllText(path));
        Assert.False(File.Exists(Path.Combine(fixture.Repository, "finding-executions.log")));
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task TimeoutRetainsSeparateClassificationAndReapsWorkers()
    {
        using var fixture = new FindingFixture();
        await fixture.Retain(bytes: "T"u8.ToArray(), source: FindingFixture.Source(FindingClass.Timeout));
        FindingResult result = await fixture.Reproduce(supervisor: FindingFixture.Supervisor(TimeSpan.FromSeconds(2)));
        Assert.Equal(FindingClass.Timeout, result.Classification);
        Assert.True(result.Agrees);
        AssertChildExited(fixture.Repository);
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task CancellationAfterChildEntryIsNotRejection()
    {
        using var fixture = new FindingFixture();
        await fixture.Retain(bytes: "T"u8.ToArray());
        using var cancellation = new CancellationTokenSource();
        Task<FindingResult> running = new FindingReproduction(FindingFixture.Supervisor()).RunAsync(fixture.Store, "case-one", cancellation.Token);
        string identity = Path.Combine(fixture.Repository, "finding-child.pid");
        var clock = Stopwatch.StartNew();
        while (!File.Exists(identity) && clock.Elapsed < TimeSpan.FromSeconds(5)) await Task.Delay(20);
        Assert.True(File.Exists(identity));
        cancellation.Cancel();
        FindingResult result = await running;
        Assert.Equal(FindingExit.Cancelled, result.ExitCode);
        Assert.Equal(FindingClass.Cancellation, result.Classification);
        Assert.Single(result.Attempts);
        AssertChildExited(fixture.Repository);
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task PreCancellationDoesNotLaunch()
    {
        using var fixture = new FindingFixture();
        await fixture.Retain();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        FindingResult result = await new FindingReproduction(FindingFixture.Supervisor()).RunAsync(fixture.Store, "case-one", cancellation.Token);
        Assert.Equal(FindingExit.Cancelled, result.ExitCode);
        Assert.Empty(result.Attempts);
        Assert.False(File.Exists(Path.Combine(fixture.Repository, "finding-executions.log")));
    }

    [Theory]
    [InlineData(SupervisionStatus.TerminationFailed, null, null, FindingClass.CleanupFailure)]
    [InlineData(SupervisionStatus.StartFailed, null, null, FindingClass.Indeterminate)]
    [InlineData(SupervisionStatus.CaptureFailed, null, null, FindingClass.Indeterminate)]
    [InlineData(SupervisionStatus.InvalidRequest, null, null, FindingClass.Indeterminate)]
    [InlineData(SupervisionStatus.WorkerFailure, 3, "Cancelled", FindingClass.Indeterminate)]
    [InlineData(SupervisionStatus.WorkerFailure, 4, "UnexpectedRunnerFailure", FindingClass.Indeterminate)]
    [InlineData(SupervisionStatus.Completed, 2, "InputUnavailable", FindingClass.Indeterminate)]
    public void InfrastructureIsNotAnEngineDefect(SupervisionStatus status, int? exit, string code, FindingClass expected) =>
        Assert.Equal(expected, FindingReproduction.Classify(status, exit, code));

    private static void AssertChildExited(string repository)
    {
        int pid = int.Parse(File.ReadAllText(Path.Combine(repository, "finding-child.pid")), System.Globalization.CultureInfo.InvariantCulture);
        try { using Process process = Process.GetProcessById(pid); Assert.True(process.HasExited); }
        catch (ArgumentException) { }
    }
}
