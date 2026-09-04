namespace BlackFlag.Engine.Fonts.Sanitization;

/// <summary>Bounds resource consumption before external font bytes become trusted.</summary>
public sealed class PdfExternalFontSanitizationOptions
{
    public int MaximumInputBytes { get; init; } = 64 * 1024 * 1024;
    public int MaximumTableCount { get; init; } = 128;
    public int MaximumSingleTableBytes { get; init; } = 32 * 1024 * 1024;
    public int MaximumGlyphCount { get; init; } = 65535;
    public int MaximumCMapSubtableCount { get; init; } = 256;
    public int MaximumNameRecordCount { get; init; } = 4096;

    internal void Validate()
    {
        ValidatePositive(MaximumInputBytes, nameof(MaximumInputBytes));
        ValidatePositive(MaximumTableCount, nameof(MaximumTableCount));
        ValidatePositive(MaximumSingleTableBytes, nameof(MaximumSingleTableBytes));
        ValidatePositive(MaximumGlyphCount, nameof(MaximumGlyphCount));
        ValidatePositive(MaximumCMapSubtableCount, nameof(MaximumCMapSubtableCount));
        ValidatePositive(MaximumNameRecordCount, nameof(MaximumNameRecordCount));
    }

    private static void ValidatePositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, value, "Sanitization limits must be positive.");
        }
    }
}
