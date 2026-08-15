namespace BlackFlag.Engine.Document.Decorations;

/// <summary>Describes logical numbering semantics for a page-number decoration.</summary>
public sealed record PdfPageNumberingOptions
{
    public PdfPageNumberingOptions(
        int firstDisplayedNumber,
        PdfPageNumberFormat numberFormat = PdfPageNumberFormat.Arabic,
        string prefix = "",
        string suffix = "")
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(firstDisplayedNumber, 1);

        if (!Enum.IsDefined(numberFormat))
        {
            throw new ArgumentOutOfRangeException(nameof(numberFormat));
        }

        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(suffix);

        FirstDisplayedNumber = firstDisplayedNumber;
        NumberFormat = numberFormat;
        Prefix = prefix;
        Suffix = suffix;
    }

    /// <summary>Gets the number assigned to the first page in the target range.</summary>
    public int FirstDisplayedNumber { get; }

    /// <summary>Gets the requested number format.</summary>
    public PdfPageNumberFormat NumberFormat { get; }

    /// <summary>Gets literal text placed before the formatted number.</summary>
    public string Prefix { get; }

    /// <summary>Gets literal text placed after the formatted number.</summary>
    public string Suffix { get; }
}
