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

        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = Path.Combine(
            directory ?? string.Empty,
            $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(
                temporaryPath,
                content,
                encoding);

            File.Move(
                temporaryPath,
                path,
                overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static bool HasEquivalentSubstantiveContent(
        string existingContent,
        string generatedContent)
    {
        string normalizedExisting = Normalize(existingContent);
        string normalizedGenerated = Normalize(generatedContent);

        return string.Equals(
            normalizedExisting,
            normalizedGenerated,
            StringComparison.Ordinal);
    }

    private static string Normalize(string content)
    {
        string normalized = content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        string[] lines = normalized.Split('\n');

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

        return string.Join('\n', lines).TrimEnd('\n');
    }
}
