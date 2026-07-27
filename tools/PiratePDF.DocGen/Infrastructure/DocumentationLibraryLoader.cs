using PiratePDF.DocGen.Library;
using PiratePDF.DocGen.Parsing;

namespace PiratePDF.DocGen.Infrastructure;

internal static class DocumentationLibraryLoader
{
    private static readonly HashSet<string> ExcludedFileNames =
        new(
            StringComparer.OrdinalIgnoreCase)
        {
            "ADR-Template.md",
            "Document-Template.md",
            "Documentation-Health.md",
            "Document-Changes.md",
            "Documentation-Index.md",
            "ValidationReport.md"
        };

    // =========================================================================
    // Public API
    // =========================================================================

    public static DocumentationLibrary Load(
        string documentationDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            documentationDirectory);

        var fullDirectoryPath = Path.GetFullPath(
            documentationDirectory);

        if (!Directory.Exists(fullDirectoryPath))
        {
            throw new DirectoryNotFoundException(
                $"Documentation directory was not found: " +
                $"{fullDirectoryPath}");
        }

        // Build complete document objects so all commands share the same
        // parsed representation of the documentation library.
        var documents = Directory
            .EnumerateFiles(
                fullDirectoryPath,
                "*.md",
                SearchOption.AllDirectories)
            .Where(ShouldInclude)
            .Select(MarkdownDocumentParser.Parse);

        return new DocumentationLibrary(documents);
    }

    // =========================================================================
    // File Selection
    // =========================================================================

    private static bool ShouldInclude(
        string filePath)
    {
        return !ExcludedFileNames.Contains(
            Path.GetFileName(filePath));
    }
}