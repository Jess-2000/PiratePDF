namespace BlackFlag.Engine.SDK.Editing;

/// <summary>Describes a normalized page rectangle in PDF user-space units.</summary>
public readonly record struct PdfEditingSdkPageBox(
    double Left,
    double Bottom,
    double Right,
    double Top);
