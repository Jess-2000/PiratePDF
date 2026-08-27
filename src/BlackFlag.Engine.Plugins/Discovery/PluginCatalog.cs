using System.Collections.ObjectModel;
using BlackFlag.Engine.SDK.Plugins;

namespace BlackFlag.Engine.Plugins.Discovery;

/// <summary>Immutable result of a bounded plugin discovery pass.</summary>
public sealed class PluginCatalog
{
    private readonly ReadOnlyCollection<PluginDescriptor> plugins;
    private readonly ReadOnlyCollection<PluginDiscoveryIssue> issues;
    private readonly Dictionary<PluginId, PluginDescriptor> byId;

    internal PluginCatalog(IEnumerable<PluginDescriptor> plugins, IEnumerable<PluginDiscoveryIssue> issues)
    {
        PluginDescriptor[] pluginSnapshot = plugins.ToArray();
        PluginDiscoveryIssue[] issueSnapshot = issues.ToArray();
        this.plugins = Array.AsReadOnly(pluginSnapshot);
        this.issues = Array.AsReadOnly(issueSnapshot);
        byId = pluginSnapshot.ToDictionary(static plugin => plugin.Manifest.Id);
    }

    public IReadOnlyList<PluginDescriptor> Plugins => plugins;
    public IReadOnlyList<PluginDiscoveryIssue> Issues => issues;
    public bool TryGetPlugin(PluginId id, out PluginDescriptor? descriptor) => byId.TryGetValue(id, out descriptor);
}
