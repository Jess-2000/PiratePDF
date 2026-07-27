using System.Text.RegularExpressions;

using PiratePDF.DocGen.Library;

namespace PiratePDF.DocGen.Parsing;

internal static partial class MarkdownDocumentParser
{
    private const string RelatedDocumentsHeading =
        "Related Documents";

    private const string RelatedWorkItemsHeading =
        "Related BF Work Items";

    private const string RevisionHistoryHeading =
        "Revision History";

    // =========================================================================
    // Public API
    // =========================================================================

    public static DocumentationDocument Parse(
        string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fullFilePath = Path.GetFullPath(filePath);

        if (!File.Exists(fullFilePath))
        {
            throw new FileNotFoundException(
                "The Markdown document was not found.",
                fullFilePath);
        }

        var rawMarkdown = File.ReadAllText(fullFilePath);
        var lines = File.ReadAllLines(fullFilePath);
        var metadata = MarkdownMetadataParser.Parse(fullFilePath);

        var sections = ParseSections(lines);
        var relatedDocumentIds = ParseRelatedDocumentIds(
            lines,
            sections);

        var relatedWorkItems = ParseRelatedWorkItems(
            metadata.RelatedWorkItems,
            lines,
            sections);

        var revisionHistory = ParseRevisionHistory(
            lines,
            sections);

        return new DocumentationDocument(
            metadata,
            rawMarkdown,
            sections,
            relatedDocumentIds,
            relatedWorkItems,
            revisionHistory);
    }

    // =========================================================================
    // Section Parsing
    // =========================================================================

    private static List<DocumentationSection> ParseSections(
        string[] lines)
    {
        var headings = new List<ParsedHeading>();
        var activeFence = string.Empty;

        for (var index = 0; index < lines.Length; index++)
        {
            var trimmedLine = lines[index].TrimStart();

            if (TryUpdateCodeFence(
                    trimmedLine,
                    ref activeFence))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(activeFence))
            {
                continue;
            }

            var match = HeadingPattern().Match(lines[index]);

            if (!match.Success)
            {
                continue;
            }

            headings.Add(
                new ParsedHeading(
                    match.Groups["heading"].Value.Trim(),
                    match.Groups["marker"].Value.Length,
                    index));
        }

        var sections = new List<DocumentationSection>(
            headings.Count);

        for (var index = 0; index < headings.Count; index++)
        {
            var heading = headings[index];

            var endLineIndex = index + 1 < headings.Count
                ? headings[index + 1].LineIndex - 1
                : lines.Length - 1;

            sections.Add(
                new DocumentationSection(
                    heading.Text,
                    heading.Level,
                    heading.LineIndex + 1,
                    endLineIndex + 1));
        }

