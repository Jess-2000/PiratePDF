
namespace BlackFlag.Engine.Parsing.Recovery;

/// <summary>Configures structural recovery for damaged PDFs.</summary>
public sealed record PdfRecoveryOptions
{
    /// <summary>Gets production defaults.</summary>
    public static PdfRecoveryOptions Default { get; } = new();

    /// <summary>Gets the recovery mode.</summary>
    public PdfRecoveryMode Mode { get; init; } = PdfRecoveryMode.Automatic;

    /// <summary>Gets bounded recovery limits.</summary>
    public PdfRecoveryLimits Limits { get; init; } = PdfRecoveryLimits.Default;

    /// <summary>Validates the options.</summary>
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Limits);
        Limits.Validate();
    }
}
