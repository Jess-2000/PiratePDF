#nullable enable

using System;
using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Policy;
using BlackFlag.Engine.Security.Reporting;

namespace BlackFlag.Engine.Security.Tests.Reporting;

public sealed class DocumentSecurityReportFactoryTests
{
    [Fact]
    public void CleanAutomaticAssessmentDoesNotCreateReport()
    {
        var assessment =
            new DocumentSecurityAssessmentEngine()
                .Evaluate(
                    [],
                    DocumentSecurityOperation.Open);

        DocumentSecurityReport? report =
            DocumentSecurityReportFactory.Create(
                assessment,
                DateTimeOffset.UnixEpoch);

        Assert.Null(report);
    }

    [Fact]
    public void FindingCreatesEphemeralReport()
    {
        var assessment =
            new DocumentSecurityAssessmentEngine()
                .Evaluate(
                [
                    new(
                        DocumentSecurityObservationCategory.EmbeddedContent,
                        "Attachment",
                        "The PDF contains an attachment.",
                        "pdf")
                ],
                DocumentSecurityOperation.Open);

        DocumentSecurityReport? report =
            DocumentSecurityReportFactory.Create(
                assessment,
                DateTimeOffset.UnixEpoch);

        Assert.NotNull(report);
        Assert.Single(report.Findings);
        Assert.Equal(
            DateTimeOffset.UnixEpoch,
            report.GeneratedAtUtc);
    }

    [Fact]
    public void CleanManualAssessmentStillCreatesReport()
    {
        var assessment =
            new DocumentSecurityAssessmentEngine()
                .Evaluate(
                    [],
                    DocumentSecurityOperation.ManualInspection);

        DocumentSecurityReport? report =
            DocumentSecurityReportFactory.Create(
                assessment,
                DateTimeOffset.UnixEpoch);

        Assert.NotNull(report);
        Assert.Empty(report.Findings);
    }
}
