namespace PiratePDF.Fuzz.Runner;

internal enum NativeExecutionOutcome
{
    Completed, NonzeroExit, StartFailed, PreCancelled, Cancelled, TimedOut, CaptureFailed, OwnershipRejected, OwnershipUncertain
}

internal enum NativeOwnership { NotLaunched, RootOnly, RootAndChild, Rejected, Uncertain }
internal enum NativeCleanup { NotRequired, Verified, Failed, Uncertain }

/// <summary>Native execution facts, independent of the managed worker JSON protocol.</summary>
internal sealed record NativeProcessResult(NativeExecutionOutcome Outcome, int? ExitCode, bool Cancelled, bool TimedOut,
    NativeOwnership Ownership, NativeCleanup Cleanup, bool ChildOutlivedRoot, string Output, string Error,
    int OutputBytes, int ErrorBytes, bool OutputTruncated, bool ErrorTruncated, int? RootId, long? RootStartTicks,
    int? ChildId, long? ChildStartTicks)
{
    internal bool ExecutionVerified => Outcome is NativeExecutionOutcome.Completed or NativeExecutionOutcome.NonzeroExit &&
        Cleanup == NativeCleanup.Verified && Ownership is NativeOwnership.RootOnly or NativeOwnership.RootAndChild;
}
