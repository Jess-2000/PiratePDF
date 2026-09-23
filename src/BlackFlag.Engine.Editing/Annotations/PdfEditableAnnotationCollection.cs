using System.Collections;

using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Annotations;

/// <summary>Exposes stable editable annotation handles for one page.</summary>
public sealed class PdfEditableAnnotationCollection : IReadOnlyList<PdfEditableAnnotation>
{
    private readonly List<PdfEditableAnnotation> annotations;
    private readonly PdfPage sourcePage;

    internal PdfEditableAnnotationCollection(
        PdfPage sourcePage,
        PdfEditingOwnershipToken owner)
    {
        ArgumentNullException.ThrowIfNull(sourcePage);
        this.sourcePage = sourcePage;
        annotations = new List<PdfEditableAnnotation>(sourcePage.Annotations.Count);

        for (var index = 0; index < sourcePage.Annotations.Count; index++)
        {
            annotations.Add(new PdfEditableAnnotation(sourcePage.Annotations[index], owner));
        }
    }

    public int Count => annotations.Count;
    public PdfEditableAnnotation this[int index] => annotations[index];
    public bool Contains(PdfEditableAnnotation annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        return annotations.Contains(annotation);
    }

    public int IndexOf(PdfEditableAnnotation annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        return annotations.IndexOf(annotation);
    }

    public IEnumerator<PdfEditableAnnotation> GetEnumerator() => annotations.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void Insert(int index, PdfEditableAnnotation annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        EnsureSynchronized();

        if (index < 0 || index > annotations.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (annotations.Contains(annotation) || annotation.IsAttached)
        {
            throw new InvalidOperationException("The annotation is already attached to a page.");
        }

        sourcePage.InsertAnnotation(index, annotation.GetSourceAnnotation());
        annotations.Insert(index, annotation);
    }

    internal int Remove(PdfEditableAnnotation annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        EnsureSynchronized();
        var index = annotations.IndexOf(annotation);

        if (index < 0)
        {
            throw new InvalidOperationException("The annotation is not attached to this page.");
        }

        _ = sourcePage.RemoveAnnotation(annotation.GetSourceAnnotation());
        annotations.RemoveAt(index);
        return index;
    }

    private void EnsureSynchronized()
    {
        if (annotations.Count != sourcePage.Annotations.Count)
        {
            throw new InvalidOperationException(
                "The authoritative annotation collection changed outside the editing session.");
        }

        for (var index = 0; index < annotations.Count; index++)
        {
            if (!ReferenceEquals(
                    annotations[index].GetSourceAnnotation(),
                    sourcePage.Annotations[index]))
            {
                throw new InvalidOperationException(
                    "The authoritative annotation collection changed outside the editing session.");
            }
        }
    }
}
