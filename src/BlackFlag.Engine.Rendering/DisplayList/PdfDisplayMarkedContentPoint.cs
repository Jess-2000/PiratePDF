using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>Represents an MP or DP marked-content point.</summary>
public sealed class PdfDisplayMarkedContentPoint : PdfDisplayItem
{
    /// <summary>Initializes a marked-content point.</summary>
    public PdfDisplayMarkedContentPoint(
        PdfName tag,
        PdfObject? properties,
        PdfResolvedProperties? resolvedProperties,
        PdfGraphicsStateSnapshot graphicsState,
        PdfContentSourceSpan sourceSpan,
        PdfMarkedContentContext? markedContent)
        : base(graphicsState, sourceSpan, null, markedContent)
    {
        ArgumentNullException.ThrowIfNull(tag);
        Tag = tag;
        Properties = properties;
        ResolvedProperties = resolvedProperties;
    }

    /// <summary>Gets the marked-content tag.</summary>
    public PdfName Tag { get; }

    /// <summary>Gets inline or named property source data when present.</summary>
    public PdfObject? Properties { get; }

    /// <summary>Gets resolved named marked-content properties when present.</summary>
    public PdfResolvedProperties? ResolvedProperties { get; }
}
