namespace BlackFlag.Engine.Document.Forms;

/// <summary>Represents the authoritative logical AcroForm root-field collection.</summary>
public sealed class PdfAcroForm
{
    private readonly List<PdfFormField> fields = [];
    private readonly IReadOnlyList<PdfFormField> readOnlyFields;

    public PdfAcroForm()
    {
        readOnlyFields = fields.AsReadOnly();
    }

    /// <summary>Gets the ordered AcroForm root fields.</summary>
    public IReadOnlyList<PdfFormField> Fields => readOnlyFields;

    internal void InsertField(int index, PdfFormField field)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (index < 0 || index > fields.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (fields.Contains(field))
        {
            throw new InvalidOperationException("The form field is already a root field.");
        }

        if (field.Parent is not null)
        {
            throw new InvalidOperationException("A child form field cannot also be an AcroForm root field.");
        }

        fields.Insert(index, field);
    }

    internal int RemoveField(PdfFormField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        var index = fields.IndexOf(field);

        if (index < 0)
        {
            throw new InvalidOperationException("The form field is not attached to the AcroForm root.");
        }

        fields.RemoveAt(index);
        return index;
    }
}
