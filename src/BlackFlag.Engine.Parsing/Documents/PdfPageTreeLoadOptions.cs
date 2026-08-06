namespace BlackFlag.Engine.Parsing.Documents;

/// <summary>Controls defensive limits applied while loading a PDF page tree.</summary>
public sealed class PdfPageTreeLoadOptions
{
    public PdfPageTreeLoadOptions(
        int maximumDepth = 256,
        int maximumPages = 1_000_000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumDepth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumPages);
        MaximumDepth = maximumDepth;
        MaximumPages = maximumPages;
    }

    public int MaximumDepth { get; }
    public int MaximumPages { get; }
}
