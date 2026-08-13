namespace BlackFlag.Engine.Parsing.Security;

/// <summary>Contains the result of a Standard security-handler password attempt.</summary>
public sealed class PdfStandardSecurityAuthenticationResult
{
    internal PdfStandardSecurityAuthenticationResult(
        PdfPasswordAuthenticationKind kind,
        PdfStandardSecuritySession? session,
        string? failureReason)
    {
        Kind = kind;
        Session = session;
        FailureReason = failureReason;
    }

    /// <summary>Gets the authentication outcome.</summary>
    public PdfPasswordAuthenticationKind Kind { get; }

    /// <summary>Gets the authenticated decryption session, or null when authentication failed.</summary>
    public PdfStandardSecuritySession? Session { get; }

    /// <summary>Gets a non-secret failure reason when authentication could not proceed.</summary>
    public string? FailureReason { get; }

    /// <summary>Gets whether a user or owner credential authenticated.</summary>
    public bool IsAuthenticated => Session is not null;
}
