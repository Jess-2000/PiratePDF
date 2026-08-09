namespace BlackFlag.Engine.Rendering.Fonts;

/// <summary>Describes a nonfatal font-decoding condition.</summary>
public sealed record PdfFontDecodeDiagnostic(string Code, string Message);
