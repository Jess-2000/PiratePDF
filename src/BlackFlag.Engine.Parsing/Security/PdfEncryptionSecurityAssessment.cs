using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Parsing.Security;

/// <summary>Contains the normalized result of PDF encryption-state inspection.</summary>
public sealed class PdfEncryptionSecurityAssessment
{
    /// <summary>Initializes an encryption assessment.</summary>
    public PdfEncryptionSecurityAssessment(
        PdfEncryptionSecurityState state,
        DocumentSecurityObservation? observation)
    {
        State = state;
        Observation = observation;
    }

    /// <summary>Gets the safe opening state.</summary>
    public PdfEncryptionSecurityState State { get; }

    /// <summary>Gets the normalized security observation, when one is required.</summary>
    public DocumentSecurityObservation? Observation { get; }

    /// <summary>Gets whether protected content must remain inaccessible.</summary>
    public bool BlocksProtectedContent =>
        State != PdfEncryptionSecurityState.None;
}
