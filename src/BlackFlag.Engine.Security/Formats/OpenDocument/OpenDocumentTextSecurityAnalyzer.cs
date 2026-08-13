using BlackFlag.Engine.Security.Containers;
using BlackFlag.Engine.Security.Formats;
using BlackFlag.Engine.Security.Inspection;

namespace BlackFlag.Engine.Security.Formats.OpenDocument;

/// <summary>
/// Performs passive security analysis of OpenDocument Text packages without
/// executing scripts, event handlers, external references, or embedded payloads.
/// </summary>
public sealed class OpenDocumentTextSecurityAnalyzer
{
    private readonly OpenDocumentTextSecurityAnalysisOptions options;

    public OpenDocumentTextSecurityAnalyzer(
        OpenDocumentTextSecurityAnalysisOptions? options = null)
    {
        this.options = options ?? OpenDocumentTextSecurityAnalysisOptions.Default;
        this.options.Validate();
    }

    public OpenDocumentTextSecurityAnalysisResult Analyze(
        ReadOnlyMemory<byte> content,
        string fileName,
        DocumentSecurityInspectionBudget? budget = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (!string.Equals(
                Path.GetExtension(fileName),
                ".odt",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "OpenDocument Text security analysis requires an ODT file name.",
                nameof(fileName));
        }

        budget ??= new DocumentSecurityInspectionBudget();

        var root = new DocumentEmbeddedObject(
            content,
            logicalPath: "/",
            declaredMediaType: "application/vnd.oasis.opendocument.text",
            declaredFileName: fileName);

        var engine = new DocumentEmbeddedInspectionEngine(
            securityAdapters:
                DocumentSecurityFormatAdapterFactory.Create(
                    openDocumentOptions: options));

        DocumentEmbeddedInspectionResult result =
            engine.InspectRoot(root, budget, cancellationToken);

        return new OpenDocumentTextSecurityAnalysisResult(result.Observations);
    }
}
