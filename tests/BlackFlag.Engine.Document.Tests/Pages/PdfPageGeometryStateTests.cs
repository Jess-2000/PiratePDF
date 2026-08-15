using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Document.Tests.Pages;

public sealed class PdfPageGeometryStateTests
{
    [Fact]
    public void NewPageSnapshotEmitsOnlyExplicitMediaBox()
    {
        var page = new PdfPage(612, 792);

        var dictionary = page.CreateDictionarySnapshot();

        Assert.True(dictionary.ContainsKey(PdfNames.MediaBox));
        Assert.False(dictionary.ContainsKey(PdfNames.CropBox));
        Assert.False(dictionary.ContainsKey(PdfNames.BleedBox));
        Assert.False(dictionary.ContainsKey(PdfNames.TrimBox));
        Assert.False(dictionary.ContainsKey(PdfNames.ArtBox));
        Assert.False(dictionary.ContainsKey(PdfNames.Rotate));
    }

    [Fact]
    public void ExplicitProductionBoxDoesNotFollowCropChange()
    {
        var page = new PdfPage(612, 792);
        var trim = new PdfPageBox(10, 10, 602, 782);
        page.SetBox(
            PdfPageBoxKind.TrimBox,
            new PdfEffectiveValue<PdfPageBox>(trim, PdfPageValueOrigin.Explicit));

        page.SetBox(
            PdfPageBoxKind.CropBox,
            new PdfEffectiveValue<PdfPageBox>(
                new PdfPageBox(20, 20, 592, 772),
                PdfPageValueOrigin.Explicit));

        Assert.Equal(trim, page.GetBox(PdfPageBoxKind.TrimBox).Value);
        Assert.Equal(PdfPageValueOrigin.Explicit, page.GetBox(PdfPageBoxKind.TrimBox).Origin);
    }
}
