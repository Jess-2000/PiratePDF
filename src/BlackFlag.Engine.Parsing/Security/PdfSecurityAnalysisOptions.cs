
namespace BlackFlag.Engine.Parsing.Security;

/// <summary>Configures bounded PDF security observation discovery.</summary>
public sealed record PdfSecurityAnalysisOptions
{
    /// <summary>Gets production defaults.</summary>
    public static PdfSecurityAnalysisOptions Default { get; } = new();

    /// <summary>Gets the maximum resolved indirect objects visited.</summary>
    public int MaximumResolvedObjects { get; init; } = 250_000;

    /// <summary>Gets the maximum graph depth traversed through PDF objects.</summary>
    public int MaximumObjectGraphDepth { get; init; } = 256;

    /// <summary>Validates configured PDF analysis limits.</summary>
    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumResolvedObjects);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumObjectGraphDepth);
    }
}
