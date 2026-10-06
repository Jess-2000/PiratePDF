namespace BlackFlag.Engine.SDK.Editing;

/// <summary>Immutable SDK snapshot of interpreted PDF text state.</summary>
public sealed record PdfExistingTextSdkTypography(
    double FontSize,
    double CharacterSpacing,
    double WordSpacing,
    double HorizontalScaling,
    int TextRenderingMode,
    double TextRise);
