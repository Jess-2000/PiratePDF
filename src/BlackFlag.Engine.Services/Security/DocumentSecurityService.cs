using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Policy;
using BlackFlag.Engine.Security.Reporting;

namespace BlackFlag.Engine.Services.Security;

/// <summary>
/// Provides application-facing orchestration for shared security policy and
/// conditional report creation.
/// </summary>
public sealed class DocumentSecurityService
{
    private readonly DocumentSecurityAssessmentEngine assessmentEngine;

    /// <summary>Initializes the document security service.</summary>
    public DocumentSecurityService(
        DocumentSecurityAssessmentEngine? assessmentEngine = null)
    {
        this.assessmentEngine =
            assessmentEngine ??
            new DocumentSecurityAssessmentEngine();
    }

    /// <summary>
    /// Evaluates normalized observations for an operation and creates an
    /// ephemeral report only when the shared reporting rules require one.
    /// </summary>
    public DocumentSecurityServiceResult Evaluate(
        IEnumerable<DocumentSecurityObservation> observations,
        DocumentSecurityOperation operation,
        DateTimeOffset generatedAtUtc,
        DocumentSecurityInspectionBudget? budget = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observations);
        cancellationToken.ThrowIfCancellationRequested();

        DocumentSecurityAssessment assessment =
            assessmentEngine.Evaluate(
                observations,
                operation,
                budget,
                cancellationToken);

        DocumentSecurityReport? report =
            DocumentSecurityReportFactory.Create(
                assessment,
                generatedAtUtc);

        return new DocumentSecurityServiceResult(
            assessment,
            report);
    }
}
