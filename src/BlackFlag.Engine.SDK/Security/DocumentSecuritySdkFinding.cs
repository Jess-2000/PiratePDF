namespace BlackFlag.Engine.SDK.Security;

/// <summary>Represents one immutable SDK-facing document security finding.</summary>
public sealed record DocumentSecuritySdkFinding(
    string Kind,
    string Description,
    string SourceFormat,
    string Severity,
    string Action,
    string? LogicalPath);
