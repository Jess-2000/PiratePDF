
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Policy;

/// <summary>
/// Provides the default silent-by-design BlackFlag security policy.
/// </summary>
public sealed class BlackFlagDocumentSecurityPolicy : IDocumentSecurityPolicy
{
    /// <inheritdoc />
    public DocumentSecurityPolicyDecision Evaluate(
        DocumentSecurityObservation observation,
        DocumentSecurityOperation operation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        if (IsPasswordRequired(observation))
        {
            return new(
                DocumentSecurityPolicyAction.RequireUserDecision,
                DocumentSecuritySeverity.Notice,
                "This document is password protected.",
                DocumentSecurityInteraction.PasswordRequired);
        }

        if (IsDamagedEncryption(observation))
        {
            return new(
                DocumentSecurityPolicyAction.Block,
                DocumentSecuritySeverity.Blocking,
                "The document's encryption information is damaged and cannot be safely reconstructed or bypassed.",
                DocumentSecurityInteraction.EncryptionDamage);
        }

        if (observation.Category == DocumentSecurityObservationCategory.StructuralAnomaly &&
            IsRecoverableStructuralDamage(observation))
        {
            return new(
                DocumentSecurityPolicyAction.AllowAndReport,
                DocumentSecuritySeverity.Notice,
                observation.Description,
                DocumentSecurityInteraction.StructuralRecovery);
        }

        if (operation is DocumentSecurityOperation.PassiveConversion or
            DocumentSecurityOperation.Sanitize)
        {
            return EvaluateSanitizingOperation(observation);
        }

        return observation.Category switch
        {
            DocumentSecurityObservationCategory.ActiveContent or
            DocumentSecurityObservationCategory.ScriptContent or
            DocumentSecurityObservationCategory.EmbeddedContent or
            DocumentSecurityObservationCategory.ExternalReference or
            DocumentSecurityObservationCategory.AutomaticAction or
            DocumentSecurityObservationCategory.ExecutableContent or
            DocumentSecurityObservationCategory.InteractiveContent or
            DocumentSecurityObservationCategory.SuspiciousRelationship =>
                new(
                    DocumentSecurityPolicyAction.AllowAndReport,
                    DefaultSeverity(observation.Category),
                    observation.Description),

            DocumentSecurityObservationCategory.StructuralAnomaly or
            DocumentSecurityObservationCategory.EncryptionCondition =>
                new(
                    DocumentSecurityPolicyAction.AllowAndReport,
                    DocumentSecuritySeverity.Warning,
                    observation.Description),

            _ => DocumentSecurityPolicyDecision.Allow()
        };
    }

    private static DocumentSecurityPolicyDecision EvaluateSanitizingOperation(
        DocumentSecurityObservation observation)
    {
        return observation.Category switch
        {
            DocumentSecurityObservationCategory.ActiveContent or
            DocumentSecurityObservationCategory.ScriptContent or
            DocumentSecurityObservationCategory.EmbeddedContent or
            DocumentSecurityObservationCategory.AutomaticAction or
            DocumentSecurityObservationCategory.ExecutableContent or
            DocumentSecurityObservationCategory.SuspiciousRelationship =>
                new(
                    DocumentSecurityPolicyAction.Sanitize,
                    DocumentSecuritySeverity.Warning,
                    $"{observation.Description} The content will be removed or neutralized for this operation.",
                    DocumentSecurityInteraction.Sanitization),

            DocumentSecurityObservationCategory.ExternalReference =>
                new(
                    DocumentSecurityPolicyAction.Sanitize,
                    DocumentSecuritySeverity.Notice,
                    $"{observation.Description} The external behavior will be disabled for this operation.",
                    DocumentSecurityInteraction.Sanitization),

            DocumentSecurityObservationCategory.InteractiveContent =>
                new(
                    DocumentSecurityPolicyAction.AllowAndReport,
                    DocumentSecuritySeverity.Notice,
                    observation.Description),

            _ => new(
                DocumentSecurityPolicyAction.AllowAndReport,
                DocumentSecuritySeverity.Warning,
                observation.Description)
        };
    }

    private static DocumentSecuritySeverity DefaultSeverity(
        DocumentSecurityObservationCategory category) =>
        category switch
        {
            DocumentSecurityObservationCategory.ExecutableContent =>
                DocumentSecuritySeverity.Warning,

            DocumentSecurityObservationCategory.ScriptContent or
            DocumentSecurityObservationCategory.AutomaticAction =>
                DocumentSecuritySeverity.Notice,

            _ => DocumentSecuritySeverity.Information
        };

    private static bool IsPasswordRequired(
        DocumentSecurityObservation observation) =>
        observation.Category == DocumentSecurityObservationCategory.EncryptionCondition &&
        string.Equals(
            observation.Kind,
            "PasswordRequired",
            StringComparison.Ordinal);

    private static bool IsDamagedEncryption(
        DocumentSecurityObservation observation) =>
        observation.Category == DocumentSecurityObservationCategory.EncryptionCondition &&
        string.Equals(
            observation.Kind,
            "DamagedEncryption",
            StringComparison.Ordinal);

    private static bool IsRecoverableStructuralDamage(
        DocumentSecurityObservation observation) =>
        string.Equals(
            observation.Kind,
            "RecoveredStructuralDamage",
            StringComparison.Ordinal) ||
        string.Equals(
            observation.Kind,
            "RepairableStructuralDamage",
            StringComparison.Ordinal);
}
