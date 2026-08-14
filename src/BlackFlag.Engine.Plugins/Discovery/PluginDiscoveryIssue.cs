namespace BlackFlag.Engine.Plugins.Discovery;

/// <summary>Describes a non-fatal discovery problem without exposing an exception object.</summary>
public sealed record PluginDiscoveryIssue(PluginDiscoveryIssueKind Kind, string Path, string Message);
