using BlackFlag.Engine.SDK.Security;
using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Policy;
using BlackFlag.Engine.Services.Security;

namespace BlackFlag.Engine.SDK.Adapters.Security;

/// <summary>Adapts public SDK security contracts to the internal BlackFlag security service.</summary>
public sealed class DocumentSecuritySdkService : IDocumentSecuritySdkService
{
    private readonly DocumentSecurityService service;

    public DocumentSecuritySdkService(DocumentSecurityService? service = null) =>
        this.service = service ?? new DocumentSecurityService();

    public DocumentSecuritySdkResult Evaluate(IEnumerable<DocumentSecuritySdkObservation> observations,
        DocumentSecuritySdkOperation operation, DateTimeOffset generatedAtUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observations);
        cancellationToken.ThrowIfCancellationRequested();
        DocumentSecurityObservation[] internalObservations = observations.Select(MapObservation).ToArray();
        DocumentSecurityServiceResult result = service.Evaluate(internalObservations, MapOperation(operation), generatedAtUtc,
            cancellationToken: cancellationToken);

        var findings = new DocumentSecuritySdkFinding[result.Assessment.Findings.Count];
        for (int index = 0; index < findings.Length; index++)
        {
            var finding = result.Assessment.Findings[index];
            findings[index] = new DocumentSecuritySdkFinding(finding.Observation.Kind, finding.Observation.Description,
                finding.Observation.SourceFormat, finding.Severity.ToString(), finding.Action.ToString(),
                finding.Observation.Location?.LogicalPath);
        }
        var interactions = new string[result.Assessment.Interactions.Count];
        for (int index = 0; index < interactions.Length; index++) interactions[index] = result.Assessment.Interactions[index].ToString();
        return new DocumentSecuritySdkResult(findings, interactions, result.Assessment.IsBlocked, result.Report is not null,
            result.Assessment.ManualInspection, result.Report?.GeneratedAtUtc);
    }

    private static DocumentSecurityObservation MapObservation(DocumentSecuritySdkObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        DocumentSecuritySourceLocation? location = observation.Location is null ? null : new DocumentSecuritySourceLocation
        {
            PageIndex = observation.Location.PageIndex,
            LogicalPath = observation.Location.LogicalPath,
            ByteOffset = observation.Location.ByteOffset,
            ObjectIdentifier = observation.Location.ObjectIdentifier
        };
        return new DocumentSecurityObservation(MapCategory(observation.Category),
            observation.Kind, observation.Description, observation.SourceFormat, location, observation.Properties);
    }

    private static DocumentSecurityObservationCategory MapCategory(DocumentSecuritySdkObservationCategory category) => category switch
    {
        DocumentSecuritySdkObservationCategory.ActiveContent => DocumentSecurityObservationCategory.ActiveContent,
        DocumentSecuritySdkObservationCategory.ScriptContent => DocumentSecurityObservationCategory.ScriptContent,
        DocumentSecuritySdkObservationCategory.EmbeddedContent => DocumentSecurityObservationCategory.EmbeddedContent,
        DocumentSecuritySdkObservationCategory.ExternalReference => DocumentSecurityObservationCategory.ExternalReference,
        DocumentSecuritySdkObservationCategory.AutomaticAction => DocumentSecurityObservationCategory.AutomaticAction,
        DocumentSecuritySdkObservationCategory.ExecutableContent => DocumentSecurityObservationCategory.ExecutableContent,
        DocumentSecuritySdkObservationCategory.InteractiveContent => DocumentSecurityObservationCategory.InteractiveContent,
        DocumentSecuritySdkObservationCategory.SuspiciousRelationship => DocumentSecurityObservationCategory.SuspiciousRelationship,
        DocumentSecuritySdkObservationCategory.StructuralAnomaly => DocumentSecurityObservationCategory.StructuralAnomaly,
        DocumentSecuritySdkObservationCategory.EncryptionCondition => DocumentSecurityObservationCategory.EncryptionCondition,
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    private static DocumentSecurityOperation MapOperation(DocumentSecuritySdkOperation operation) => operation switch
    {
        DocumentSecuritySdkOperation.Open => DocumentSecurityOperation.Open,
        DocumentSecuritySdkOperation.ManualInspection => DocumentSecurityOperation.ManualInspection,
        DocumentSecuritySdkOperation.PassiveConversion => DocumentSecurityOperation.PassiveConversion,
        DocumentSecuritySdkOperation.Sanitize => DocumentSecurityOperation.Sanitize,
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };
}
