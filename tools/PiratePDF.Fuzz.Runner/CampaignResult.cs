using System.Text.Json;

namespace PiratePDF.Fuzz.Runner;

public sealed record CampaignTargetResult(string Target, CampaignStop Stop, int Attempts, int WorkerRequests,
    int Executed, int Successes, int Rejections, int Duplicates, int ProcessedBytes, int PoolInputs, int PoolBytes, double ElapsedSeconds);

public sealed class CampaignResult
{
    internal CampaignResult(CampaignStop stop, IReadOnlyList<CampaignTargetResult>? targets = null,
        string? run = null, string? finding = null, bool cleanup = true, SupervisionStatus? workerStatus = null, bool outerExpired = false,
        bool evidencePublished = false)
    {
        Stop = stop;
        Targets = Array.AsReadOnly(targets?.ToArray() ?? []);
        Run = run;
        Finding = finding;
        CleanupSucceeded = cleanup;
        WorkerStatus = workerStatus;
        OuterExpired = outerExpired;
        EvidencePublished = evidencePublished;
    }

    public CampaignStop Stop { get; }
    public IReadOnlyList<CampaignTargetResult> Targets { get; }
    public string? Run { get; }
    public string? Finding { get; }
    public bool CleanupSucceeded { get; }
    public SupervisionStatus? WorkerStatus { get; }
    public bool OuterExpired { get; }
    public bool EvidencePublished { get; }
    internal CampaignResult WithPublishedEvidence() => new(Stop, Targets, Run, Finding, CleanupSucceeded,
        WorkerStatus, OuterExpired, evidencePublished: true);
    public int ExitCode => Stop switch
    {
        CampaignStop.CaseLimit or CampaignStop.Deadline or CampaignStop.ProcessedByteLimit or CampaignStop.PoolLimit or
            CampaignStop.TelemetryLimit or CampaignStop.DuplicateLimit => 0,
        CampaignStop.Cancelled => 3,
        CampaignStop.WorkerFailure => 4,
        CampaignStop.RetentionFailure => 5,
        CampaignStop.CleanupFailure => 6,
        CampaignStop.OuterDeadline => 7,
        CampaignStop.PublicationFailure => 8,
        _ => 2
    };
    public string Json => JsonSerializer.Serialize(new { schema = "piratepdf-mutation-campaign", version = 1,
        algorithm = CampaignMutation.Algorithm, seed = CampaignMutation.Seed, mutationOnly = true,
        stop = Stop.ToString(), exitCode = ExitCode, run = Run, finding = Finding, CleanupSucceeded, WorkerStatus, OuterExpired,
        EvidencePublished, targets = Targets });
}
