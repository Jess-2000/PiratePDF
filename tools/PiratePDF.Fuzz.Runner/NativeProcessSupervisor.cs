using System.Diagnostics;
using System.Text;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Trusted process execution only; no campaign, native-driver protocol or arbitrary-descendant containment.</summary>
internal sealed class NativeProcessSupervisor
{
    private readonly Action<Process> terminate;
    private readonly Func<Stream, Stream> captureSource;
    internal bool ChildObserved { get; private set; }

    // The internal termination seam permits a deterministic access-denied cleanup test; admission is never replaceable.
    internal NativeProcessSupervisor(Action<Process>? terminate = null, Func<Stream, Stream>? captureSource = null)
    {
        this.terminate = terminate ?? (process => process.Kill(entireProcessTree: true));
        this.captureSource = captureSource ?? (stream => stream);
    }

    internal async Task<NativeProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments,
        string workingDirectory, string? childLease, TimeSpan timeout, CancellationToken caller = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (!Path.IsPathFullyQualified(executable) || !Path.IsPathFullyQualified(workingDirectory) ||
            childLease is not null && !Path.IsPathFullyQualified(childLease) || timeout <= TimeSpan.Zero || timeout > TimeSpan.FromSeconds(20))
            throw new ArgumentException("Trusted absolute paths and a bounded H2a deadline are required.");
        if (childLease is not null && Path.Exists(childLease)) throw new IOException("Ownership evidence already exists.");
        ChildObserved = false;
        var output = new BoundedCapture();
        var error = new BoundedCapture();
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true,
            RedirectStandardError = true, WorkingDirectory = workingDirectory
        };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        start.Environment["DOTNET_GCHeapHardLimit"] = "0x10000000";
        start.Environment["DOTNET_DbgEnableMiniDump"] = "0";
        if (childLease is not null) start.Environment["B9_CHILD_LEASE"] = childLease;
        using var root = new Process { StartInfo = start };
        Process? child = null;
        NativeExecutionOutcome outcome = NativeExecutionOutcome.Completed;
        NativeOwnership ownership = NativeOwnership.NotLaunched;
        NativeCleanup cleanup = NativeCleanup.NotRequired;
        bool cancelled = false, timedOut = false, childOutlivedRoot = false, launched = false;
        int? rootId = null, childId = null, exit = null;
        long? rootTicks = null, childTicks = null;
        Task drains = Task.CompletedTask;
        using var drainCancellation = new CancellationTokenSource();
        // Starts before synchronous Start: elapsed startup counts, but a blocked OS Start cannot be interrupted here.
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(caller);
        deadline.CancelAfter(timeout);
        try
        {
            if (caller.IsCancellationRequested) { cancelled = true; outcome = NativeExecutionOutcome.PreCancelled; }
            else
            {
                try
                {
                    launched = root.Start();
                    if (!launched) outcome = NativeExecutionOutcome.StartFailed;
                }
                catch (Exception failure) when (ProcessExecution.IsProcessError(failure)) { outcome = NativeExecutionOutcome.StartFailed; }
                if (launched)
                {
                    _ = root.SafeHandle;
                    rootId = root.Id;
                    rootTicks = root.StartTime.ToUniversalTime().Ticks;
                    ownership = childLease is null ? NativeOwnership.RootOnly : NativeOwnership.Uncertain;
                    drains = Task.WhenAll(output.DrainAsync(captureSource(root.StandardOutput.BaseStream), drainCancellation.Token),
                        error.DrainAsync(captureSource(root.StandardError.BaseStream), drainCancellation.Token));
                    while (true)
                    {
                        deadline.Token.ThrowIfCancellationRequested();
                        if (childLease is not null && child is null && Path.Exists(childLease))
                        {
                            try
                            {
                                child = await ProcessExecution.AdmitChildAsync(root, childLease, deadline.Token).ConfigureAwait(false);
                                childId = child.Id;
                                childTicks = child.StartTime.ToUniversalTime().Ticks;
                                ownership = NativeOwnership.RootAndChild;
                                ChildObserved = true;
                            }
                            catch (Exception failure) when (failure is PlatformNotSupportedException or DllNotFoundException or EntryPointNotFoundException)
                            { ownership = NativeOwnership.Uncertain; outcome = NativeExecutionOutcome.OwnershipUncertain; break; }
                            catch (Exception failure) when (ProcessExecution.IsProcessError(failure))
                            { ownership = NativeOwnership.Rejected; outcome = NativeExecutionOutcome.OwnershipRejected; break; }
                        }
                        if (drains.IsFaulted) { outcome = NativeExecutionOutcome.CaptureFailed; break; }
                        if (root.HasExited)
                        {
                            childOutlivedRoot = child is not null && !child.HasExited;
                            if (childOutlivedRoot || childLease is not null && child is null || drains.IsCompleted) break;
                        }
                        await Task.Delay(TimeSpan.FromMilliseconds(10), deadline.Token).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            cancelled = caller.IsCancellationRequested;
            timedOut = !cancelled;
            outcome = cancelled ? NativeExecutionOutcome.Cancelled : NativeExecutionOutcome.TimedOut;
        }
        catch (Exception failure) when (ProcessExecution.IsProcessError(failure))
        { outcome = NativeExecutionOutcome.OwnershipUncertain; ownership = NativeOwnership.Uncertain; }
        finally
        {
            if (launched)
            {
                using var allowance = new CancellationTokenSource(WorkerSupervisor.CleanupTimeout);
                bool exited = await ReapAsync(root, allowance.Token).ConfigureAwait(false);
                if (child is not null) exited &= await ReapAsync(child, allowance.Token).ConfigureAwait(false);
                bool drained = false;
                try { await drains.WaitAsync(allowance.Token).ConfigureAwait(false); drained = true; }
                catch (OperationCanceledException) when (allowance.IsCancellationRequested) { }
                catch (Exception failure) when (ProcessExecution.IsProcessError(failure)) { outcome = NativeExecutionOutcome.CaptureFailed; }
                cleanup = !exited || !drained ? NativeCleanup.Failed :
                    ownership is NativeOwnership.Uncertain or NativeOwnership.Rejected ? NativeCleanup.Uncertain : NativeCleanup.Verified;
                try { if (root.HasExited) exit = root.ExitCode; }
                catch (InvalidOperationException) { cleanup = NativeCleanup.Failed; }
                await drainCancellation.CancelAsync().ConfigureAwait(false);
                root.StandardOutput.Close();
                root.StandardError.Close();
                _ = drains.ContinueWith(task => _ = task.Exception, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            child?.Dispose();
        }
        if (outcome == NativeExecutionOutcome.Completed)
            outcome = ownership == NativeOwnership.Uncertain ? NativeExecutionOutcome.OwnershipUncertain :
                exit == 0 ? NativeExecutionOutcome.Completed : NativeExecutionOutcome.NonzeroExit;
        return new(outcome, exit, cancelled, timedOut, ownership, cleanup, childOutlivedRoot,
            Encoding.UTF8.GetString(output.Bytes.Span), Encoding.UTF8.GetString(error.Bytes.Span), output.Count, error.Count,
            output.Truncated, error.Truncated, rootId, rootTicks, childId, childTicks);
    }

    private async Task<bool> ReapAsync(Process process, CancellationToken token)
    {
        try
        {
            if (!process.HasExited) terminate(process);
            await process.WaitForExitAsync(token).ConfigureAwait(false);
            return process.HasExited;
        }
        catch (Exception failure) when (ProcessExecution.IsProcessError(failure) || failure is OperationCanceledException) { return false; }
    }

    internal static async Task<NativeExecutionResult> RunContainedAsync(NativeExecutionRequest request, CancellationToken caller = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (caller.IsCancellationRequested) return new() { Outcome = "PreCancelled", Cancelled = true };
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            return new() { Admission = NativeBoundaryState.Refused, Refusal = "Windows 11 x64 qualification is required." };
        var result = new NativeExecutionResult();
        NativeExecutionEnvironment.Admission? admitted = null;
        WindowsJob? job = null;
        WindowsNativeProcess? native = null;
        WindowsExecutionDesktop? desktop = null;
        bool executionStarted = false;
        var stdout = new BoundedCapture();
        var stderr = new BoundedCapture();
        Task outputTask = Task.CompletedTask, errorTask = Task.CompletedTask;
        using var drainCancellation = new CancellationTokenSource();
        using var startupDeadline = new CancellationTokenSource(request.Policy.Startup);
        using var startup = CancellationTokenSource.CreateLinkedTokenSource(caller, startupDeadline.Token);
        using var executionDeadline = new CancellationTokenSource();
        using var execution = CancellationTokenSource.CreateLinkedTokenSource(caller, executionDeadline.Token);
        try
        {
            admitted = await NativeExecutionEnvironment.AdmitAsync(request, startup.Token).ConfigureAwait(false);
            startup.Token.ThrowIfCancellationRequested();
            result = result with { Admission = NativeBoundaryState.Verified, Image = NativeBoundaryState.Verified,
                Identity = NativeBoundaryState.Verified, Filesystem = NativeBoundaryState.Verified, Network = NativeBoundaryState.Verified,
                Storage = NativeBoundaryState.Verified };
            job = new(request.Policy.MemoryBytes, request.Policy.Processes);
            desktop = new(admitted.Plan.RunId, admitted.Plan.AccountSid, admitted.Plan.RestrictingSid);
            string[] arguments = [admitted.Plan.FixtureAssembly, "h2b-probe", request.Mode, admitted.Storage.Root];
            native = new(admitted.Identity, job, admitted.Executable, arguments, admitted.Storage.Root,
                Environment.GetFolderPath(Environment.SpecialFolder.Windows), admitted.Storage.Temporary, desktop.Name);
            startup.Token.ThrowIfCancellationRequested();
            result = result with { Job = NativeBoundaryState.Verified, Resources = NativeBoundaryState.Verified };
            outputTask = stdout.DrainAsync(native.Output, drainCancellation.Token);
            errorTask = stderr.DrainAsync(native.Error, drainCancellation.Token);
            executionDeadline.CancelAfter(request.Policy.Execution);
            native.Resume();
            executionStarted = true;
            startupDeadline.CancelAfter(Timeout.InfiniteTimeSpan);
            result = result with { Outcome = "Running" };
            while (true)
            {
                execution.Token.ThrowIfCancellationRequested();
                admitted.Network.Verify();
                NativeInspection inspection = NativeWorkspace.InspectOwned(admitted.Storage.Root, request.Policy);
                if (inspection != NativeInspection.WithinPolicy)
                {
                    result = result with { Outcome = inspection == NativeInspection.PolicyViolation ? "WorkspaceViolation" : "MonitoringFailed",
                        PolicyViolation = inspection == NativeInspection.PolicyViolation, MonitoringFailed = inspection == NativeInspection.Incomplete,
                        Inspection = NativeBoundaryState.Failed };
                    break;
                }
                if (outputTask.IsFaulted || errorTask.IsFaulted) { result = result with { Outcome = "CaptureFailed", CaptureFailed = true }; break; }
                if (native.HasExited)
                {
                    if (!job.IsEmpty()) { result = result with { Outcome = "DescendantsOutlivedRoot", DescendantsOutlivedRoot = true }; break; }
                    if (outputTask.IsCompleted && errorTask.IsCompleted)
                    { result = result with { Outcome = native.ExitCode == 0 ? "Completed" : "NonzeroExit" }; break; }
                }
                await Task.Delay(request.Policy.ScanInterval, execution.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (caller.IsCancellationRequested || startupDeadline.IsCancellationRequested || executionDeadline.IsCancellationRequested)
        {
            result = result with { Outcome = caller.IsCancellationRequested ? "Cancelled" : "TimedOut", Cancelled = caller.IsCancellationRequested,
                DeadlineExpired = startupDeadline.IsCancellationRequested && !executionStarted || executionDeadline.IsCancellationRequested };
        }
        catch (Exception failure) when (ProcessExecution.IsProcessError(failure) || failure is System.Security.SecurityException or System.Text.Json.JsonException)
        {
            result = result with { Outcome = native is null ? "AdmissionFailed" : "ExecutionFailed", Admission = native is null ? NativeBoundaryState.Refused : result.Admission,
                Refusal = failure.GetType().Name, Network = admitted is not null ? NativeBoundaryState.Uncertain : result.Network,
                StorageExhausted = failure is IOException && (failure.HResult & 0xffff) is 39 or 112 };
        }
        finally
        {
            if (job is not null)
            {
                using var cleanup = new CancellationTokenSource(request.Policy.Cleanup);
                bool empty = false, drained = false;
                try
                {
                    if (!job.IsEmpty()) job.Terminate();
                    while (!job.IsEmpty() || native is not null && !native.HasExited) await Task.Delay(10, cleanup.Token).ConfigureAwait(false);
                    empty = true;
                    await Task.WhenAll(outputTask, errorTask).WaitAsync(cleanup.Token).ConfigureAwait(false);
                    drained = true;
                }
                catch (OperationCanceledException) when (cleanup.IsCancellationRequested) { }
                catch (Exception failure) when (ProcessExecution.IsProcessError(failure))
                { result = result with { CaptureFailed = outputTask.IsFaulted || errorTask.IsFaulted }; }
                result = result with { Cleanup = empty && drained ? NativeBoundaryState.Verified : NativeBoundaryState.Failed };
                try { result = result with { ExitCode = native?.ExitCode }; }
                catch (Exception failure) when (ProcessExecution.IsProcessError(failure)) { result = result with { Cleanup = NativeBoundaryState.Uncertain }; }
            }
            await drainCancellation.CancelAsync().ConfigureAwait(false);
            native?.CloseCaptures();
            _ = Task.WhenAll(outputTask, errorTask).ContinueWith(task => _ = task.Exception, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            if (admitted is not null && result.Cleanup == NativeBoundaryState.Verified)
            {
                try
                {
                    NativeInspection final = NativeWorkspace.Inspect(admitted.Storage.Root, request.Policy.FileBytes, request.Policy.Entries,
                        request.Policy.StorageBytes, (int)Math.Min(1000, request.Policy.Finalization.TotalMilliseconds));
                    result = result with { Inspection = final == NativeInspection.WithinPolicy ? NativeBoundaryState.Verified : NativeBoundaryState.Uncertain,
                        EvidenceRetained = true, EvidenceComplete = false }; // H4 owns durable export/publication.
                }
                catch (Exception failure) when (ProcessExecution.IsProcessError(failure))
                { result = result with { Inspection = NativeBoundaryState.Uncertain, EvidenceComplete = false }; }
            }
            native?.Dispose();
            job?.Dispose();
            desktop?.Dispose();
            admitted?.Dispose();
        }
        return result with { OutputBytes = stdout.Count, ErrorBytes = stderr.Count,
            OutputTruncated = stdout.Truncated, ErrorTruncated = stderr.Truncated };
    }
}
