
namespace BlackFlag.Engine.Rendering.Images;

/// <summary>Controls how automatic best-fit image conversion chooses output.</summary>
public enum PdfImageConversionPolicy
{
    PreserveExactly,
    PreferLossless,
    Balanced,
    PreferCompact
}
