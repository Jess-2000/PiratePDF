namespace BlackFlag.Engine.Rendering.Raster;

/// <summary>Represents one nonfatal or fatal rendering diagnostic.</summary>
public sealed record PdfRenderDiagnostic(
    string Code,
    string Message,
    int DisplayItemIndex);
