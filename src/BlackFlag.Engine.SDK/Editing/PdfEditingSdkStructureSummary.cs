namespace BlackFlag.Engine.SDK.Editing;

/// <summary>Summarizes structural editing state without exposing mutable engine objects.</summary>
public sealed record PdfEditingSdkStructureSummary(
    int PageCount,
    int AnnotationCount,
    int RootFormFieldCount,
    int TotalFormFieldCount,
    int RootOutlineItemCount,
    int TotalOutlineItemCount,
    int PageDecorationCount,
    int ExplicitDocumentInformationCount);
