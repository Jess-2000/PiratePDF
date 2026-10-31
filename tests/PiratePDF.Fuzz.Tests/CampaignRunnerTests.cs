using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

[Trait(TestPlatform.Key, TestPlatform.Windows)]
public sealed class CampaignRunnerTests
{
    [Theory]
    [InlineData("success")]
    [InlineData("rejected")]
    public async Task RepeatedRunsHaveIdenticalCasesAndPreserveCorpus(string mode)
    {
        using var fixture = new CampaignFixture(mode);
        string original = File.ReadAllText(fixture.Manifest);
        CampaignResult first = await fixture.Run();
        CampaignResult repeated = await fixture.Run("repeated-run");
        Assert.Equal(CampaignStop.CaseLimit, first.Stop);
        Assert.Equal(CampaignStop.CaseLimit, repeated.Stop);
        Assert.Equal(2, first.Targets.Single().Executed);
        Assert.Equal(mode == "success" ? 2 : 0, first.Targets.Single().Successes);
        Assert.Equal(mode == "rejected" ? 2 : 0, first.Targets.Single().Rejections);
        Assert.Equal(CaseRecords(fixture, "owned-run"), CaseRecords(fixture, "repeated-run"));
        Assert.Equal(original, File.ReadAllText(fixture.Manifest));
        Assert.True((await CorpusValidator.ValidateAsync(fixture.Repository)).IsValid);
        Assert.False(File.Exists(fixture.Incoming("owned-run")));
        Assert.True(first.CleanupSucceeded);
        AssertSafe(first);
    }

    [Fact]
    public async Task RealTargetsRunSequentiallyUsingOnlyExistingRegistry()
    {
        using var fixture = new CampaignFixture();
        CampaignResult result = await fixture.Run(config: new(1, TimeSpan.FromSeconds(10)), real: true, target: "all");
        Assert.Equal(CampaignStop.CaseLimit, result.Stop);
        Assert.Equal(new[] { "filter-asciihex", "filter-flate", "font-sanitize" }, result.Targets.Select(item => item.Target));
        Assert.All(result.Targets, item => Assert.Equal(1, item.Executed));
        Assert.True(result.CleanupSucceeded);
        Assert.True((await CorpusValidator.ValidateAsync(fixture.Repository)).IsValid);
    }

    [Theory]
    [InlineData("failure", SupervisionStatus.WorkerFailure, FindingClass.UnexpectedManagedFailure)]
    [InlineData("malformed", SupervisionStatus.InvalidProtocol, FindingClass.WorkerProtocolFailure)]
    [InlineData("abnormal", SupervisionStatus.AbnormalExit, FindingClass.AbnormalExit)]
    [InlineData("both-over", SupervisionStatus.InvalidProtocol, FindingClass.WorkerProtocolFailure)]
    public async Task FailuresAreRetainedBeforeInputCleanup(string mode, SupervisionStatus status, FindingClass classification)
    {
        using var fixture = new CampaignFixture(mode);
        CampaignResult result = await fixture.Run();
        Assert.Equal(CampaignStop.WorkerFailure, result.Stop);
        Assert.Equal(status, result.WorkerStatus);
        Assert.Equal(1, result.Targets.Single().WorkerRequests);
        string finding = fixture.Finding("owned-run");
        Assert.Equal(File.ReadAllBytes(Path.Combine(fixture.Repository, "last-candidate.bin")), File.ReadAllBytes(Path.Combine(finding, "input.bin")));
        FindingMetadata metadata = await new FindingStore(fixture.Repository, "owned-run").LoadAsync(result.Finding);
        Assert.Equal(classification, metadata.Source.Observed);
        Assert.Equal("unattempted", metadata.Reproduction);
        Assert.Equal("not-requested", metadata.Promotion);
        Assert.False(File.Exists(fixture.Incoming("owned-run")));
        AssertSafe(result);
        Assert.DoesNotContain("PRIVATE", File.ReadAllText(Path.Combine(fixture.Evidence("owned-run"), "ledger.jsonl")));
    }

