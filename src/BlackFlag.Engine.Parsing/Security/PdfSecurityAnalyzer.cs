using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Parsing.Security;

/// <summary>
/// Inventories security-relevant PDF structures without executing document
/// actions, JavaScript, embedded content, or interactive behavior.
/// </summary>
public sealed partial class PdfSecurityAnalyzer
{
    private static readonly PdfName TypeKey = new("Type");
    private static readonly PdfName SubtypeKey = new("Subtype");
    private static readonly PdfName ActionTypeKey = new("S");
    private static readonly PdfName JavaScriptKey = new("JS");
    private static readonly PdfName OpenActionKey = new("OpenAction");
    private static readonly PdfName AdditionalActionsKey = new("AA");
    private static readonly PdfName ActionKey = new("A");
    private static readonly PdfName NextKey = new("Next");
    private static readonly PdfName NamesKey = new("Names");
    private static readonly PdfName JavaScriptNamesKey = new("JavaScript");
    private static readonly PdfName EmbeddedFilesKey = new("EmbeddedFiles");
    private static readonly PdfName EmbeddedFileKey = new("EF");
    private static readonly PdfName AcroFormKey = new("AcroForm");
    private static readonly PdfName XfaKey = new("XFA");
    private static readonly PdfName UriKey = new("URI");
    private static readonly PdfName FileKey = new("F");
    private static readonly PdfName UnicodeFileKey = new("UF");
    private static readonly PdfName FileSpecName = new("Filespec");
    private static readonly PdfName AnnotationName = new("Annot");
    private static readonly PdfName FileAttachmentName = new("FileAttachment");
    private static readonly PdfName RichMediaName = new("RichMedia");
    private static readonly PdfName ScreenName = new("Screen");
    private static readonly PdfName MovieName = new("Movie");
    private static readonly PdfName SoundName = new("Sound");
    private static readonly PdfName WidgetName = new("Widget");

    private readonly IPdfIndirectObjectResolver resolver;

    /// <summary>Initializes a PDF security analyzer.</summary>
    public PdfSecurityAnalyzer(IPdfIndirectObjectResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        this.resolver = resolver;
    }

    /// <summary>
    /// Analyzes a resolved PDF catalog and all reachable security-relevant
    /// objects using the supplied shared BlackFlag inspection budget.
    /// </summary>
    public PdfSecurityAnalysisResult Analyze(
        PdfDictionary catalog,
        DocumentSecurityInspectionBudget? budget = null,
        PdfSecurityAnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        budget ??= new DocumentSecurityInspectionBudget();
        options ??= PdfSecurityAnalysisOptions.Default;
        options.Validate();

        var state = new AnalysisState(
            resolver,
            budget,
            options,
            cancellationToken);

        state.Visit(
            catalog,
            logicalPath: "/Catalog",
            sourceObjectId: null,
            depth: 0,
            context: VisitContext.General);

        return new PdfSecurityAnalysisResult(state.Observations);
    }

    private enum VisitContext
    {
        General,
        OpenAction,
        AdditionalAction,
        Action,
        JavaScriptNameTree,
        EmbeddedFilesNameTree,
        AcroForm
    }

}
