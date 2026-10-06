using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class CorpusReplayTests
{
    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task CommittedCorpusReplaysAllSeedsInManifestOrderWithRepeatableMetadata()
    {
        string repository = ReplayFixture.FindRepository();
        var replay = new CorpusReplay(ReplayFixture.Supervisor(real: true));
        ReplayResult first = await replay.RunAsync(repository);
        Assert.Equal(ReplayExitCode.Success, first.ExitCode);
        Assert.Equal(15, first.Admitted);
        Assert.Equal(15, first.Attempted);
        Assert.Equal(15, first.Executed);
        Assert.Equal(15, first.Matched);
        Assert.Equal(0, first.Mismatched);
        Assert.Equal(new[] { "filter-asciihex", "filter-flate", "font-sanitize" },
            first.Observations.Select(entry => entry.Target).Distinct());
        Assert.Equal(new[] { 6, 6, 3 }, first.Observations.GroupBy(entry => entry.Target).Select(group => group.Count()));
        CorpusValidationResult manifest = await CorpusValidator.ValidateAsync(repository);
        Assert.Equal(manifest.Seeds.Select(seed => seed.Id), first.Observations.Select(entry => entry.Seed));
        Assert.All(first.Observations, entry => Assert.Equal(entry.Expected, entry.Actual));
        ReplayResult second = await replay.RunAsync(repository);
        Assert.Equal(first.Json, second.Json);
        Assert.Equal(first.Observations, second.Observations);
        Assert.Throws<NotSupportedException>(() => Assert.IsAssignableFrom<IList<ReplayObservation>>(first.Observations).Clear());
        AssertSafe(first, repository);
    }

    [Theory]
    [InlineData("a-success", "success", ReplayExitCode.Success)]
    [InlineData("a-rejected", "rejected", ReplayExitCode.Success)]
    [InlineData("a-success", "rejected", ReplayExitCode.OracleMismatch)]
    [InlineData("a-rejected", "success", ReplayExitCode.OracleMismatch)]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task OracleComparisonIsFailFastAndNeverRewritesExpectations(string id, string expected, ReplayExitCode exit)
    {
        using var fixture = new ReplayFixture();
        fixture.Add(id, expected);
        fixture.Add("z-last");
        string before = File.ReadAllText(fixture.ManifestPath);
        ReplayResult result = await new CorpusReplay(ReplayFixture.Supervisor()).RunAsync(fixture.Repository);

        Assert.Equal(exit, result.ExitCode);
        Assert.Equal(exit == ReplayExitCode.Success ? 2 : 1, result.Executed);
        Assert.Equal(exit == ReplayExitCode.Success ? 0 : 1, result.Mismatched);
        Assert.Equal(result.Executed, File.ReadAllLines(fixture.LogPath).Length);
        Assert.Equal(before, File.ReadAllText(fixture.ManifestPath));
        AssertSafe(result, fixture.Repository);
    }

    [Theory]
    [InlineData("invalid", CorpusFailure.InvalidManifest)]
    [InlineData("hash", CorpusFailure.HashMismatch)]
    [InlineData("unlisted", CorpusFailure.UnlistedFile)]
    [InlineData("missing", CorpusFailure.MissingSeed)]
    [InlineData("oversized", CorpusFailure.SeedTooLarge)]
    [InlineData("target", CorpusFailure.UnknownTarget)]
    [InlineData("config", CorpusFailure.ConfigurationMismatch)]
    [InlineData("path", CorpusFailure.InvalidPath)]
    [InlineData("order", CorpusFailure.InvalidOrdering)]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task InitialIntegrityFailureExecutesZeroSeeds(string mode, CorpusFailure failure)
    {
        using var fixture = new ReplayFixture();
        fixture.Add("a-success");
        fixture.Add("z-last");
        JsonNode last = fixture.Seeds[1];
        switch (mode)
        {
            case "invalid": File.WriteAllText(fixture.ManifestPath, "PRIVATE_INVALID_JSON"); break;
            case "hash": File.WriteAllBytes(fixture.SeedPath("z-last"), [63]); break;
            case "unlisted": File.WriteAllBytes(fixture.SeedPath("unlisted"), [62]); break;
            case "missing": File.Delete(fixture.SeedPath("z-last")); break;
            case "oversized": File.WriteAllBytes(fixture.SeedPath("z-last"), new byte[16385]); break;
            case "target": last["target"] = "unknown"; fixture.Save(); break;
            case "config": last["configurationVersion"] = 2; fixture.Save(); break;
            case "path": last["path"] = "../PRIVATE"; fixture.Save(); break;
            case "order": JsonNode first = fixture.Seeds[0]; fixture.Seeds.RemoveAt(0); fixture.Seeds.Add(first); fixture.Save(); break;
        }
        ReplayResult result = await new CorpusReplay(ReplayFixture.Supervisor()).RunAsync(fixture.Repository);

        Assert.Equal(ReplayExitCode.IntegrityFailure, result.ExitCode);
        Assert.Equal(failure, result.CorpusFailure);
        Assert.Equal(0, result.Admitted);
        Assert.Equal(0, result.Attempted);
        Assert.Empty(result.Observations);
        Assert.False(File.Exists(fixture.LogPath));
        AssertSafe(result, fixture.Repository);
    }

    [Theory]
    [InlineData("hash", CorpusFailure.HashMismatch)]
    [InlineData("length", CorpusFailure.LengthMismatch)]
    [InlineData("over", CorpusFailure.SeedTooLarge)]
    [InlineData("missing", CorpusFailure.MissingSeed)]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task ChangeAfterValidationIsDetectedBeforeNextChild(string mode, CorpusFailure failure)
    {
        using var fixture = new ReplayFixture();
        string first = "a-change-" + mode;
        fixture.Add(first);
        fixture.Add("z-last");
        string before = File.ReadAllText(fixture.ManifestPath);
        ReplayResult result = await new CorpusReplay(ReplayFixture.Supervisor()).RunAsync(fixture.Repository);

        Assert.Equal(ReplayExitCode.IntegrityFailure, result.ExitCode);
        Assert.Equal(failure, result.CorpusFailure);
        Assert.Equal(2, result.Admitted);
        Assert.Equal(1, result.Attempted);
        Assert.Equal(1, result.Matched);
        Assert.Equal(new[] { first }, File.ReadAllLines(fixture.LogPath));
        Assert.Equal(before, File.ReadAllText(fixture.ManifestPath));
        AssertSafe(result, fixture.Repository);
    }

    [Theory]
    [InlineData("abnormal", SupervisionStatus.AbnormalExit)]
    [InlineData("malformed", SupervisionStatus.InvalidProtocol)]
    [InlineData("worker-failure", SupervisionStatus.WorkerFailure)]
    [InlineData("spontaneous-cancel", SupervisionStatus.WorkerFailure)]
    [InlineData("input-invalid", SupervisionStatus.Completed)]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task SupervisorFailuresCannotSatisfyRejectionOracle(string mode, SupervisionStatus status)
    {
        using var fixture = new ReplayFixture();
        fixture.Add("a-" + mode, "rejected");
        fixture.Add("z-last");
        ReplayResult result = await new CorpusReplay(ReplayFixture.Supervisor()).RunAsync(fixture.Repository);

        Assert.Equal(ReplayExitCode.SupervisionFailure, result.ExitCode);
        Assert.Equal(status, result.SupervisionStatus);
        Assert.Equal(1, result.Attempted);
        Assert.Equal(0, result.Executed);
        Assert.Equal(0, result.Matched);
        Assert.Single(File.ReadAllLines(fixture.LogPath));
        AssertSafe(result, fixture.Repository);
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task StartFailureRemainsDistinct()
    {
        using var fixture = new ReplayFixture();
        fixture.Add("a-success");
        var supervisor = new WorkerSupervisor(Path.Combine(fixture.Repository, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"),
            typeof(RunCommand).Assembly.Location);
        ReplayResult result = await new CorpusReplay(supervisor).RunAsync(fixture.Repository);

        Assert.Equal(ReplayExitCode.SupervisionFailure, result.ExitCode);
        Assert.Equal(SupervisionStatus.StartFailed, result.SupervisionStatus);
        Assert.Equal(0, result.Executed);
        Assert.False(File.Exists(fixture.LogPath));
        AssertSafe(result, fixture.Repository);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task TimeoutAndCancellationStopNewChildrenAfterCleanup(bool cancel)
    {
        using var fixture = new ReplayFixture();
        fixture.Add("a-tree");
        fixture.Add("z-last");
        using var cancellation = new CancellationTokenSource();
        var replay = new CorpusReplay(ReplayFixture.Supervisor(timeout: TimeSpan.FromSeconds(cancel ? 10 : 2)));
        Task<ReplayResult> pending = replay.RunAsync(fixture.Repository, cancellation.Token);
        string child = Path.Combine(fixture.Root, "filter-asciihex", "tree.child");
        if (cancel)
        {
            try { await WaitForFile(child); }
            finally { cancellation.Cancel(); }
        }
        ReplayResult result = await pending;

        Assert.Equal(cancel ? ReplayExitCode.Cancelled : ReplayExitCode.SupervisionFailure, result.ExitCode);
        Assert.Equal(cancel ? SupervisionStatus.Cancelled : SupervisionStatus.TimedOut, result.SupervisionStatus);
        Assert.Single(File.ReadAllLines(fixture.LogPath));
        Assert.Equal(0, result.Matched);
        await EnsureExited(child, expectAlreadyExited: true);
        await EnsureExited(Path.Combine(fixture.Root, "filter-asciihex", "tree.root"), expectAlreadyExited: true);
        AssertSafe(result, fixture.Repository);
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task TerminationFailureRemainsDistinctAndOwnedOrphanIsIndependentlyReaped()
    {
        using var fixture = new ReplayFixture();
        fixture.Add("a-orphanpipe");
        fixture.Add("z-last");
        string child = Path.Combine(fixture.Root, "filter-asciihex", "orphanpipe.child");
        try
        {
            ReplayResult result = await new CorpusReplay(ReplayFixture.Supervisor(timeout: TimeSpan.FromSeconds(2))).RunAsync(fixture.Repository);

            Assert.Equal(ReplayExitCode.SupervisionFailure, result.ExitCode);
            Assert.Equal(SupervisionStatus.TerminationFailed, result.SupervisionStatus);
            Assert.Single(File.ReadAllLines(fixture.LogPath));
            AssertSafe(result, fixture.Repository);
        }
        finally
        {
            await EnsureExited(child, expectAlreadyExited: false);
        }
    }

    [Fact]
    public async Task PreCancellationLaunchesNothing()
    {
        using var fixture = new ReplayFixture();
        fixture.Add("a-success");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        ReplayResult result = await new CorpusReplay(ReplayFixture.Supervisor()).RunAsync(fixture.Repository, cancellation.Token);
        Assert.Equal(ReplayExitCode.Cancelled, result.ExitCode);
        Assert.Equal(0, result.Attempted);
        Assert.False(File.Exists(fixture.LogPath));
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task ExplicitReplayCommandUsesRealSupervisedCorpus()
    {
        string repository = ReplayFixture.FindRepository();
        ReplayResult result = await ReplayCommand.ExecuteAsync(["replay", "--repository-root", repository]);

        Assert.Equal(ReplayExitCode.Success, result.ExitCode);
        Assert.Equal(15, result.Matched);
        AssertSafe(result, repository);
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task SpacesAndShellCharactersRemainLiteralWithRealWorker()
    {
        using var fixture = new ReplayFixture();
        fixture.Add("a-success");
        WorkerSupervisor supervisor = ReplayFixture.Supervisor(real: true);
        ProcessStartInfo start = supervisor.CreateStartInfo("filter-asciihex", fixture.SeedPath("a-success"), 1);
        Assert.False(start.UseShellExecute);
        Assert.Equal(fixture.SeedPath("a-success"), start.ArgumentList[5]);
        ReplayResult result = await new CorpusReplay(supervisor).RunAsync(fixture.Repository);

        Assert.Equal(ReplayExitCode.Success, result.ExitCode);
        AssertSafe(result, fixture.Repository);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("relative")]
    [InlineData("extra")]
    [InlineData("wrong-option")]
    [InlineData("null")]
    [InlineData("empty")]
    [InlineData("long")]
    public async Task InvalidInvocationIsBoundedAndDeterministic(string mode)
    {
        string[] args = mode switch
        {
            "missing" => ["replay"],
            "extra" => ["replay", "--repository-root", "PRIVATE", "--mutate"],
            "wrong-option" => ["replay", "--corpus", "PRIVATE"],
            "null" => null,
            "empty" => ["replay", "--repository-root", ""],
            "long" => ["replay", "--repository-root", new string('X', 32768)],
            _ => ["replay", "--repository-root", "PRIVATE"]
        };
        ReplayResult result = await ReplayCommand.ExecuteAsync(args);
        Assert.Equal(ReplayExitCode.InvalidInvocation, result.ExitCode);
        Assert.Equal(0, result.Attempted);
        Assert.Equal(result.Json, (await ReplayCommand.ExecuteAsync(args)).Json);
        AssertSafe(result, "PRIVATE");
    }

    [Fact]
    public void ExitCodesAreStable() => Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, new[]
    {
        (int)ReplayExitCode.Success, (int)ReplayExitCode.IntegrityFailure, (int)ReplayExitCode.OracleMismatch,
        (int)ReplayExitCode.SupervisionFailure, (int)ReplayExitCode.Cancelled, (int)ReplayExitCode.InvalidInvocation
    });

    private static void AssertSafe(ReplayResult result, string repository)
    {
        Assert.InRange(Encoding.UTF8.GetByteCount(result.Json), 1, ReplayResult.MaximumJsonBytes);
        using JsonDocument document = JsonDocument.Parse(result.Json);
        Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("piratepdf-fuzz-corpus", document.RootElement.GetProperty("manifestSchema").GetString());
        foreach (string secret in new[] { repository, "PRIVATE", "Exception", "stdout", "stderr", "pid", "duration", "timestamp", "observation" })
            Assert.DoesNotContain(secret, result.Json);
    }

    internal static async Task WaitForFile(string path)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!File.Exists(path) || new FileInfo(path).Length == 0) await Task.Delay(20, deadline.Token);
    }

    internal static async Task EnsureExited(string path, bool expectAlreadyExited)
    {
        await WaitForFile(path);
        string[] identity = File.ReadAllText(path).Split(':');
        Process process;
        try { process = Process.GetProcessById(int.Parse(identity[0], CultureInfo.InvariantCulture)); }
        catch (ArgumentException) { return; }
        using (process)
        {
            if (process.HasExited || process.StartTime.ToUniversalTime().Ticks != long.Parse(identity[1], CultureInfo.InvariantCulture)) return;
            bool exited = process.HasExited;
            process.Kill(entireProcessTree: true);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await process.WaitForExitAsync(deadline.Token);
            if (expectAlreadyExited) Assert.True(exited, "Controlled child remained alive after supervisor cleanup.");
        }
    }
}
