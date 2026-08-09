namespace BlackFlag.Engine.Rendering.Images;

/// <summary>Describes a recoverable image decoding condition.</summary>
public sealed record PdfImageDecodeDiagnostic(string Code, string Message);
