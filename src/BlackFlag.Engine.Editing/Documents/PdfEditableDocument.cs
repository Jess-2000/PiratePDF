using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Editing.Annotations;
using BlackFlag.Engine.Editing.Decorations;
using BlackFlag.Engine.Editing.Forms;
using BlackFlag.Engine.Editing.Outlines;
using BlackFlag.Engine.Editing.Ownership;
using BlackFlag.Engine.Editing.Pages;

namespace BlackFlag.Engine.Editing.Documents;

/// <summary>
/// Represents the controlled editing-session view of a PDF document.
/// </summary>
/// <remarks>
/// This type is a facade over the authoritative high-level document model. It
/// does not expose that mutable model publicly. Concrete mutation APIs enforce
/// session ownership and preserve untargeted structures.
/// </remarks>
public sealed partial class PdfEditableDocument
{
    private readonly PdfDocument document;
    private readonly PdfEditingOwnershipToken owner;
    private readonly PdfEditingSession session;

    internal PdfEditableDocument(
        PdfDocument document,
        PdfEditingOwnershipToken owner,
        PdfEditingSession session)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(session);

        this.document = document;
        this.owner = owner;
        this.session = session;
        Pages = new PdfEditablePageCollection(
            document.Pages,
            owner);
        PageDecorations = new PdfEditablePageDecorationCollection(
            document.PageDecorations,
            owner);
        FormFields = new PdfEditableFormFieldCollection(
            document.AcroForm,
            owner);
        OutlineItems = new PdfEditableOutlineItemCollection(
            document.Outline,
            owner);
    }

    /// <summary>
    /// Gets the PDF version of the authoritative source document.
    /// </summary>
    public PdfVersion Version =>
        document.Version;

    /// <summary>
    /// Gets the session-owned editable page collection.
    /// </summary>
    public PdfEditablePageCollection Pages { get; }

    /// <summary>
    /// Gets the number of pages currently attached to the editable document.
    /// </summary>
    public int PageCount =>
        Pages.Count;

    /// <summary>Gets the session-owned ordered page-decoration collection.</summary>
    public PdfEditablePageDecorationCollection PageDecorations { get; }

    /// <summary>Gets the session-owned AcroForm root-field collection.</summary>
    public PdfEditableFormFieldCollection FormFields { get; }

    /// <summary>Gets the session-owned document-outline root collection.</summary>
    public PdfEditableOutlineItemCollection OutlineItems { get; }

    /// <summary>Gets the authoritative logical document-information metadata.</summary>
    public PdfDocumentInformation Information =>
        document.Information;

    internal PdfDocument GetSourceDocument() =>
        document;

    internal void EnsureOwnership(PdfEditablePage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (page.Owner != owner)
        {
            throw new PdfEditingOwnershipException(
                "The editable page belongs to another editing session.");
        }
    }

    private void EnsureOwnership(PdfEditableAnnotation annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);

        if (annotation.Owner != owner)
        {
            throw new PdfEditingOwnershipException(
                "The editable annotation belongs to another editing session.");
        }
    }

    private void EnsureOwnership(PdfEditableFormField field)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (field.Owner != owner)
        {
            throw new PdfEditingOwnershipException(
                "The editable form field belongs to another editing session.");
        }
    }

    private void EnsureOwnership(PdfEditableOutlineItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.Owner != owner)
        {
            throw new PdfEditingOwnershipException(
                "The editable outline item belongs to another editing session.");
        }
    }

    private void EnsureOwnership(PdfEditablePageDecoration decoration)
    {
        ArgumentNullException.ThrowIfNull(decoration);

        if (decoration.Owner.SessionId != owner.SessionId)
        {
            throw new PdfEditingOwnershipException(
                "The page decoration belongs to another editing session.");
        }
    }
}
