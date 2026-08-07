namespace BlackFlag.Engine.Rendering.DisplayList;

/// <summary>
/// Preserves one ordered text or positioning element from a PDF text-showing
/// operation.
/// </summary>
public sealed class PdfTextRunElement
{
    private readonly byte[] encodedBytes;

    private PdfTextRunElement(
        PdfTextRunElementKind kind,
        ReadOnlySpan<byte> encodedBytes,
        string? unicodeText,
        double adjustment)
    {
        Kind = kind;
        this.encodedBytes = encodedBytes.ToArray();
        UnicodeText = unicodeText;
        Adjustment = adjustment;
    }

    /// <summary>Gets the element kind.</summary>
    public PdfTextRunElementKind Kind { get; }

    /// <summary>Gets encoded source bytes for a text element.</summary>
    public ReadOnlyMemory<byte> EncodedBytes => encodedBytes;

    /// <summary>Gets directly derivable Unicode text when available.</summary>
    public string? UnicodeText { get; }

    /// <summary>Gets the PDF TJ positioning adjustment for an adjustment element.</summary>
    public double Adjustment { get; }

    /// <summary>Creates a text element.</summary>
    public static PdfTextRunElement CreateText(
        ReadOnlySpan<byte> encodedBytes,
        string? unicodeText = null) =>
        new(PdfTextRunElementKind.Text, encodedBytes, unicodeText, 0);

    /// <summary>Creates a positioning-adjustment element.</summary>
    public static PdfTextRunElement CreateAdjustment(double adjustment) =>
        new(PdfTextRunElementKind.Adjustment, ReadOnlySpan<byte>.Empty, null, adjustment);
}
