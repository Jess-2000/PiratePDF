
namespace BlackFlag.Engine.Security.Policy;

/// <summary>Identifies the operation for which security policy is being evaluated.</summary>
public enum DocumentSecurityOperation
{
    /// <summary>Opening a document for ordinary viewing or editing.</summary>
    Open,

    /// <summary>Performing a user-requested manual security inspection.</summary>
    ManualInspection,

    /// <summary>Converting a supported source document through passive conversion.</summary>
    PassiveConversion,

    /// <summary>Explicitly sanitizing a document.</summary>
    Sanitize
}
