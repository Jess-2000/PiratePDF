namespace BlackFlag.Engine.SDK.Security;

/// <summary>Identifies an SDK-facing source location without exposing engine implementation types.</summary>
public sealed record DocumentSecuritySdkSourceLocation
{
    public int? PageIndex { get; init; }
    public string? LogicalPath { get; init; }
    public long? ByteOffset { get; init; }
    public string? ObjectIdentifier { get; init; }
}
