using BlackFlag.Engine.SDK.Plugins;
using BlackFlag.Engine.Security.Plugins;

namespace BlackFlag.Engine.Plugins.Security;

internal static class PluginSecurityAuthorization
{
    public static void Demand(IPluginSecurityPolicy policy, PluginId pluginId, PluginSecurityOperation operation, string resourceId, PluginSecuritySensitivity sensitivity)
    {
        ArgumentNullException.ThrowIfNull(policy); ArgumentNullException.ThrowIfNull(pluginId);
        PluginSecurityDecision decision = policy.Evaluate(new(pluginId.Value, operation, resourceId, sensitivity));
        if (!decision.Allowed) throw new PluginSecurityPolicyDeniedException(decision.Message);
    }
}
