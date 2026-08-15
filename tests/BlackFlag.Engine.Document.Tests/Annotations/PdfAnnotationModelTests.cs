using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Document.Tests.Annotations;

public sealed class PdfAnnotationModelTests
{
    [Fact]
    public void AnnotationHasStableIdentityAndCommonState()
    {
        var rectangle = new PdfAnnotationRectangle(10, 20, 110, 70);
        var annotation = new PdfAnnotation(PdfAnnotationSubtype.Text, rectangle);

        Assert.NotEqual(Guid.Empty, annotation.Id);
        Assert.Equal(PdfAnnotationSubtype.Text, annotation.Subtype);
        Assert.Equal(rectangle, annotation.Rectangle);
        Assert.Null(annotation.Contents);
        Assert.Equal(PdfAnnotationOptions.None, annotation.Flags);
    }

    [Fact]
    public void PageOwnsAnnotationCollectionWithoutChangingAnnotationIdentity()
    {
        var page = new PdfPage(612, 792);
        var annotation = new PdfAnnotation(
            PdfAnnotationSubtype.Link,
            new PdfAnnotationRectangle(20, 30, 120, 60));
        var id = annotation.Id;

        page.InsertAnnotation(0, annotation);

        Assert.Same(annotation, Assert.Single(page.Annotations));
        Assert.Equal(id, page.Annotations[0].Id);
        Assert.Same(page, annotation.Parent);

        _ = page.RemoveAnnotation(annotation);

        Assert.Empty(page.Annotations);
        Assert.Null(annotation.Parent);
        Assert.Equal(id, annotation.Id);
    }

    [Fact]
    public void AnnotationRectangleRejectsInvalidGeometry()
    {
        Assert.Throws<ArgumentException>(
            () => new PdfAnnotationRectangle(10, 10, 10, 20));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PdfAnnotationRectangle(double.NaN, 0, 10, 20));
    }
}
