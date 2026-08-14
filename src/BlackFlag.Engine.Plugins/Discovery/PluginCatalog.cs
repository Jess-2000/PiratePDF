using BlackFlag.Engine.SDK.Plugins;

namespace BlackFlag.Engine.Plugins.Discovery;

/// <summary>Immutable result of a bounded plugin discovery pass.</summary>
public sealed class PluginCatalog
{
    private readonly PluginDescriptor[] plugins;
    private readonly PluginDiscoveryIssue[] issues;
    private readonly Dictionary<PluginId, PluginDescriptor> byId;

    internal PluginCatalog(IEnumerable<PluginDescriptor> plugins, IEnumerable<PluginDiscoveryIssue> issues)
    {
        this.plugins = plugins.ToArray();
        this.issues = issues.ToArray();
        byId = this.plugins.ToDictionary(static plugin => plugin.Manifest.Id);
    }

    public IReadOnlyList<PluginDescriptor> Plugins => plugins;
    public IReadOnlyList<PluginDiscoveryIssue> Issues => issues;
    public bool TryGetPlugin(PluginId id, out PluginDescriptor? descriptor) => byId.TryGetValue(id, out descriptor);
}
