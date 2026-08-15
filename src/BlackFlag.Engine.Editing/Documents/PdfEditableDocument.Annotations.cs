using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Editing.Annotations;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Documents;

public sealed partial class PdfEditableDocument
{
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
}
