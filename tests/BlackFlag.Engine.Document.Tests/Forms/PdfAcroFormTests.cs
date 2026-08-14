using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Forms;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Document.Tests.Forms;

public sealed class PdfAcroFormTests
{
    [Fact]
    public void NewDocumentHasEmptyAcroForm()
    {
        var document = PdfDocument.CreateEmpty();

        Assert.Empty(document.AcroForm.Fields);
    }

    [Fact]
    public void FieldRejectsHierarchyCycles()
    {
        var parent = new PdfFormField(PdfFormFieldType.Text);
        var child = new PdfFormField(PdfFormFieldType.Text);
        parent.InsertChild(0, child);

        Assert.Throws<InvalidOperationException>(
            () => child.InsertChild(0, parent));
    }

    [Fact]
    public void FieldRejectsNonWidgetAnnotationRelationship()
    {
        var document = PdfDocument.CreateEmpty();
        var page = new PdfPage(612, 792);
        document.Pages.Add(page);
        var field = new PdfFormField(PdfFormFieldType.Text);
        var annotation = new PdfAnnotation(
            PdfAnnotationSubtype.Text,
            new PdfAnnotationRectangle(0, 0, 10, 10));
        page.InsertAnnotation(0, annotation);

        Assert.Throws<InvalidOperationException>(
            () => field.AttachWidget(annotation));
    }
}