    [Fact]
    public async Task FailedRetentionKeepsExactTemporaryCandidate()
    {
        using var fixture = new CampaignFixture("retention-failure");
        CampaignResult result = await fixture.Run();
        Assert.Equal(CampaignStop.RetentionFailure, result.Stop);
        Assert.False(result.CleanupSucceeded);
        Assert.Equal(File.ReadAllBytes(Path.Combine(fixture.Repository, "last-candidate.bin")), File.ReadAllBytes(fixture.Incoming("owned-run")));
        Assert.Equal(1, result.Targets.Single().WorkerRequests);
    }

    [Fact]
    public async Task ForeignWorkspaceContentsFailCleanupAndRetainOriginal()
    {
        using var fixture = new CampaignFixture("cleanup-failure");
        CampaignResult result = await fixture.Run();
        Assert.Equal(CampaignStop.CleanupFailure, result.Stop);
        Assert.False(result.CleanupSucceeded);
        Assert.Equal(File.ReadAllBytes(Path.Combine(fixture.Repository, "last-candidate.bin")),
            File.ReadAllBytes(Path.Combine(fixture.Finding("owned-run"), "input.bin")));
        Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(fixture.Incoming("owned-run")), "foreign.txt")));
    }

    [Theory]
    [InlineData("manifest-change")]
    [InlineData("seed-change")]
    public async Task ChangedOwnedIdentityStopsNextWorker(string mode)
    {
        using var fixture = new CampaignFixture(mode);
        CampaignResult result = await fixture.Run(config: new(3, TimeSpan.FromSeconds(10)));
        Assert.Equal(CampaignStop.IntegrityFailure, result.Stop);
        Assert.Equal(1, result.Targets.Single().WorkerRequests);
    }

    [Fact]
    public async Task MissingSeedFailsBeforeCreatingWorkspaceOrLaunching()
    {
        using var fixture = new CampaignFixture();
        File.Delete(Path.Combine(fixture.Repository, "tests", "FuzzCorpus", "font-sanitize", "font-minimal.bin"));
        CampaignResult result = await fixture.Run();
        Assert.Equal(CampaignStop.IntegrityFailure, result.Stop);
        Assert.Empty(result.Targets);
        Assert.False(File.Exists(Path.Combine(fixture.Repository, "campaign-executions.log")));
        Assert.False(Directory.Exists(fixture.Evidence("owned-run")));
    }

    [Theory]
    [InlineData("campaigns")]
    [InlineData("incoming")]
    [InlineData("findings")]
    public async Task ExistingWorkspaceOrStoreIsNeverReusedOrOverwritten(string area)
    {
        using var fixture = new CampaignFixture();
        string directory = Path.Combine(fixture.Repository, "artifacts", "fuzz", area, "owned-run");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "foreign.txt"), "preserve me");
        CampaignResult result = await fixture.Run();
        Assert.Equal(CampaignStop.WorkspaceFailure, result.Stop);
        Assert.Equal("preserve me", File.ReadAllText(Path.Combine(directory, "foreign.txt")));
        Assert.False(File.Exists(Path.Combine(fixture.Repository, "campaign-executions.log")));
    }

    [Fact]
    public async Task LaunchFailureIsNotAnExecutedSuccessfulCase()
    {
        using var fixture = new CampaignFixture();
        string missingHost = Path.Combine(fixture.Repository, "dotnet.exe");
        var runner = new CampaignRunner(new WorkerSupervisor(missingHost, typeof(RunCommand).Assembly.Location));
        CampaignResult result = await runner.RunAsync(fixture.Repository, "owned-run", "filter-asciihex", CampaignFixture.Revision, new(1, TimeSpan.FromSeconds(10)));
        Assert.Equal(CampaignStop.WorkerFailure, result.Stop);
        Assert.Equal(SupervisionStatus.StartFailed, result.WorkerStatus);
        Assert.Equal(0, result.Targets.Single().Executed);
        Assert.Equal(1, result.Targets.Single().WorkerRequests);
        Assert.True(File.Exists(Path.Combine(fixture.Finding("owned-run"), "input.bin")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitialProcessedAndPoolLimitsStopBeforeWorker(bool pool)
    {
        using var fixture = new CampaignFixture();
        CampaignConfiguration config = pool ? new(100, TimeSpan.FromSeconds(10), poolInputs: 6) : new(100, TimeSpan.FromSeconds(10), processedBytes: 39);
        CampaignResult result = await fixture.Run(config: config);
        Assert.Equal(pool ? CampaignStop.PoolLimit : CampaignStop.ProcessedByteLimit, result.Stop);
        Assert.Equal(0, result.Targets.Single().WorkerRequests);
        Assert.True(result.CleanupSucceeded);
    }

    [Fact]
    public async Task TimedOutWorkerAndDescendantAreReapedWithImmutableFinding()
    {
        using var fixture = new CampaignFixture("tree");
        var runner = fixture.Runner(timeout: TimeSpan.FromSeconds(2));
        var clock = Stopwatch.StartNew();
        CampaignResult result = await runner.RunAsync(fixture.Repository, "owned-run", "filter-asciihex", CampaignFixture.Revision, new(2, TimeSpan.FromSeconds(10)));
        Assert.Equal(CampaignStop.WorkerFailure, result.Stop);
        Assert.Equal(SupervisionStatus.TimedOut, result.WorkerStatus);
        Assert.True(result.CleanupSucceeded);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(15));
        AssertExited(Path.Combine(fixture.Repository, "tree.root"));
        AssertExited(Path.Combine(fixture.Repository, "tree.child"));
        Assert.Equal(FindingClass.Timeout, (await new FindingStore(fixture.Repository, "owned-run").LoadAsync(result.Finding)).Source.Observed);
    }

    [Fact]
    public async Task ExplicitCancellationReapsTreeAndRetainsCase()
    {
        using var fixture = new CampaignFixture("tree");
        using var cancellation = new CancellationTokenSource();
        Task<CampaignResult> execution = fixture.Run(token: cancellation.Token);
        await WaitForFile(Path.Combine(fixture.Repository, "tree.child"));
        cancellation.Cancel();
        CampaignResult result = await execution.WaitAsync(TimeSpan.FromSeconds(12));
        Assert.Equal(CampaignStop.Cancelled, result.Stop);
        Assert.Equal(SupervisionStatus.Cancelled, result.WorkerStatus);
        Assert.True(result.CleanupSucceeded);
        AssertExited(Path.Combine(fixture.Repository, "tree.root"));
        AssertExited(Path.Combine(fixture.Repository, "tree.child"));
        Assert.True(File.Exists(Path.Combine(fixture.Finding("owned-run"), "input.bin")));
    }

    [Fact]
    public async Task PreCancellationLaunchesNothing()
    {
        using var fixture = new CampaignFixture();
        CampaignResult result = await fixture.Run(token: new CancellationToken(true));
        Assert.Equal(CampaignStop.Cancelled, result.Stop);
        Assert.False(Directory.Exists(fixture.Evidence("owned-run")));
    }

    [Fact]
    public async Task TelemetryBudgetStopsBeforeAnotherWorkerAndPublishesTerminalRecord()
    {
        using var fixture = new CampaignFixture();
        CampaignResult result = await fixture.Run(config: new(10, TimeSpan.FromSeconds(10), telemetryBytes: 16384));
        Assert.Equal(CampaignStop.TelemetryLimit, result.Stop);
        Assert.Equal(1, result.Targets.Single().WorkerRequests);
        Assert.True(new FileInfo(Path.Combine(fixture.Evidence("owned-run"), "ledger.jsonl")).Length +
            new FileInfo(Path.Combine(fixture.Evidence("owned-run"), "summary.json")).Length <= 16384);
    }

    [Fact]
    public async Task EvidenceInventoryFailureRetainsCandidateAndNeverPublishesSuccess()
    {
        using var fixture = new CampaignFixture("evidence-corruption");
        CampaignResult result = await fixture.Run();
        Assert.Equal(CampaignStop.CleanupFailure, result.Stop);
        Assert.False(result.CleanupSucceeded);
        Assert.True(File.Exists(Path.Combine(fixture.Finding("owned-run"), "input.bin")));
        Assert.False(File.Exists(Path.Combine(fixture.Evidence("owned-run"), "summary.json")));
    }

    [Fact]
    public async Task FullFindingStoreIsRefusedDuringPreflight()
    {
        using var fixture = new CampaignFixture();
        string root = Path.Combine(fixture.Repository, "artifacts", "fuzz", "findings", "owned-run");
        for (int index = 0; index < FindingStore.MaximumFindings; index++) Directory.CreateDirectory(Path.Combine(root, "owned-" + index));
        CampaignResult result = await fixture.Run();
        Assert.Equal(CampaignStop.WorkspaceFailure, result.Stop);
        Assert.Equal(16, Directory.GetDirectories(root).Length);
        Assert.False(File.Exists(Path.Combine(fixture.Repository, "campaign-executions.log")));
    }

    [Fact]
    public async Task CampaignWorkspaceLinkIsRejected()
    {
        using var fixture = new CampaignFixture();
        string outside = Path.Combine(fixture.Repository, "outside");
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "preserve.txt"), "owned fixture");
        string incoming = Path.GetDirectoryName(fixture.Incoming("owned-run"));
        Directory.CreateDirectory(Path.GetDirectoryName(incoming));
        Directory.CreateSymbolicLink(incoming, outside);
        CampaignResult result = await fixture.Run();
        Assert.Equal(CampaignStop.WorkspaceFailure, result.Stop);
        Assert.Equal("owned fixture", File.ReadAllText(Path.Combine(outside, "preserve.txt")));
        Assert.False(File.Exists(Path.Combine(fixture.Repository, "campaign-executions.log")));
    }

    [Fact]
    public async Task FailedPipeCleanupIsNotACompletedCampaign()
    {
        using var fixture = new CampaignFixture("orphanpipe");
        try
        {
            CampaignResult result = await fixture.Runner(timeout: TimeSpan.FromSeconds(2)).RunAsync(fixture.Repository,
                "owned-run", "filter-asciihex", CampaignFixture.Revision, new(2, TimeSpan.FromSeconds(10)));
            Assert.Equal(CampaignStop.CleanupFailure, result.Stop);
            Assert.Equal(SupervisionStatus.TerminationFailed, result.WorkerStatus);
            Assert.False(result.CleanupSucceeded);
            Assert.True(File.Exists(Path.Combine(fixture.Finding("owned-run"), "input.bin")));
        }
        finally
        {
            // This intentionally detached controlled child proves failure, not successful supervisor cleanup.
            string identity = Path.Combine(fixture.Repository, "orphanpipe.child");
            await WaitForFile(identity);
            string[] parts = File.ReadAllText(identity).Split(':');
            try
            {
                using Process child = Process.GetProcessById(int.Parse(parts[0], CultureInfo.InvariantCulture));
                if (!child.HasExited && child.StartTime.ToUniversalTime().Ticks == long.Parse(parts[1], CultureInfo.InvariantCulture))
                {
                    child.Kill(entireProcessTree: true);
                    await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                }
            }
            catch (ArgumentException) { }
        }
    }

    [Fact]
    public async Task CampaignDeadlineCancelsActiveWorkerWithoutUsingSmokeMaximum()
    {
        using var fixture = new CampaignFixture("tree");
        CampaignResult result = await fixture.Run(config: new(2, TimeSpan.FromSeconds(2)));
        Assert.Equal(CampaignStop.Deadline, result.Stop);
        Assert.Equal(SupervisionStatus.Cancelled, result.WorkerStatus);
        Assert.True(result.CleanupSucceeded);
        AssertExited(Path.Combine(fixture.Repository, "tree.root"));
        AssertExited(Path.Combine(fixture.Repository, "tree.child"));
    }

    [Fact]
    public async Task OuterExpirationReapsWorkerAndPreservesInputWhenRetentionDeadlineHasExpired()
    {
        using var fixture = new CampaignFixture("tree");
        var time = new ManualCampaignTime();
        var runner = new CampaignRunner(ReplayFixture.Supervisor(), time);
        Task<CampaignResult> task = runner.RunAsync(fixture.Repository, "owned-run", "filter-asciihex",
            CampaignFixture.Revision, new(2, TimeSpan.FromSeconds(60)));
        await WaitForFile(Path.Combine(fixture.Repository, "tree.child"));
        time.Expire();
        CampaignResult result = await task.WaitAsync(TimeSpan.FromSeconds(12));
        Assert.True(result.OuterExpired);
        Assert.Equal(CampaignStop.RetentionFailure, result.Stop);
        Assert.Equal(SupervisionStatus.Cancelled, result.WorkerStatus);
        Assert.False(result.CleanupSucceeded);
        Assert.Equal(File.ReadAllBytes(Path.Combine(fixture.Repository, "last-candidate.bin")), File.ReadAllBytes(fixture.Incoming("owned-run")));
        AssertExited(Path.Combine(fixture.Repository, "tree.root"));
        AssertExited(Path.Combine(fixture.Repository, "tree.child"));
    }

    [Fact]
    public async Task WorkExhaustionStopsBeforeGenerationOrLaunch()
    {
        using var fixture = new CampaignFixture();
        CampaignResult result = await fixture.Run(config: new(10, TimeSpan.FromSeconds(10), processedBytes: 201));
        Assert.Equal(CampaignStop.ProcessedByteLimit, result.Stop);
        Assert.Equal(0, result.Targets.Single().Attempts);
        Assert.Equal(0, result.Targets.Single().WorkerRequests);
        Assert.False(File.Exists(Path.Combine(fixture.Repository, "campaign-executions.log")));
    }

    [Fact]
    public async Task ExactAttemptReservationRunsOnceThenStopsBeforeSecondManifestRead()
    {
        using var fixture = new CampaignFixture();
        var corpus = await CorpusValidator.ValidateAsync(fixture.Repository);
        var seeds = corpus.Seeds.Where(seed => seed.Target == "filter-asciihex").ToArray();
        long initial = 2 * CampaignWork.ManifestUnits + CampaignWork.ValidationUnits(corpus.Seeds) +
            seeds.Sum(seed => CampaignWork.SeedUnits(seed.Length));
        long attempt = CampaignWork.ManifestUnits + CampaignWork.AttemptUnits(seeds[0].Length, seeds[0].Length, 16384) + seeds[0].Length;
        int allowance = checked((int)(initial + attempt));
        CampaignResult result = await fixture.Run(config: new(2, TimeSpan.FromSeconds(10), processedBytes: allowance));
        Assert.Equal(CampaignStop.ProcessedByteLimit, result.Stop);
        Assert.Equal(allowance, result.Targets.Single().ProcessedBytes);
        Assert.Equal(1, result.Targets.Single().Attempts);
        Assert.Equal(1, result.Targets.Single().WorkerRequests);
        Assert.Equal(1, result.Targets.Single().Executed);
    }

    private static string[] CaseRecords(CampaignFixture fixture, string run) =>
        File.ReadAllLines(Path.Combine(fixture.Evidence(run), "ledger.jsonl"))
            .Where(line => JsonDocument.Parse(line).RootElement.GetProperty("schema").GetString() == "piratepdf-mutation-case").ToArray();

    private static void AssertSafe(CampaignResult result)
    {
        Assert.InRange(Encoding.UTF8.GetByteCount(result.Json), 1, 4096);
        Assert.DoesNotContain("PRIVATE", result.Json);
        Assert.DoesNotContain("Exception", result.Json);
        Assert.DoesNotContain("C:\\", result.Json);
    }

    private static async Task WaitForFile(string path)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!File.Exists(path) || new FileInfo(path).Length == 0) await Task.Delay(10, timeout.Token);
    }

    private static void AssertExited(string path)
    {
        string[] identity = File.ReadAllText(path).Split(':');
        try
        {
            using Process process = Process.GetProcessById(int.Parse(identity[0], CultureInfo.InvariantCulture));
            Assert.True(process.HasExited || process.StartTime.ToUniversalTime().Ticks != long.Parse(identity[1], CultureInfo.InvariantCulture));
        }
        catch (ArgumentException) { } // The exact process instance is already gone.
    }
}
