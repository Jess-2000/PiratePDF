namespace BlackFlag.Engine.SDK.Editing;

/// <summary>Describes a partial SDK typography update. Null values preserve the current state.</summary>
public sealed class PdfExistingTextSdkTypographyUpdate
{
    public double? FontSize { get; init; }
    public double? CharacterSpacing { get; init; }
    public double? WordSpacing { get; init; }
    public double? HorizontalScaling { get; init; }
    public int? TextRenderingMode { get; init; }
    public double? TextRise { get; init; }
}
