using PiratePDF.DocGen.Models;

namespace PiratePDF.DocGen.Parsing;

internal static class MarkdownMetadataParser
{
    // =========================================================================
    // Configuration
    // =========================================================================

    private const string MetadataHeaderField = "Field";
    private const string MetadataHeaderValue = "Value";

    // =========================================================================
    // Public API
    // =========================================================================

    public static DocumentMetadata Parse(
        string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var lines = File.ReadAllLines(filePath);

        var titleIndex = FindTitleIndex(lines);
        var title = GetTitle(lines, titleIndex);

        var values = ParseMetadataTable(
            lines,
            titleIndex);

        return new DocumentMetadata(
            filePath,
            title,
            GetValue(values, "Document ID"),
            GetValue(values, "Category"),
            GetValue(values, "Version"),
            GetValue(values, "Status"),
            GetValue(values, "Owner"),
            GetValue(values, "Related BF Work Items"),
            GetValue(values, "Last Updated"),
            GetValue(values, "Next Review"));
    }

    // =========================================================================
    // Title Parsing
    // =========================================================================

    private static int FindTitleIndex(
        string[] lines)
    {
        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].StartsWith(
                    "# ",
                    StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private static string GetTitle(
        string[] lines,
        int titleIndex)
    {
        if (titleIndex < 0)
        {
            return string.Empty;
        }

        return lines[titleIndex][2..].Trim();
    }

    // =========================================================================
    // Metadata Parsing
    // =========================================================================

    private static Dictionary<string, string> ParseMetadataTable(
        string[] lines,
        int titleIndex)
    {
        var values = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        if (titleIndex < 0)
        {
            return values;
        }

        var index = titleIndex + 1;

        // Metadata must be the first nonblank block after the document title.
        index = SkipBlankLines(
            lines,
            index);

        if (!IsMetadataHeader(
                lines,
                index))
        {
            return values;
        }

        index++;

        if (!IsTableSeparator(
                lines,
                index))
        {
            return values;
        }

        index++;

        // Read only this table. Later Markdown tables are document content and
        // must never overwrite authoritative metadata.
        while (
            index < lines.Length &&
            IsTableRow(lines[index]))
        {
            if (TryParseMetadataRow(
                    lines[index],
                    out var key,
                    out var value))
            {
                values[key] = value;
            }

            index++;
        }

        return values;
    }

    private static bool IsMetadataHeader(
        string[] lines,
        int index)
    {
        if (
            index < 0 ||
            index >= lines.Length)
        {
            return false;
        }

        if (!TrySplitTableRow(
                lines[index],
                out var cells))
        {
            return false;
        }

        return
            cells.Count == 2 &&
            string.Equals(
                RemoveBoldFormatting(cells[0]),
                MetadataHeaderField,
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                RemoveBoldFormatting(cells[1]),
                MetadataHeaderValue,
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTableSeparator(
        string[] lines,
        int index)
    {
        if (
            index < 0 ||
            index >= lines.Length)
        {
            return false;
        }

        if (!TrySplitTableRow(
                lines[index],
                out var cells))
        {
            return false;
        }

        if (cells.Count != 2)
        {
            return false;
        }

        return cells.All(IsSeparatorCell);
    }

    private static bool IsSeparatorCell(
        string value)
    {
        var trimmed = value.Trim();

        if (
            trimmed.StartsWith(
                ':') &&
            trimmed.Length > 0)
        {
            trimmed = trimmed[1..];
        }

        if (
            trimmed.EndsWith(
                ':') &&
            trimmed.Length > 0)
        {
            trimmed = trimmed[..^1];
        }

        return
            trimmed.Length >= 3 &&
            trimmed.All(character => character == '-');
    }

    private static bool TryParseMetadataRow(
        string line,
        out string key,
        out string value)
    {
        key = string.Empty;
        value = string.Empty;

        if (!TrySplitTableRow(
                line,
                out var cells))
        {
            return false;
        }

        if (cells.Count != 2)
        {
            return false;
        }

        key = RemoveBoldFormatting(
            cells[0]);

        value = cells[1].Trim();

        return !string.IsNullOrWhiteSpace(key);
    }

    private static bool TrySplitTableRow(
        string line,
        out IReadOnlyList<string> cells)
    {
        cells = Array.Empty<string>();

        if (!IsTableRow(line))
        {
            return false;
        }

        var trimmed = line.Trim();

        if (
            trimmed.StartsWith(
                '|'))
        {
            trimmed = trimmed[1..];
        }

        if (
            trimmed.EndsWith(
                '|'))
        {
            trimmed = trimmed[..^1];
        }

        cells = trimmed
            .Split(
                '|',
                StringSplitOptions.TrimEntries)
            .ToArray();

        return true;
    }

    private static bool IsTableRow(
        string line)
    {
        return line
            .TrimStart()
            .StartsWith(
                '|');
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static int SkipBlankLines(
        string[] lines,
        int startIndex)
    {
        var index = startIndex;

        while (
            index < lines.Length &&
            string.IsNullOrWhiteSpace(lines[index]))
        {
            index++;
        }

        return index;
    }

    private static string RemoveBoldFormatting(
        string value)
    {
        var trimmed = value.Trim();

        if (
            trimmed.Length >= 4 &&
            trimmed.StartsWith(
                "**",
                StringComparison.Ordinal) &&
            trimmed.EndsWith(
                "**",
                StringComparison.Ordinal))
        {
            return trimmed[2..^2].Trim();
        }

        return trimmed;
    }

    private static string GetValue(
        Dictionary<string, string> values,
        string key)
    {
        return values.TryGetValue(
            key,
            out var value)
            ? value
            : string.Empty;
    }
}
