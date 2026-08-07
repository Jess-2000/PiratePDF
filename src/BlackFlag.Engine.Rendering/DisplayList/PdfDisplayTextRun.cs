using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>
/// Represents an interpreted text run while preserving ordered source text and
/// TJ adjustments needed for rendering, extraction, selection, and future font
/// replacement.
/// </summary>
public sealed class PdfDisplayTextRun : PdfDisplayItem
{
    private readonly PdfTextRunElement[] elements;

    /// <summary>Initializes a text-run display item.</summary>
    public PdfDisplayTextRun(
        PdfResolvedFont font,
        double fontSize,
        IEnumerable<PdfTextRunElement> elements,
        PdfMatrix textMatrix,
        PdfGraphicsStateSnapshot graphicsState,
        PdfContentSourceSpan sourceSpan,
        PdfMarkedContentContext? markedContent)
        : base(graphicsState, sourceSpan, null, markedContent)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(elements);

        Font = font;
        FontSize = fontSize;
        this.elements = elements.ToArray();
        TextMatrix = textMatrix;
    }

    /// <summary>Gets the resolved font resource.</summary>
    public PdfResolvedFont Font { get; }

    /// <summary>Gets the font size.</summary>
    public double FontSize { get; }

    /// <summary>Gets ordered text and positioning elements.</summary>
    public IReadOnlyList<PdfTextRunElement> Elements => elements;

    /// <summary>Gets the effective text matrix at the start of the run.</summary>
    public PdfMatrix TextMatrix { get; }

    /// <summary>Gets all encoded text bytes concatenated in source order.</summary>
    public ReadOnlyMemory<byte> EncodedBytes =>
        elements
            .Where(static element => element.Kind == PdfTextRunElementKind.Text)
            .SelectMany(static element => element.EncodedBytes.ToArray())
            .ToArray();

    /// <summary>
    /// Gets directly derivable Unicode text when every text element has such a
    /// representation; otherwise returns <see langword="null"/>.
    /// </summary>
    public string? UnicodeText
    {
        get
        {
            PdfTextRunElement[] textElements = elements
                .Where(static element => element.Kind == PdfTextRunElementKind.Text)
                .ToArray();

            if (textElements.Any(static element => element.UnicodeText is null))
            {
                return null;
            }

            return string.Concat(textElements.Select(static element => element.UnicodeText));
        }
    }
}
