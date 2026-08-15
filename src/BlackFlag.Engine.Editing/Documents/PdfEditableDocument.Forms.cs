using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Forms;
using BlackFlag.Engine.Editing.Annotations;
using BlackFlag.Engine.Editing.Forms;
using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Documents;

public sealed partial class PdfEditableDocument
{
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
}
