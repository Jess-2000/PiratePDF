namespace BlackFlag.Engine.Document.Outlines;

/// <summary>Identifies a navigation action represented by an outline item.</summary>
public enum PdfOutlineNavigationActionType
{
    /// <summary>Navigate to a URI target.</summary>
    Uri = 0,

    /// <summary>Invoke a named PDF navigation action.</summary>
    Named = 1
}
