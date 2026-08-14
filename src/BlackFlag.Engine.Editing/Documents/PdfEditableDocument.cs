using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
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
        Pages.EnsureCanInsert(
            index,
            page);

        var operation =
            new PdfPageEditOperation(
                owner,
                PdfPageMutationKind.Insert,
                page.Id,
                destinationIndex: index);

        session.ValidateOperation(operation);

        Pages.Insert(
            index,
            page);

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
            Pages.EnsureCanRemove(page);

        var operation =
            new PdfPageEditOperation(
                owner,
                PdfPageMutationKind.Remove,
                page.Id,
                sourceIndex: sourceIndex >= 0
                    ? sourceIndex
                    : null);

        session.ValidateOperation(operation);

        sourceIndex =
            Pages.Remove(page);

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
            Pages.EnsureCanMove(
                page,
                destinationIndex);

        var operation =
            new PdfPageEditOperation(
                owner,
                PdfPageMutationKind.Move,
                page.Id,
                sourceIndex: sourceIndex >= 0
                    ? sourceIndex
                    : null,
                destinationIndex: destinationIndex);

        session.ValidateOperation(operation);

        sourceIndex =
            Pages.Move(
                page,
                destinationIndex);

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
            Pages.EnsureCanReplace(
                target,
                replacement);

        var operation =
            new PdfPageEditOperation(
                owner,
                PdfPageMutationKind.Replace,
                target.Id,
                relatedPageId: replacement.Id,
                sourceIndex: sourceIndex >= 0
                    ? sourceIndex
                    : null,
                destinationIndex: sourceIndex >= 0
                    ? sourceIndex
                    : null);

        session.ValidateOperation(operation);

        sourceIndex =
            Pages.Replace(
                target,
                replacement);

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

        if (!Enum.IsDefined(rotation))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rotation),
                rotation,
                "The PDF page rotation is not supported.");
        }

        var previousRotation =
            page.Rotation;

        var operation =
            new PdfPageEditOperation(
                owner,
                PdfPageMutationKind.SetRotation,
                page.Id,
                rotation: rotation);

        session.ValidateOperation(operation);

        page.GetSourcePage()
            .SetRotation(rotation);

        session.RecordChange(
            operation,
            () => page.GetSourcePage().SetRotation(previousRotation),
            () => page.GetSourcePage().SetRotation(rotation));

        return operation;
    }


    /// <summary>
    /// Replaces the complete content-stream byte sequence owned by a page.
    /// </summary>
    public PdfContentEditOperation ReplacePageContent(
        PdfEditablePage page,
        ReadOnlySpan<byte> replacement)
    {
        EnsureOwnership(page);

        var sourcePage =
            page.GetSourcePage();

        var previous =
            sourcePage.GetContentBytes();

        var replacementCopy =
            replacement.ToArray();

        var operation =
            new PdfContentEditOperation(
                owner,
                PdfContentMutationKind.Replace,
                page.Id,
                previous.Length,
                replacementCopy.Length);

        session.ValidateOperation(operation);

        sourcePage.ReplaceContent(replacementCopy);

        session.RecordChange(
            operation,
            () => sourcePage.ReplaceContent(previous),
            () => sourcePage.ReplaceContent(replacementCopy));

        return operation;
    }

    /// <summary>
    /// Adds or replaces one named resource in a direct page resource category.
    /// </summary>
    public PdfResourceEditOperation SetPageResource(
        PdfEditablePage page,
        PdfName category,
        PdfName name,
        PdfObject resource)
    {
        EnsureOwnership(page);
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(resource);

        var sourcePage =
            page.GetSourcePage();

        var replacedExisting =
            sourcePage.TryGetResource(
                category,
                name,
                out var previous);

        var operation =
            new PdfResourceEditOperation(
                owner,
                PdfResourceMutationKind.Set,
                page.Id,
                category,
                name,
                replacedExisting);

        session.ValidateOperation(operation);

        sourcePage.SetResource(
            category,
            name,
            resource);

        session.RecordChange(
            operation,
            () =>
            {
                if (replacedExisting)
                {
                    sourcePage.SetResource(
                        category,
                        name,
                        previous!);
                    return;
                }

                _ = sourcePage.RemoveResource(
                    category,
                    name);
            },
            () => sourcePage.SetResource(
                category,
                name,
                resource));

        return operation;
    }

    /// <summary>
    /// Removes one named resource from a direct page resource category.
    /// </summary>
    public PdfResourceEditOperation RemovePageResource(
        PdfEditablePage page,
        PdfName category,
        PdfName name)
    {
        EnsureOwnership(page);
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(name);

        var sourcePage =
            page.GetSourcePage();

        if (!sourcePage.TryGetResource(
                category,
                name,
                out var previous))
        {
            throw new KeyNotFoundException(
                $"Page resource '{category}/{name.GetLatin1Text()}' does not exist.");
        }

        var operation =
            new PdfResourceEditOperation(
                owner,
                PdfResourceMutationKind.Remove,
                page.Id,
                category,
                name,
                replacedExisting: true);

        session.ValidateOperation(operation);

        _ = sourcePage.RemoveResource(
            category,
            name);

        session.RecordChange(
            operation,
            () => sourcePage.SetResource(
                category,
                name,
                previous!),
            () => sourcePage.RemoveResource(
                category,
                name));

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
