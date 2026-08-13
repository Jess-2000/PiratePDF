using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Remediation;

namespace BlackFlag.Engine.Security.Tests.Remediation;

public sealed class PassiveConversionSecurityServiceTests
{
    [Fact]
    public void CleanConversionNeedsNoReportOrRemediation()
    {
        var service = new PassiveConversionSecurityService();
        PassiveConversionSecurityResult result = service.Evaluate([]);

        Assert.True(result.CanProceed);
        Assert.False(result.RequiresRemediation);
        Assert.False(result.Assessment.ShouldGenerateReport);
        Assert.Empty(result.RemediationPlan.Actions);
    }

    [Fact]
    public void ActiveAndExternalContentProduceDeterministicRemediation()
    {
        var service = new PassiveConversionSecurityService();
        PassiveConversionSecurityResult result =
            service.Evaluate(
            [
                new(
                    DocumentSecurityObservationCategory.ScriptContent,
                    "VbaProject",
                    "The document contains VBA.",
                    "docm"),
                new(
                    DocumentSecurityObservationCategory.ExternalReference,
                    "ExternalRelationship",
                    "The document contains an external relationship.",
                    "docm")
            ]);

        Assert.True(result.CanProceed);
        Assert.True(result.RequiresRemediation);
        Assert.True(result.Assessment.ShouldGenerateReport);
        Assert.Equal(2, result.RemediationPlan.Actions.Count);
        Assert.Equal(
            DocumentSecurityRemediationIntent.RemoveActiveContent,
            result.RemediationPlan.Actions[0].Intent);
        Assert.Equal(
            DocumentSecurityRemediationIntent.DisableExternalReference,
            result.RemediationPlan.Actions[1].Intent);
    }

    [Fact]
    public void DamagedEncryptionBlocksConversion()
    {
        var service = new PassiveConversionSecurityService();
        PassiveConversionSecurityResult result =
            service.Evaluate(
            [
                new(
                    DocumentSecurityObservationCategory.EncryptionCondition,
                    "DamagedEncryption",
                    "Encryption metadata is damaged.",
                    "pdf")
            ]);

        Assert.False(result.CanProceed);
        Assert.False(result.RequiresRemediation);
        Assert.True(result.RemediationPlan.IsBlocked);
        Assert.Equal(
            DocumentSecurityRemediationIntent.RejectOperation,
            Assert.Single(result.RemediationPlan.Actions).Intent);
    }
}
