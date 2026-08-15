using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Forms;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Forms;
using BlackFlag.Engine.Editing.Operations;
using BlackFlag.Engine.Editing.Structural;
using BlackFlag.Engine.Editing.Validation;

namespace BlackFlag.Engine.Editing.Tests.Forms;

public sealed class PdfFormFieldEditingTests
{
    [Fact]
    public void AddRootFieldCreatesDocumentScopedFormOperation()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var field = session.Document.CreateFormField(PdfFormFieldType.Text);

        var operation = session.Document.AddFormField(field);

        Assert.Equal(PdfEditScope.Document, operation.Scope);
        Assert.Equal(PdfStructureKind.Form, operation.StructureKind);
        Assert.Equal(PdfFormFieldMutationKind.AddRoot, operation.Kind);
        Assert.Equal(field.Id, operation.FieldId);
        Assert.Equal(PdfStructuralValueOrigin.Absent, operation.PreviousState.Origin);
        Assert.Equal(PdfStructuralValueOrigin.Explicit, operation.CurrentState.Origin);
        Assert.Same(field, Assert.Single(session.Document.FormFields));
    }

    [Fact]
    public void ChildFieldHierarchyPreservesStableIdentity()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var parent = session.Document.CreateFormField(PdfFormFieldType.Text);
        var child = session.Document.CreateFormField(PdfFormFieldType.Text);
        _ = session.Document.AddFormField(parent);

        _ = session.Document.AddChildFormField(parent, child);

        Assert.Same(child, Assert.Single(parent.Children));
        Assert.Equal(parent.Id, child.ParentFieldId);

        var operation = session.Document.RemoveChildFormField(parent, child);

        Assert.Equal(PdfFormFieldMutationKind.RemoveChild, operation.Kind);
        Assert.Empty(parent.Children);
        Assert.Null(child.ParentFieldId);
    }

    [Fact]
    public void ExplicitEmptyValueRemainsDistinctFromAbsentValue()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var field = session.Document.CreateFormField(PdfFormFieldType.Text);
        _ = session.Document.AddFormField(field);

        _ = session.Document.SetFormFieldValue(field, string.Empty);
        Assert.Equal(string.Empty, field.Value);

        var operation = session.Document.ClearFormFieldValue(field);

        Assert.Equal(string.Empty, operation.PreviousState.Value.Value);
        Assert.Null(operation.CurrentState.Value.Value);
        Assert.Null(field.Value);
    }

    [Fact]
    public void CommonStateMutationPreservesUntargetedFieldState()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var field = session.Document.CreateFormField(PdfFormFieldType.Text);
        _ = session.Document.AddFormField(field);
        _ = session.Document.SetFormFieldPartialName(field, "Customer.Name");
        _ = session.Document.SetFormFieldValue(field, "Alice");
        _ = session.Document.SetFormFieldDefaultValue(field, "Default");
        _ = session.Document.SetFormFieldOptions(
            field,
            PdfFormFieldOptions.ReadOnly | PdfFormFieldOptions.Required);

        _ = session.Document.SetFormFieldValue(field, "Bob");

        Assert.Equal("Customer.Name", field.PartialName);
        Assert.Equal("Bob", field.Value);
        Assert.Equal("Default", field.DefaultValue);
        Assert.Equal(
            PdfFormFieldOptions.ReadOnly | PdfFormFieldOptions.Required,
            field.Options);
    }

    [Fact]
    public void WidgetCanBeAssociatedWithFieldAndPreservesAnnotationIdentity()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var field = session.Document.CreateFormField(PdfFormFieldType.Text);
        var widget = session.Document.CreateAnnotation(
            PdfAnnotationSubtype.Widget,
            new PdfAnnotationRectangle(10, 10, 110, 30));
        _ = session.Document.AddFormField(field);
        _ = session.Document.AddAnnotation(page, widget);

        var operation = session.Document.AttachWidgetToFormField(field, widget);

        Assert.Equal(PdfFormFieldMutationKind.AttachWidget, operation.Kind);
        Assert.Equal(widget.Id, operation.WidgetAnnotationId);
        Assert.Equal(1, field.WidgetCount);
        Assert.Equal(widget.Id, Assert.Single(field.WidgetIds));

        _ = session.Document.DetachWidgetFromFormField(field, widget);
        Assert.Equal(0, field.WidgetCount);
        Assert.True(widget.IsAttached);
        Assert.Equal(page.Id, widget.PageId);
    }

    [Fact]
    public void LinkedWidgetMustBeDetachedBeforePageRemoval()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var field = session.Document.CreateFormField(PdfFormFieldType.Text);
        var widget = session.Document.CreateAnnotation(
            PdfAnnotationSubtype.Widget,
            new PdfAnnotationRectangle(10, 10, 110, 30));
        _ = session.Document.AddFormField(field);
        _ = session.Document.AddAnnotation(page, widget);
        _ = session.Document.AttachWidgetToFormField(field, widget);

        Assert.Throws<InvalidOperationException>(
            () => session.Document.RemoveAnnotation(page, widget));

        Assert.True(widget.IsAttached);
        Assert.Equal(1, field.WidgetCount);
    }

    [Fact]
    public void TransactionRollbackRestoresFieldValue()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var field = session.Document.CreateFormField(PdfFormFieldType.Text);
        _ = session.Document.AddFormField(field);
        _ = session.Document.SetFormFieldValue(field, "Before");

        using var transaction = session.BeginTransaction();
        _ = session.Document.SetFormFieldValue(field, "During");
        transaction.Rollback();

        Assert.Equal("Before", field.Value);
    }

    [Fact]
    public void CommittedWidgetRelationshipSupportsUndoAndRedo()
    {
        var document = CreateDocumentWithPage();
        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var field = session.Document.CreateFormField(PdfFormFieldType.Text);
        var widget = session.Document.CreateAnnotation(
            PdfAnnotationSubtype.Widget,
            new PdfAnnotationRectangle(10, 10, 110, 30));
        _ = session.Document.AddFormField(field);
        _ = session.Document.AddAnnotation(page, widget);

        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.AttachWidgetToFormField(field, widget);
            transaction.Commit();
        }

        Assert.Equal(1, field.WidgetCount);
        _ = session.History.Undo();
        Assert.Equal(0, field.WidgetCount);
        _ = session.History.Redo();
        Assert.Equal(1, field.WidgetCount);
    }

    [Fact]
    public void ValidatorRunsBeforeFormMutation()
    {
        var document = CreateDocumentWithPage();
        var validator = new RejectFormValidator();
        using var session = new PdfEditingSession(
            document,
            new PdfEditingSessionOptions { Validators = [validator] });
        var field = session.Document.CreateFormField(PdfFormFieldType.Text);

        Assert.Throws<PdfEditingValidationException>(
            () => session.Document.AddFormField(field));
        Assert.Empty(session.Document.FormFields);
    }

    [Fact]
    public void FormFieldFromAnotherSessionIsRejected()
    {
        var firstDocument = CreateDocumentWithPage();
        var secondDocument = CreateDocumentWithPage();
        using var firstSession = new PdfEditingSession(firstDocument);
        using var secondSession = new PdfEditingSession(secondDocument);
        var field = firstSession.Document.CreateFormField(PdfFormFieldType.Text);

        Assert.Throws<BlackFlag.Engine.Editing.Ownership.PdfEditingOwnershipException>(
            () => secondSession.Document.AddFormField(field));
    }

    private static PdfDocument CreateDocumentWithPage()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));
        return document;
    }

    private sealed class RejectFormValidator : IPdfEditingValidator
    {
        public void Validate(PdfEditingSession session, IPdfEditOperation operation)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(operation);

            if (operation is IPdfStructuralEditOperation structural &&
                structural.StructureKind == PdfStructureKind.Form)
            {
                throw new PdfEditingValidationException("Form edit rejected.");
            }
        }
    }
}
