using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Decorations;
using BlackFlag.Engine.Document.Forms;
using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Outlines;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Annotations;
using BlackFlag.Engine.Editing.Decorations;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Forms;
using BlackFlag.Engine.Editing.Geometry;
using BlackFlag.Engine.Editing.Metadata;
using BlackFlag.Engine.Editing.Outlines;
using BlackFlag.Engine.Editing.Structural;
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

    /// <summary>Gets the exact state of one document-information entry.</summary>
    public PdfStructuralValueState<string> GetDocumentInformationState(
        PdfDocumentInformationField field)
    {
        var value = document.Information.GetValue(field);
        return value is null
            ? PdfStructuralValueState.Absent<string>()
            : PdfStructuralValueState.Explicit(value);
    }

    /// <summary>Sets one document-information entry explicitly.</summary>
    public PdfDocumentInformationEditOperation SetDocumentInformation(
        PdfDocumentInformationField field,
        string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var previousState =
            GetDocumentInformationState(field);
        var currentState =
            PdfStructuralValueState.Explicit(value);
        var operation =
            new PdfDocumentInformationEditOperation(
                owner,
                PdfDocumentInformationMutationKind.Set,
                field,
                previousState,
                currentState);

        session.ValidateOperation(operation);
        document.Information.SetValue(field, value);
        session.RecordChange(
            operation,
            () => RestoreDocumentInformation(field, previousState),
            () => document.Information.SetValue(field, value));

        return operation;
    }

    /// <summary>Clears one explicitly present document-information entry.</summary>
    public PdfDocumentInformationEditOperation ClearDocumentInformation(
        PdfDocumentInformationField field)
    {
        var previousState =
            GetDocumentInformationState(field);

        if (!previousState.HasValue)
        {
            throw new KeyNotFoundException(
                $"Document information field '{field}' is not present.");
        }

        var currentState =
            PdfStructuralValueState.Absent<string>();
        var operation =
            new PdfDocumentInformationEditOperation(
                owner,
                PdfDocumentInformationMutationKind.Clear,
                field,
                previousState,
                currentState);

        session.ValidateOperation(operation);
        _ = document.Information.Remove(field);
        session.RecordChange(
            operation,
            () => RestoreDocumentInformation(field, previousState),
            () => document.Information.Remove(field));

        return operation;
    }

    private void RestoreDocumentInformation(
        PdfDocumentInformationField field,
        PdfStructuralValueState<string> state)
    {
        if (state.HasValue)
        {
            document.Information.SetValue(field, state.Value);
            return;
        }

        _ = document.Information.Remove(field);
    }

    /// <summary>Creates a detached reusable page-decoration handle.</summary>
    public PdfEditablePageDecoration CreatePageDecoration(
        PdfPageDecorationKind kind,
        PdfPageDecorationRegion region,
        PdfPageDecorationAlignment alignment = PdfPageDecorationAlignment.Center)
    {
        return new PdfEditablePageDecoration(
            new PdfPageDecoration(
                kind,
                region,
                alignment,
                PdfPageDecorationTarget.AllPages),
            owner);
    }

    /// <summary>Appends a detached page decoration to the document definition order.</summary>
    public PdfPageDecorationEditOperation AddPageDecoration(
        PdfEditablePageDecoration decoration)
    {
        EnsureOwnership(decoration);
        var index = PageDecorations.Count;
        var snapshot = CreatePageDecorationSnapshot(decoration);
        var operation = new PdfPageDecorationEditOperation(
            owner,
            PdfPageDecorationMutationKind.Add,
            decoration.Id,
            PdfStructuralValueState.Absent<PdfPageDecorationSnapshot>(),
            PdfStructuralValueState.Explicit(snapshot),
            index);

        session.ValidateOperation(operation);
        PageDecorations.Insert(index, decoration);
        session.RecordChange(
            operation,
            () => PageDecorations.Remove(decoration),
            () => PageDecorations.Insert(index, decoration));

        return operation;
    }

    /// <summary>Removes an attached page decoration while retaining its stable session handle.</summary>
    public PdfPageDecorationEditOperation RemovePageDecoration(
        PdfEditablePageDecoration decoration)
    {
        EnsureOwnership(decoration);
        var index = PageDecorations.IndexOf(decoration);

        if (index < 0)
        {
            throw new InvalidOperationException(
                "The page decoration is not attached to this document.");
        }

        var snapshot = CreatePageDecorationSnapshot(decoration);
        var operation = new PdfPageDecorationEditOperation(
            owner,
            PdfPageDecorationMutationKind.Remove,
            decoration.Id,
            PdfStructuralValueState.Explicit(snapshot),
            PdfStructuralValueState.Absent<PdfPageDecorationSnapshot>(),
            index);

        session.ValidateOperation(operation);
        _ = PageDecorations.Remove(decoration);
        session.RecordChange(
            operation,
            () => PageDecorations.Insert(index, decoration),
            () => PageDecorations.Remove(decoration));

        return operation;
    }

    /// <summary>Sets literal text on a text decoration, including an empty string.</summary>
    public PdfPageDecorationEditOperation SetPageDecorationText(
        PdfEditablePageDecoration decoration,
        string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        EnsureTextDecoration(decoration);
        return UpdatePageDecoration(
            decoration,
            PdfPageDecorationMutationKind.SetText,
            source => source.Text = text);
    }

    /// <summary>Clears explicitly present literal text from a text decoration.</summary>
    public PdfPageDecorationEditOperation ClearPageDecorationText(
        PdfEditablePageDecoration decoration)
    {
        EnsureTextDecoration(decoration);

        if (decoration.Text is null)
        {
            throw new KeyNotFoundException(
                "The page decoration does not have explicit text.");
        }

        return UpdatePageDecoration(
            decoration,
            PdfPageDecorationMutationKind.ClearText,
            source => source.Text = null);
    }

    /// <summary>Changes the logical header/footer region and horizontal placement.</summary>
    public PdfPageDecorationEditOperation SetPageDecorationPlacement(
        PdfEditablePageDecoration decoration,
        PdfPageDecorationRegion region,
        PdfPageDecorationAlignment alignment)
    {
        EnsureOwnership(decoration);

        if (!Enum.IsDefined(region))
        {
            throw new ArgumentOutOfRangeException(nameof(region));
        }

        if (!Enum.IsDefined(alignment))
        {
            throw new ArgumentOutOfRangeException(nameof(alignment));
        }

        return UpdatePageDecoration(
            decoration,
            PdfPageDecorationMutationKind.SetPlacement,
            source =>
            {
                source.Region = region;
                source.Alignment = alignment;
            });
    }

    /// <summary>Changes the page-selection rule for a reusable decoration.</summary>
    public PdfPageDecorationEditOperation SetPageDecorationTarget(
        PdfEditablePageDecoration decoration,
        PdfPageDecorationTarget target)
    {
        return UpdatePageDecoration(
            decoration,
            PdfPageDecorationMutationKind.SetTarget,
            source => source.Target = target);
    }

    /// <summary>Changes numbering semantics for a page-number decoration.</summary>
    public PdfPageDecorationEditOperation SetPageNumbering(
        PdfEditablePageDecoration decoration,
        PdfPageNumberingOptions numbering)
    {
        ArgumentNullException.ThrowIfNull(numbering);
        EnsureOwnership(decoration);

        if (decoration.Kind != PdfPageDecorationKind.PageNumber)
        {
            throw new InvalidOperationException(
                "Numbering semantics can only be assigned to a page-number decoration.");
        }

        return UpdatePageDecoration(
            decoration,
            PdfPageDecorationMutationKind.SetNumbering,
            source => source.Numbering = numbering);
    }

    private PdfPageDecorationEditOperation UpdatePageDecoration(
        PdfEditablePageDecoration decoration,
        PdfPageDecorationMutationKind kind,
        Action<PdfPageDecoration> mutate)
    {
        EnsureOwnership(decoration);
        ArgumentNullException.ThrowIfNull(mutate);
        var source = decoration.GetSourceDecoration();
        var previous = CreatePageDecorationSnapshot(source);
        var preview = new PdfPageDecoration(
            source.Kind,
            source.Region,
            source.Alignment,
            source.Target)
        {
            Text = source.Text,
            Numbering = source.Numbering
        };
        mutate(preview);
        var current = CreatePageDecorationSnapshot(preview);
        var operation = new PdfPageDecorationEditOperation(
            owner,
            kind,
            decoration.Id,
            PdfStructuralValueState.Explicit(previous),
            PdfStructuralValueState.Explicit(current));

        session.ValidateOperation(operation);
        RestorePageDecorationSnapshot(source, current);
        session.RecordChange(
            operation,
            () => RestorePageDecorationSnapshot(source, previous),
            () => RestorePageDecorationSnapshot(source, current));

        return operation;
    }

    private static PdfPageDecorationSnapshot CreatePageDecorationSnapshot(
        PdfEditablePageDecoration decoration)
    {
        return CreatePageDecorationSnapshot(decoration.GetSourceDecoration());
    }

    private static PdfPageDecorationSnapshot CreatePageDecorationSnapshot(
        PdfPageDecoration decoration)
    {
        return new PdfPageDecorationSnapshot(
            decoration.Kind,
            decoration.Region,
            decoration.Alignment,
            decoration.Target,
            decoration.Text,
            decoration.Numbering);
    }

    private static void RestorePageDecorationSnapshot(
        PdfPageDecoration decoration,
        PdfPageDecorationSnapshot snapshot)
    {
        decoration.Region = snapshot.Region;
        decoration.Alignment = snapshot.Alignment;
        decoration.Target = snapshot.Target;
        decoration.Text = snapshot.Text;
        decoration.Numbering = snapshot.Numbering;
    }

    private void EnsureTextDecoration(PdfEditablePageDecoration decoration)
    {
        EnsureOwnership(decoration);

        if (decoration.Kind != PdfPageDecorationKind.Text)
        {
            throw new InvalidOperationException(
                "Literal text can only be assigned to a text page decoration.");
        }
    }

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

    /// <summary>Creates a detached annotation handle owned by this editing session.</summary>
    public PdfEditableAnnotation CreateAnnotation(
        PdfAnnotationSubtype subtype,
        PdfAnnotationRectangle rectangle)
    {
        return new PdfEditableAnnotation(
            new PdfAnnotation(subtype, rectangle),
            owner);
    }

    /// <summary>Appends a detached annotation to a page.</summary>
    public PdfAnnotationEditOperation AddAnnotation(
        PdfEditablePage page,
        PdfEditableAnnotation annotation)
    {
        EnsureOwnership(page);
        EnsureOwnership(annotation);

        var index = page.Annotations.Count;
        var snapshot = CreateAnnotationSnapshot(annotation);
        var operation = new PdfAnnotationEditOperation(
            owner,
            PdfAnnotationMutationKind.Add,
            page.Id,
            annotation.Id,
            annotation.Subtype,
            PdfStructuralValueState.Absent<PdfAnnotationSnapshot>(),
            PdfStructuralValueState.Explicit(snapshot),
            index);

        session.ValidateOperation(operation);
        page.Annotations.Insert(index, annotation);
        session.RecordChange(
            operation,
            () => page.Annotations.Remove(annotation),
            () => page.Annotations.Insert(index, annotation));

        return operation;
    }

    /// <summary>Removes an annotation from its owning page while retaining its stable handle.</summary>
    public PdfAnnotationEditOperation RemoveAnnotation(
        PdfEditablePage page,
        PdfEditableAnnotation annotation)
    {
        EnsureAnnotationAttachedToPage(page, annotation);

        if (annotation.GetSourceAnnotation().FormField is not null)
        {
            throw new InvalidOperationException(
                "Detach the Widget annotation from its AcroForm field before removing it from the page.");
        }
        var index = page.Annotations.IndexOf(annotation);
        var snapshot = CreateAnnotationSnapshot(annotation);
        var operation = new PdfAnnotationEditOperation(
            owner,
            PdfAnnotationMutationKind.Remove,
            page.Id,
            annotation.Id,
            annotation.Subtype,
            PdfStructuralValueState.Explicit(snapshot),
            PdfStructuralValueState.Absent<PdfAnnotationSnapshot>(),
            index);

        session.ValidateOperation(operation);
        _ = page.Annotations.Remove(annotation);
        session.RecordChange(
            operation,
            () => page.Annotations.Insert(index, annotation),
            () => page.Annotations.Remove(annotation));

        return operation;
    }

    /// <summary>Changes an annotation rectangle without regenerating appearance streams.</summary>
    public PdfAnnotationEditOperation SetAnnotationRectangle(
        PdfEditablePage page,
        PdfEditableAnnotation annotation,
        PdfAnnotationRectangle rectangle)
    {
        return UpdateAnnotation(
            page,
            annotation,
            PdfAnnotationMutationKind.SetRectangle,
            source => source.Rectangle = rectangle);
    }

    /// <summary>Sets annotation contents explicitly, including an empty string.</summary>
    public PdfAnnotationEditOperation SetAnnotationContents(
        PdfEditablePage page,
        PdfEditableAnnotation annotation,
        string contents)
    {
        ArgumentNullException.ThrowIfNull(contents);
        return UpdateAnnotation(
            page,
            annotation,
            PdfAnnotationMutationKind.SetContents,
            source => source.Contents = contents);
    }

    /// <summary>Clears explicitly present annotation contents.</summary>
    public PdfAnnotationEditOperation ClearAnnotationContents(
        PdfEditablePage page,
        PdfEditableAnnotation annotation)
    {
        EnsureAnnotationAttachedToPage(page, annotation);

        if (annotation.Contents is null)
        {
            throw new KeyNotFoundException(
                "The annotation does not have an explicit contents value.");
        }

        return UpdateAnnotation(
            page,
            annotation,
            PdfAnnotationMutationKind.ClearContents,
            source => source.Contents = null);
    }

    /// <summary>Sets the common PDF annotation flags.</summary>
    public PdfAnnotationEditOperation SetAnnotationFlags(
        PdfEditablePage page,
        PdfEditableAnnotation annotation,
        PdfAnnotationOptions flags)
    {
        return UpdateAnnotation(
            page,
            annotation,
            PdfAnnotationMutationKind.SetFlags,
            source => source.Flags = flags);
    }

    private PdfAnnotationEditOperation UpdateAnnotation(
        PdfEditablePage page,
        PdfEditableAnnotation annotation,
        PdfAnnotationMutationKind kind,
        Action<PdfAnnotation> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        EnsureAnnotationAttachedToPage(page, annotation);
        var source = annotation.GetSourceAnnotation();
        var previous = CreateAnnotationSnapshot(annotation);

        var preview = new PdfAnnotation(
            source.Subtype,
            source.Rectangle)
        {
            Contents = source.Contents,
            Flags = source.Flags
        };
        mutate(preview);
        var current = CreateAnnotationSnapshot(preview);
        var operation = new PdfAnnotationEditOperation(
            owner,
            kind,
            page.Id,
            annotation.Id,
            annotation.Subtype,
            PdfStructuralValueState.Explicit(previous),
            PdfStructuralValueState.Explicit(current));

        session.ValidateOperation(operation);
        RestoreAnnotationSnapshot(source, current);
        session.RecordChange(
            operation,
            () => RestoreAnnotationSnapshot(source, previous),
            () => RestoreAnnotationSnapshot(source, current));

        return operation;
    }

    private static PdfAnnotationSnapshot CreateAnnotationSnapshot(
        PdfEditableAnnotation annotation)
    {
        return CreateAnnotationSnapshot(annotation.GetSourceAnnotation());
    }

    private static PdfAnnotationSnapshot CreateAnnotationSnapshot(
        PdfAnnotation annotation)
    {
        return new PdfAnnotationSnapshot(
            annotation.Subtype,
            annotation.Rectangle,
            annotation.Contents,
            annotation.Flags);
    }

    private static void RestoreAnnotationSnapshot(
        PdfAnnotation annotation,
        PdfAnnotationSnapshot snapshot)
    {
        annotation.Rectangle = snapshot.Rectangle;
        annotation.Contents = snapshot.Contents;
        annotation.Flags = snapshot.Flags;
    }

    private void EnsureAnnotationAttachedToPage(
        PdfEditablePage page,
        PdfEditableAnnotation annotation)
    {
        EnsureOwnership(page);
        EnsureOwnership(annotation);

        if (!page.Annotations.Contains(annotation))
        {
            throw new InvalidOperationException(
                "The annotation is not attached to the supplied page.");
        }
    }



    /// <summary>Creates a detached AcroForm field handle owned by this editing session.</summary>
    public PdfEditableFormField CreateFormField(PdfFormFieldType fieldType)
    {
        return new PdfEditableFormField(
            new PdfFormField(fieldType),
            owner);
    }

    /// <summary>Appends a detached field to the AcroForm root collection.</summary>
    public PdfFormFieldEditOperation AddFormField(PdfEditableFormField field)
    {
        EnsureOwnership(field);

        if (field.IsChild)
        {
            throw new InvalidOperationException(
                "A child form field cannot also be attached as an AcroForm root field.");
        }

        var index = FormFields.Count;
        var snapshot = CreateFormFieldSnapshot(field);
        var operation = new PdfFormFieldEditOperation(
            owner,
            PdfFormFieldMutationKind.AddRoot,
            field.Id,
            field.FieldType,
            PdfStructuralValueState.Absent<PdfFormFieldSnapshot>(),
            PdfStructuralValueState.Explicit(snapshot),
            index: index);

        session.ValidateOperation(operation);
        FormFields.Insert(index, field);
        session.RecordChange(
            operation,
            () => FormFields.Remove(field),
            () => FormFields.Insert(index, field));

        return operation;
    }

    /// <summary>Removes a root field while retaining its stable session handle.</summary>
    public PdfFormFieldEditOperation RemoveFormField(PdfEditableFormField field)
    {
        EnsureOwnership(field);
        var index = FormFields.IndexOf(field);

        if (index < 0)
        {
            throw new InvalidOperationException(
                "The form field is not attached to the AcroForm root.");
        }

        var snapshot = CreateFormFieldSnapshot(field);
        var operation = new PdfFormFieldEditOperation(
            owner,
            PdfFormFieldMutationKind.RemoveRoot,
            field.Id,
            field.FieldType,
            PdfStructuralValueState.Explicit(snapshot),
            PdfStructuralValueState.Absent<PdfFormFieldSnapshot>(),
            index: index);

        session.ValidateOperation(operation);
        _ = FormFields.Remove(field);
        session.RecordChange(
            operation,
            () => FormFields.Insert(index, field),
            () => FormFields.Remove(field));

        return operation;
    }

    /// <summary>Appends a detached field as a child of another session-owned field.</summary>
    public PdfFormFieldEditOperation AddChildFormField(
        PdfEditableFormField parent,
        PdfEditableFormField child)
    {
        EnsureOwnership(parent);
        EnsureOwnership(child);

        if (FormFields.Contains(child))
        {
            throw new InvalidOperationException(
                "A root form field must be removed from the AcroForm root before it can become a child.");
        }

        var index = parent.Children.Count;
        var previous = CreateFormFieldSnapshot(child);
        var operation = new PdfFormFieldEditOperation(
            owner,
            PdfFormFieldMutationKind.AddChild,
            child.Id,
            child.FieldType,
            PdfStructuralValueState.Explicit(previous),
            PdfStructuralValueState.Explicit(
                previous with { ParentFieldId = parent.Id }),
            parent.Id,
            index: index);

        session.ValidateOperation(operation);
        parent.Children.Insert(index, child);
        session.RecordChange(
            operation,
            () => parent.Children.Remove(child),
            () => parent.Children.Insert(index, child));

        return operation;
    }

    /// <summary>Detaches a child field from its parent while retaining its stable handle.</summary>
    public PdfFormFieldEditOperation RemoveChildFormField(
        PdfEditableFormField parent,
        PdfEditableFormField child)
    {
        EnsureOwnership(parent);
        EnsureOwnership(child);
        var index = parent.Children.IndexOf(child);

        if (index < 0)
        {
            throw new InvalidOperationException(
                "The form field is not a child of the supplied parent field.");
        }

        var previous = CreateFormFieldSnapshot(child);
        var operation = new PdfFormFieldEditOperation(
            owner,
            PdfFormFieldMutationKind.RemoveChild,
            child.Id,
            child.FieldType,
            PdfStructuralValueState.Explicit(previous),
            PdfStructuralValueState.Explicit(
                previous with { ParentFieldId = null }),
            parent.Id,
            index: index);

        session.ValidateOperation(operation);
        _ = parent.Children.Remove(child);
        session.RecordChange(
            operation,
            () => parent.Children.Insert(index, child),
            () => parent.Children.Remove(child));

        return operation;
    }

    /// <summary>Sets a field partial name explicitly, including an empty string.</summary>
    public PdfFormFieldEditOperation SetFormFieldPartialName(
        PdfEditableFormField field,
        string partialName)
    {
        ArgumentNullException.ThrowIfNull(partialName);
        return UpdateFormField(
            field,
            PdfFormFieldMutationKind.SetPartialName,
            source => source.PartialName = partialName);
    }

    /// <summary>Clears an explicitly present field partial name.</summary>
    public PdfFormFieldEditOperation ClearFormFieldPartialName(
        PdfEditableFormField field)
    {
        if (field.PartialName is null)
        {
            throw new KeyNotFoundException("The form field does not have an explicit partial name.");
        }

        return UpdateFormField(
            field,
            PdfFormFieldMutationKind.ClearPartialName,
            source => source.PartialName = null);
    }

    /// <summary>Sets a field value explicitly, including an empty string.</summary>
    public PdfFormFieldEditOperation SetFormFieldValue(
        PdfEditableFormField field,
        string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return UpdateFormField(
            field,
            PdfFormFieldMutationKind.SetValue,
            source => source.Value = value);
    }

    /// <summary>Clears an explicitly present field value.</summary>
    public PdfFormFieldEditOperation ClearFormFieldValue(
        PdfEditableFormField field)
    {
        if (field.Value is null)
        {
            throw new KeyNotFoundException("The form field does not have an explicit value.");
        }

        return UpdateFormField(
            field,
            PdfFormFieldMutationKind.ClearValue,
            source => source.Value = null);
    }

    /// <summary>Sets a field default value explicitly, including an empty string.</summary>
    public PdfFormFieldEditOperation SetFormFieldDefaultValue(
        PdfEditableFormField field,
        string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return UpdateFormField(
            field,
            PdfFormFieldMutationKind.SetDefaultValue,
            source => source.DefaultValue = value);
    }

    /// <summary>Clears an explicitly present field default value.</summary>
    public PdfFormFieldEditOperation ClearFormFieldDefaultValue(
        PdfEditableFormField field)
    {
        if (field.DefaultValue is null)
        {
            throw new KeyNotFoundException("The form field does not have an explicit default value.");
        }

        return UpdateFormField(
            field,
            PdfFormFieldMutationKind.ClearDefaultValue,
            source => source.DefaultValue = null);
    }

    /// <summary>Sets the common AcroForm field options.</summary>
    public PdfFormFieldEditOperation SetFormFieldOptions(
        PdfEditableFormField field,
        PdfFormFieldOptions options)
    {
        return UpdateFormField(
            field,
            PdfFormFieldMutationKind.SetOptions,
            source => source.Options = options);
    }

    /// <summary>Associates an attached Widget annotation with an AcroForm field.</summary>
    public PdfFormFieldEditOperation AttachWidgetToFormField(
        PdfEditableFormField field,
        PdfEditableAnnotation widget)
    {
        EnsureOwnership(field);
        EnsureOwnership(widget);

        if (widget.Subtype != PdfAnnotationSubtype.Widget)
        {
            throw new InvalidOperationException(
                "Only Widget annotations can be associated with AcroForm fields.");
        }

        if (!widget.IsAttached)
        {
            throw new InvalidOperationException(
                "The Widget annotation must be attached to a page before it can be associated with a form field.");
        }

        var sourceField = field.GetSourceField();
        var sourceWidget = widget.GetSourceAnnotation();
        var previous = CreateFormFieldSnapshot(field);
        var current = previous with { WidgetCount = previous.WidgetCount + 1 };
        var operation = new PdfFormFieldEditOperation(
            owner,
            PdfFormFieldMutationKind.AttachWidget,
            field.Id,
            field.FieldType,
            PdfStructuralValueState.Explicit(previous),
            PdfStructuralValueState.Explicit(current),
            parentFieldId: field.ParentFieldId,
            widgetAnnotationId: widget.Id);

        session.ValidateOperation(operation);
        sourceField.AttachWidget(sourceWidget);
        session.RecordChange(
            operation,
            () => sourceField.DetachWidget(sourceWidget),
            () => sourceField.AttachWidget(sourceWidget));

        return operation;
    }

    /// <summary>Detaches a Widget annotation from its AcroForm field.</summary>
    public PdfFormFieldEditOperation DetachWidgetFromFormField(
        PdfEditableFormField field,
        PdfEditableAnnotation widget)
    {
        EnsureOwnership(field);
        EnsureOwnership(widget);
        var sourceField = field.GetSourceField();
        var sourceWidget = widget.GetSourceAnnotation();

        if (!sourceField.Widgets.Contains(sourceWidget))
        {
            throw new InvalidOperationException(
                "The Widget annotation is not associated with the supplied form field.");
        }

        var previous = CreateFormFieldSnapshot(field);
        var current = previous with { WidgetCount = previous.WidgetCount - 1 };
        var operation = new PdfFormFieldEditOperation(
            owner,
            PdfFormFieldMutationKind.DetachWidget,
            field.Id,
            field.FieldType,
            PdfStructuralValueState.Explicit(previous),
            PdfStructuralValueState.Explicit(current),
            parentFieldId: field.ParentFieldId,
            widgetAnnotationId: widget.Id);

        session.ValidateOperation(operation);
        _ = sourceField.DetachWidget(sourceWidget);
        session.RecordChange(
            operation,
            () => sourceField.AttachWidget(sourceWidget),
            () => sourceField.DetachWidget(sourceWidget));

        return operation;
    }

    private PdfFormFieldEditOperation UpdateFormField(
        PdfEditableFormField field,
        PdfFormFieldMutationKind kind,
        Action<PdfFormField> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        EnsureOwnership(field);
        var source = field.GetSourceField();
        var previous = CreateFormFieldSnapshot(field);
        var preview = new PdfFormField(source.FieldType)
        {
            PartialName = source.PartialName,
            Value = source.Value,
            DefaultValue = source.DefaultValue,
            Options = source.Options
        };
        mutate(preview);

        var current = new PdfFormFieldSnapshot(
            preview.FieldType,
            preview.PartialName,
            preview.Value,
            preview.DefaultValue,
            preview.Options,
            previous.ParentFieldId,
            previous.ChildCount,
            previous.WidgetCount);
        var operation = new PdfFormFieldEditOperation(
            owner,
            kind,
            field.Id,
            field.FieldType,
            PdfStructuralValueState.Explicit(previous),
            PdfStructuralValueState.Explicit(current),
            parentFieldId: field.ParentFieldId);

        session.ValidateOperation(operation);
        RestoreFormFieldSnapshot(source, current);
        session.RecordChange(
            operation,
            () => RestoreFormFieldSnapshot(source, previous),
            () => RestoreFormFieldSnapshot(source, current));

        return operation;
    }

    private static PdfFormFieldSnapshot CreateFormFieldSnapshot(
        PdfEditableFormField field)
    {
        var source = field.GetSourceField();
        return new PdfFormFieldSnapshot(
            source.FieldType,
            source.PartialName,
            source.Value,
            source.DefaultValue,
            source.Options,
            source.Parent?.Id,
            source.Children.Count,
            source.Widgets.Count);
    }

    private static void RestoreFormFieldSnapshot(
        PdfFormField field,
        PdfFormFieldSnapshot snapshot)
    {
        field.PartialName = snapshot.PartialName;
        field.Value = snapshot.Value;
        field.DefaultValue = snapshot.DefaultValue;
        field.Options = snapshot.Options;
    }

    /// <summary>Creates a detached outline-item handle owned by this editing session.</summary>
    public PdfEditableOutlineItem CreateOutlineItem(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        return new PdfEditableOutlineItem(
            new PdfOutlineItem(title),
            owner);
    }

    /// <summary>Appends a detached item to the document-outline root.</summary>
    public PdfOutlineEditOperation AddOutlineItem(PdfEditableOutlineItem item)
    {
        EnsureOwnership(item);

        if (item.IsChild)
        {
            throw new InvalidOperationException(
                "A child outline item cannot also be attached at the outline root.");
        }

        var index = OutlineItems.Count;
        var snapshot = CreateOutlineItemSnapshot(item);
        var operation = new PdfOutlineEditOperation(
            owner,
            PdfOutlineMutationKind.AddRoot,
            item.Id,
            PdfStructuralValueState.Absent<PdfOutlineItemSnapshot>(),
            PdfStructuralValueState.Explicit(snapshot),
            index: index);

        session.ValidateOperation(operation);
        OutlineItems.Insert(index, item);
        session.RecordChange(
            operation,
            () => OutlineItems.Remove(item),
            () => OutlineItems.Insert(index, item));

        return operation;
    }

    /// <summary>Removes a root outline item while retaining its stable session handle.</summary>
    public PdfOutlineEditOperation RemoveOutlineItem(PdfEditableOutlineItem item)
    {
        EnsureOwnership(item);
        var index = OutlineItems.IndexOf(item);

        if (index < 0)
        {
            throw new InvalidOperationException(
                "The outline item is not attached at the document-outline root.");
        }

        var snapshot = CreateOutlineItemSnapshot(item);
        var operation = new PdfOutlineEditOperation(
            owner,
            PdfOutlineMutationKind.RemoveRoot,
            item.Id,
            PdfStructuralValueState.Explicit(snapshot),
            PdfStructuralValueState.Absent<PdfOutlineItemSnapshot>(),
            index: index);

        session.ValidateOperation(operation);
        _ = OutlineItems.Remove(item);
        session.RecordChange(
            operation,
            () => OutlineItems.Insert(index, item),
            () => OutlineItems.Remove(item));

        return operation;
    }

    /// <summary>Appends a detached outline item as a child of another item.</summary>
    public PdfOutlineEditOperation AddChildOutlineItem(
        PdfEditableOutlineItem parent,
        PdfEditableOutlineItem child)
    {
        EnsureOwnership(parent);
        EnsureOwnership(child);

        if (OutlineItems.Contains(child))
        {
            throw new InvalidOperationException(
                "A root outline item must be removed from the outline root before it can become a child.");
        }

        var index = parent.Children.Count;
        var previous = CreateOutlineItemSnapshot(child);
        var operation = new PdfOutlineEditOperation(
            owner,
            PdfOutlineMutationKind.AddChild,
            child.Id,
            PdfStructuralValueState.Explicit(previous),
            PdfStructuralValueState.Explicit(
                previous with { ParentItemId = parent.Id }),
            parent.Id,
            index);

        session.ValidateOperation(operation);
        parent.Children.Insert(index, child);
        session.RecordChange(
            operation,
            () => parent.Children.Remove(child),
            () => parent.Children.Insert(index, child));

        return operation;
    }

    /// <summary>Detaches a child outline item while retaining its stable handle.</summary>
    public PdfOutlineEditOperation RemoveChildOutlineItem(
        PdfEditableOutlineItem parent,
        PdfEditableOutlineItem child)
    {
        EnsureOwnership(parent);
        EnsureOwnership(child);
        var index = parent.Children.IndexOf(child);

        if (index < 0)
        {
            throw new InvalidOperationException(
                "The outline item is not a child of the supplied parent item.");
        }

        var previous = CreateOutlineItemSnapshot(child);
        var operation = new PdfOutlineEditOperation(
            owner,
            PdfOutlineMutationKind.RemoveChild,
            child.Id,
            PdfStructuralValueState.Explicit(previous),
            PdfStructuralValueState.Explicit(
                previous with { ParentItemId = null }),
            parent.Id,
            index);

        session.ValidateOperation(operation);
        _ = parent.Children.Remove(child);
        session.RecordChange(
            operation,
            () => parent.Children.Insert(index, child),
            () => parent.Children.Remove(child));

        return operation;
    }

    /// <summary>Sets the visible outline title explicitly, including an empty string.</summary>
    public PdfOutlineEditOperation SetOutlineTitle(
        PdfEditableOutlineItem item,
        string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        return UpdateOutlineItem(
            item,
            PdfOutlineMutationKind.SetTitle,
            source => source.Title = title);
    }

    /// <summary>Sets whether the outline item's children are logically expanded.</summary>
    public PdfOutlineEditOperation SetOutlineOpenState(
        PdfEditableOutlineItem item,
        bool isOpen)
    {
        return UpdateOutlineItem(
            item,
            PdfOutlineMutationKind.SetOpenState,
            source => source.IsOpen = isOpen);
    }

    /// <summary>Sets a direct page destination and clears any navigation action.</summary>
    public PdfOutlineEditOperation SetOutlineDestination(
        PdfEditableOutlineItem item,
        PdfEditablePage page)
    {
        EnsureOwnership(page);

        if (!Pages.Contains(page))
        {
            throw new InvalidOperationException(
                "An outline destination must reference a page attached to the editable document.");
        }

        return UpdateOutlineItem(
            item,
            PdfOutlineMutationKind.SetDestination,
            source =>
            {
                source.Destination = new PdfOutlineDestination(page.Id);
                source.Action = null;
            });
    }

    /// <summary>Sets a navigation action and clears any direct page destination.</summary>
    public PdfOutlineEditOperation SetOutlineAction(
        PdfEditableOutlineItem item,
        PdfOutlineNavigationActionType actionType,
        string target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        var action = new PdfOutlineNavigationAction(actionType, target);

        return UpdateOutlineItem(
            item,
            PdfOutlineMutationKind.SetAction,
            source =>
            {
                source.Action = action;
                source.Destination = null;
            });
    }

    /// <summary>Clears the item's destination or navigation action.</summary>
    public PdfOutlineEditOperation ClearOutlineTarget(PdfEditableOutlineItem item)
    {
        if (item.Destination is null && item.Action is null)
        {
            throw new KeyNotFoundException(
                "The outline item does not have an explicit destination or navigation action.");
        }

        return UpdateOutlineItem(
            item,
            PdfOutlineMutationKind.ClearTarget,
            source =>
            {
                source.Destination = null;
                source.Action = null;
            });
    }

    private PdfOutlineEditOperation UpdateOutlineItem(
        PdfEditableOutlineItem item,
        PdfOutlineMutationKind kind,
        Action<PdfOutlineItem> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        EnsureOwnership(item);
        var source = item.GetSourceItem();
        var previous = CreateOutlineItemSnapshot(item);
        var preview = new PdfOutlineItem(source.Title)
        {
            IsOpen = source.IsOpen,
            Destination = source.Destination,
            Action = source.Action
        };
        mutate(preview);

        var current = new PdfOutlineItemSnapshot(
            preview.Title,
            preview.IsOpen,
            preview.Destination,
            preview.Action,
            previous.ParentItemId,
            previous.ChildCount);
        var operation = new PdfOutlineEditOperation(
            owner,
            kind,
            item.Id,
            PdfStructuralValueState.Explicit(previous),
            PdfStructuralValueState.Explicit(current),
            item.ParentItemId);

        session.ValidateOperation(operation);
        RestoreOutlineItemSnapshot(source, current);
        session.RecordChange(
            operation,
            () => RestoreOutlineItemSnapshot(source, previous),
            () => RestoreOutlineItemSnapshot(source, current));

        return operation;
    }

    private static PdfOutlineItemSnapshot CreateOutlineItemSnapshot(
        PdfEditableOutlineItem item)
    {
        var source = item.GetSourceItem();
        return new PdfOutlineItemSnapshot(
            source.Title,
            source.IsOpen,
            source.Destination,
            source.Action,
            source.Parent?.Id,
            source.Children.Count);
    }

    private static void RestoreOutlineItemSnapshot(
        PdfOutlineItem item,
        PdfOutlineItemSnapshot snapshot)
    {
        item.Title = snapshot.Title;
        item.IsOpen = snapshot.IsOpen;
        item.Destination = snapshot.Destination;
        item.Action = snapshot.Action;
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
