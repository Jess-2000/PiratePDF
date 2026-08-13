namespace BlackFlag.Engine.Parsing.Security;

/// <summary>Describes the safe opening state established from PDF encryption metadata.</summary>
public enum PdfEncryptionSecurityState
{
    /// <summary>The document does not declare encryption.</summary>
    None,

    /// <summary>Structurally valid Standard security metadata requires authentication.</summary>
    PasswordRequired,

    /// <summary>Encryption metadata is damaged, unsupported, or cannot be trusted.</summary>
    Damaged
}
