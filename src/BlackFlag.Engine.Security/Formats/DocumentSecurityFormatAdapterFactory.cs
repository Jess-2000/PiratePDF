using BlackFlag.Engine.Security.Containers;
using BlackFlag.Engine.Security.Formats.Markdown;
using BlackFlag.Engine.Security.Formats.Ole;
using BlackFlag.Engine.Security.Formats.OpenDocument;
using BlackFlag.Engine.Security.Formats.Ooxml;
using BlackFlag.Engine.Security.Formats.Rtf;

namespace BlackFlag.Engine.Security.Formats;

/// <summary>
/// Creates the built-in passive security adapters used by recursive document inspection.
/// </summary>
public static class DocumentSecurityFormatAdapterFactory
{
    /// <summary>Creates all built-in format security adapters.</summary>
    public static IReadOnlyList<IDocumentContainerSecurityAdapter> Create(
        OoxmlWordSecurityAnalysisOptions? ooxmlOptions = null,
        OpenDocumentTextSecurityAnalysisOptions? openDocumentOptions = null,
        OleCompoundFileSecurityAnalysisOptions? oleOptions = null,
        RtfSecurityAnalysisOptions? rtfOptions = null,
        MarkdownSecurityAnalysisOptions? markdownOptions = null) =>
        [
            new OoxmlWordContainerSecurityAdapter(ooxmlOptions),
            new OpenDocumentTextContainerSecurityAdapter(openDocumentOptions),
            new OleCompoundFileSecurityAdapter(oleOptions),
            new RtfContainerSecurityAdapter(rtfOptions),
            new MarkdownContainerSecurityAdapter(markdownOptions)
        ];
}
