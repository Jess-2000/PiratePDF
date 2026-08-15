namespace BlackFlag.Engine.Document.Annotations;

/// <summary>Represents common PDF annotation behavior options encoded by the PDF annotation flags bitmask.</summary>
[Flags]
public enum PdfAnnotationOptions
{
    None = 0,
    Invisible = 1 << 0,
    Hidden = 1 << 1,
    Print = 1 << 2,
    NoZoom = 1 << 3,
    NoRotate = 1 << 4,
    NoView = 1 << 5,
    ReadOnly = 1 << 6,
    Locked = 1 << 7,
    ToggleNoView = 1 << 8,
    LockedContents = 1 << 9
}
