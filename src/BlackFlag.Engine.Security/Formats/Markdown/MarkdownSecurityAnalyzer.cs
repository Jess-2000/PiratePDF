using BlackFlag.Engine.Security.Containers;
using BlackFlag.Engine.Security.Formats;
using BlackFlag.Engine.Security.Inspection;

namespace BlackFlag.Engine.Security.Formats.Markdown;

/// <summary>Performs passive bounded security analysis of Markdown documents.</summary>
public sealed class MarkdownSecurityAnalyzer
{
    private readonly MarkdownSecurityAnalysisOptions options;

    public MarkdownSecurityAnalyzer(MarkdownSecurityAnalysisOptions? options = null)
    {
        this.options = options ?? MarkdownSecurityAnalysisOptions.Default;
        this.options.Validate();
    }

    public MarkdownSecurityAnalysisResult Analyze(
        ReadOnlyMemory<byte> content,
        string fileName,
        DocumentSecurityInspectionBudget? budget = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        string extension = Path.GetExtension(fileName);
        if (!string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".markdown", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Markdown security analysis requires an MD or MARKDOWN file name.",
                nameof(fileName));
        }

        budget ??= new DocumentSecurityInspectionBudget();

        var root = new DocumentEmbeddedObject(
            content,
            "/",
            declaredMediaType: "text/markdown",
            declaredFileName: fileName);

        var engine = new DocumentEmbeddedInspectionEngine(
            securityAdapters:
                DocumentSecurityFormatAdapterFactory.Create(
                    markdownOptions: options));

        DocumentEmbeddedInspectionResult result =
            engine.InspectRoot(root, budget, cancellationToken);

        return new MarkdownSecurityAnalysisResult(result.Observations);
    }
}
