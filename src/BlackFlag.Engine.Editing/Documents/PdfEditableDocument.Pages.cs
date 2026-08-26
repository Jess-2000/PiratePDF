using BlackFlag.Engine.Document.Outlines;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Geometry;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Documents;

public sealed partial class PdfEditableDocument
{
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
                destinationIndex: index,
                objectIdentifier: page.ObjectIdentifier);

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

        if (OutlineReferencesPage(document.Outline.Items, page.Id))
        {
            throw new InvalidOperationException(
                "Clear or retarget outline destinations before removing a referenced page.");
        }

        var sourceIndex =
            Pages.EnsureCanRemove(page);

        var operation =
            new PdfPageEditOperation(
                owner,
                PdfPageMutationKind.Remove,
                page.Id,
                sourceIndex: sourceIndex >= 0
                    ? sourceIndex
                    : null,
                objectIdentifier: page.ObjectIdentifier);

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
                destinationIndex: destinationIndex,
                objectIdentifier: page.ObjectIdentifier);

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
                    : null,
                objectIdentifier: target.ObjectIdentifier,
                relatedObjectIdentifier: replacement.ObjectIdentifier);

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

        var sourcePage =
            page.GetSourcePage();

        var previousRotation =
            sourcePage.GetRotationState();

        var operation =
            new PdfPageEditOperation(
                owner,
                PdfPageMutationKind.SetRotation,
                page.Id,
                rotation: rotation);

        session.ValidateOperation(operation);

        sourcePage.SetRotation(rotation);

        session.RecordChange(
            operation,
            () => sourcePage.RestoreRotationState(previousRotation),
            () => sourcePage.SetRotation(rotation));

        return operation;
    }

    /// <summary>Sets one standard page box explicitly.</summary>
    public PdfPageGeometryEditOperation SetPageBox(
        PdfEditablePage page,
        PdfPageBoxKind kind,
        PdfPageBox box)
    {
        return SetPageBoxCore(
            page,
            kind,
            box,
            PdfPageGeometryMutationKind.SetBox);
    }

    /// <summary>Changes the visible crop boundary without moving or scaling content.</summary>
    public PdfPageGeometryEditOperation CropPage(
        PdfEditablePage page,
        PdfPageBox cropBox)
    {
        return SetPageBoxCore(
            page,
            PdfPageBoxKind.CropBox,
            cropBox,
            PdfPageGeometryMutationKind.Crop);
    }

    private PdfPageGeometryEditOperation SetPageBoxCore(
        PdfEditablePage page,
        PdfPageBoxKind kind,
        PdfPageBox box,
        PdfPageGeometryMutationKind mutationKind)
    {
        EnsureOwnership(page);
        var sourcePage = page.GetSourcePage();
        var previous = sourcePage.GetBox(kind);
        var current = new PdfEffectiveValue<PdfPageBox>(box, PdfPageValueOrigin.Explicit);
        var operation = new PdfPageGeometryEditOperation(
            owner,
            mutationKind,
            page.Id,
            kind,
            MapState(previous),
            MapState(current));

        session.ValidateOperation(operation);
        sourcePage.SetBox(kind, current);
        session.RecordChange(
            operation,
            () => sourcePage.SetBox(kind, previous),
            () => sourcePage.SetBox(kind, current));
        return operation;
    }

    /// <summary>Resizes page geometry without scaling page content.</summary>
    public PdfPageGeometryEditOperation ResizePage(
        PdfEditablePage page,
        double width,
        double height,
        PdfPageResizeAnchor anchor = PdfPageResizeAnchor.LowerLeft)
    {
        EnsureOwnership(page);
        if (!double.IsFinite(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }
        if (!double.IsFinite(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }
        if (!Enum.IsDefined(anchor))
        {
            throw new ArgumentOutOfRangeException(nameof(anchor));
        }

        var sourcePage = page.GetSourcePage();
        var previous = sourcePage.GetBox(PdfPageBoxKind.MediaBox);
        var old = previous.Value;
        var left = anchor == PdfPageResizeAnchor.Center
            ? old.Left + ((old.Width - width) / 2)
            : old.Left;
        var bottom = anchor == PdfPageResizeAnchor.Center
            ? old.Bottom + ((old.Height - height) / 2)
            : old.Bottom;
        var resized = new PdfPageBox(left, bottom, left + width, bottom + height);
        var current = new PdfEffectiveValue<PdfPageBox>(resized, PdfPageValueOrigin.Explicit);
        var operation = new PdfPageGeometryEditOperation(
            owner,
            PdfPageGeometryMutationKind.Resize,
            page.Id,
            PdfPageBoxKind.MediaBox,
            MapState(previous),
            MapState(current),
            anchor);

        session.ValidateOperation(operation);
        sourcePage.SetBox(PdfPageBoxKind.MediaBox, current);
        session.RecordChange(
            operation,
            () => sourcePage.SetBox(PdfPageBoxKind.MediaBox, previous),
            () => sourcePage.SetBox(PdfPageBoxKind.MediaBox, current));
        return operation;
    }

    private static PdfStructuralValueState<T> MapState<T>(PdfEffectiveValue<T> state)
        where T : notnull
    {
        return state.Origin switch
        {
            PdfPageValueOrigin.Explicit => PdfStructuralValueState.Explicit(state.Value),
            PdfPageValueOrigin.Inherited => PdfStructuralValueState.Inherited(state.Value),
            PdfPageValueOrigin.Defaulted => PdfStructuralValueState.Defaulted(state.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state.Origin, "The page-value origin is not supported.")
        };
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

    private static bool OutlineReferencesPage(
        IReadOnlyList<PdfOutlineItem> items,
        Guid pageId)
    {
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];

            if (item.Destination?.PageId == pageId ||
                OutlineReferencesPage(item.Children, pageId))
            {
                return true;
            }
        }

        return false;
    }
}
