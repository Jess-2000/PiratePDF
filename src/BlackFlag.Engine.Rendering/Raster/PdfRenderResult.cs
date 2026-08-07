namespace BlackFlag.Engine.Rendering.Raster;

/// <summary>Contains the raster and diagnostics produced by a render operation.</summary>
public sealed class PdfRenderResult
{
    private readonly PdfRenderDiagnostic[] diagnostics;

    /// <summary>Initializes a render result.</summary>
    public PdfRenderResult(
        PdfRasterSurface surface,
        IEnumerable<PdfRenderDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(diagnostics);
        Surface = surface;
        this.diagnostics = diagnostics.ToArray();
    }

    /// <summary>Gets the completed raster surface.</summary>
    public PdfRasterSurface Surface { get; }

    /// <summary>Gets diagnostics in encounter order.</summary>
    public IReadOnlyList<PdfRenderDiagnostic> Diagnostics => diagnostics;
}
