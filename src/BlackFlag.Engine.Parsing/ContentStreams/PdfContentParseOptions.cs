namespace BlackFlag.Engine.Parsing.ContentStreams;

/// <summary>Configures bounded PDF content-stream parsing.</summary>
public sealed class PdfContentParseOptions
{
    /// <summary>Initializes safe default content limits.</summary>
    public PdfContentParseOptions(
        bool allowUnknownOperators = false,
        bool validateGraphicsStateBalance = true,
        int maximumOperandCount = 4096,
        int maximumOperationCount = 1_000_000,
        int maximumGraphicsStateDepth = 1024,
        int maximumInlineImageDictionaryEntries = 256,
        int maximumInlineImageBytes = 256 * 1024 * 1024)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumOperandCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumOperationCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumGraphicsStateDepth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumInlineImageDictionaryEntries);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumInlineImageBytes);

        AllowUnknownOperators = allowUnknownOperators;
        ValidateGraphicsStateBalance = validateGraphicsStateBalance;
        MaximumOperandCount = maximumOperandCount;
        MaximumOperationCount = maximumOperationCount;
        MaximumGraphicsStateDepth = maximumGraphicsStateDepth;
        MaximumInlineImageDictionaryEntries = maximumInlineImageDictionaryEntries;
        MaximumInlineImageBytes = maximumInlineImageBytes;
    }

    /// <summary>Gets whether unknown operators are preserved.</summary>
    public bool AllowUnknownOperators { get; }
    /// <summary>Gets whether q/Q nesting is validated.</summary>
    public bool ValidateGraphicsStateBalance { get; }
    /// <summary>Gets the maximum pending operand count.</summary>
    public int MaximumOperandCount { get; }
    /// <summary>Gets the maximum operation count per stream.</summary>
    public int MaximumOperationCount { get; }
    /// <summary>Gets the maximum validated graphics-state depth.</summary>
    public int MaximumGraphicsStateDepth { get; }
    /// <summary>Gets the maximum inline-image dictionary entry count.</summary>
    public int MaximumInlineImageDictionaryEntries { get; }
    /// <summary>Gets the maximum encoded inline-image byte count.</summary>
    public int MaximumInlineImageBytes { get; }
}
