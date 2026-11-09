namespace PiratePDF.Fuzz.Runner;

internal enum NativeBoundaryState { NotChecked, Refused, Verified, Failed, Uncertain }

/// <summary>Independent facts; not a finding schema and not evidence of libFuzzer mutation ancestry.</summary>
internal sealed record NativeExecutionResult
{
    internal string Outcome { get; init; } = "Refused";
    internal NativeBoundaryState Admission { get; init; }
    internal NativeBoundaryState Image { get; init; }
    internal NativeBoundaryState Identity { get; init; }
    internal NativeBoundaryState Filesystem { get; init; }
    internal NativeBoundaryState Network { get; init; }
    internal NativeBoundaryState Job { get; init; }
    internal NativeBoundaryState Storage { get; init; }
    internal NativeBoundaryState Resources { get; init; }
    internal NativeBoundaryState Cleanup { get; init; }
    internal NativeBoundaryState Inspection { get; init; }
    internal int? ExitCode { get; init; }
    internal bool Cancelled { get; init; }
    internal bool DeadlineExpired { get; init; }
    internal bool MonitoringFailed { get; init; }
    internal bool PolicyViolation { get; init; }
    internal bool EvidenceComplete { get; init; }
    internal int OutputBytes { get; init; }
    internal int ErrorBytes { get; init; }
    internal bool OutputTruncated { get; init; }
    internal bool ErrorTruncated { get; init; }
    internal string Refusal { get; init; } = "";
    internal bool CaptureFailed { get; init; }
    internal bool DescendantsOutlivedRoot { get; init; }
    internal bool EvidenceRetained { get; init; }
    internal bool StorageExhausted { get; init; }
}
