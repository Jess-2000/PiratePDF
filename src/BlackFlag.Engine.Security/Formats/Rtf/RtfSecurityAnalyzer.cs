using BlackFlag.Engine.Security.Containers;
using BlackFlag.Engine.Security.Formats;
using BlackFlag.Engine.Security.Inspection;

namespace BlackFlag.Engine.Security.Formats.Rtf;

/// <summary>Performs passive bounded security analysis of Rich Text Format documents.</summary>
public sealed class RtfSecurityAnalyzer
{
    private readonly RtfSecurityAnalysisOptions options;

    public RtfSecurityAnalyzer(RtfSecurityAnalysisOptions? options = null)
    {
        this.options = options ?? RtfSecurityAnalysisOptions.Default;
        this.options.Validate();
    }

    public RtfSecurityAnalysisResult Analyze(
        ReadOnlyMemory<byte> content,
        string fileName,
        DocumentSecurityInspectionBudget? budget = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (!string.Equals(Path.GetExtension(fileName), ".rtf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "RTF security analysis requires an RTF file name.",
                nameof(fileName));
        }

        budget ??= new DocumentSecurityInspectionBudget();

        var root = new DocumentEmbeddedObject(
            content,
            "/",
            declaredMediaType: "application/rtf",
            declaredFileName: fileName);

        var engine = new DocumentEmbeddedInspectionEngine(
            securityAdapters:
                DocumentSecurityFormatAdapterFactory.Create(
                    rtfOptions: options));

        DocumentEmbeddedInspectionResult result =
            engine.InspectRoot(root, budget, cancellationToken);

        return new RtfSecurityAnalysisResult(result.Observations);
    }
}
