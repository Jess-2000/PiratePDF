namespace BlackFlag.Engine.Editing.Structural;

/// <summary>
/// Identifies how a structural value exists in the logical editable document.
/// </summary>
public enum PdfStructuralValueOrigin
{
    /// <summary>The structure has no value at the targeted location.</summary>
    Absent = 0,

    /// <summary>The value is explicitly declared at the targeted location.</summary>
    Explicit = 1,

    /// <summary>The effective value is inherited from an owning ancestor.</summary>
    Inherited = 2,

    /// <summary>The effective value is supplied by a PDF-defined fallback.</summary>
    Defaulted = 3
}
