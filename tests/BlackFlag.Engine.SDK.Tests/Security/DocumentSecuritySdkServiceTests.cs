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
        var originalFinding = new DocumentSecuritySdkFinding(
            "JavaScript",
            "Contains JavaScript.",
            "pdf",
            "Warning",
            "Report",
            null);
        var replacementFinding = new DocumentSecuritySdkFinding(
            "Launch",
            "Contains a launch action.",
            "pdf",
            "Blocking",
            "Block",
            null);
        var findings = new List<DocumentSecuritySdkFinding> { originalFinding };
        var interactions = new List<string> { "None" };
        var result = new DocumentSecuritySdkResult(
            findings,
            interactions,
            false,
            true,
            false,
            DateTimeOffset.UnixEpoch);

        findings.Clear();
        interactions.Clear();

        Assert.False(result.Findings is DocumentSecuritySdkFinding[]);
        IList<DocumentSecuritySdkFinding> mutableFindings =
            Assert.IsAssignableFrom<IList<DocumentSecuritySdkFinding>>(result.Findings);
        Assert.True(mutableFindings.IsReadOnly);
        Assert.Throws<NotSupportedException>(
            () => mutableFindings[0] = replacementFinding);

        Assert.False(result.Interactions is string[]);
        IList<string> mutableInteractions =
            Assert.IsAssignableFrom<IList<string>>(result.Interactions);
        Assert.True(mutableInteractions.IsReadOnly);
        Assert.Throws<NotSupportedException>(
            () => mutableInteractions[0] = "Sanitization");

        Assert.Same(originalFinding, Assert.Single(result.Findings));
        Assert.Equal("None", Assert.Single(result.Interactions));
        Assert.Same(result.Findings, result.Findings);
        Assert.Same(result.Interactions, result.Interactions);
        Assert.False(result.IsBlocked);
        Assert.True(result.HasReport);
        Assert.False(result.ManualInspection);
        Assert.Equal(DateTimeOffset.UnixEpoch, result.ReportGeneratedAtUtc);
    }
}
