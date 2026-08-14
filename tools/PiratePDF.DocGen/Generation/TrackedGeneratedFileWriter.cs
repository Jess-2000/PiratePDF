using System.Text;

namespace PiratePDF.DocGen.Generation;

internal static class TrackedGeneratedFileWriter
{
    private const string GeneratedPrefix = "Generated: ";

    public static void WriteIfChanged(
        string path,
        string content,
        Encoding encoding)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(encoding);

        if (File.Exists(path))
        {
            string existingContent = File.ReadAllText(path, encoding);

            if (HasEquivalentSubstantiveContent(existingContent, content))
            {
                return;
            }
        }

        File.WriteAllText(path, content, encoding);
    }

    private static bool HasEquivalentSubstantiveContent(
        string existingContent,
        string generatedContent)
    {
        string normalizedExisting = NormalizeGeneratedTimestamp(existingContent);
        string normalizedGenerated = NormalizeGeneratedTimestamp(generatedContent);

        return string.Equals(
            normalizedExisting,
            normalizedGenerated,
            StringComparison.Ordinal);
    }

    private static string NormalizeGeneratedTimestamp(string content)
    {
        string[] lines = content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n');

        for (int index = 0; index < lines.Length; index++)
        {
            if (lines[index].StartsWith(
                    GeneratedPrefix,
                    StringComparison.Ordinal))
            {
                lines[index] = GeneratedPrefix;
                break;
            }
        }

        return string.Join('\n', lines);
    }
}