namespace BlackFlag.Engine.SDK.Security;

/// <summary>Evaluates SDK security observations through BlackFlag policy.</summary>
public interface IDocumentSecuritySdkService
{
    DocumentSecuritySdkResult Evaluate(
        IEnumerable<DocumentSecuritySdkObservation> observations,
        DocumentSecuritySdkOperation operation,
        DateTimeOffset generatedAtUtc,
        CancellationToken cancellationToken = default);
}
