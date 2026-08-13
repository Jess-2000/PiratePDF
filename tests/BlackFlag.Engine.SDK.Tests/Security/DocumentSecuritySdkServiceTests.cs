using BlackFlag.Engine.SDK.Security;

namespace BlackFlag.Engine.SDK.Tests.Security;

public sealed class DocumentSecuritySdkServiceTests
{
    [Fact]
    public void ObservationCopiesProperties()
    {
        var source = new Dictionary<string, string> { ["key"] = "value" };
        var observation = new DocumentSecuritySdkObservation(DocumentSecuritySdkObservationCategory.ActiveContent,
            "JavaScript", "Contains JavaScript.", "pdf", properties: source);
        source["key"] = "changed";
        Assert.Equal("value", observation.Properties["key"]);
    }

    [Fact]
    public void ResultCopiesCollections()
    {
        var findings = new List<DocumentSecuritySdkFinding> { new("JavaScript", "Contains JavaScript.", "pdf", "Warning", "Report", null) };
        var result = new DocumentSecuritySdkResult(findings, ["None"], false, true, false, DateTimeOffset.UnixEpoch);
        findings.Clear();
        Assert.Single(result.Findings);
    }
}
