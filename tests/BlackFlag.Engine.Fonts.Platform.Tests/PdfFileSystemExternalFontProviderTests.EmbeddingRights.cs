using System.IO;
using BlackFlag.Engine.Fonts.Platform.Windows;

namespace BlackFlag.Engine.Fonts.Platform.Tests;

public sealed partial class PdfFileSystemExternalFontProviderTests
{
    [Theory]
    [InlineData(3, 0, PdfExternalFontEmbeddingRights.Allowed, false, false, true)]
    [InlineData(3, 2, PdfExternalFontEmbeddingRights.Restricted, false, false, false)]
    [InlineData(3, 4, PdfExternalFontEmbeddingRights.PreviewAndPrint, false, false, false)]
    [InlineData(3, 8, PdfExternalFontEmbeddingRights.Editable, false, false, true)]
    [InlineData(3, 0x100, PdfExternalFontEmbeddingRights.Allowed, true, false, true)]
    [InlineData(3, 0x108, PdfExternalFontEmbeddingRights.Editable, true, false, true)]
    [InlineData(3, 0x200, PdfExternalFontEmbeddingRights.Allowed, false, true, false)]
    [InlineData(3, 0x208, PdfExternalFontEmbeddingRights.Editable, false, true, false)]
    [InlineData(0, 0x100, PdfExternalFontEmbeddingRights.Allowed, false, false, true)]
    [InlineData(1, 0x200, PdfExternalFontEmbeddingRights.Allowed, false, false, true)]
    [InlineData(2, 0x108, PdfExternalFontEmbeddingRights.Editable, true, false, true)]
    [InlineData(3, 12, PdfExternalFontEmbeddingRights.Unknown, false, false, false)]
    public void CatalogRetainsBasePermissionAndIndependentRestrictions(
        ushort version, ushort flags, PdfExternalFontEmbeddingRights rights, bool noSubset, bool bitmap, bool embeddable)
    {
        File.WriteAllBytes(Path.Combine(directory, "Rights.ttf"), BuildFont(version: version, fsType: flags));
        File.WriteAllBytes(Path.Combine(directory, "Malformed.ttf"), [1, 2, 3]);
        var provider = new PdfWindowsExternalFontProvider([directory]);
        var face = Assert.Single(provider.GetFaces());
        Assert.Equal(rights, face.EmbeddingRights);
        Assert.Equal(noSubset, face.NoSubsetting);
        Assert.Equal(bitmap, face.BitmapEmbeddingOnly);
        var matches = new PdfExternalFontCatalog([provider]).Match(new PdfExternalFontMatchRequest("Test Family"), "A");
        Assert.Equal(embeddable ? 1 : 0, matches.Count);
        Assert.Same(face, Assert.Single(provider.GetFaces()));
    }
}
