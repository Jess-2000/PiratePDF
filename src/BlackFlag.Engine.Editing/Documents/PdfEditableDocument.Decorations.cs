using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Document.Decorations;
using BlackFlag.Engine.Editing.Decorations;
using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Documents;

public sealed partial class PdfEditableDocument
{
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
}
