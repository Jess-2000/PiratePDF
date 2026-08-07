namespace BlackFlag.Engine.Rendering.Raster;

/// <summary>Identifies renderer quality policy without exposing backend-specific knobs.</summary>
public enum PdfRenderQuality
{
    /// <summary>Favors interactive speed.</summary>
    Draft,

    /// <summary>Balances quality and speed.</summary>
    Normal,

    /// <summary>Favors output fidelity.</summary>
    High
}
