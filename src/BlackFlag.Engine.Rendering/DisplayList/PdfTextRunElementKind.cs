namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>Identifies the kind of one ordered text-run element.</summary>
public enum PdfTextRunElementKind
{
    /// <summary>The element contains encoded text bytes.</summary>
    Text,

    /// <summary>The element contains a TJ numeric positioning adjustment.</summary>
    Adjustment
}
