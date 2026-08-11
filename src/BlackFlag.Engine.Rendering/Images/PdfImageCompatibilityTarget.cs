
namespace BlackFlag.Engine.Rendering.Images;

/// <summary>Describes the preferred supported PDF representation for decoded image content.</summary>
public enum PdfImageCompatibilityTarget
{
    PreserveEncoded,
    FlateGray,
    FlateRgb,
    FlateRgbWithSoftMask,
    IndexedFlate,
    CcittBilevel,
    Jpeg,
    VectorImport,
    Unsupported
}
