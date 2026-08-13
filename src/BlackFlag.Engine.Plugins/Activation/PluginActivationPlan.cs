using BlackFlag.Engine.Plugins.Discovery;
using BlackFlag.Engine.SDK.Plugins;
namespace BlackFlag.Engine.Plugins.Activation;

/// <summary>Immutable, non-executing result describing safe plugin activation order and blocked plugins.</summary>
public sealed class PluginActivationPlan
{
    private readonly PluginDescriptor[] activationOrder;
    private readonly PluginDescriptor[] blockedPlugins;
    private readonly PluginActivationIssue[] issues;
    internal PluginActivationPlan(IEnumerable<PluginDescriptor> activationOrder, IEnumerable<PluginDescriptor> blockedPlugins, IEnumerable<PluginActivationIssue> issues)
    {
        this.activationOrder = activationOrder.ToArray(); this.blockedPlugins = blockedPlugins.ToArray(); this.issues = issues.ToArray();
    }
    public IReadOnlyList<PluginDescriptor> ActivationOrder => activationOrder;
    public IReadOnlyList<PluginDescriptor> BlockedPlugins => blockedPlugins;
    public IReadOnlyList<PluginActivationIssue> Issues => issues;
    public bool IsBlocked(PluginId id) => blockedPlugins.Any(plugin => plugin.Manifest.Id == id);
}
