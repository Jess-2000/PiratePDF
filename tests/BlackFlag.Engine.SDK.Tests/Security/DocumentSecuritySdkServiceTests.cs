using BlackFlag.Engine.SDK.Security;
using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Policy;

namespace BlackFlag.Engine.SDK.Tests.Security;

public sealed class DocumentSecuritySdkServiceTests
{
    [Fact]
    public void SdkMapsSecurityFindingWithoutParserTypes()
    {
        var service = new DocumentSecuritySdkService();

        DocumentSecuritySdkResult result =
            service.Evaluate(
                [
                    new(
                        DocumentSecurityObservationCategory.ScriptContent,
                        "VbaProject",
                        "The document contains VBA.",
                        "docm",
                        new DocumentSecuritySourceLocation
                        {
                            LogicalPath = "/word/vbaProject.bin"
                        })
                ],
                DocumentSecurityOperation.Open,
                DateTimeOffset.UnixEpoch);

        DocumentSecuritySdkFinding finding =
            Assert.Single(result.Findings);

        Assert.Equal("VbaProject", finding.Kind);
        Assert.Equal("docm", finding.SourceFormat);
        Assert.Equal(
            "/word/vbaProject.bin",
            finding.LogicalPath);
        Assert.True(result.HasReport);
        Assert.False(result.ManualInspection);
        Assert.Equal(
            DateTimeOffset.UnixEpoch,
            result.ReportGeneratedAtUtc);
    }

    [Fact]
    public void CleanAutomaticSdkResultIsSilent()
    {
        var service = new DocumentSecuritySdkService();

        DocumentSecuritySdkResult result =
            service.Evaluate(
                [],
                DocumentSecurityOperation.Open,
                DateTimeOffset.UnixEpoch);

        Assert.Empty(result.Findings);
        Assert.Empty(result.Interactions);
        Assert.False(result.IsBlocked);
        Assert.False(result.HasReport);
        Assert.Null(result.ReportGeneratedAtUtc);
    }
}
