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
        if (!Enum.IsDefined(colorSpace))
        {
            throw new ArgumentOutOfRangeException(nameof(colorSpace));
        }

        double[] values = [.. components];
        int expected = colorSpace switch
        {
            PdfExistingTextSdkColorSpace.DeviceGray => 1,
            PdfExistingTextSdkColorSpace.DeviceRgb => 3,
            PdfExistingTextSdkColorSpace.DeviceCmyk => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(colorSpace))
        };

        if (values.Length != expected)
        {
            throw new ArgumentException(
                $"The selected device color space requires {expected} components.",
                nameof(components));
        }

        for (int index = 0; index < values.Length; index++)
        {
            double value = values[index];
            if (!double.IsFinite(value) || value is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(components),
                    value,
                    "Device color components must be finite values from 0 through 1.");
            }
        }

        ColorSpace = colorSpace;
        this.components = Array.AsReadOnly(values);
    }

    public PdfExistingTextSdkColorSpace ColorSpace { get; }
    public IReadOnlyList<double> Components => components;
}

/// <summary>SDK-owned local underline or strikeout description.</summary>
public sealed class PdfExistingTextSdkDecoration
{
    public PdfExistingTextSdkDecoration(
        PdfExistingTextSdkDecorationKind kind,
        double? advanceWidth = null,
        double? offset = null,
        double? thickness = null)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (advanceWidth.HasValue &&
            (!double.IsFinite(advanceWidth.Value) || advanceWidth.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(advanceWidth));
        }

        if (offset.HasValue && !double.IsFinite(offset.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (thickness.HasValue &&
            (!double.IsFinite(thickness.Value) || thickness.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(thickness));
        }

        Kind = kind;
        AdvanceWidth = advanceWidth;
        Offset = offset;
        Thickness = thickness;
    }

    public PdfExistingTextSdkDecorationKind Kind { get; }
    public double? AdvanceWidth { get; }
    public double? Offset { get; }
    public double? Thickness { get; }
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
