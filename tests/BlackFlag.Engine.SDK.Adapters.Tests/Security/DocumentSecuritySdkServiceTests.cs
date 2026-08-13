using System;
using BlackFlag.Engine.SDK.Adapters.Security;
using BlackFlag.Engine.SDK.Security;

namespace BlackFlag.Engine.SDK.Adapters.Tests.Security;

public sealed class DocumentSecuritySdkServiceTests
{
    [Fact]
    public void AdapterMapsPublicObservationToInternalPolicyAndBack()
    {
        IDocumentSecuritySdkService service = new DocumentSecuritySdkService();
        DocumentSecuritySdkResult result = service.Evaluate([
            new DocumentSecuritySdkObservation(DocumentSecuritySdkObservationCategory.ScriptContent, "VbaProject",
                "The document contains VBA.", "docm", new DocumentSecuritySdkSourceLocation { LogicalPath = "/word/vbaProject.bin" })
        ], DocumentSecuritySdkOperation.Open, DateTimeOffset.UnixEpoch);
        DocumentSecuritySdkFinding finding = Assert.Single(result.Findings);
        Assert.Equal("VbaProject", finding.Kind);
        Assert.Equal("/word/vbaProject.bin", finding.LogicalPath);
        Assert.True(result.HasReport);
    }

    [Fact]
    public void CleanAutomaticEvaluationRemainsSilent()
    {
        IDocumentSecuritySdkService service = new DocumentSecuritySdkService();
        DocumentSecuritySdkResult result = service.Evaluate([], DocumentSecuritySdkOperation.Open, DateTimeOffset.UnixEpoch);
        Assert.Empty(result.Findings);
        Assert.Empty(result.Interactions);
        Assert.False(result.IsBlocked);
        Assert.False(result.HasReport);
    }
}
