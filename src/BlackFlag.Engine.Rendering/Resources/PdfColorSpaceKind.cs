namespace BlackFlag.Engine.Rendering.Resources;

/// <summary>
/// Identifies a canonical PDF color-space family.
/// </summary>
public enum PdfColorSpaceKind
{
    /// <summary>An unrecognized color space.</summary>
    Unknown,

    /// <summary>DeviceGray.</summary>
    DeviceGray,

    /// <summary>DeviceRGB.</summary>
    DeviceRgb,

    /// <summary>DeviceCMYK.</summary>
    DeviceCmyk,

    /// <summary>CalGray.</summary>
    CalGray,

    /// <summary>CalRGB.</summary>
    CalRgb,

    /// <summary>Lab.</summary>
    Lab,

    /// <summary>ICCBased.</summary>
    IccBased,

    /// <summary>Indexed.</summary>
    Indexed,

    /// <summary>Pattern.</summary>
    Pattern,

    /// <summary>Separation.</summary>
    Separation,

    /// <summary>DeviceN.</summary>
    DeviceN
}
