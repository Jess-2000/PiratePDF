using BlackFlag.Engine.Rendering.DisplayList;

namespace BlackFlag.Engine.Rendering.Raster;

/// <summary>Rasterizes an immutable semantic PDF display list.</summary>
public interface IPdfPageRenderer
{
    /// <summary>Renders the requested page-space region.</summary>
    PdfRenderResult Render(
        PdfDisplayList displayList,
        PdfRenderRequest request,
        CancellationToken cancellationToken = default);
}
