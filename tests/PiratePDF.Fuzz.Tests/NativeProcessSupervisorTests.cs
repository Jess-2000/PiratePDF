#nullable enable annotations
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

// Reflection keeps H2a tooling contracts internal without changing project/friend-assembly contracts.
public sealed class NativeProcessSupervisorTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "PiratePDF H2a " + Guid.NewGuid().ToString("N"));
    private static readonly Type Supervisor = typeof(ChildProcess).Assembly.GetType("PiratePDF.Fuzz.Runner.NativeProcessSupervisor", true)!;
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private static string Host => Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..",
        OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"));
    private static string Assembly => typeof(SupervisorTestChild).Assembly.Location;
    private string Lease => Path.Combine(directory, "child.lease");

    public NativeProcessSupervisorTests() => Directory.CreateDirectory(directory);

    [Theory]
    [InlineData("exit0", "Completed", 0)]
    [InlineData("exit17", "NonzeroExit", 17)]
    public async Task ExitCodesAreNativeFactsWithoutWorkerProtocol(string mode, string outcome, int code)
    {
        object result = await Run(New(), mode);
        Assert.Equal(outcome, Value(result, "Outcome").ToString());
        Assert.Equal(code, Value(result, "ExitCode"));
        Assert.Equal("Verified", Value(result, "Cleanup").ToString());
        Assert.Equal("RootOnly", Value(result, "Ownership").ToString());
    }

    [Fact]
    public async Task LaunchFailureDoesNotClaimAnOwnedProcess()
    {
        object result = await Run(New(), "exit0", executable: Path.Combine(directory, "missing.exe"));
        Assert.Equal("StartFailed", Value(result, "Outcome").ToString());
        Assert.Equal("NotLaunched", Value(result, "Ownership").ToString());
        Assert.Null(Value(result, "ExitCode"));
    }

    [Fact]
    public async Task PreCancellationDoesNotLaunch()
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        object result = await Run(New(), "hang", token: caller.Token);
        Assert.Equal("PreCancelled", Value(result, "Outcome").ToString());
        Assert.False(File.Exists(Path.Combine(directory, "root.identity")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationAndTimeoutHaveSeparateFactsAndReapRoot(bool cancel)
    {
        using var caller = new CancellationTokenSource();
        Task<object> run = Run(New(), "hang", seconds: cancel ? 10 : 1, token: caller.Token);
        await WaitForFile("root.identity");
        if (cancel) caller.Cancel();
        object result = await run;
        Assert.Equal(cancel ? "Cancelled" : "TimedOut", Value(result, "Outcome").ToString());
        Assert.Equal(cancel, Value(result, "Cancelled"));
        Assert.Equal(!cancel, Value(result, "TimedOut"));
        Assert.Equal("Verified", Value(result, "Cleanup").ToString());
        AssertGone("root.identity");
    }

    [Fact]
    public async Task CallerCancellationAtTimeoutBoundaryCannotProduceSuccess()
    {
        using var caller = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        object result = await Run(New(), "hang", seconds: 1, token: caller.Token);
        Assert.Contains(Value(result, "Outcome").ToString(), new[] { "Cancelled", "TimedOut" });
        Assert.NotEqual(Value(result, "Cancelled"), Value(result, "TimedOut"));
        Assert.Equal("Verified", Value(result, "Cleanup").ToString());
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task RetainedDirectChildIsReapedAfterRootExitAndInheritedPipesDrain()
    {
        object supervisor = New();
        Task<object> run = Run(supervisor, "root-child", lease: true);
        await WaitForChild(supervisor);
        File.WriteAllText(Path.Combine(directory, "release"), "release");
        object result = await run;
        Assert.True((bool)Value(result, "ChildOutlivedRoot")!);
        Assert.Equal("RootAndChild", Value(result, "Ownership").ToString());
        Assert.Equal("Verified", Value(result, "Cleanup").ToString());
        Assert.Equal(0, Value(result, "ExitCode"));
        AssertGone("root.identity");
        AssertGone("child.lease");
    }

    [Theory]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedDirectChildIsReapedOnTimeoutOrCancellation(bool cancel)
    {
        object supervisor = New();
        using var caller = new CancellationTokenSource();
        Task<object> run = Run(supervisor, "root-child", lease: true, seconds: cancel ? 10 : 2, token: caller.Token);
        await WaitForChild(supervisor);
        if (cancel) caller.Cancel();
        object result = await run;
        Assert.Equal(cancel ? "Cancelled" : "TimedOut", Value(result, "Outcome").ToString());
        Assert.Equal("RootAndChild", Value(result, "Ownership").ToString());
        Assert.Equal("Verified", Value(result, "Cleanup").ToString());
        AssertGone("child.lease");
    }

    [Theory]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    [InlineData("hang", "Uncertain")]
    [InlineData("malformed", "Rejected")]
    [InlineData("self", "Rejected")]
    public async Task MissingMalformedOrRootIdentityCannotCertifyOwnership(string mode, string ownership)
    {
        object result = await Run(New(), mode, lease: true, seconds: 1);
        Assert.Equal(ownership, Value(result, "Ownership").ToString());
        Assert.Equal("Uncertain", Value(result, "Cleanup").ToString());
        AssertGone("root.identity");
    }

    [Theory]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    [InlineData("stale")]
    [InlineData("unrelated")]
    [InlineData("parent")]
    public async Task RejectedIdentitiesNeverTerminateUnrelatedProcess(string identity)
    {
        Task<object> run = Run(New(), "external", lease: true);
        await WaitForFile("root.identity");
        using Process unrelated = Start("hang", Path.Combine(directory, "unrelated"));
        using Process self = Process.GetCurrentProcess();
        Process advertised = identity == "parent" ? self : unrelated;
        long ticks = advertised.StartTime.ToUniversalTime().Ticks + (identity == "stale" ? 1 : 0);
        string temporary = Path.Combine(directory, "external.pending");
        File.WriteAllText(temporary, advertised.Id.ToString(CultureInfo.InvariantCulture) + ":" + ticks.ToString(CultureInfo.InvariantCulture));
        File.Move(temporary, Path.Combine(directory, "external.identity"));
        try
        {
            object result = await run;
            Assert.Equal("Rejected", Value(result, "Ownership").ToString());
            Assert.Equal("Uncertain", Value(result, "Cleanup").ToString());
            Assert.False(unrelated.HasExited);
            Assert.False(self.HasExited);
        }
        finally
        {
            if (!unrelated.HasExited) unrelated.Kill(entireProcessTree: true);
            await unrelated.WaitForExitAsync();
        }
    }

    [Theory]
    [InlineData("stdout", 65536, 0)]
    [InlineData("stderr", 0, 65536)]
    [InlineData("both", 65536, 65536)]
    public async Task SaturatedPipesRemainByteBoundedAndContinueDraining(string mode, int output, int error)
    {
        object result = await Run(New(), mode);
        Assert.Equal("Completed", Value(result, "Outcome").ToString());
        Assert.Equal(output, Value(result, "OutputBytes"));
        Assert.Equal(error, Value(result, "ErrorBytes"));
        Assert.Equal(output != 0, Value(result, "OutputTruncated"));
        Assert.Equal(error != 0, Value(result, "ErrorTruncated"));
        Assert.Equal("Verified", Value(result, "Cleanup").ToString());
    }

    [Fact]
    public async Task DeniedTerminationCannotCertifyCleanup()
    {
        object supervisor = New(_ => throw new Win32Exception(5));
        object result = await Run(supervisor, "hang", seconds: 1);
        Assert.Equal("Failed", Value(result, "Cleanup").ToString());
        Assert.False((bool)Value(result, "ExecutionVerified")!);
        Assert.False((bool)Value(result, "OutputTruncated")!);
        // This fixture owns the process; the supervisor deliberately failed to terminate it.
        KillFixture("root.identity");
    }

    [Fact]
    public async Task UnobservedInheritedPipeCannotBeReportedAsSuccessfulCleanup()
    {
        using var caller = new CancellationTokenSource();
        Task<object> run = Run(New(), "inherited-unleased", token: caller.Token);
        try
        {
            await WaitForFile("unowned.identity");
            using Process? root = FixtureProcess("root.identity");
            using Process? child = FixtureProcess("unowned.identity");
            Assert.NotNull(root);
            Assert.NotNull(child);
            // Retain the query handle before releasing the root so its exit code survives process teardown.
            _ = root.SafeHandle;
            File.WriteAllText(Path.Combine(directory, "release"), "release");
            await root.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(0, root.ExitCode);
            Assert.False(child.HasExited);
            // Establish the orphaned-pipe precondition before cleanup, rather than racing startup against a short deadline.
            caller.Cancel();
            object result = await run;
            Assert.Equal("Failed", Value(result, "Cleanup").ToString());
            Assert.False((bool)Value(result, "ExecutionVerified")!);
        }
        finally { caller.Cancel(); KillFixture("unowned.identity"); await run; }
    }

    [Fact]
    public async Task CaptureFailureCannotBecomeVerifiedExecution()
    {
        object supervisor = New(captureSource: _ => new FailedReadStream());
        object result = await Run(supervisor, "hang");
        Assert.Equal("CaptureFailed", Value(result, "Outcome").ToString());
        Assert.False((bool)Value(result, "ExecutionVerified")!);
        AssertGone("root.identity");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task InvalidDeadlineCannotLaunch(int seconds)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Run(New(), "hang", seconds: seconds));
        Assert.False(File.Exists(Path.Combine(directory, "root.identity")));
    }

    [Fact]
    public async Task ExistingLeaseIsNotOverwrittenOrConsumed()
    {
        File.WriteAllText(Lease, "foreign evidence");
        await Assert.ThrowsAsync<IOException>(() => Run(New(), "hang", lease: true));
        Assert.Equal("foreign evidence", File.ReadAllText(Lease));
        Assert.False(File.Exists(Path.Combine(directory, "root.identity")));
    }

    private static object New(Action<Process>? terminate = null, Func<Stream, Stream>? captureSource = null) =>
        Activator.CreateInstance(Supervisor, Members, null, [terminate, captureSource], null)!;
    private static object? Value(object result, string name) => result.GetType().GetProperty(name, Members)!.GetValue(result);

    private async Task<object> Run(object supervisor, string mode, bool lease = false, int seconds = 10,
        CancellationToken token = default, string? executable = null)
    {
        string[] arguments = [Assembly, "native-probe", mode, directory, "unused"];
        var task = (Task)Supervisor.GetMethod("RunAsync", Members)!.Invoke(supervisor,
            [executable ?? Host, arguments, directory, lease ? Lease : null, TimeSpan.FromSeconds(seconds), token])!;
        await task;
        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static Process Start(string mode, string area)
    {
        Directory.CreateDirectory(area);
        var start = new ProcessStartInfo(Host) { UseShellExecute = false, CreateNoWindow = true };
        foreach (string argument in new[] { Assembly, "native-probe", mode, area, "unused" }) start.ArgumentList.Add(argument);
        return Process.Start(start)!;
    }

    private async Task WaitForChild(object supervisor)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!(bool)Supervisor.GetProperty("ChildObserved", Members)!.GetValue(supervisor)!)
            await Task.Delay(10, deadline.Token);
    }

    private async Task WaitForFile(string name)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!File.Exists(Path.Combine(directory, name))) await Task.Delay(10, deadline.Token);
    }

    private Process? FixtureProcess(string name)
    {
        string path = Path.Combine(directory, name);
        if (!File.Exists(path)) return null;
        string[] identity = File.ReadAllText(path).Split(':');
        if (identity.Length != 2 || !int.TryParse(identity[0], out int pid) || pid == Environment.ProcessId ||
            !long.TryParse(identity[1], out long ticks)) return null;
        Process process;
        try { process = Process.GetProcessById(pid); }
        catch (ArgumentException) { return null; }
        if (process.StartTime.ToUniversalTime().Ticks == ticks) return process;
        process.Dispose();
        return null;
    }

    private void AssertGone(string name)
    {
        using Process? process = FixtureProcess(name);
        Assert.True(process is null || process.HasExited);
    }

    private void KillFixture(string name)
    {
        using Process? process = FixtureProcess(name);
        if (process is not null && !process.HasExited) { process.Kill(entireProcessTree: true); process.WaitForExit(5000); }
    }

    public void Dispose()
    {
        KillFixture("root.identity");
        // Never use the deliberately forged/rejected lease as fixture cleanup authority.
        KillFixture("owned.child.identity");
        Directory.Delete(directory, recursive: true); // Only this test's fresh temporary directory.
    }

    private sealed class FailedReadStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("Controlled capture failure.");
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromException<int>(new IOException("Controlled capture failure."));
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
