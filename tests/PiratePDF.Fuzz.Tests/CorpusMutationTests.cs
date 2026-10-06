using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class CorpusMutationTests
{
    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task RealOwnedCorpusBatchIsBoundedAndPreservesEverySeedAndManifest()
    {
        string repository = ReplayFixture.FindRepository();
        string manifest = File.ReadAllText(Path.Combine(repository, "tests", "FuzzCorpus", "manifest.json"));
        CorpusValidationResult before = await CorpusValidator.ValidateAsync(repository);
        var runner = new CorpusMutation(ReplayFixture.Supervisor(real: true));
        MutationResult result = await runner.RunBatchAsync(repository, DeterministicMutation.Algorithm, 1);

        Assert.Equal(MutationExitCode.Completed, result.ExitCode);
        Assert.Equal(15, result.Admitted);
        Assert.Equal(15, result.Attempted);
        Assert.Equal(15, result.Completed);
        Assert.Equal(before.Seeds.Select(seed => seed.Id), result.Observations.Select(entry => entry.Identity.Seed));
        MutationResult repeated = await runner.RunBatchAsync(repository, DeterministicMutation.Algorithm, 1);
        Assert.Equal(result.Json, repeated.Json);
        Assert.Equal(result.Observations, repeated.Observations);
        Assert.Equal(manifest, File.ReadAllText(Path.Combine(repository, "tests", "FuzzCorpus", "manifest.json")));
        CorpusValidationResult after = await CorpusValidator.ValidateAsync(repository);
        Assert.True(after.IsValid);
        Assert.Equal(before.Seeds.Select(seed => seed.Sha256), after.Seeds.Select(seed => seed.Sha256));
        Assert.False(Directory.Exists(Path.Combine(repository, "artifacts", "fuzz", "mutation-work")));
    }

    [Theory]
    [InlineData("success")]
    [InlineData("rejected")]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task BatchesRepeatInOrdinalIndexOrderAndLeaveNoTemporaryInput(string outcome)
    {
        using var fixture = Fixture(outcome);
        fixture.Add("z-last");
        string manifest = File.ReadAllText(fixture.ManifestPath);
        var runner = new CorpusMutation(ReplayFixture.Supervisor());
        MutationResult first = await runner.RunBatchAsync(fixture.Repository, DeterministicMutation.Algorithm, 7);
        Assert.Equal(MutationExitCode.Completed, first.ExitCode);
        Assert.Equal(14, first.Attempted);
        Assert.Equal(14, first.Completed);
        Assert.Equal(outcome == "success" ? 14 : 0, first.Successes);
        Assert.Equal(outcome == "rejected" ? 14 : 0, first.Rejections);
        string[] hashes = File.ReadAllLines(Log(fixture));
        Assert.Equal(new[] { "a-success", "z-last" }.SelectMany(id => Enumerable.Range(0, 7)
            .Select(index => Convert.ToHexString(SHA256.HashData(DeterministicMutation.Generate("filter-asciihex", id, 1, index, [62]))))), hashes);
        Assert.Equal(new[] { "a-success", "z-last" }.SelectMany(id => Enumerable.Range(0, 7).Select(index => (id, index))),
            first.Observations.Select(entry => (entry.Identity.Seed, entry.Identity.Index)));
        AssertClean(fixture);
        MutationResult second = await runner.RunBatchAsync(fixture.Repository, DeterministicMutation.Algorithm, 7);
        Assert.Equal(first.Json, second.Json);
        Assert.Equal(first.Observations, second.Observations);
        Assert.Equal(manifest, File.ReadAllText(fixture.ManifestPath));
        Assert.Equal(new byte[] { 62 }, File.ReadAllBytes(fixture.SeedPath("a-success")));
        AssertClean(fixture);
        AssertSafe(first, fixture);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task RealSingleReproductionUsesAdmittedSeedAndPreservesCorpus(int index)
    {
        using var fixture = Fixture("success");
        string[] args = ["mutate-one", "--repository-root", fixture.Repository, "--target", "filter-asciihex",
            "--seed", "a-success", "--config", "v1", "--mutation", index.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--algorithm", DeterministicMutation.Algorithm];
        string manifest = File.ReadAllText(fixture.ManifestPath);
        MutationResult result = await MutationCommand.ExecuteAsync(args);

        Assert.Equal(MutationExitCode.Completed, result.ExitCode);
        Assert.Equal(1, result.Attempted);
        Assert.Equal(1, result.Completed);
        Assert.Equal(index, result.Identity.Index);
        // Index 2 inserts E8 before '>', an invalid ASCIIHex byte, not a valid empty stream.
        if (index == 2) Assert.Equal(new byte[] { 232, 62 }, DeterministicMutation.Generate("filter-asciihex", "a-success", 1, index, [62]));
        Assert.Equal(0, result.Successes);
        Assert.Equal(1, result.Rejections);
        Assert.Equal(result.Json, (await MutationCommand.ExecuteAsync(args)).Json);
        Assert.Equal(manifest, File.ReadAllText(fixture.ManifestPath));
        Assert.Equal(new byte[] { 62 }, File.ReadAllBytes(fixture.SeedPath("a-success")));
        AssertClean(fixture);
        AssertSafe(result, fixture);
    }

    [Theory]
    [InlineData(4, 7, false)]
    [InlineData(5, 7, true)]
    [InlineData(16, 2, false)]
    [InlineData(17, 2, true)]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task TotalAttemptBudgetIsCheckedBeforeLaunch(int seeds, int perSeed, bool exceeds)
    {
        using var fixture = Fixture("success");
        for (int index = 1; index < seeds; index++) fixture.Add("b-" + index.ToString("D2", System.Globalization.CultureInfo.InvariantCulture));
        MutationResult result = await new CorpusMutation(ReplayFixture.Supervisor()).RunBatchAsync(fixture.Repository, DeterministicMutation.Algorithm, perSeed);

        Assert.Equal(exceeds ? MutationExitCode.InvalidRequest : MutationExitCode.Completed, result.ExitCode);
        Assert.Equal(exceeds ? 0 : seeds * perSeed, result.Attempted);
        Assert.InRange(result.Observations.Count, 0, 32);
        Assert.Equal(32, DeterministicMutation.MaximumAttempts);
        if (exceeds) Assert.Equal(MutationFailure.Budget, result.Failure);
        AssertClean(fixture);
    }

    [Theory]
    [InlineData("algorithm", MutationFailure.Algorithm)]
    [InlineData("index-low", MutationFailure.Index)]
    [InlineData("index-high", MutationFailure.Index)]
    [InlineData("target", MutationFailure.Target)]
    [InlineData("config", MutationFailure.Configuration)]
    [InlineData("seed", MutationFailure.Seed)]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task UnadmittedRequestsNeverLaunch(string kind, MutationFailure failure)
    {
        using var fixture = Fixture("success");
        MutationResult result = await new CorpusMutation(ReplayFixture.Supervisor()).RunOneAsync(fixture.Repository,
            kind == "algorithm" ? "unknown" : DeterministicMutation.Algorithm, kind == "target" ? "unknown" : "filter-asciihex",
            kind == "seed" ? "unlisted" : "a-success", kind == "config" ? 2 : 1, kind == "index-low" ? -1 : kind == "index-high" ? 7 : 0);

        Assert.Equal(MutationExitCode.InvalidRequest, result.ExitCode);
        Assert.Equal(failure, result.Failure);
        Assert.Equal(0, result.Attempted);
        Assert.False(File.Exists(Log(fixture)));
        AssertClean(fixture);
        AssertSafe(result, fixture);
    }

    [Theory]
    [InlineData("manifest")]
    [InlineData("hash")]
    [InlineData("length")]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task InitialIntegrityFailureExecutesNothing(string kind)
    {
        using var fixture = Fixture("success");
        if (kind == "manifest") File.WriteAllText(fixture.ManifestPath, "PRIVATE invalid");
        else File.WriteAllBytes(fixture.SeedPath("a-success"), kind == "hash" ? [63] : [62, 62]);
        MutationResult result = await new CorpusMutation(ReplayFixture.Supervisor()).RunBatchAsync(fixture.Repository, DeterministicMutation.Algorithm, 1);

        Assert.Equal(MutationExitCode.IntegrityFailure, result.ExitCode);
        Assert.Equal(0, result.Attempted);
        Assert.False(File.Exists(Log(fixture)));
        AssertClean(fixture);
    }

    [Theory]
    [InlineData("hash")]
    [InlineData("length")]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task SourceIsRecheckedBeforeEveryMutation(string kind)
    {
        using var fixture = Fixture("change-" + kind);
        string manifest = File.ReadAllText(fixture.ManifestPath);
        MutationResult result = await new CorpusMutation(ReplayFixture.Supervisor()).RunBatchAsync(fixture.Repository, DeterministicMutation.Algorithm, 2);

        Assert.Equal(MutationExitCode.IntegrityFailure, result.ExitCode);
        Assert.Equal(MutationFailure.SeedIdentity, result.Failure);
        Assert.Equal(1, result.Attempted);
        Assert.Single(File.ReadAllLines(Log(fixture)));
        Assert.Equal(manifest, File.ReadAllText(fixture.ManifestPath));
        AssertClean(fixture);
    }

    [Theory]
    [InlineData("abnormal", SupervisionStatus.AbnormalExit)]
    [InlineData("malformed", SupervisionStatus.InvalidProtocol)]
    [InlineData("worker-failure", SupervisionStatus.WorkerFailure)]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task FailuresAreNeverCountedAsTargetRejections(string mode, SupervisionStatus status)
    {
        using var fixture = Fixture(mode);
        MutationResult result = await new CorpusMutation(ReplayFixture.Supervisor()).RunBatchAsync(fixture.Repository, DeterministicMutation.Algorithm, 2);

        Assert.Equal(MutationExitCode.SupervisionFailure, result.ExitCode);
        Assert.Equal(status, result.SupervisionStatus);
        Assert.Equal(1, result.Attempted);
        Assert.Equal(0, result.Completed);
        Assert.Single(File.ReadAllLines(Log(fixture)));
        AssertClean(fixture);
        AssertSafe(result, fixture);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task TimeoutAndCancellationStopLaunchesAndCleanTemporaryInput(bool cancel)
    {
        using var fixture = Fixture("tree");
        using var cancellation = new CancellationTokenSource();
        Task<MutationResult> pending = new CorpusMutation(ReplayFixture.Supervisor(timeout: TimeSpan.FromSeconds(cancel ? 10 : 2)))
            .RunBatchAsync(fixture.Repository, DeterministicMutation.Algorithm, 2, cancellation.Token);
        string child = Path.Combine(fixture.Repository, "mutation-probes", "tree.child");
        if (cancel)
        {
            try
            {
                await CorpusReplayTests.WaitForFile(child);
                MutationResult concurrent = await new CorpusMutation(ReplayFixture.Supervisor())
                    .RunBatchAsync(fixture.Repository, DeterministicMutation.Algorithm, 1);
                Assert.Equal(MutationExitCode.TemporaryInputFailure, concurrent.ExitCode);
                Assert.Equal(0, concurrent.Attempted);
            }
            finally { cancellation.Cancel(); }
        }
        MutationResult result = await pending;

        Assert.Equal(cancel ? MutationExitCode.Cancelled : MutationExitCode.TimedOut, result.ExitCode);
        Assert.Equal(cancel ? SupervisionStatus.Cancelled : SupervisionStatus.TimedOut, result.SupervisionStatus);
        Assert.Equal(1, result.Attempted);
        await CorpusReplayTests.EnsureExited(child, true);
        await CorpusReplayTests.EnsureExited(Path.Combine(fixture.Repository, "mutation-probes", "tree.root"), true);
        AssertClean(fixture);
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task TerminationFailureIsPreservedAndOwnedOrphanIsReaped()
    {
        using var fixture = Fixture("orphanpipe");
        try
        {
            MutationResult result = await new CorpusMutation(ReplayFixture.Supervisor(timeout: TimeSpan.FromSeconds(2)))
                .RunBatchAsync(fixture.Repository, DeterministicMutation.Algorithm, 2);

            Assert.Equal(MutationExitCode.CleanupFailed, result.ExitCode);
            Assert.Equal(SupervisionStatus.TerminationFailed, result.SupervisionStatus);
            Assert.Equal(1, result.Attempted);
            AssertClean(fixture);
        }
        finally
        {
            await CorpusReplayTests.EnsureExited(Path.Combine(fixture.Repository, "mutation-probes", "orphanpipe.child"), false);
        }
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task UnexpectedWorkspaceContentsSurfaceCleanupFailureWithoutRecursiveDeletion()
    {
        using var fixture = Fixture("block-cleanup");
        MutationResult result = await new CorpusMutation(ReplayFixture.Supervisor()).RunBatchAsync(fixture.Repository, DeterministicMutation.Algorithm, 2);

        Assert.Equal(MutationExitCode.CleanupFailed, result.ExitCode);
        Assert.Equal(1, result.Successes);
        Assert.Equal(1, result.Attempted);
        Assert.True(File.Exists(Path.Combine(Work(fixture), "foreign.txt")));
        Assert.False(File.Exists(Path.Combine(Work(fixture), "case.bin")));
        Assert.False(File.Exists(Path.Combine(Work(fixture), "active.lock")));
        AssertSafe(result, fixture);
    }

    [Theory]
    [InlineData("active.lock")]
    [InlineData("case.bin")]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task ExistingWorkspaceFilesAreNotOverwrittenOrDeleted(string name)
    {
        using var fixture = Fixture("success");
        Directory.CreateDirectory(Work(fixture));
        string path = Path.Combine(Work(fixture), name);
        File.WriteAllText(path, "PRIVATE preserve");
        MutationResult result = await new CorpusMutation(ReplayFixture.Supervisor()).RunBatchAsync(fixture.Repository, DeterministicMutation.Algorithm, 1);

        Assert.NotEqual(MutationExitCode.Completed, result.ExitCode);
        Assert.Equal(0, result.Attempted);
        Assert.Equal("PRIVATE preserve", File.ReadAllText(path));
        AssertSafe(result, fixture);
    }

    [Fact]
    public async Task PreCancellationDoesNotCreateWorkspace()
    {
        using var fixture = Fixture("success");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        MutationResult result = await new CorpusMutation(ReplayFixture.Supervisor()).RunBatchAsync(fixture.Repository,
            DeterministicMutation.Algorithm, 1, cancellation.Token);
        Assert.Equal(MutationExitCode.Cancelled, result.ExitCode);
        Assert.Equal(0, result.Attempted);
        AssertClean(fixture);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public async Task PerSeedCountCannotExtendTheAlgorithm(int count)
    {
        using var fixture = Fixture("success");
        MutationResult result = await new CorpusMutation(ReplayFixture.Supervisor()).RunBatchAsync(fixture.Repository, DeterministicMutation.Algorithm, count);
        Assert.Equal(MutationExitCode.InvalidRequest, result.ExitCode);
        Assert.Equal(0, result.Attempted);
        AssertClean(fixture);
    }

    [Fact]
    public async Task CliRejectsExtraMissingDuplicateAndNoncanonicalArguments()
    {
        using var fixture = Fixture("success");
        string[][] invalid = [[], ["mutate"], ["mutate", "--repository-root", fixture.Repository, "--algorithm", DeterministicMutation.Algorithm, "--per-seed", "01"],
            ["mutate", "--repository-root", fixture.Repository, "--algorithm", DeterministicMutation.Algorithm, "--arbitrary-file", "PRIVATE"],
            ["mutate", "--repository-root", fixture.Repository, "--repository-root", fixture.Repository, "--per-seed", "1"]];
        foreach (string[] args in invalid)
        {
            MutationResult result = await MutationCommand.ExecuteAsync(args);
            Assert.Equal(MutationExitCode.InvalidRequest, result.ExitCode);
            Assert.Equal(result.Json, (await MutationCommand.ExecuteAsync(args)).Json);
            AssertSafe(result, fixture);
        }
    }

    [Fact]
    public void ExitCodesAreStable() => Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, Enum.GetValues<MutationExitCode>().Select(value => (int)value));

    private static ReplayFixture Fixture(string mode)
    {
        var fixture = new ReplayFixture();
        fixture.Add("a-success");
        File.WriteAllText(Path.Combine(fixture.Repository, "mutation-mode.txt"), mode);
        return fixture;
    }

    private static string Work(ReplayFixture fixture) => Path.Combine(fixture.Repository, "artifacts", "fuzz", "mutation-work");
    private static string Log(ReplayFixture fixture) => Path.Combine(fixture.Repository, "mutation-executions.log");
    private static void AssertClean(ReplayFixture fixture) => Assert.False(Directory.Exists(Work(fixture)));

    private static void AssertSafe(MutationResult result, ReplayFixture fixture)
    {
        Assert.InRange(Encoding.UTF8.GetByteCount(result.Json), 1, MutationResult.MaximumJsonBytes);
        using JsonDocument json = JsonDocument.Parse(result.Json);
        Assert.Equal(DeterministicMutation.Algorithm, json.RootElement.GetProperty("algorithm").GetString());
        foreach (string secret in new[] { fixture.Repository, "PRIVATE", "Exception", "stdout", "stderr", "duration", "pid", "timestamp", "case.bin" })
            Assert.DoesNotContain(secret, result.Json);
    }
}
