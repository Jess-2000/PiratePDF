using System.Collections.ObjectModel;

namespace BlackFlag.Engine.SDK.Editing;

public enum PdfExistingTextSdkScriptStyle
{
    Normal = 0,
    Superscript = 1,
    Subscript = 2
}

public enum PdfExistingTextSdkColorSpace
{
    DeviceGray = 0,
    DeviceRgb = 1,
    DeviceCmyk = 2
}

public enum PdfExistingTextSdkDecorationKind
{
    Underline = 0,
    Strikeout = 1
}

/// <summary>SDK-owned device-space color value.</summary>
public sealed class PdfExistingTextSdkColor
{
    private readonly ReadOnlyCollection<double> components;

    public PdfExistingTextSdkColor(
        PdfExistingTextSdkColorSpace colorSpace,
        IEnumerable<double> components)
    {
        ArgumentNullException.ThrowIfNull(components);
        ColorSpace = colorSpace;
        this.components = Array.AsReadOnly(components.ToArray());
    }

    public PdfExistingTextSdkColorSpace ColorSpace { get; }
    public IReadOnlyList<double> Components => components;
}

/// <summary>SDK-owned local underline or strikeout description.</summary>
public sealed class PdfExistingTextSdkDecoration(
    PdfExistingTextSdkDecorationKind kind,
    double? advanceWidth = null,
    double? offset = null,
    double? thickness = null)
{
    public PdfExistingTextSdkDecorationKind Kind { get; } = kind;
    public double? AdvanceWidth { get; } = advanceWidth;
    public double? Offset { get; } = offset;
    public double? Thickness { get; } = thickness;
    public bool IsAutomatic => !AdvanceWidth.HasValue;
}

/// <summary>Describes higher-level SDK text styling.</summary>
public sealed class PdfExistingTextSdkStyleUpdate
{
    public PdfExistingTextSdkScriptStyle? ScriptStyle { get; init; }
    public double SuperscriptScale { get; init; } = 0.65;
    public double SuperscriptRise { get; init; } = 0.35;
    public double SubscriptScale { get; init; } = 0.65;
    public double SubscriptDrop { get; init; } = 0.20;
    public PdfExistingTextSdkColor? FillColor { get; init; }
    public PdfExistingTextSdkColor? StrokeColor { get; init; }
    public PdfExistingTextSdkDecoration? Decoration { get; init; }
}
