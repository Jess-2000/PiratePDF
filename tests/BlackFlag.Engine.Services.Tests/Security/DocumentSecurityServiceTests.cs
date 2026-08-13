using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Policy;
using BlackFlag.Engine.Services.Security;

namespace BlackFlag.Engine.Services.Tests.Security;

public sealed class DocumentSecurityServiceTests
{
    [Fact]
    public void CleanAutomaticEvaluationDoesNotCreateReport()
    {
        var service = new DocumentSecurityService();

        DocumentSecurityServiceResult result =
            service.Evaluate(
                [],
                DocumentSecurityOperation.Open,
                DateTimeOffset.UnixEpoch);

        Assert.False(result.Assessment.IsBlocked);
        Assert.Null(result.Report);
    }

    [Fact]
    public void FindingCreatesEphemeralReportWithAssessmentContext()
    {
        var service = new DocumentSecurityService();

        DocumentSecurityServiceResult result =
            service.Evaluate(
                [
                    new(
                        DocumentSecurityObservationCategory.EmbeddedContent,
                        "Attachment",
                        "The document contains an attachment.",
                        "pdf")
                ],
                DocumentSecurityOperation.ManualInspection,
                DateTimeOffset.UnixEpoch);

        Assert.NotNull(result.Report);
        Assert.True(result.Report.ManualInspection);
        Assert.Single(result.Report.Findings);
        Assert.Equal(
            DateTimeOffset.UnixEpoch,
            result.Report.GeneratedAtUtc);
    }
}
