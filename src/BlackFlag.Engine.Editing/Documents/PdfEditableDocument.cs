using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Operations;
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
public sealed class PdfEditableDocument
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

    /// <summary>
    /// Creates a session-owned page handle without attaching the new page to
    /// the document page tree.
    /// </summary>
    public PdfEditablePage CreatePage(
        double width,
        double height,
        PdfPageRotation rotation = PdfPageRotation.None)
    {
        return new PdfEditablePage(
            new PdfPage(
                width,
                height,
                rotation),
            owner,
            PdfEditablePageOrigin.SessionCreated,
            sourceIndex: null);
    }

    /// <summary>
    /// Inserts a detached page at the requested document index.
    /// </summary>
    public PdfPageEditOperation InsertPage(
        int index,
        PdfEditablePage page)
    {
        EnsureOwnership(page);
        Pages.Insert(
            index,
            page);

        var operation =
            new PdfPageEditOperation(
                owner,
                PdfPageMutationKind.Insert,
                page.Id,
                destinationIndex: index);

        session.RecordChange(
            operation,
            () => Pages.Remove(page),
            () => Pages.Insert(index, page));

        return operation;
    }

    /// <summary>
    /// Removes an attached page from the document while keeping its editable
    /// handle available to the owning session.
    /// </summary>
    public PdfPageEditOperation RemovePage(
        PdfEditablePage page)
    {
        EnsureOwnership(page);

        var sourceIndex =
            Pages.Remove(page);

        var operation =
            new PdfPageEditOperation(
                owner,
                PdfPageMutationKind.Remove,
                page.Id,
                sourceIndex: sourceIndex);

        session.RecordChange(
            operation,
            () => Pages.Insert(sourceIndex, page),
            () => Pages.Remove(page));

        return operation;
    }

    /// <summary>
    /// Moves an attached page to another index in the document page order.
    /// </summary>
    public PdfPageEditOperation MovePage(
        PdfEditablePage page,
        int destinationIndex)
    {
        EnsureOwnership(page);

        var sourceIndex =
            Pages.Move(
                page,
                destinationIndex);

        var operation =
            new PdfPageEditOperation(
                owner,
                PdfPageMutationKind.Move,
                page.Id,
                sourceIndex: sourceIndex,
                destinationIndex: destinationIndex);

        session.RecordChange(
            operation,
            () => Pages.Move(page, sourceIndex),
            () => Pages.Move(page, destinationIndex));

        return operation;
    }

    /// <summary>
    /// Replaces an attached page with a detached page owned by the same
    /// editing session.
    /// </summary>
    public PdfPageEditOperation ReplacePage(
        PdfEditablePage target,
        PdfEditablePage replacement)
    {
        EnsureOwnership(target);
        EnsureOwnership(replacement);

        var sourceIndex =
            Pages.Replace(
                target,
                replacement);

        var operation =
            new PdfPageEditOperation(
                owner,
                PdfPageMutationKind.Replace,
                target.Id,
                relatedPageId: replacement.Id,
                sourceIndex: sourceIndex,
                destinationIndex: sourceIndex);

        session.RecordChange(
            operation,
            () => Pages.Replace(replacement, target),
            () => Pages.Replace(target, replacement));

        return operation;
    }

    /// <summary>
    /// Updates the rotation of a page owned by this editing session.
    /// </summary>
    public PdfPageEditOperation SetPageRotation(
        PdfEditablePage page,
        PdfPageRotation rotation)
    {
        EnsureOwnership(page);

        var previousRotation =
            page.Rotation;

        page.GetSourcePage()
            .SetRotation(rotation);

        var operation =
            new PdfPageEditOperation(
                owner,
                PdfPageMutationKind.SetRotation,
                page.Id,
                rotation: rotation);

        session.RecordChange(
            operation,
            () => page.GetSourcePage().SetRotation(previousRotation),
            () => page.GetSourcePage().SetRotation(rotation));

        return operation;
    }

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
}
