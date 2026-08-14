namespace BlackFlag.Engine.SDK.Editing;

/// <summary>
/// Describes a page visible through an SDK editing session.
/// </summary>
public sealed record PdfEditingSdkPage(
    Guid Id,
    double Width,
    double Height,
    PdfEditingSdkPageRotation Rotation,
    bool IsSourcePage,
    int? SourceIndex,
    bool HasContent,
    bool HasResources);
