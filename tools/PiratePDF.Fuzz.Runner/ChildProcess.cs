using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace PiratePDF.Fuzz.Runner;

public sealed record ChildResult(int ExitCode, bool TimedOut, string Output, string Error)
{
    public bool Cancelled { get; init; }
    public bool CleanupSucceeded { get; init; }
    public bool OutputTruncated { get; init; }
    public bool ErrorTruncated { get; init; }
}

/// <summary>Bounded smoke-only execution of a trusted driver and its cooperating leased child.</summary>
public static class ChildProcess
{
    public static async Task<ChildResult> RunAsync(
        string executable, IReadOnlyList<string> arguments, string workingDirectory,
        TimeSpan timeout, string leasePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromSeconds(20))
            throw new ArgumentOutOfRangeException(nameof(timeout));
        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(leasePath)) throw new IOException("Smoke lease already exists.");

        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory
        };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        start.Environment["DOTNET_GCHeapHardLimit"] = "0x10000000";
        start.Environment["DOTNET_DbgEnableMiniDump"] = "0";
        start.Environment["B9_CHILD_LEASE"] = leasePath;
        using var process = new Process { StartInfo = start };
        process.Start();
        var output = new BoundedCapture();
        var error = new BoundedCapture();
        using var drainCancellation = new CancellationTokenSource();
        Task drains = Task.WhenAll(
            output.DrainAsync(process.StandardOutput.BaseStream, drainCancellation.Token),
            error.DrainAsync(process.StandardError.BaseStream, drainCancellation.Token));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        bool timedOut = false;
        bool cancelled = false;
        bool cleanupSucceeded = true;
        try
        {
            await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            cancelled = cancellationToken.IsCancellationRequested;
            timedOut = !cancelled;
        }
        finally
        {
            // One independent cleanup budget covers root, cooperating child and inherited pipes.
            using var cleanup = new CancellationTokenSource(WorkerSupervisor.CleanupTimeout);
            try
            {
                await TerminateAsync(process, cleanup.Token).ConfigureAwait(false);
                await ReapLeaseAsync(leasePath, cleanup.Token).ConfigureAwait(false);
                await drains.WaitAsync(cleanup.Token).ConfigureAwait(false);
            }
            catch (Exception failure) when (failure is IOException or Win32Exception or InvalidOperationException
                or OperationCanceledException or AggregateException or NotSupportedException)
            {
                cleanupSucceeded = false;
            }
            finally
            {
                await drainCancellation.CancelAsync().ConfigureAwait(false);
                process.StandardOutput.Close();
                process.StandardError.Close();
                _ = drains.ContinueWith(static task => _ = task.Exception, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        return new ChildResult(process.HasExited ? process.ExitCode : -1, timedOut,
            Encoding.UTF8.GetString(output.Bytes.Span), Encoding.UTF8.GetString(error.Bytes.Span))
        {
            Cancelled = cancelled,
            CleanupSucceeded = cleanupSucceeded,
            OutputTruncated = output.Truncated,
            ErrorTruncated = error.Truncated
        };
    }

    private static async Task TerminateAsync(Process process, CancellationToken token)
    {
        if (process.HasExited) return;
        process.Kill(entireProcessTree: true);
        await process.WaitForExitAsync(token).ConfigureAwait(false);
    }

    private static async Task ReapLeaseAsync(string path, CancellationToken token)
    {
        if (!File.Exists(path)) return;
        ReadOnlyMemory<byte> bytes = await InputFile.ReadAsync(path, 100, token).ConfigureAwait(false);
        string[] lease = Encoding.ASCII.GetString(bytes.Span).Split(':');
        if (lease.Length != 2 || !int.TryParse(lease[0], NumberStyles.None, CultureInfo.InvariantCulture, out int pid)
            || pid <= 0 || pid == Environment.ProcessId
            || !long.TryParse(lease[1], NumberStyles.None, CultureInfo.InvariantCulture, out long ticks) || ticks <= 0)
            throw new InvalidDataException("Invalid child lease.");
        Process child;
        try { child = Process.GetProcessById(pid); }
        catch (ArgumentException) { return; }
        using (child)
        {
            // PID alone is insufficient: only terminate the exact cooperating process instance.
            if (!child.HasExited && child.StartTime.ToUniversalTime().Ticks == ticks)
                await TerminateAsync(child, token).ConfigureAwait(false);
        }
    }
}
