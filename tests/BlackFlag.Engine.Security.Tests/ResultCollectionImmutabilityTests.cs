using System;
using System.Collections.Generic;
using BlackFlag.Engine.Security.Containers;
using BlackFlag.Engine.Security.Findings;
using BlackFlag.Engine.Security.Formats.Markdown;
using BlackFlag.Engine.Security.Formats.Ole;
using BlackFlag.Engine.Security.Formats.OpenDocument;
using BlackFlag.Engine.Security.Formats.Ooxml;
using BlackFlag.Engine.Security.Formats.Rtf;
using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Policy;
using BlackFlag.Engine.Security.Remediation;
using BlackFlag.Engine.Security.Reporting;

namespace BlackFlag.Engine.Security.Tests;

public sealed class ResultCollectionImmutabilityTests
{
    [Fact]
    public void AssessmentCollectionsCannotChangePolicyResult()
    {
        DocumentSecurityFinding blocking = Finding(DocumentSecurityPolicyAction.Block);
        DocumentSecurityFinding replacement = Finding(DocumentSecurityPolicyAction.AllowAndReport);
        var assessment = new DocumentSecurityAssessment(
            [blocking],
            [DocumentSecurityInteraction.None, DocumentSecurityInteraction.PasswordRequired,
                DocumentSecurityInteraction.PasswordRequired],
            manualInspection: false);

        AssertReadOnlySnapshot(assessment.Findings, blocking, replacement);
        AssertReadOnlySnapshot(
            assessment.Interactions,
            DocumentSecurityInteraction.PasswordRequired,
            DocumentSecurityInteraction.Sanitization);
        Assert.Same(assessment.Findings, assessment.Findings);
        Assert.Same(assessment.Interactions, assessment.Interactions);
        Assert.True(assessment.IsBlocked);
        Assert.True(assessment.ShouldGenerateReport);
        Assert.Equal([DocumentSecurityInteraction.PasswordRequired], assessment.Interactions);
    }

    [Fact]
    public void ReportCollectionsCannotChangeReportContents()
    {
        DocumentSecurityFinding original = Finding(DocumentSecurityPolicyAction.AllowAndReport);
        DocumentSecurityFinding replacement = Finding(DocumentSecurityPolicyAction.Block);
        var report = new DocumentSecurityReport(
            [original],
            [DocumentSecurityInteraction.None, DocumentSecurityInteraction.StructuralRecovery,
                DocumentSecurityInteraction.StructuralRecovery],
            manualInspection: true,
            DateTimeOffset.UnixEpoch);

        AssertReadOnlySnapshot(report.Findings, original, replacement);
        AssertReadOnlySnapshot(
            report.Interactions,
            DocumentSecurityInteraction.StructuralRecovery,
            DocumentSecurityInteraction.Sanitization);
        Assert.Same(report.Findings, report.Findings);
        Assert.Same(report.Interactions, report.Interactions);
        Assert.True(report.ManualInspection);
        Assert.Equal(DateTimeOffset.UnixEpoch, report.GeneratedAtUtc);
    }

    [Fact]
    public void RemediationActionsCannotChangePlan()
    {
        var original = new DocumentSecurityRemediationAction(
            Finding(DocumentSecurityPolicyAction.Sanitize),
            DocumentSecurityRemediationIntent.RemoveActiveContent);
        var replacement = new DocumentSecurityRemediationAction(
            Finding(DocumentSecurityPolicyAction.Block),
            DocumentSecurityRemediationIntent.RejectOperation);
        var plan = new DocumentSecurityRemediationPlan([original], isBlocked: false);

        AssertReadOnlySnapshot(plan.Actions, original, replacement);
        Assert.Same(plan.Actions, plan.Actions);
        Assert.False(plan.IsBlocked);
        Assert.True(plan.RequiresMutation);
    }

