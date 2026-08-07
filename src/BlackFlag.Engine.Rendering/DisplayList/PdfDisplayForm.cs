using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>Represents one interpreted Form XObject placement.</summary>
public sealed class PdfDisplayForm : PdfDisplayItem
{
    /// <summary>Initializes a Form XObject display item.</summary>
    public PdfDisplayForm(
        PdfResolvedFormXObject form,
        PdfMatrix transform,
        PdfDisplayList content,
        PdfGraphicsStateSnapshot graphicsState,
        PdfContentSourceSpan sourceSpan,
        PdfMarkedContentContext? markedContent)
        : base(graphicsState, sourceSpan, null, markedContent)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(content);
        Form = form;
        Transform = transform;
        Content = content;
    }

    /// <summary>Gets the resolved Form XObject.</summary>
    public PdfResolvedFormXObject Form { get; }

    /// <summary>Gets the effective Form transformation.</summary>
    public PdfMatrix Transform { get; }

    /// <summary>Gets the immutable nested display list.</summary>
    public PdfDisplayList Content { get; }
}
