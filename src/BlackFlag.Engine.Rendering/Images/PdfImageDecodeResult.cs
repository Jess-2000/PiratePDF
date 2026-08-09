namespace BlackFlag.Engine.Rendering.Images;

/// <summary>Contains a decoded image and its diagnostics.</summary>
public sealed record PdfImageDecodeResult(
    PdfDecodedImage Image,
    IReadOnlyList<PdfImageDecodeDiagnostic> Diagnostics);
