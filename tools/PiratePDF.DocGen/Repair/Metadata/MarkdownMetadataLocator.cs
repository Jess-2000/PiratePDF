using System.Text.RegularExpressions;

namespace PiratePDF.DocGen.Repair.Metadata;

internal static partial class MarkdownMetadataLocator
{
    // =========================================================================
    // Public API
    // =========================================================================

    public static Dictionary<string, MarkdownMetadataValueSpan> FindFields(
        string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var fields =
            new Dictionary<string, MarkdownMetadataValueSpan>(
                StringComparer.OrdinalIgnoreCase);

        var lines = EnumerateLines(markdown);
        var titleLineIndex = FindTitleLineIndex(lines);

        if (titleLineIndex < 0)
        {
            return fields;
        }

        var index = titleLineIndex + 1;

        while (
            index < lines.Count &&
            string.IsNullOrWhiteSpace(lines[index].Text))
        {
            index++;
        }

        if (
            index >= lines.Count ||
            !MetadataHeaderPattern().IsMatch(lines[index].Text))
        {
            return fields;
        }

        index++;

        if (
            index >= lines.Count ||
            !MetadataSeparatorPattern().IsMatch(lines[index].Text))
        {
            return fields;
        }

        index++;

        // Only the first table after the level-one title is authoritative
        // metadata. Later tables remain ordinary document content.
        while (
            index < lines.Count &&
            IsTableRow(lines[index].Text))
        {
            if (TryParseMetadataValue(
                    lines[index],
                    out var key,
                    out var valueSpan))
            {
                fields[key] = valueSpan;
            }

            index++;
        }

        return fields;
    }

    public static bool TryFindField(
        string markdown,
        string fieldName,
        out MarkdownMetadataValueSpan valueSpan)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            fieldName);

        var fields = FindFields(markdown);

        return fields.TryGetValue(
            fieldName,
            out valueSpan);
    }

    // =========================================================================
    // Metadata Parsing
    // =========================================================================

    private static bool TryParseMetadataValue(
        MarkdownLine line,
        out string key,
        out MarkdownMetadataValueSpan valueSpan)
    {
        key = string.Empty;
        valueSpan = default;

        var match = MetadataRowPattern().Match(
            line.Text);

        if (!match.Success)
        {
            return false;
        }

        var keyGroup = match.Groups["key"];
        var valueGroup = match.Groups["value"];
        var untrimmedValue = valueGroup.Value;

        key = keyGroup.Value.Trim();

        var leadingWhitespaceLength =
            untrimmedValue.Length -
            untrimmedValue.TrimStart().Length;

        var trailingWhitespaceLength =
            untrimmedValue.Length -
            untrimmedValue.TrimEnd().Length;

        var trimmedLength =
            untrimmedValue.Length -
            leadingWhitespaceLength -
            trailingWhitespaceLength;

        if (trimmedLength < 0)
        {
            return false;
        }

        var startIndex = checked(
            line.StartIndex +
            valueGroup.Index +
            leadingWhitespaceLength);

        valueSpan = new MarkdownMetadataValueSpan(
            startIndex,
            trimmedLength,
            untrimmedValue.Trim());

        return !string.IsNullOrWhiteSpace(key);
    }

    private static bool IsTableRow(
        string line)
    {
        return line
            .TrimStart()
            .StartsWith('|');
    }

    // =========================================================================
    // Line Parsing
    // =========================================================================

    private static int FindTitleLineIndex(
        List<MarkdownLine> lines)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            if (lines[index].Text.StartsWith(
                    "# ",
                    StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private static List<MarkdownLine> EnumerateLines(
        string markdown)
    {
        var lines = new List<MarkdownLine>();
        var lineStartIndex = 0;

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
                    lineStartIndex,
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
        }

        if (lineStartIndex <= markdown.Length)
        {
            lines.Add(
                new MarkdownLine(
                    lineStartIndex,
                    markdown[lineStartIndex..]));
        }

        return lines;
    }

    // =========================================================================
    // Internal Models
    // =========================================================================

    private readonly record struct MarkdownLine(
        int StartIndex,
        string Text);

    // =========================================================================
    // Patterns
    // =========================================================================

    [GeneratedRegex(
        @"^\|\s*Field\s*\|\s*Value\s*\|\s*$",
        RegexOptions.CultureInvariant |
        RegexOptions.IgnoreCase)]
    private static partial Regex MetadataHeaderPattern();

    [GeneratedRegex(
        @"^\|\s*:?-{3,}:?\s*\|\s*:?-{3,}:?\s*\|\s*$",
        RegexOptions.CultureInvariant)]
    private static partial Regex MetadataSeparatorPattern();

    [GeneratedRegex(
        @"^\|\s*\*{0,2}(?<key>[^|*]+?)\*{0,2}\s*\|(?<value>.*?)\|\s*$",
        RegexOptions.CultureInvariant)]
    private static partial Regex MetadataRowPattern();
}
