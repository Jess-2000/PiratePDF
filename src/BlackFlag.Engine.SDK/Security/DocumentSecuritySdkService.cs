using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Policy;
using BlackFlag.Engine.Services.Security;
// using alias removed to avoid type ambiguity with SDK types

namespace BlackFlag.Engine.SDK.Security;

/// <summary>
/// Exposes document-security policy results through an immutable SDK model
/// without exposing parser implementation types or UI concerns.
/// </summary>
public sealed class DocumentSecuritySdkService
{
    private readonly DocumentSecurityService service;

    /// <summary>Initializes the SDK security service.</summary>
    public DocumentSecuritySdkService(
        DocumentSecurityService? service = null)
    {
        this.service =
            service ??
            new DocumentSecurityService();
    }

    /// <summary>Evaluates normalized security observations for an operation.</summary>
    public DocumentSecuritySdkResult Evaluate(
        IEnumerable<DocumentSecurityObservation> observations,
        DocumentSecurityOperation operation,
        DateTimeOffset generatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observations);

        DocumentSecurityServiceResult result =
            service.Evaluate(
                observations.Cast<DocumentSecurityObservation>(),
                operation,
                generatedAtUtc,
                cancellationToken: cancellationToken);

        var findings =
            new DocumentSecuritySdkFinding[
                result.Assessment.Findings.Count];

        for (int index = 0; index < findings.Length; index++)
        {
            var finding =
                result.Assessment.Findings[index];

            findings[index] =
                new DocumentSecuritySdkFinding(
                    finding.Observation.Kind,
                    finding.Observation.Description,
                    finding.Observation.SourceFormat,
                    finding.Severity.ToString(),
                    finding.Action.ToString(),
                    finding.Observation.Location?.LogicalPath);
        }

        var interactions =
            new string[
                result.Assessment.Interactions.Count];

        for (int index = 0; index < interactions.Length; index++)
        {
            interactions[index] =
                result.Assessment.Interactions[index].ToString();
        }

        return new DocumentSecuritySdkResult(
            findings,
            interactions,
            result.Assessment.IsBlocked,
            result.Report is not null,
            result.Assessment.ManualInspection,
            result.Report?.GeneratedAtUtc);
    }
}
