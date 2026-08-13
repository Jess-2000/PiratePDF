using System.Text;
using BlackFlag.Engine.Security.Formats.Markdown;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Tests.Formats.Markdown;

public sealed class MarkdownSecurityAnalyzerTests
{
    [Fact]
    public void CleanMarkdownProducesNoObservations()
    {
        byte[] content = Encoding.UTF8.GetBytes("# Heading\n\nPlain text.");

        MarkdownSecurityAnalysisResult result =
            new MarkdownSecurityAnalyzer().Analyze(content, "clean.md");

        Assert.Empty(result.Observations);
    }

    [Fact]
    public void ExternalLinkIsDetected()
    {
        byte[] content =
            Encoding.UTF8.GetBytes("[link](https://example.invalid/resource)");

        MarkdownSecurityAnalysisResult result =
            new MarkdownSecurityAnalyzer().Analyze(content, "external.md");

        Assert.Contains(
            result.Observations,
            item => item.Kind == "ExternalReference" &&
                    item.Category == DocumentSecurityObservationCategory.ExternalReference);
    }

    [Fact]
    public void RawScriptContentIsDetected()
    {
        byte[] content =
            Encoding.UTF8.GetBytes("<script>harmlessFixture()</script>");

        MarkdownSecurityAnalysisResult result =
            new MarkdownSecurityAnalyzer().Analyze(content, "script.markdown");

        Assert.Contains(
            result.Observations,
            item => item.Kind == "RawHtmlActiveContent" &&
                    item.Category == DocumentSecurityObservationCategory.ActiveContent);
    }

    [Fact]
    public void ScriptUriIsDetected()
    {
        byte[] content =
            Encoding.UTF8.GetBytes("[link](javascript:harmlessFixture())");

        MarkdownSecurityAnalysisResult result =
            new MarkdownSecurityAnalyzer().Analyze(content, "script.md");

        Assert.Contains(
            result.Observations,
            item => item.Kind == "ScriptUri" &&
                    item.Category == DocumentSecurityObservationCategory.ScriptContent);
    }
}
