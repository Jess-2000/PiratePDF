using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class SmokeChildProcessTests : IDisposable
{
    private readonly string directory = Path.Combine(ReplayFixture.FindRepository(), "artifacts", "fuzz",
        "smoke-process-tests", Guid.NewGuid().ToString("N"));
    private static string Host => Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..", "dotnet.exe"));
    private static string Child => typeof(SupervisorTestChild).Assembly.Location;
    private string Lease => Path.Combine(directory, "child.lease");

    public SmokeChildProcessTests() => Directory.CreateDirectory(directory);

    [Fact]
    public async Task BothPipesAreByteBoundedAndFullyDrained()
    {
        ChildResult result = await RunProbe("both-over");
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(65536, result.Output.Length);
        Assert.Equal(65536, result.Error.Length);
        Assert.True(result.OutputTruncated);
        Assert.True(result.ErrorTruncated);
        Assert.True(result.CleanupSucceeded);
        Assert.False(result.TimedOut);
        Assert.False(result.Cancelled);
    }

    [Fact]
    public async Task AbnormalExitIsPreserved()
    {
        ChildResult result = await RunProbe("abnormal");
        Assert.Equal(91, result.ExitCode);
        Assert.True(result.CleanupSucceeded);
        Assert.False(result.TimedOut);
        Assert.False(result.Cancelled);
    }

    [Fact]
    public async Task ExplicitCancellationReapsObservedLeasedProcess()
    {
        using var cancellation = new CancellationTokenSource();
        Task<ChildResult> run = ChildProcess.RunAsync(Host, [Child, "grandchild", Lease], directory,
            TimeSpan.FromSeconds(10), Lease, cancellation.Token);
        try
        {
            await CorpusReplayTests.WaitForFile(Lease);
            cancellation.Cancel();
            ChildResult result = await run;
            Assert.True(result.Cancelled);
            Assert.False(result.TimedOut);
            Assert.True(result.CleanupSucceeded);
            AssertLeasedProcessExited();
        }
        finally
        {
            cancellation.Cancel();
            await run;
        }
    }

    [Fact]
    public async Task TimeoutReapsObservedLeasedProcess()
    {
        ChildResult result = await ChildProcess.RunAsync(Host, [Child, "grandchild", Lease], directory,
            TimeSpan.FromSeconds(2), Lease);
        Assert.True(result.TimedOut);
        Assert.False(result.Cancelled);
        Assert.True(result.CleanupSucceeded);
        AssertLeasedProcessExited();
    }

    [Fact]
    public async Task PreCancellationDoesNotLaunch()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => ChildProcess.RunAsync(Host,
            [Child, "grandchild", Lease], directory, TimeSpan.FromSeconds(1), Lease, cancellation.Token));
        Assert.False(File.Exists(Lease));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task InvalidDeadlineDoesNotLaunch(int seconds)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ChildProcess.RunAsync(Host,
            [Child, "grandchild", Lease], directory, TimeSpan.FromSeconds(seconds), Lease));
        Assert.False(File.Exists(Lease));
    }

    private Task<ChildResult> RunProbe(string mode) => ChildProcess.RunAsync(Host,
        [Child, "run", "--target", "filter-asciihex", "--input", Path.Combine(directory, mode), "--config", "v1"],
        directory, TimeSpan.FromSeconds(10), Lease);

    private void AssertLeasedProcessExited()
    {
        string[] identity = File.ReadAllText(Lease).Split(':');
        int pid = int.Parse(identity[0], CultureInfo.InvariantCulture);
        long ticks = long.Parse(identity[1], CultureInfo.InvariantCulture);
        Process process;
        try { process = Process.GetProcessById(pid); }
        catch (ArgumentException) { return; }
        using (process) Assert.True(process.HasExited || process.StartTime.ToUniversalTime().Ticks != ticks);
    }

    public void Dispose()
    {
        foreach (string file in Directory.EnumerateFiles(directory)) File.Delete(file);
        Directory.Delete(directory);
    }
}
