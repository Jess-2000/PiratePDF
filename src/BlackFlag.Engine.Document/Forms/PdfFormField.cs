using BlackFlag.Engine.Document.Annotations;

namespace BlackFlag.Engine.Document.Forms;

/// <summary>Represents the authoritative logical state of one AcroForm field.</summary>
public sealed class PdfFormField
{
    private readonly List<PdfFormField> children = [];
    private readonly IReadOnlyList<PdfFormField> readOnlyChildren;
    private readonly List<PdfAnnotation> widgets = [];
    private readonly IReadOnlyList<PdfAnnotation> readOnlyWidgets;
    private PdfFormField? parent;

    public PdfFormField(PdfFormFieldType fieldType)
    {
        if (!Enum.IsDefined(fieldType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(fieldType),
                fieldType,
                "The AcroForm field type is not supported.");
        }

        Id = Guid.NewGuid();
        FieldType = fieldType;
        readOnlyChildren = children.AsReadOnly();
        readOnlyWidgets = widgets.AsReadOnly();
    }

    /// <summary>Gets the stable logical identity of the field.</summary>
    public Guid Id { get; }

    /// <summary>Gets the field type.</summary>
    public PdfFormFieldType FieldType { get; }

    /// <summary>Gets the partial field name, or null when absent.</summary>
    public string? PartialName { get; internal set; }

    /// <summary>Gets the current logical field value, or null when absent.</summary>
    public string? Value { get; internal set; }

    /// <summary>Gets the default logical field value, or null when absent.</summary>
    public string? DefaultValue { get; internal set; }

    /// <summary>Gets the common field options.</summary>
    public PdfFormFieldOptions Options { get; internal set; }

    /// <summary>Gets the ordered child fields.</summary>
    public IReadOnlyList<PdfFormField> Children => readOnlyChildren;

    /// <summary>Gets Widget annotations associated with this field.</summary>
    public IReadOnlyList<PdfAnnotation> Widgets => readOnlyWidgets;

    internal PdfFormField? Parent => parent;

    internal void InsertChild(int index, PdfFormField child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (index < 0 || index > children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (ReferenceEquals(child, this))
        {
            throw new InvalidOperationException("A form field cannot be its own child.");
        }

        for (var ancestor = this; ancestor is not null; ancestor = ancestor.parent)
        {
            if (ReferenceEquals(ancestor, child))
            {
                throw new InvalidOperationException("The form-field hierarchy cannot contain a cycle.");
            }
        }

        if (child.parent is not null)
        {
            throw new InvalidOperationException("The form field is already attached to a parent field.");
        }

        children.Insert(index, child);
        child.parent = this;
    }

    internal int RemoveChild(PdfFormField child)
    {
        ArgumentNullException.ThrowIfNull(child);
        var index = children.IndexOf(child);

        if (index < 0)
        {
            throw new InvalidOperationException("The form field is not a child of this field.");
        }

        children.RemoveAt(index);
        child.parent = null;
        return index;
    }

    internal void AttachWidget(PdfAnnotation widget)
    {
        ArgumentNullException.ThrowIfNull(widget);

        if (widget.Subtype != PdfAnnotationSubtype.Widget)
        {
            throw new InvalidOperationException("Only Widget annotations can be associated with AcroForm fields.");
        }

        if (widget.Parent is null)
        {
            throw new InvalidOperationException("A Widget annotation must be attached to a page before it can be associated with a field.");
        }

        if (widget.FormField is not null)
        {
            throw new InvalidOperationException("The Widget annotation is already associated with a form field.");
        }

        widgets.Add(widget);
        widget.AttachToFormField(this);
    }

    internal int DetachWidget(PdfAnnotation widget)
    {
        ArgumentNullException.ThrowIfNull(widget);
        var index = widgets.IndexOf(widget);

        if (index < 0)
        {
            throw new InvalidOperationException("The Widget annotation is not associated with this form field.");
        }

        widgets.RemoveAt(index);
        widget.DetachFromFormField(this);
        return index;
    }
}
