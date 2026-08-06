using System.Globalization;
using System.Text.RegularExpressions;

using PiratePDF.DocGen.Changes;
using PiratePDF.DocGen.Repair.Diagnostics;
using PiratePDF.DocGen.Repair.References;

namespace PiratePDF.DocGen.Repair.Rules;

internal sealed class BrokenDocumentReferenceRepairRule
    : IDocumentRepairRule
{
    public string Name =>
        "Broken document reference";

    // =========================================================================
    // Repair Analysis
    // =========================================================================

    public void Analyze(
        RepairRuleContext context,
        List<DocumentChange> changes,
        List<RepairDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(diagnostics);

        foreach (
            var referencedDocumentId in
            context.Document.RelatedDocumentIds)
        {
            if (context.Library.Documents.FindByDocumentId(
                    referencedDocumentId) is not null)
            {
                continue;
            }

            AnalyzeMissingReference(
                context,
                referencedDocumentId,
                changes,
                diagnostics);
        }
    }

    private void AnalyzeMissingReference(
        RepairRuleContext context,
        string missingDocumentId,
        List<DocumentChange> changes,
        List<RepairDiagnostic> diagnostics)
    {
        if (!DocumentationRedirectMap.TryResolve(
                missingDocumentId,
                out var replacementDocumentId))
        {
            diagnostics.Add(
                CreateDiagnostic(
                    context,
                    RepairDiagnosticSeverity.Warning,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Related document ID '{0}' does not resolve to an " +
                        "authoritative document. No explicit redirect is " +
                        "registered; manual review is required.",
                        missingDocumentId)));

            return;
        }

        var replacementDocument =
            context.Library.Documents.FindByDocumentId(
                replacementDocumentId);

        if (replacementDocument is null)
        {
            diagnostics.Add(
                CreateDiagnostic(
                    context,
                    RepairDiagnosticSeverity.Error,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Redirect '{0}' -> '{1}' is invalid because the " +
                        "replacement document does not exist.",
                        missingDocumentId,
                        replacementDocumentId)));

            return;
        }

        var matchingSpans = FindExactReferenceSpans(
            context.FileContents,
            missingDocumentId);

        if (matchingSpans.Length != 1)
        {
            diagnostics.Add(
                CreateDiagnostic(
                    context,
                    RepairDiagnosticSeverity.Warning,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Related document ID '{0}' redirects to '{1}', but " +
                        "{2} exact occurrences were found in the file. " +
                        "Automatic repair requires exactly one occurrence.",
                        missingDocumentId,
                        replacementDocumentId,
                        matchingSpans.Length)));

            return;
        }

        var span = matchingSpans[0];

        changes.Add(
            DocumentChange.Replace(
                context.Document.FilePath,
                span.StartIndex,
                span.Length,
                replacementDocumentId,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Replace obsolete related document ID '{0}' with its " +
                    "registered redirect '{1}'.",
                    missingDocumentId,
                    replacementDocumentId)));

        diagnostics.Add(
            CreateDiagnostic(
                context,
                RepairDiagnosticSeverity.Information,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "A deterministic redirect is available: '{0}' -> '{1}'.",
                    missingDocumentId,
                    replacementDocumentId)));
    }

    // =========================================================================
    // Reference Location
    // =========================================================================

    private static ReferenceSpan[] FindExactReferenceSpans(
        string content,
        string documentId)
    {
        ArgumentNullException.ThrowIfNull(content);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            documentId);

        var pattern = string.Concat(
            @"(?<![A-Z0-9-])",
            Regex.Escape(documentId),
            @"(?![A-Z0-9-])");

        return Regex
            .Matches(
                content,
                pattern,
                RegexOptions.CultureInvariant |
                RegexOptions.IgnoreCase)
            .Select(
                match => new ReferenceSpan(
                    match.Index,
                    match.Length))
            .ToArray();
    }

    // =========================================================================
    // Diagnostics
    // =========================================================================

    private RepairDiagnostic CreateDiagnostic(
        RepairRuleContext context,
        RepairDiagnosticSeverity severity,
        string message)
    {
        var documentId =
            string.IsNullOrWhiteSpace(
                context.Document.DocumentId)
                ? "(missing)"
                : context.Document.DocumentId;

        return new RepairDiagnostic(
            severity,
            Name,
            context.Document.FilePath,
            documentId,
            message);
    }

    private readonly record struct ReferenceSpan(
        int StartIndex,
        int Length);
}
