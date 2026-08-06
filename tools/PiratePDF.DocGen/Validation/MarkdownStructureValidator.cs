using System.Globalization;
using System.Text.RegularExpressions;

using PiratePDF.DocGen.Library;

namespace PiratePDF.DocGen.Validation;

internal static partial class MarkdownStructureValidator
{
    // =========================================================================
    // Public API
    // =========================================================================

    public static List<ValidationIssue> Validate(
        DocumentationDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var issues = new List<ValidationIssue>();

        ValidateCodeFences(
            document,
            issues);

        return issues;
    }

    // =========================================================================
    // Code-Fence Validation
    // =========================================================================

    private static void ValidateCodeFences(
        DocumentationDocument document,
        List<ValidationIssue> issues)
    {
        var lines = EnumerateLines(
            document.RawMarkdown);

        ActiveFence? activeFence = null;

        foreach (var line in lines)
        {
            var match = FencePattern().Match(
                line.Text);

            if (!match.Success)
            {
                continue;
            }

            var marker = match.Groups["marker"].Value;
            var markerCharacter = marker[0];

            if (activeFence is null)
            {
                activeFence = new ActiveFence(
                    markerCharacter,
                    marker.Length,
                    line.LineNumber);

                continue;
            }

            if (!CanCloseFence(
                    activeFence,
                    markerCharacter,
                    marker.Length))
            {
                continue;
            }

            activeFence = null;
        }

        if (activeFence is null)
        {
            return;
        }

        var markerText = new string(
            activeFence.MarkerCharacter,
            activeFence.MarkerLength);

        issues.Add(
            new ValidationIssue(
            ValidationSeverity.Error,
            GetDisplayDocumentId(document),
            document.FilePath,
            string.Format(
                CultureInfo.InvariantCulture,
                "Unclosed fenced code block beginning on line {0}. " +
                "Opening marker: '{1}'. Manual review is required.",
                activeFence.OpeningLineNumber,
                markerText)));
    }

    private static bool CanCloseFence(
        ActiveFence activeFence,
        char markerCharacter,
        int markerLength)
    {
        return
            markerCharacter == activeFence.MarkerCharacter &&
            markerLength >= activeFence.MarkerLength;
    }

    // =========================================================================
    // Markdown Lines
    // =========================================================================

    private static List<MarkdownLine> EnumerateLines(
        string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var lines = new List<MarkdownLine>();
        var lineStartIndex = 0;
        var lineNumber = 1;

        for (var index = 0; index < markdown.Length; index++)
        {
            if (
                markdown[index] != '\r' &&
                markdown[index] != '\n')
            {
                continue;
            }

            lines.Add(
                new MarkdownLine(
                    lineNumber,
                    markdown.Substring(
                        lineStartIndex,
                        index - lineStartIndex)));

            if (
                markdown[index] == '\r' &&
                index + 1 < markdown.Length &&
                markdown[index + 1] == '\n')
            {
                index++;
            }

            lineStartIndex = index + 1;
            lineNumber++;
        }

        if (lineStartIndex <= markdown.Length)
        {
            lines.Add(
                new MarkdownLine(
                    lineNumber,
                    markdown[lineStartIndex..]));
        }

        return lines;
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static string GetDisplayDocumentId(
        DocumentationDocument document)
    {
        return string.IsNullOrWhiteSpace(document.DocumentId)
            ? "(missing)"
            : document.DocumentId;
    }

    private readonly record struct MarkdownLine(
        int LineNumber,
        string Text);

    private sealed record ActiveFence(
        char MarkerCharacter,
        int MarkerLength,
        int OpeningLineNumber);

    // =========================================================================
    // Patterns
    // =========================================================================

    [GeneratedRegex(
        @"^\s{0,3}(?<marker>`{3,}|~{3,})(?:[^`~].*)?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex FencePattern();
}