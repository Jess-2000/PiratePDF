
namespace BlackFlag.Engine.Parsing.Recovery;

/// <summary>Describes encryption state discovered during structural recovery.</summary>
public enum PdfRecoveredEncryptionState
{
    /// <summary>No encryption entry was present in the recovered trailer.</summary>
    None,

    /// <summary>Structurally valid Standard security metadata requires authentication.</summary>
    AuthenticationRequired,

    /// <summary>Encryption metadata is damaged or cannot be trusted.</summary>
    Damaged
}
