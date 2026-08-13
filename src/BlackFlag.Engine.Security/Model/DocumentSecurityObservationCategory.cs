
namespace BlackFlag.Engine.Security.Model;

/// <summary>
/// Describes a format-independent category of security-relevant document
/// content or document state.
/// </summary>
public enum DocumentSecurityObservationCategory
{
    /// <summary>Executable or potentially executable document behavior.</summary>
    ActiveContent,

    /// <summary>Script or macro content such as JavaScript or VBA.</summary>
    ScriptContent,

    /// <summary>Content embedded inside a document or document container.</summary>
    EmbeddedContent,

    /// <summary>A reference to content outside the current document.</summary>
    ExternalReference,

    /// <summary>Behavior configured to run automatically on an event.</summary>
    AutomaticAction,

    /// <summary>Executable binary content or an executable payload.</summary>
    ExecutableContent,

    /// <summary>Interactive content such as forms, media, or interactive annotations.</summary>
    InteractiveContent,

    /// <summary>A structurally valid but security-relevant relationship between parts.</summary>
    SuspiciousRelationship,

    /// <summary>Structural damage, inconsistency, or malformed document state.</summary>
    StructuralAnomaly,

    /// <summary>Password, encryption, or authentication state.</summary>
    EncryptionCondition
}
