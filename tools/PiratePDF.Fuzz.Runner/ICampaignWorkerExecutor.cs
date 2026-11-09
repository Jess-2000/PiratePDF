namespace PiratePDF.Fuzz.Runner;

/// <summary>Campaign-only orchestration seam; ordinary single-case execution remains unchanged.</summary>
public interface ICampaignWorkerExecutor
{
    Task BeginAsync(string target, CancellationToken cancellationToken);
    Task<SupervisionResult> ExecuteAsync(CampaignCaseRequest request, CancellationToken cancellationToken);
    Task EndAsync(CampaignTargetResult target, CancellationToken cancellationToken);
}
