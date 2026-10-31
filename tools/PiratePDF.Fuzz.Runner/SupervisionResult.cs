using System.Text.Json;
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

public enum SupervisionStatus
{
    Completed,
    TimedOut,
    Cancelled,
    AbnormalExit,
    InvalidProtocol,
    StartFailed,
    TerminationFailed,
    CaptureFailed,
    InvalidRequest,
    WorkerFailure
}

/// <summary>Safe metadata only. Captured child bytes never leave the supervisor.</summary>
public sealed class SupervisionResult
{
    internal SupervisionResult(CampaignWorkerReply reply, FuzzTarget target) : this(reply.Status, target, reply.ExitCode, reply.Code)
    {
        StdoutRetainedBytes = reply.Stdout;
        StderrRetainedBytes = reply.Stderr;
        StdoutTruncated = reply.StdoutTruncated;
        StderrTruncated = reply.StderrTruncated;
    }
    internal SupervisionResult(SupervisionStatus status, FuzzTarget? target = null, int? exitCode = null,
        string? workerCode = null, BoundedCapture? output = null, BoundedCapture? error = null)
    {
        Status = status;
        TargetId = target?.Id;
        ConfigurationVersion = target?.ConfigurationVersion;
        ExitCode = exitCode;
        WorkerCode = workerCode;
        StdoutRetainedBytes = output?.Count ?? 0;
        StderrRetainedBytes = error?.Count ?? 0;
        StdoutTruncated = output?.Truncated ?? false;
        StderrTruncated = error?.Truncated ?? false;
    }

    public SupervisionStatus Status { get; }
    public string? TargetId { get; }
    public int? ConfigurationVersion { get; }
    public int? ExitCode { get; }
    public string? WorkerCode { get; }
    public int StdoutRetainedBytes { get; }
    public int StderrRetainedBytes { get; }
    public bool StdoutTruncated { get; }
    public bool StderrTruncated { get; }

    public string ToJson() => JsonSerializer.Serialize(new
    {
        schemaVersion = 1,
        status = Status.ToString(),
        target = TargetId,
        configurationVersion = ConfigurationVersion,
        exitCode = ExitCode,
        workerCode = WorkerCode,
        stdoutRetainedBytes = StdoutRetainedBytes,
        stderrRetainedBytes = StderrRetainedBytes,
        stdoutTruncated = StdoutTruncated,
        stderrTruncated = StderrTruncated
    });
}
