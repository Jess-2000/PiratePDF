using System.Collections.ObjectModel;

namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>
/// Represents one immutable clipping node with optional ancestor clipping.
/// A node is defined either by interpreted path geometry or by accumulated text
/// runs whose glyph outlines become active as a clip at ET.
/// </summary>
public sealed class PdfClipState
{
    private readonly ReadOnlyCollection<PdfDisplayTextRun> textRuns;

    /// <summary>Initializes a path clipping state.</summary>
    public PdfClipState(
        PdfPathGeometry path,
        PdfFillRule fillRule,
        PdfClipState? parent)
    {
        ArgumentNullException.ThrowIfNull(path);

        Kind = PdfClipKind.Path;
        Path = path;
        FillRule = fillRule;
        textRuns = Array.AsReadOnly(Array.Empty<PdfDisplayTextRun>());
        Parent = parent;
        Depth = parent is null ? 1 : checked(parent.Depth + 1);
    }

    /// <summary>Initializes a text-outline clipping state.</summary>
    public PdfClipState(
        IEnumerable<PdfDisplayTextRun> textRuns,
        PdfClipState? parent)
    {
        ArgumentNullException.ThrowIfNull(textRuns);

        this.textRuns = Array.AsReadOnly(textRuns.ToArray());
        if (this.textRuns.Count == 0)
        {
            throw new ArgumentException(
                "A text clipping state requires at least one text run.",
                nameof(textRuns));
        }

        Kind = PdfClipKind.Text;
        FillRule = PdfFillRule.Nonzero;
        Parent = parent;
        Depth = parent is null ? 1 : checked(parent.Depth + 1);
    }

    /// <summary>Gets the clipping node kind.</summary>
    public PdfClipKind Kind { get; }

    /// <summary>Gets path geometry for a path clipping node.</summary>
    public PdfPathGeometry? Path { get; }

    /// <summary>Gets the clipping fill rule.</summary>
    public PdfFillRule FillRule { get; }

    /// <summary>Gets text runs for a text-outline clipping node.</summary>
    public IReadOnlyList<PdfDisplayTextRun> TextRuns => textRuns;

    /// <summary>Gets the preceding clipping state.</summary>
    public PdfClipState? Parent { get; }

    /// <summary>Gets the one-based clipping depth.</summary>
    public int Depth { get; }
}
