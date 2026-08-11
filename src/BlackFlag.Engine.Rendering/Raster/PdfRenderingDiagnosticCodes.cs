
namespace BlackFlag.Engine.Rendering.Raster;

/// <summary>
/// Contains the intentionally small set of public rendering diagnostic codes.
/// Most rendering failures are communicated by human-readable messages.
/// </summary>
public static class PdfRenderingDiagnosticCodes
{
    /// <summary>Color conversion failed for rendered content.</summary>
    public const string ColorConversionFailed = "COLOR_CONVERSION_FAILED";

    /// <summary>A shading definition or stream is malformed.</summary>
    public const string ShadingMalformed = "SHADING_MALFORMED";
}
