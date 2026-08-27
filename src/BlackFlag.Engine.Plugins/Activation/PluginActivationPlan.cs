using System.Collections.ObjectModel;
using BlackFlag.Engine.Plugins.Discovery;
using BlackFlag.Engine.SDK.Plugins;
namespace BlackFlag.Engine.Plugins.Activation;

/// <summary>Immutable, non-executing result describing safe plugin activation order and blocked plugins.</summary>
public sealed class PluginActivationPlan
{
    private readonly ReadOnlyCollection<PluginDescriptor> activationOrder;
    private readonly ReadOnlyCollection<PluginDescriptor> blockedPlugins;
    private readonly ReadOnlyCollection<PluginActivationIssue> issues;
    internal PluginActivationPlan(IEnumerable<PluginDescriptor> activationOrder, IEnumerable<PluginDescriptor> blockedPlugins, IEnumerable<PluginActivationIssue> issues)
    {
        this.activationOrder = Array.AsReadOnly(activationOrder.ToArray());
        this.blockedPlugins = Array.AsReadOnly(blockedPlugins.ToArray());
        this.issues = Array.AsReadOnly(issues.ToArray());
    }
    public IReadOnlyList<PluginDescriptor> ActivationOrder => activationOrder;
    public IReadOnlyList<PluginDescriptor> BlockedPlugins => blockedPlugins;
    public IReadOnlyList<PluginActivationIssue> Issues => issues;
    public bool IsBlocked(PluginId id) => blockedPlugins.Any(plugin => plugin.Manifest.Id == id);
}
