
namespace BlackFlag.Engine.Rendering.Images;

/// <summary>Contains an automatic image-compatibility conversion decision.</summary>
public sealed record PdfImageCompatibilityDecision(
    PdfImageSourceFormat SourceFormat,
    PdfImageCompatibilityTarget Target,
    bool RequiresDecode,
    bool IsLossless,
    bool RequiresUserConfirmation,
    string Message);
