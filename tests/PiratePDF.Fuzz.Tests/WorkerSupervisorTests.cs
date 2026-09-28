using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class WorkerSupervisorTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "PiratePDF Supervisor " + Guid.NewGuid().ToString("N"));
    private static string Host => Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..",
        OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"));

    public WorkerSupervisorTests() => Directory.CreateDirectory(directory);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RealWorkerSuccessAndRejectionAreValidated(bool reject)
    {
        string path = Path.Combine(directory, "case with spaces.bin");
        File.WriteAllBytes(path, reject ? "G>"u8.ToArray() : "4869>"u8.ToArray());
        var supervisor = new WorkerSupervisor(Host, typeof(RunCommand).Assembly.Location);
        SupervisionResult result = await supervisor.RunAsync("filter-asciihex", path, 1);
        Assert.Equal(SupervisionStatus.Completed, result.Status);
        Assert.Equal(OperatingSystem.IsWindows() ? (reject ? 1 : 0) : 2, result.ExitCode);
        Assert.Equal(OperatingSystem.IsWindows() ? "TargetResult" : "UnsupportedInputPlatform", result.WorkerCode);
        AssertSafe(result);
        Assert.Equal(result.ToJson(), (await supervisor.RunAsync("filter-asciihex", path, 1)).ToJson());
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("duplicate")]
    [InlineData("duplicate-property")]
    [InlineData("oversized-record")]
    [InlineData("extra")]
    [InlineData("wrong-target")]
    [InlineData("wrong-config")]
    [InlineData("wrong-schema")]
    [InlineData("wrong-type")]
    [InlineData("wrong-outcome")]
    [InlineData("wrong-code")]
    [InlineData("wrong-digest")]
    [InlineData("invalid-utf8")]
    [InlineData("stderr-only")]
    [InlineData("stderr-message")]
    [InlineData("mismatch")]
    public async Task InvalidProtocolNeverBecomesAResult(string mode)
    {
        SupervisionResult result = await RunProbe(mode);
        Assert.Equal(SupervisionStatus.InvalidProtocol, result.Status);
        Assert.Null(result.WorkerCode);
        AssertSafe(result);
    }

    [Fact]
    public async Task AbnormalNonProtocolExitIsNotTargetRejection()
    {
        SupervisionResult result = await RunProbe("abnormal");
        Assert.Equal(SupervisionStatus.AbnormalExit, result.Status);
        Assert.Equal(91, result.ExitCode);
        AssertSafe(result);
    }

    [Theory]
    [InlineData("worker-failure", 4)]
    [InlineData("spontaneous-cancel", 3)]
    public async Task WorkerDefectsAndUnrequestedCancellationFail(string mode, int exitCode)
    {
        SupervisionResult result = await RunProbe(mode);
        Assert.Equal(SupervisionStatus.WorkerFailure, result.Status);
        Assert.Equal(exitCode, result.ExitCode);
        AssertSafe(result);
    }

    [Theory]
    [InlineData("stdout-exact", true, false)]
    [InlineData("stdout-over", true, true)]
    [InlineData("unicode-over", true, true)]
    [InlineData("stderr-exact", false, false)]
    [InlineData("stderr-over", false, true)]
    public async Task RetentionIsByteBoundedAndTruncationIsExplicit(string mode, bool stdout, bool truncated)
    {
        SupervisionResult result = await RunProbe(mode);
        Assert.Equal(SupervisionStatus.InvalidProtocol, result.Status);
        Assert.Equal(65536, stdout ? result.StdoutRetainedBytes : result.StderrRetainedBytes);
        Assert.Equal(truncated, stdout ? result.StdoutTruncated : result.StderrTruncated);
        AssertSafe(result);
    }

    [Fact]
    public async Task BothPipesContinueDrainingBeyondTheirCaps()
    {
        SupervisionResult result = await RunProbe("both-over");
        Assert.Equal(SupervisionStatus.InvalidProtocol, result.Status);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(65536, result.StdoutRetainedBytes);
        Assert.Equal(65536, result.StderrRetainedBytes);
        Assert.True(result.StdoutTruncated);
        Assert.True(result.StderrTruncated);
        AssertSafe(result);
    }

    [Theory]
    [InlineData("record-exact", SupervisionStatus.Completed)]
    [InlineData("record-over", SupervisionStatus.InvalidProtocol)]
    public async Task ProtocolByteCeilingIncludesTheWholeRecord(string mode, SupervisionStatus status)
    {
        SupervisionResult result = await RunProbe(mode);
        Assert.Equal(status, result.Status);
        AssertSafe(result);
    }

    [Fact]
    public async Task AnExitedParentWithAnOpenDescendantPipeFailsFiniteCleanup()
    {
        string childIdentity = Path.Combine(directory, "orphanpipe.child");
        try
        {
            SupervisionResult result = await RunProbe("orphanpipe", TimeSpan.FromSeconds(1));
            Assert.Equal(SupervisionStatus.TerminationFailed, result.Status);
            AssertSafe(result);
            await AssertExited(Path.Combine(directory, "orphanpipe.root"));
        }
        finally
        {
            // This intentionally orphaned fixture cannot be discovered through an exited parent.
            // The test owns its PID/start-time identity and must independently reap it.
            await WaitForIdentity(childIdentity);
            using Process child = OpenIdentity(childIdentity);
            if (child is not null && !child.HasExited)
            {
                child.Kill(entireProcessTree: true);
                using var wait = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await child.WaitForExitAsync(wait.Token);
            }
        }
    }

    [Fact]
    public async Task FloodingChildStillTimesOutAndIsReaped()
    {
        SupervisionResult result = await RunProbe("flood", TimeSpan.FromSeconds(2));
        Assert.Equal(SupervisionStatus.TimedOut, result.Status);
        Assert.True(result.StdoutTruncated);
        Assert.True(result.StderrTruncated);
        await AssertExited(Path.Combine(directory, "flood.root"));
        AssertSafe(result);
    }

    [Fact]
    public async Task TimeoutKillsAndReapsTheRootAndItsControlledDescendant()
    {
        SupervisionResult result = await RunProbe("tree", TimeSpan.FromSeconds(2));
        Assert.Equal(SupervisionStatus.TimedOut, result.Status);
        await AssertExited(Path.Combine(directory, "tree.root"));
        await AssertExited(Path.Combine(directory, "tree.child"));
        AssertSafe(result);
    }

    [Fact]
    public async Task ExplicitCancellationKillsTheTreeAndIsNotTimeout()
    {
        using var cancellation = new CancellationTokenSource();
        Task<SupervisionResult> pending = Probe().RunAsync("filter-asciihex", Path.Combine(directory, "tree"), 1, cancellation.Token);
        try
        {
            await WaitForIdentity(Path.Combine(directory, "tree.child"));
        }
        finally
        {
            cancellation.Cancel();
        }
        SupervisionResult result = await pending;
        Assert.Equal(SupervisionStatus.Cancelled, result.Status);
        await AssertExited(Path.Combine(directory, "tree.root"));
        await AssertExited(Path.Combine(directory, "tree.child"));
        AssertSafe(result);
    }

    [Fact]
    public async Task PreCancellationDoesNotStartAChild()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        SupervisionResult result = await Probe().RunAsync("filter-asciihex", Path.Combine(directory, "tree"), 1, cancellation.Token);
        Assert.Equal(SupervisionStatus.Cancelled, result.Status);
        Assert.Null(result.ExitCode);
        Assert.False(File.Exists(Path.Combine(directory, "tree.root")));
    }

    [Fact]
    public async Task MissingHostReportsStartFailureWithoutPaths()
    {
        var supervisor = new WorkerSupervisor(Path.Combine(directory, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"),
            typeof(SupervisorTestChild).Assembly.Location);
        SupervisionResult result = await supervisor.RunAsync("filter-asciihex", "PRIVATE_PATH", 1);
        Assert.Equal(SupervisionStatus.StartFailed, result.Status);
        Assert.Null(result.ExitCode);
        AssertSafe(result);
    }

    [Theory]
    [InlineData("unknown", 1)]
    [InlineData("filter-asciihex", 2)]
    public async Task InvalidRequestsNeverStartWorker(string id, int version)
    {
        SupervisionResult result = await Probe().RunAsync(id, Path.Combine(directory, "tree"), version);
        Assert.Equal(SupervisionStatus.InvalidRequest, result.Status);
        Assert.Null(result.ExitCode);
        Assert.Null(result.TargetId);
        Assert.False(File.Exists(Path.Combine(directory, "tree.root")));
    }

    [Fact]
    public async Task UsesNoShellAndPassesSpacesAndMetacharactersAsOneArgument()
    {
        const string name = "args with spaces & ; $() ' literal";
        string path = Path.Combine(directory, name);
        ProcessStartInfo start = Probe().CreateStartInfo("filter-asciihex", path, 1);
        Assert.False(start.UseShellExecute);
        Assert.True(start.CreateNoWindow);
        Assert.True(start.RedirectStandardOutput);
        Assert.True(start.RedirectStandardError);
        Assert.Equal(Host, start.FileName);
        Assert.Equal(string.Empty, start.Arguments);
        Assert.Equal(new[] { typeof(SupervisorTestChild).Assembly.Location, "run", "--target", "filter-asciihex",
            "--input", path, "--config", "v1" }, start.ArgumentList);
        Assert.Equal("0x10000000", start.Environment["DOTNET_GCHeapHardLimit"]);
        Assert.Equal("0", start.Environment["DOTNET_DbgEnableMiniDump"]);
        SupervisionResult result = await RunProbe(name);
        Assert.Equal(SupervisionStatus.Completed, result.Status);
        Assert.Equal(0, result.ExitCode);
        AssertSafe(result);
    }

    [Fact]
    public void TimeoutCannotBeDisabledOrExtendedAndShellHostsAreRejected()
    {
        Assert.Equal(TimeSpan.FromSeconds(10), WorkerSupervisor.CaseTimeout);
        Assert.Equal(TimeSpan.FromSeconds(5), WorkerSupervisor.CleanupTimeout);
        Assert.Throws<ArgumentOutOfRangeException>(() => Probe(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => Probe(Timeout.InfiniteTimeSpan));
        Assert.Throws<ArgumentOutOfRangeException>(() => Probe(TimeSpan.FromSeconds(11)));
        Assert.Throws<ArgumentException>(() => new WorkerSupervisor(Path.Combine(directory, "cmd.exe"),
            typeof(SupervisorTestChild).Assembly.Location));
    }

    private static WorkerSupervisor Probe(TimeSpan? timeout = null) =>
        new(Host, typeof(SupervisorTestChild).Assembly.Location, timeout);

    private Task<SupervisionResult> RunProbe(string mode, TimeSpan? timeout = null) =>
        Probe(timeout).RunAsync("filter-asciihex", Path.Combine(directory, mode), 1);

    private static void AssertSafe(SupervisionResult result)
    {
        Assert.InRange(result.StdoutRetainedBytes, 0, 65536);
        Assert.InRange(result.StderrRetainedBytes, 0, 65536);
        string json = result.ToJson();
        Assert.InRange(Encoding.UTF8.GetByteCount(json), 1, 512);
        Assert.DoesNotContain("PRIVATE", json);
        Assert.DoesNotContain("PiratePDF Supervisor", json);
        Assert.DoesNotContain("XXXX", json);
        Assert.DoesNotContain("Exception", json);
        Assert.DoesNotContain("DOTNET", json);
    }

    private static async Task WaitForIdentity(string path)
    {
        using var wait = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!File.Exists(path) || new FileInfo(path).Length == 0) await Task.Delay(10, wait.Token);
    }

    private static async Task AssertExited(string path)
    {
        await WaitForIdentity(path);
        using Process process = OpenIdentity(path);
        if (process is null || process.HasExited) return;
        using var wait = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try { await process.WaitForExitAsync(wait.Token); }
        finally { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        Assert.True(process.HasExited);
    }

    private static Process OpenIdentity(string path)
    {
        string[] identity = File.ReadAllText(path).Split(':');
        int pid = int.Parse(identity[0], CultureInfo.InvariantCulture);
        long ticks = long.Parse(identity[1], CultureInfo.InvariantCulture);
        Process process;
        try { process = Process.GetProcessById(pid); }
        catch (ArgumentException) { return null; }
        if (process.HasExited || process.StartTime.ToUniversalTime().Ticks != ticks)
        {
            process.Dispose();
            return null;
        }
        return process;
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);
}
