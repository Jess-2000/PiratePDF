using BlackFlag.Engine.Security.Containers;
using BlackFlag.Engine.Security.Formats;
using BlackFlag.Engine.Security.Inspection;

namespace BlackFlag.Engine.Security.Formats.Ole;

/// <summary>Performs passive security analysis of legacy Word Binary DOC files.</summary>
public sealed class LegacyWordBinarySecurityAnalyzer
{
    private readonly OleCompoundFileSecurityAnalysisOptions options;

    public LegacyWordBinarySecurityAnalyzer(
        OleCompoundFileSecurityAnalysisOptions? options = null)
    {
        this.options = options ?? OleCompoundFileSecurityAnalysisOptions.Default;
        this.options.Validate();
    }

    public LegacyWordBinarySecurityAnalysisResult Analyze(
        ReadOnlyMemory<byte> content,
        string fileName,
        DocumentSecurityInspectionBudget? budget = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (!string.Equals(
                Path.GetExtension(fileName),
                ".doc",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Legacy Word Binary security analysis requires a DOC file name.",
                nameof(fileName));
        }

        budget ??= new DocumentSecurityInspectionBudget();

        var root = new DocumentEmbeddedObject(
            content,
            "/",
            declaredMediaType: "application/msword",
            declaredFileName: fileName);

        var engine = new DocumentEmbeddedInspectionEngine(
            securityAdapters:
                DocumentSecurityFormatAdapterFactory.Create(
                    oleOptions: options));

        DocumentEmbeddedInspectionResult result =
            engine.InspectRoot(root, budget, cancellationToken);

        return new LegacyWordBinarySecurityAnalysisResult(result.Observations);
    }
}