    [Fact]
    public void ContainerCollectionsCannotChangeInspectionResult()
    {
        DocumentSecurityObservation originalObservation = Observation("Original");
        DocumentSecurityObservation replacementObservation = Observation("Replacement");
        var originalEntry = new DocumentContainerEntry(
            new DocumentEmbeddedObject(new byte[] { 1 }, "original.bin"),
            compressedLength: 1);
        var replacementEntry = new DocumentContainerEntry(
            new DocumentEmbeddedObject(new byte[] { 2 }, "replacement.bin"),
            compressedLength: 1);
        var inspection = new DocumentContainerSecurityInspection(
            [originalObservation],
            [originalEntry]);

        AssertReadOnlySnapshot(
            inspection.Observations,
            originalObservation,
            replacementObservation);
        AssertReadOnlySnapshot(
            inspection.EmbeddedObjects,
            originalEntry,
            replacementEntry);
        Assert.Same(inspection.Observations, inspection.Observations);
        Assert.Same(inspection.EmbeddedObjects, inspection.EmbeddedObjects);
    }

    [Fact]
    public void EmbeddedInspectionObservationsCannotChange()
    {
        DocumentSecurityObservation original = Observation("Original");
        var result = new DocumentEmbeddedInspectionResult([original]);

        AssertReadOnlySnapshot(result.Observations, original, Observation("Replacement"));
        Assert.Same(result.Observations, result.Observations);
    }

    [Fact]
    public void MarkdownObservationsCannotChange()
    {
        DocumentSecurityObservation original = Observation("Original");
        var result = new MarkdownSecurityAnalysisResult([original]);

        AssertReadOnlySnapshot(result.Observations, original, Observation("Replacement"));
        Assert.Same(result.Observations, result.Observations);
    }

    [Fact]
    public void RtfObservationsCannotChange()
    {
        DocumentSecurityObservation original = Observation("Original");
        var result = new RtfSecurityAnalysisResult([original]);

        AssertReadOnlySnapshot(result.Observations, original, Observation("Replacement"));
        Assert.Same(result.Observations, result.Observations);
    }

    [Fact]
    public void LegacyWordObservationsCannotChange()
    {
        DocumentSecurityObservation original = Observation("Original");
        var result = new LegacyWordBinarySecurityAnalysisResult([original]);

        AssertReadOnlySnapshot(result.Observations, original, Observation("Replacement"));
        Assert.Same(result.Observations, result.Observations);
    }

    [Fact]
    public void OoxmlObservationsCannotChange()
    {
        DocumentSecurityObservation original = Observation("Original");
        var result = new OoxmlWordSecurityAnalysisResult([original]);

        AssertReadOnlySnapshot(result.Observations, original, Observation("Replacement"));
        Assert.Same(result.Observations, result.Observations);
    }

    [Fact]
    public void OpenDocumentObservationsCannotChange()
    {
        DocumentSecurityObservation original = Observation("Original");
        var result = new OpenDocumentTextSecurityAnalysisResult([original]);

        AssertReadOnlySnapshot(result.Observations, original, Observation("Replacement"));
        Assert.Same(result.Observations, result.Observations);
    }

    private static void AssertReadOnlySnapshot<T>(
        IReadOnlyList<T> items,
        T original,
        T replacement)
    {
        Assert.False(items is T[]);
        IList<T> mutableView = Assert.IsAssignableFrom<IList<T>>(items);
        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutableView[0] = replacement);
        Assert.Equal(original, items[0]);
    }

    private static DocumentSecurityFinding Finding(DocumentSecurityPolicyAction action) =>
        new(
            Observation(action.ToString()),
            action,
            action == DocumentSecurityPolicyAction.Block
                ? DocumentSecuritySeverity.Blocking
                : DocumentSecuritySeverity.Warning,
            "Finding message.",
            DocumentSecurityInteraction.None);

    private static DocumentSecurityObservation Observation(string kind) =>
        new(
            DocumentSecurityObservationCategory.ActiveContent,
            kind,
            $"{kind} description.",
            "test");
}
