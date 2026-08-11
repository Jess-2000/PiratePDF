
using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Policy;

namespace BlackFlag.Engine.Security.Tests.Policy;

public sealed class BlackFlagDocumentSecurityPolicyTests
{
    private readonly BlackFlagDocumentSecurityPolicy policy = new();

    [Fact]
    public void JavaScriptDuringOpenIsReportOnlyAndSilent()
    {
        DocumentSecurityPolicyDecision decision =
            policy.Evaluate(
                Observation(
                    DocumentSecurityObservationCategory.ScriptContent,
                    "JavaScript",
                    "The PDF contains JavaScript."),
                DocumentSecurityOperation.Open);

        Assert.Equal(
            DocumentSecurityPolicyAction.AllowAndReport,
            decision.Action);

        Assert.Equal(
            DocumentSecurityInteraction.None,
            decision.Interaction);
    }

    [Fact]
    public void MacroDuringPassiveConversionRequiresSanitizationNotification()
    {
        DocumentSecurityPolicyDecision decision =
            policy.Evaluate(
                Observation(
                    DocumentSecurityObservationCategory.ScriptContent,
                    "VbaProject",
                    "The Word document contains VBA macros.",
                    "docm"),
                DocumentSecurityOperation.PassiveConversion);

        Assert.Equal(
            DocumentSecurityPolicyAction.Sanitize,
            decision.Action);

        Assert.Equal(
            DocumentSecurityInteraction.Sanitization,
            decision.Interaction);
    }

    [Fact]
    public void PasswordRequirementIsTheOnlyNormalEncryptionPrompt()
    {
        DocumentSecurityPolicyDecision decision =
            policy.Evaluate(
                Observation(
                    DocumentSecurityObservationCategory.EncryptionCondition,
                    "PasswordRequired",
                    "This PDF requires a password."),
                DocumentSecurityOperation.Open);

        Assert.Equal(
            DocumentSecurityPolicyAction.RequireUserDecision,
            decision.Action);

        Assert.Equal(
            DocumentSecurityInteraction.PasswordRequired,
            decision.Interaction);
    }

    [Fact]
    public void DamagedEncryptionBlocksWithoutRecoveryFallback()
    {
        DocumentSecurityPolicyDecision decision =
            policy.Evaluate(
                Observation(
                    DocumentSecurityObservationCategory.EncryptionCondition,
                    "DamagedEncryption",
                    "The PDF encryption dictionary is damaged."),
                DocumentSecurityOperation.Open);

        Assert.Equal(
            DocumentSecurityPolicyAction.Block,
            decision.Action);

        Assert.Equal(
            DocumentSecurityInteraction.EncryptionDamage,
            decision.Interaction);
    }

    [Fact]
    public void RecoveredStructuralDamageRequiresInformationalNotification()
    {
        DocumentSecurityPolicyDecision decision =
            policy.Evaluate(
                Observation(
                    DocumentSecurityObservationCategory.StructuralAnomaly,
                    "RecoveredStructuralDamage",
                    "PiratePDF recovered a damaged cross-reference table."),
                DocumentSecurityOperation.Open);

        Assert.Equal(
            DocumentSecurityInteraction.StructuralRecovery,
            decision.Interaction);
    }

    private static DocumentSecurityObservation Observation(
        DocumentSecurityObservationCategory category,
        string kind,
        string description,
        string format = "pdf") =>
        new(
            category,
            kind,
            description,
            format);
}