        return sections;
    }

    private static bool TryUpdateCodeFence(
        string line,
        ref string activeFence)
    {
        if (string.IsNullOrEmpty(activeFence))
        {
            if (line.StartsWith(
                    "```",
                    StringComparison.Ordinal))
            {
                activeFence = "```";
                return true;
            }

            if (line.StartsWith(
                    "~~~",
                    StringComparison.Ordinal))
            {
                activeFence = "~~~";
                return true;
            }

            return false;
        }

        if (line.StartsWith(
                activeFence,
                StringComparison.Ordinal))
        {
            activeFence = string.Empty;
            return true;
        }

        return false;
    }

    // =========================================================================
    // Related Document Parsing
    // =========================================================================

    private static string[] ParseRelatedDocumentIds(
        string[] lines,
        IReadOnlyList<DocumentationSection> sections)
    {
        var section = FindSection(
            sections,
            RelatedDocumentsHeading);

        if (section is null)
        {
            return [];
        }

        return EnumerateSectionLines(
                lines,
                section)
            .SelectMany(line =>
                DocumentIdPattern()
                    .Matches(line)
                    .Select(match =>
                        match.Value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(
                value => value,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    // =========================================================================
    // BF Work-Item Parsing
    // =========================================================================

    private static string[] ParseRelatedWorkItems(
        string metadataValue,
        string[] lines,
        IReadOnlyList<DocumentationSection> sections)
    {
        var workItems = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        AddPatternMatches(
            workItems,
            WorkItemPattern(),
            metadataValue);

        var section = FindSection(
            sections,
            RelatedWorkItemsHeading);

        if (section is not null)
        {
            foreach (var line in EnumerateSectionLines(
                         lines,
                         section))
            {
                AddPatternMatches(
                    workItems,
                    WorkItemPattern(),
                    line);
            }
        }

        return workItems
            .OrderBy(
                value => value,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    // =========================================================================
    // Revision History Parsing
    // =========================================================================

    private static List<RevisionHistoryEntry> ParseRevisionHistory(
        string[] lines,
        IReadOnlyList<DocumentationSection> sections)
    {
        var section = FindSection(
            sections,
            RevisionHistoryHeading);

        if (section is null)
        {
            return [];
        }

        var entries = new List<RevisionHistoryEntry>();

        foreach (var line in EnumerateSectionLines(
                     lines,
                     section))
        {
            if (!TrySplitTableRow(
                    line,
                    out var cells))
            {
                continue;
            }

            if (cells.Length != 3)
            {
                continue;
            }

            if (IsRevisionHistoryHeader(cells) ||
                IsTableSeparator(cells))
            {
                continue;
            }

            entries.Add(
                new RevisionHistoryEntry(
                    cells[0],
                    cells[1],
                    cells[2]));
        }

        return entries;
    }

    private static bool IsRevisionHistoryHeader(
        string[] cells)
    {
        return
            string.Equals(
                cells[0],
                "Version",
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                cells[1],
                "Date",
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                cells[2],
                "Description",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTableSeparator(
        string[] cells)
    {
        return cells.All(IsSeparatorCell);
    }

    private static bool IsSeparatorCell(
        string value)
    {
        var trimmed = value.Trim().Trim(':');

        return
            trimmed.Length >= 3 &&
            trimmed.All(character => character == '-');
    }

    // =========================================================================
    // Section Helpers
    // =========================================================================

    private static DocumentationSection? FindSection(
        IReadOnlyList<DocumentationSection> sections,
        string heading)
    {
        return sections.FirstOrDefault(
            section => string.Equals(
                section.Heading,
                heading,
                StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> EnumerateSectionLines(
        string[] lines,
        DocumentationSection section)
    {
        var startIndex = Math.Clamp(
            section.StartLine,
            0,
            lines.Length);

        var endIndexExclusive = Math.Clamp(
            section.EndLine,
            startIndex,
            lines.Length);

        for (
            var index = startIndex;
            index < endIndexExclusive;
            index++)
        {
            yield return lines[index];
        }
    }

    // =========================================================================
    // Markdown Table Helpers
    // =========================================================================

    private static bool TrySplitTableRow(
        string line,
        out string[] cells)
    {
        cells = [];

        var trimmed = line.Trim();

        if (!trimmed.StartsWith(
                '|'))
        {
            return false;
        }

        trimmed = trimmed[1..];

        if (trimmed.EndsWith(
                '|'))
        {
            trimmed = trimmed[..^1];
        }

        cells = trimmed
            .Split(
                '|',
                StringSplitOptions.TrimEntries);

        return true;
    }

    // =========================================================================
    // Pattern Helpers
    // =========================================================================

    private static void AddPatternMatches(
        HashSet<string> destination,
        Regex pattern,
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        foreach (Match match in pattern.Matches(value))
        {
            destination.Add(match.Value);
        }
    }

    private sealed record ParsedHeading(
        string Text,
        int Level,
        int LineIndex);

    [GeneratedRegex(
        @"^(?<marker>#{1,6})\s+(?<heading>.+?)\s*$",
        RegexOptions.CultureInvariant)]
    private static partial Regex HeadingPattern();

    [GeneratedRegex(
        @"\bPP-[A-Z][A-Z0-9]*(?:-[A-Z][A-Z0-9]*)*-\d{3,}\b",
        RegexOptions.CultureInvariant |
        RegexOptions.IgnoreCase)]
    private static partial Regex DocumentIdPattern();

    [GeneratedRegex(
        @"\bBF-\d{4,}\b",
        RegexOptions.CultureInvariant |
        RegexOptions.IgnoreCase)]
    private static partial Regex WorkItemPattern();
}
