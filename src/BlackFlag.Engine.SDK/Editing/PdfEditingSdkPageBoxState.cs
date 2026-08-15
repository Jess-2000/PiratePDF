namespace BlackFlag.Engine.SDK.Editing;

/// <summary>Describes the effective value and representation origin of one page box.</summary>
public sealed record PdfEditingSdkPageBoxState(
    PdfEditingSdkStructuralValueOrigin Origin,
    PdfEditingSdkPageBox Value);
