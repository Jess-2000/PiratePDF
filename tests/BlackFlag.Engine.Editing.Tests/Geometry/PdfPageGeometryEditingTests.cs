using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Geometry;
using BlackFlag.Engine.Editing.Structural;

namespace BlackFlag.Engine.Editing.Tests.Geometry;

public sealed class PdfPageGeometryEditingTests
{
    [Fact]
    public void NewPageExposesDefaultedCropAndProductionBoxes()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];

        Assert.Equal(PdfStructuralValueOrigin.Explicit, page.GetBoxState(PdfPageBoxKind.MediaBox).Origin);
        Assert.Equal(PdfStructuralValueOrigin.Defaulted, page.GetBoxState(PdfPageBoxKind.CropBox).Origin);
        Assert.Equal(PdfStructuralValueOrigin.Defaulted, page.GetBoxState(PdfPageBoxKind.BleedBox).Origin);
        Assert.Equal(PdfStructuralValueOrigin.Defaulted, page.GetBoxState(PdfPageBoxKind.TrimBox).Origin);
        Assert.Equal(PdfStructuralValueOrigin.Defaulted, page.GetBoxState(PdfPageBoxKind.ArtBox).Origin);
    }

    [Fact]
    public void CropChangesOnlyCropAndDefaultedEffectiveProductionBoxes()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var mediaBefore = page.GetBoxState(PdfPageBoxKind.MediaBox);
        var crop = new PdfPageBox(18, 24, 594, 768);

        var operation = session.Document.CropPage(page, crop);

        Assert.Equal(PdfPageGeometryMutationKind.Crop, operation.Kind);
        Assert.Equal(PdfStructureKind.PageGeometry, operation.StructureKind);
        Assert.Equal(mediaBefore, page.GetBoxState(PdfPageBoxKind.MediaBox));
        Assert.Equal(PdfStructuralValueOrigin.Explicit, page.GetBoxState(PdfPageBoxKind.CropBox).Origin);
        Assert.Equal(crop, page.GetBoxState(PdfPageBoxKind.CropBox).Value);
        Assert.Equal(PdfStructuralValueOrigin.Defaulted, page.GetBoxState(PdfPageBoxKind.BleedBox).Origin);
        Assert.Equal(crop, page.GetBoxState(PdfPageBoxKind.BleedBox).Value);
    }

    [Fact]
    public void CropRollbackRestoresDefaultedRepresentation()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var before = page.GetBoxState(PdfPageBoxKind.CropBox);

        using var transaction = session.BeginTransaction();
        _ = session.Document.CropPage(page, new PdfPageBox(10, 10, 602, 782));
        transaction.Rollback();

        Assert.Equal(before, page.GetBoxState(PdfPageBoxKind.CropBox));
        Assert.Equal(PdfStructuralValueOrigin.Defaulted, page.GetBoxState(PdfPageBoxKind.CropBox).Origin);
    }

    [Fact]
    public void ResizeLowerLeftChangesMediaGeometryWithoutScalingContent()
    {
        var document = PdfDocument.CreateEmpty();
        var source = new PdfPage(612, 792);
        document.Pages.Add(source);

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        _ = session.Document.ReplacePageContent(page, [1, 2, 3, 4]);

        var operation = session.Document.ResizePage(page, 720, 540);

        Assert.Equal(PdfPageGeometryMutationKind.Resize, operation.Kind);
        Assert.Equal(PdfPageResizeAnchor.LowerLeft, operation.ResizeAnchor);
        Assert.Equal(720, page.Width);
        Assert.Equal(540, page.Height);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, page.GetContentBytes());
    }

    [Fact]
    public void ResizeCenterPreservesMediaBoxCenter()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(600, 800));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];

        _ = session.Document.ResizePage(page, 400, 600, PdfPageResizeAnchor.Center);
        var media = page.GetBoxState(PdfPageBoxKind.MediaBox).Value;

        Assert.Equal(100, media.Left);
        Assert.Equal(100, media.Bottom);
        Assert.Equal(500, media.Right);
        Assert.Equal(700, media.Top);
    }

    [Fact]
    public void RotationRollbackRestoresDefaultedOrigin()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        Assert.Equal(PdfStructuralValueOrigin.Defaulted, page.RotationState.Origin);

        using var transaction = session.BeginTransaction();
        _ = session.Document.SetPageRotation(page, PdfPageRotation.Clockwise90);
        transaction.Rollback();

        Assert.Equal(PdfPageRotation.None, page.Rotation);
        Assert.Equal(PdfStructuralValueOrigin.Defaulted, page.RotationState.Origin);
    }

    [Fact]
    public void SettingOneProductionBoxDoesNotMaterializeOthers()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var trim = new PdfPageBox(12, 12, 600, 780);

        _ = session.Document.SetPageBox(page, PdfPageBoxKind.TrimBox, trim);

        Assert.Equal(PdfStructuralValueOrigin.Explicit, page.GetBoxState(PdfPageBoxKind.TrimBox).Origin);
        Assert.Equal(PdfStructuralValueOrigin.Defaulted, page.GetBoxState(PdfPageBoxKind.BleedBox).Origin);
        Assert.Equal(PdfStructuralValueOrigin.Defaulted, page.GetBoxState(PdfPageBoxKind.ArtBox).Origin);
    }
}
