using BlackFlag.Engine.Security.Containers;
using BlackFlag.Engine.Security.Inspection;

namespace BlackFlag.Engine.Security.Formats.Ooxml;

/// <summary>
/// Performs passive security analysis of Word OOXML DOCX/DOCM packages
/// without executing macros, controls, fields, links, or embedded payloads.
/// </summary>
public sealed class OoxmlWordSecurityAnalyzer
{
    private readonly OoxmlWordSecurityAnalysisOptions options;

    public OoxmlWordSecurityAnalyzer(
        OoxmlWordSecurityAnalysisOptions? options = null)
    {
        this.options = options ?? OoxmlWordSecurityAnalysisOptions.Default;
        this.options.Validate();
    }

    public OoxmlWordSecurityAnalysisResult Analyze(
        ReadOnlyMemory<byte> content,
        string fileName,
        DocumentSecurityInspectionBudget? budget = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ValidateFileName(fileName);

        budget ??= new DocumentSecurityInspectionBudget();

        var root = new DocumentEmbeddedObject(
            content,
            logicalPath: "/",
            declaredMediaType: GetDeclaredMediaType(fileName),
            declaredFileName: fileName);

        var engine =
            new DocumentEmbeddedInspectionEngine(
                securityAdapters:
                    DocumentSecurityFormatAdapterFactory.Create(
                        ooxmlOptions: options));

        DocumentEmbeddedInspectionResult result =
            engine.InspectRoot(
                root,
                budget,
                cancellationToken);

        return new OoxmlWordSecurityAnalysisResult(result.Observations);
    }

    private static void ValidateFileName(string fileName)
    {
        string extension =
            Path.GetExtension(fileName);

        if (!string.Equals(
                extension,
                ".docx",
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                extension,
                ".docm",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "OOXML Word security analysis requires a DOCX or DOCM file name.",
                nameof(fileName));
        }
    }

    private static string GetDeclaredMediaType(string fileName) =>
        fileName.EndsWith(
            ".docm",
            StringComparison.OrdinalIgnoreCase)
            ? "application/vnd.ms-word.document.macroEnabled.12"
            : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
}
