
namespace BlackFlag.Engine.Security.Policy;

/// <summary>Describes the significance of a security finding.</summary>
public enum DocumentSecuritySeverity
{
    /// <summary>Informational content that should be recorded when reporting is warranted.</summary>
    Information,

    /// <summary>Content that deserves awareness but is not inherently dangerous.</summary>
    Notice,

    /// <summary>Content that may require protection or sanitization depending on the operation.</summary>
    Warning,

    /// <summary>A condition that prevents a requested operation from proceeding safely.</summary>
    Blocking
}
