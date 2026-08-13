using System;
using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Policy;
using BlackFlag.Engine.Security.Remediation;

namespace BlackFlag.Engine.Security.Tests.Remediation;

public sealed class DocumentSecurityRemediationPlannerTests
{
    [Theory]
    [InlineData(DocumentSecurityObservationCategory.ScriptContent, DocumentSecurityRemediationIntent.RemoveActiveContent)]
    [InlineData(DocumentSecurityObservationCategory.ActiveContent, DocumentSecurityRemediationIntent.RemoveActiveContent)]
    [InlineData(DocumentSecurityObservationCategory.AutomaticAction, DocumentSecurityRemediationIntent.RemoveActiveContent)]
    [InlineData(DocumentSecurityObservationCategory.ExecutableContent, DocumentSecurityRemediationIntent.RemoveActiveContent)]
    [InlineData(DocumentSecurityObservationCategory.SuspiciousRelationship, DocumentSecurityRemediationIntent.RemoveActiveContent)]
    [InlineData(DocumentSecurityObservationCategory.EmbeddedContent, DocumentSecurityRemediationIntent.RemoveEmbeddedContent)]
    [InlineData(DocumentSecurityObservationCategory.ExternalReference, DocumentSecurityRemediationIntent.DisableExternalReference)]
    public void SanitizingFindingMapsToExpectedIntent(
        DocumentSecurityObservationCategory category,
        DocumentSecurityRemediationIntent expectedIntent)
    {
        DocumentSecurityAssessment assessment = Assessment(Observation(category));
        var planner = new DocumentSecurityRemediationPlanner();

        DocumentSecurityRemediationPlan plan =
            DocumentSecurityRemediationPlanner.CreatePlan(
                assessment,
                DocumentSecurityOperation.PassiveConversion);

        DocumentSecurityRemediationAction action = Assert.Single(plan.Actions);
        Assert.Equal(expectedIntent, action.Intent);
        Assert.False(plan.IsBlocked);
        Assert.True(plan.RequiresMutation);
    }

    [Fact]
    public void PasswordRequiredRejectsPassiveConversion()
    {
        DocumentSecurityAssessment assessment =
            Assessment(
                new(
                    DocumentSecurityObservationCategory.EncryptionCondition,
                    "PasswordRequired",
                    "The document requires authentication.",
                    "pdf"));

        var planner = new DocumentSecurityRemediationPlanner();
        DocumentSecurityRemediationPlan plan =
            DocumentSecurityRemediationPlanner.CreatePlan(
                assessment,
                DocumentSecurityOperation.PassiveConversion);

        Assert.True(plan.IsBlocked);
        Assert.Equal(
            DocumentSecurityRemediationIntent.RejectOperation,
            Assert.Single(plan.Actions).Intent);
    }

    [Fact]
    public void ReportOnlyFindingDoesNotCreateMutationAction()
    {
        var engine = new DocumentSecurityAssessmentEngine();
        DocumentSecurityAssessment assessment =
            engine.Evaluate(
                [Observation(DocumentSecurityObservationCategory.InteractiveContent)],
                DocumentSecurityOperation.PassiveConversion);

        var planner = new DocumentSecurityRemediationPlanner();
        DocumentSecurityRemediationPlan plan =
            DocumentSecurityRemediationPlanner.CreatePlan(
                assessment,
                DocumentSecurityOperation.PassiveConversion);

        Assert.Empty(plan.Actions);
        Assert.False(plan.RequiresMutation);
    }

    [Fact]
    public void NonSanitizingOperationIsRejected()
    {
        var planner = new DocumentSecurityRemediationPlanner();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => DocumentSecurityRemediationPlanner.CreatePlan(
                Assessment(Observation(DocumentSecurityObservationCategory.ScriptContent)),
                DocumentSecurityOperation.Open));
    }

    private static DocumentSecurityAssessment Assessment(
        DocumentSecurityObservation observation)
    {
        var engine = new DocumentSecurityAssessmentEngine();
        return engine.Evaluate(
            [observation],
            DocumentSecurityOperation.PassiveConversion);
    }

    private static DocumentSecurityObservation Observation(
        DocumentSecurityObservationCategory category) =>
        new(category, "TestObservation", "Test security observation.", "test");
}
