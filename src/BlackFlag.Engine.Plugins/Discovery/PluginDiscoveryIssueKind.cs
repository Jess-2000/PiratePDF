namespace BlackFlag.Engine.Plugins.Discovery;

public enum PluginDiscoveryIssueKind
{
    RootUnavailable,
    RootLimitExceeded,
    PackageLimitExceeded,
    ManifestUnreadable,
    ManifestInvalid,
    DuplicatePluginId
}
