namespace BlackFlag.Engine.Editing.Structural;

/// <summary>
/// Identifies the structured PDF feature targeted by a structural editing
/// operation.
/// </summary>
public enum PdfStructureKind
{
    /// <summary>Page geometry, page boxes, rotation, crop, or resize state.</summary>
    PageGeometry = 0,

    /// <summary>Document information or metadata state.</summary>
    Metadata = 1,

    /// <summary>Page annotation state.</summary>
    Annotation = 2,

    /// <summary>Interactive form or field state.</summary>
    Form = 3,

    /// <summary>Document outline, bookmark, or navigation-tree state.</summary>
    Outline = 4,

    /// <summary>Generated header, footer, or page-decoration state.</summary>
    PageDecoration = 5
}
