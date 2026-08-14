using System.Collections;

using BlackFlag.Engine.Document.Forms;
using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Forms;

/// <summary>Exposes stable editable handles for one ordered AcroForm field collection.</summary>
public sealed class PdfEditableFormFieldCollection : IReadOnlyList<PdfEditableFormField>
{
    private readonly List<PdfEditableFormField> fields;
    private readonly PdfAcroForm? form;
    private readonly PdfFormField? parent;

    internal PdfEditableFormFieldCollection(
        PdfAcroForm form,
        PdfEditingOwnershipToken owner)
    {
        ArgumentNullException.ThrowIfNull(form);
        this.form = form;
        fields = CreateHandles(form.Fields, owner);
    }

    internal PdfEditableFormFieldCollection(
        PdfFormField parent,
        PdfEditingOwnershipToken owner)
    {
        ArgumentNullException.ThrowIfNull(parent);
        this.parent = parent;
        fields = CreateHandles(parent.Children, owner);
    }

    public int Count => fields.Count;
    public PdfEditableFormField this[int index] => fields[index];

    public bool Contains(PdfEditableFormField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return fields.Contains(field);
    }

    public int IndexOf(PdfEditableFormField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return fields.IndexOf(field);
    }

    public IEnumerator<PdfEditableFormField> GetEnumerator() => fields.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void Insert(int index, PdfEditableFormField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        EnsureSynchronized();

        if (index < 0 || index > fields.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (fields.Contains(field))
        {
            throw new InvalidOperationException("The form field is already attached to this collection.");
        }

        if (form is not null)
        {
            form.InsertField(index, field.GetSourceField());
        }
        else
        {
            parent!.InsertChild(index, field.GetSourceField());
        }

        fields.Insert(index, field);
    }

    internal int Remove(PdfEditableFormField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        EnsureSynchronized();
        var index = fields.IndexOf(field);

        if (index < 0)
        {
            throw new InvalidOperationException("The form field is not attached to this collection.");
        }

        if (form is not null)
        {
            _ = form.RemoveField(field.GetSourceField());
        }
        else
        {
            _ = parent!.RemoveChild(field.GetSourceField());
        }

        fields.RemoveAt(index);
        return index;
    }

    private void EnsureSynchronized()
    {
        var source = form is not null ? form.Fields : parent!.Children;

        if (fields.Count != source.Count)
        {
            throw new InvalidOperationException(
                "The authoritative form-field collection changed outside the editing session.");
        }

        for (var index = 0; index < fields.Count; index++)
        {
            if (!ReferenceEquals(fields[index].GetSourceField(), source[index]))
            {
                throw new InvalidOperationException(
                    "The authoritative form-field collection changed outside the editing session.");
            }
        }
    }

    private static List<PdfEditableFormField> CreateHandles(
        IReadOnlyList<PdfFormField> source,
        PdfEditingOwnershipToken owner)
    {
        var result = new List<PdfEditableFormField>(source.Count);

        for (var index = 0; index < source.Count; index++)
        {
            result.Add(new PdfEditableFormField(source[index], owner));
        }

        return result;
    }
}
