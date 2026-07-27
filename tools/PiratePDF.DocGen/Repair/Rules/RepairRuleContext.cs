using PiratePDF.DocGen.Library;

namespace PiratePDF.DocGen.Repair.Rules;

internal sealed class RepairRuleContext
{
    public RepairRuleContext(
        DocumentationLibrary library,
        DocumentationDocument document)
    {
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(document);

        Library = library;
        Document = document;
    }

    // =========================================================================
    // Repair Inputs
    // =========================================================================

    public DocumentationLibrary Library { get; }

    public DocumentationDocument Document { get; }

    public string FileContents =>
        Document.RawMarkdown;
}