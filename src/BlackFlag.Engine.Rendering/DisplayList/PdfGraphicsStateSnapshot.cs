using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>
/// Represents an immutable snapshot of the interpreted PDF graphics state.
/// </summary>
public sealed record PdfGraphicsStateSnapshot
{
    /// <summary>Gets or initializes the current transformation matrix.</summary>
    public PdfMatrix Transform { get; init; } = PdfMatrix.Identity;

    /// <summary>Gets or initializes the line width.</summary>
    public double LineWidth { get; init; } = 1;

    /// <summary>Gets or initializes the line-cap style.</summary>
    public PdfLineCapStyle LineCap { get; init; } =
        PdfLineCapStyle.Butt;

    /// <summary>Gets or initializes the line-join style.</summary>
    public PdfLineJoinStyle LineJoin { get; init; } =
        PdfLineJoinStyle.Miter;

    /// <summary>Gets or initializes the miter limit.</summary>
    public double MiterLimit { get; init; } = 10;

    /// <summary>Gets or initializes the current dash pattern.</summary>
    public PdfDashPattern DashPattern { get; init; } =
        new([], 0);

    /// <summary>Gets or initializes the current rendering intent.</summary>
    public string? RenderingIntent { get; init; }

    /// <summary>Gets or initializes the current flatness tolerance.</summary>
    public double Flatness { get; init; } = 1;

    /// <summary>Gets or initializes the stroking color.</summary>
    public PdfColorValue StrokeColor { get; init; } =
        new("DeviceGray", [0]);

    /// <summary>Gets or initializes the nonstroking color.</summary>
    public PdfColorValue FillColor { get; init; } =
        new("DeviceGray", [0]);

    /// <summary>Gets or initializes stroking alpha.</summary>
    public double StrokingAlpha { get; init; } = 1;

    /// <summary>Gets or initializes nonstroking alpha.</summary>
    public double NonstrokingAlpha { get; init; } = 1;

    /// <summary>Gets or initializes the stroking overprint flag.</summary>
    public bool OverprintStroke { get; init; }

    /// <summary>Gets or initializes the nonstroking overprint flag.</summary>
    public bool OverprintFill { get; init; }

    /// <summary>Gets or initializes the overprint mode.</summary>
    public int OverprintMode { get; init; }

    /// <summary>Gets or initializes the blend-mode source value.</summary>
    public BlackFlag.Engine.Objects.PdfObject? BlendMode { get; init; }

    /// <summary>Gets or initializes the soft-mask source value.</summary>
    public BlackFlag.Engine.Objects.PdfObject? SoftMask { get; init; }

    /// <summary>Gets or initializes the active font resource.</summary>
    public PdfResolvedFont? Font { get; init; }

    /// <summary>Gets or initializes the active font size.</summary>
    public double FontSize { get; init; }

    /// <summary>Gets or initializes character spacing.</summary>
    public double CharacterSpacing { get; init; }

    /// <summary>Gets or initializes word spacing.</summary>
    public double WordSpacing { get; init; }

    /// <summary>Gets or initializes horizontal scaling as a percentage.</summary>
    public double HorizontalScaling { get; init; } = 100;

    /// <summary>Gets or initializes text leading.</summary>
    public double Leading { get; init; }

    /// <summary>Gets or initializes the PDF text rendering mode.</summary>
    public int TextRenderingMode { get; init; }

    /// <summary>Gets or initializes text rise.</summary>
    public double TextRise { get; init; }

    /// <summary>Gets or initializes the current text matrix.</summary>
    public PdfMatrix TextMatrix { get; init; } =
        PdfMatrix.Identity;

    /// <summary>Gets or initializes the current text line matrix.</summary>
    public PdfMatrix TextLineMatrix { get; init; } =
        PdfMatrix.Identity;

    /// <summary>Gets or initializes the active immutable clipping state.</summary>
    public PdfClipState? Clip { get; init; }
}