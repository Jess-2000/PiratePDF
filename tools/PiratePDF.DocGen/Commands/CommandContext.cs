using PiratePDF.DocGen.Library;

namespace PiratePDF.DocGen.Commands;

internal sealed class CommandContext
{
    private DocumentationLibrary? documentationLibrary;

    public CommandContext(
        string repositoryRoot,
        string documentationDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentationDirectory);

        RepositoryRoot = Path.GetFullPath(repositoryRoot);
        DocumentationDirectory = Path.GetFullPath(
            documentationDirectory);
    }

    // =========================================================================
    // Paths
    // =========================================================================

    public string RepositoryRoot { get; }

    public string DocumentationDirectory { get; }

    public string DocumentationIndexPath =>
        Path.Combine(
            DocumentationDirectory,
            "Documentation-Index.md");

    public string ValidationReportPath =>
        Path.Combine(
            DocumentationDirectory,
            "ValidationReport.md");

    public string DocumentationHealthReportPath =>
        Path.Combine(
            DocumentationDirectory,
            "Documentation-Health.md");

    public string DocumentChangeReportPath =>
        Path.Combine(
            DocumentationDirectory,
            "Document-Changes.md");

    // =========================================================================
    // Documentation Library
    // =========================================================================

    public DocumentationLibrary DocumentationLibrary =>
        documentationLibrary
        ?? throw new InvalidOperationException(
            "The documentation library has not been loaded.");

    public bool IsDocumentationLibraryLoaded =>
        documentationLibrary is not null;

    public void SetDocumentationLibrary(
        DocumentationLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);

        documentationLibrary = library;
    }
}