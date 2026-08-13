using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Policy;

namespace BlackFlag.Engine.Security.Tests.Inspection;

public sealed class DocumentSecurityAssessmentEngineTests
{
    [Fact]
    public void CleanAutomaticInspectionProducesNoFindingsOrInteractions()
    {
        var engine =
            new DocumentSecurityAssessmentEngine();

        DocumentSecurityAssessment assessment =
            engine.Evaluate(
                [],
                DocumentSecurityOperation.Open);

        Assert.Empty(assessment.Findings);
        Assert.Empty(assessment.Interactions);
        Assert.False(assessment.ShouldGenerateReport);
        Assert.False(assessment.IsBlocked);
    }

    [Fact]
    public void NonBlockingActiveContentProducesFindingWithoutPopupInteraction()
    {
        var engine =
            new DocumentSecurityAssessmentEngine();

        DocumentSecurityAssessment assessment =
            engine.Evaluate(
            [
                new(
                    DocumentSecurityObservationCategory.AutomaticAction,
                    "OpenAction",
                    "The PDF contains an automatic open action.",
                    "pdf")
            ],
            DocumentSecurityOperation.Open);

        Assert.Single(assessment.Findings);
        Assert.Empty(assessment.Interactions);
        Assert.True(assessment.ShouldGenerateReport);
    }

    [Fact]
    public void MultipleSanitizationFindingsRequireOnlyOneInteractionKind()
    {
        var engine =
            new DocumentSecurityAssessmentEngine();

        DocumentSecurityAssessment assessment =
            engine.Evaluate(
            [
                new(
                    DocumentSecurityObservationCategory.ScriptContent,
                    "VbaProject",
                    "The document contains macros.",
                    "docm"),
                new(
                    DocumentSecurityObservationCategory.EmbeddedContent,
                    "EmbeddedOleObject",
                    "The document contains an embedded OLE object.",
                    "docm")
            ],
            DocumentSecurityOperation.PassiveConversion);

        Assert.Equal(2, assessment.Findings.Count);
        Assert.Single(assessment.Interactions);
        Assert.Equal(
            DocumentSecurityInteraction.Sanitization,
            assessment.Interactions[0]);
    }

    [Fact]
    public void ManualInspectionAlwaysRequestsReportEvenWhenClean()
    {
        var engine =
            new DocumentSecurityAssessmentEngine();

        DocumentSecurityAssessment assessment =
            engine.Evaluate(
                [],
                DocumentSecurityOperation.ManualInspection);

        Assert.True(assessment.ShouldGenerateReport);
    }
}
