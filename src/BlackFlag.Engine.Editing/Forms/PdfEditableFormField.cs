using BlackFlag.Engine.Document.Forms;
using BlackFlag.Engine.Editing.Ownership;

namespace BlackFlag.Engine.Editing.Forms;

/// <summary>Represents a stable editing-session handle for one AcroForm field.</summary>
public sealed class PdfEditableFormField
{
    private readonly PdfFormField field;

    internal PdfEditableFormField(
        PdfFormField field,
        PdfEditingOwnershipToken owner)
    {
        ArgumentNullException.ThrowIfNull(field);
        this.field = field;
        Owner = owner;
        Children = new PdfEditableFormFieldCollection(field, owner);
    }

    public Guid Id => this.field.Id;
    public PdfFormFieldType FieldType => this.field.FieldType;
    public string? PartialName => this.field.PartialName;
    public string? Value => this.field.Value;
    public string? DefaultValue => this.field.DefaultValue;
    public PdfFormFieldOptions Options => this.field.Options;
    public Guid? ParentFieldId => this.field.Parent?.Id;
    public bool IsChild => this.field.Parent is not null;
    public int WidgetCount => this.field.Widgets.Count;
    public IReadOnlyList<Guid> WidgetIds => this.field.Widgets.Select(widget => widget.Id).ToArray();
    public PdfEditableFormFieldCollection Children { get; }

    internal PdfEditingOwnershipToken Owner { get; }
    internal PdfFormField GetSourceField() => this.field;
}
