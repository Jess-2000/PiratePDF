namespace BlackFlag.Engine.SDK.Security;

/// <summary>Identifies an SDK-facing document operation requiring security policy evaluation.</summary>
public enum DocumentSecuritySdkOperation
{
    Open,
    ManualInspection,
    PassiveConversion,
    Sanitize
}
