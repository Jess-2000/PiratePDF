namespace BlackFlag.Engine.Rendering.Raster;

/// <summary>Controls how unsupported or malformed rendering content is handled.</summary>
public enum PdfRenderMode
{
    /// <summary>Reports unsupported content and continues where safe.</summary>
    Tolerant,

    /// <summary>Fails when faithful rendering cannot continue.</summary>
    Strict
}
