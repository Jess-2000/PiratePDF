using BlackFlag.Engine.Editing.Text;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Describes one logical find match backed by an immutable existing-text
/// descriptor and, when applicable, one positioned TJ text segment.
/// </summary>
public sealed class PdfExistingTextSearchMatch
{
    internal PdfExistingTextSearchMatch(
        int scopeIndex,
        PdfExistingTextDescriptor descriptor,
        int textElementIndex,
        int textOffset,
        int textLength,
        string matchedText)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(scopeIndex);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentOutOfRangeException.ThrowIfNegative(textElementIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(textOffset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(textLength);
        ArgumentException.ThrowIfNullOrEmpty(matchedText);

        ScopeIndex = scopeIndex;
        Descriptor = descriptor;
        TextElementIndex = textElementIndex;
        TextOffset = textOffset;
        TextLength = textLength;
        MatchedText = matchedText;
    }

    /// <summary>Gets the zero-based index of the supplied search scope.</summary>
    public int ScopeIndex { get; }

    /// <summary>Gets the provenance-backed target containing this match.</summary>
    public PdfExistingTextDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the zero-based text-element index within the display run. Direct
    /// one-string text uses element index zero.
    /// </summary>
    public int TextElementIndex { get; }

    /// <summary>Gets the UTF-16 offset within the matched text element.</summary>
    public int TextOffset { get; }

    /// <summary>Gets the UTF-16 length of the logical match.</summary>
    public int TextLength { get; }

    /// <summary>Gets the matched Unicode text snapshot.</summary>
    public string MatchedText { get; }
}
