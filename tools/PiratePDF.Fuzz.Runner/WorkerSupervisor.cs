using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Runs one managed worker with a deadline and bounded output. Not an OS sandbox.</summary>
public sealed class WorkerSupervisor
{
    public const int MaximumRetainedBytes = 64 * 1024;
    public static readonly TimeSpan CaseTimeout = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan CleanupTimeout = TimeSpan.FromSeconds(5);

    private readonly string dotnetHost;
    private readonly string workerAssembly;
    private readonly TimeSpan timeout;

    // Trusted build identities for retained reproduction evidence, never paths from artifact metadata.
    internal string WorkerIdentity => AssemblyIdentity(workerAssembly);
    internal static string TargetIdentity => AssemblyIdentity(typeof(TargetRegistry).Assembly.Location);

    private static string AssemblyIdentity(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream));
    }

    /// <param name="dotnetHost">Trusted, absolute path to the installed dotnet host; never input-derived.</param>
    /// <param name="workerAssembly">Trusted, absolute path to the neutral runner assembly.</param>
    /// <param name="timeout">May shorten, but never extend, the ten-second case deadline.</param>
    public WorkerSupervisor(string dotnetHost, string workerAssembly, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dotnetHost);
        ArgumentException.ThrowIfNullOrWhiteSpace(workerAssembly);
        if (!Path.IsPathFullyQualified(dotnetHost) || !Path.IsPathFullyQualified(workerAssembly) ||
            !Path.GetFileName(dotnetHost).Equals(OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet", StringComparison.Ordinal) ||
            !workerAssembly.EndsWith(".dll", StringComparison.Ordinal))
        {
            throw new ArgumentException("An absolute dotnet host and worker assembly are required.");
        }

        this.timeout = timeout ?? CaseTimeout;
        if (this.timeout <= TimeSpan.Zero || this.timeout > CaseTimeout) throw new ArgumentOutOfRangeException(nameof(timeout));
        this.dotnetHost = dotnetHost;
        this.workerAssembly = workerAssembly;
    }

    public ProcessStartInfo CreateStartInfo(string targetId, string inputPath, int configurationVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetId);
        if (targetId.Length > 64) throw new ArgumentException("Invalid target ID.", nameof(targetId));
        FuzzTarget target = TargetRegistry.Get(targetId);
        ArgumentException.ThrowIfNullOrEmpty(inputPath);
        if (configurationVersion != target.ConfigurationVersion || inputPath.Length > 32767 || inputPath.Contains('\0'))
        {
            throw new ArgumentException("Invalid worker request.");
        }

        var start = new ProcessStartInfo(dotnetHost)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (string argument in new[] { workerAssembly, "run", "--target", target.Id, "--input", inputPath,
            "--config", "v" + configurationVersion.ToString(CultureInfo.InvariantCulture) })
        {
            start.ArgumentList.Add(argument);
        }

        // PP-AUD-029: managed heap only, not native/stacks/mappings/RSS. Preserve dump suppression.
        start.Environment["DOTNET_GCHeapHardLimit"] = "0x10000000";
        start.Environment["DOTNET_DbgEnableMiniDump"] = "0";
        return start;
    }

    public Task<SupervisionResult> RunAsync(string targetId, string inputPath, int configurationVersion,
        CancellationToken cancellationToken = default) => RunCoreAsync(targetId, inputPath, configurationVersion, null, 0, cancellationToken);

    /// <summary>Campaign-only gated worker; gate ownership remains with the caller until confirmed exit.</summary>
    public Task<SupervisionResult> RunCampaignAsync(string targetId, string inputPath, int configurationVersion,
        CampaignWorkerGate gate, int caseIndex, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gate);
        if (caseIndex is < 0 or >= CampaignConfiguration.MaximumCases) return Task.FromResult(new SupervisionResult(SupervisionStatus.InvalidRequest));
        return RunCoreAsync(targetId, inputPath, configurationVersion, gate, caseIndex, cancellationToken);
    }

    private async Task<SupervisionResult> RunCoreAsync(string targetId, string inputPath, int configurationVersion,
        CampaignWorkerGate? gate, int caseIndex, CancellationToken cancellationToken)
    {
        ProcessStartInfo start;
        FuzzTarget target;
        try
        {
            start = CreateStartInfo(targetId, inputPath, configurationVersion);
            target = TargetRegistry.Get(targetId);
            if (gate is not null)
            {
                start.ArgumentList.Clear();
                foreach (string argument in new[] { workerAssembly, "campaign-worker-internal", targetId, inputPath,
                    gate.PipeName, gate.InvocationHash, caseIndex.ToString(CultureInfo.InvariantCulture) })
                    start.ArgumentList.Add(argument);
            }
        }
        catch (ArgumentException)
        {
            return new(SupervisionStatus.InvalidRequest);
        }

        if (cancellationToken.IsCancellationRequested) return new(SupervisionStatus.Cancelled, target);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        using var process = new Process { StartInfo = start };
        try
        {
            if (!process.Start()) return new(SupervisionStatus.StartFailed, target);
        }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException or IOException)
        {
            return new(SupervisionStatus.StartFailed, target);
        }

        var output = new BoundedCapture();
        var errorOutput = new BoundedCapture();
        using var drainCancellation = new CancellationTokenSource();
        Task outputTask = output.DrainAsync(process.StandardOutput.BaseStream, drainCancellation.Token);
        Task errorTask = errorOutput.DrainAsync(process.StandardError.BaseStream, drainCancellation.Token);
        Task drains = Task.WhenAll(outputTask, errorTask);
        Task execution = Task.WhenAll(process.WaitForExitAsync(deadline.Token), drains);
        SupervisionStatus status = SupervisionStatus.Completed;
        try
        {
            if (gate is not null) await gate.RegisterAsync(process, caseIndex, deadline.Token).ConfigureAwait(false);
            // The deadline covers exit AND EOF; inherited pipes must not extend worker execution indefinitely.
            await execution.WaitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            status = cancellationToken.IsCancellationRequested ? SupervisionStatus.Cancelled : SupervisionStatus.TimedOut;
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or Win32Exception ||
            (gate is not null && error is ArgumentException or System.Text.Json.JsonException))
        {
            status = gate is { Acknowledged: false } ? SupervisionStatus.InvalidProtocol : SupervisionStatus.CaptureFailed;
        }

        // Cleanup has an independent finite budget even when the caller token is cancelled.
        using var cleanup = new CancellationTokenSource(CleanupTimeout);
        if (!process.HasExited)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception error) when (error is Win32Exception or InvalidOperationException or AggregateException or NotSupportedException)
            {
                status = SupervisionStatus.TerminationFailed;
            }

            try
            {
                await process.WaitForExitAsync(cleanup.Token).ConfigureAwait(false);
            }
            catch (Exception error) when (error is Win32Exception or InvalidOperationException or OperationCanceledException)
            {
                status = SupervisionStatus.TerminationFailed;
            }
        }

        if (gate is not null && !gate.ConfirmExit() && !await gate.CleanupAsync(cleanup.Token).ConfigureAwait(false))
            status = SupervisionStatus.TerminationFailed;

        try
        {
            await drains.WaitAsync(cleanup.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cleanup.IsCancellationRequested)
        {
            status = SupervisionStatus.TerminationFailed;
        }
        catch (Exception error) when (error is IOException or ObjectDisposedException)
        {
            if (status != SupervisionStatus.TerminationFailed) status = SupervisionStatus.CaptureFailed;
        }
        finally
        {
            await drainCancellation.CancelAsync().ConfigureAwait(false);
            process.StandardOutput.Close();
            process.StandardError.Close();
            // Observe fault/cancellation completion without an unbounded cleanup wait.
            _ = drains.ContinueWith(static task => _ = task.Exception, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            _ = execution.ContinueWith(static task => _ = task.Exception, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        int? exitCode = process.HasExited ? process.ExitCode : null;
        if (status != SupervisionStatus.Completed) return new(status, target, exitCode, output: output, error: errorOutput);
        if (exitCode is null or < 0 or > 4) return new(SupervisionStatus.AbnormalExit, target, exitCode, output: output, error: errorOutput);
        string? code = output.Truncated || errorOutput.Count != 0 || errorOutput.Truncated ? null :
            WorkerProtocol.Validate(output.Bytes, exitCode.Value, target);
        if (code is null) return new(SupervisionStatus.InvalidProtocol, target, exitCode, output: output, error: errorOutput);
        // A spontaneous worker cancellation is a failure; requested cancellation is handled above.
        status = exitCode is 3 or 4 ? SupervisionStatus.WorkerFailure : SupervisionStatus.Completed;
        return new(status, target, exitCode, code, output, errorOutput);
    }
}
