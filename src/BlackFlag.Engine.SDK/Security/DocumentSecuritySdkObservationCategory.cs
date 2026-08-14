namespace BlackFlag.Engine.SDK.Security;

/// <summary>Describes a stable SDK-facing category of security-relevant document state.</summary>
public enum DocumentSecuritySdkObservationCategory
{
    ActiveContent,
    ScriptContent,
    EmbeddedContent,
    ExternalReference,
    AutomaticAction,
    ExecutableContent,
    InteractiveContent,
    SuspiciousRelationship,
    StructuralAnomaly,
    EncryptionCondition
}
