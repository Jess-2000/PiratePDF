using BlackFlag.Engine.Plugins.Security;
using BlackFlag.Engine.SDK.Plugins;
using BlackFlag.Engine.SDK.Plugins.Capabilities;
using BlackFlag.Engine.Security.Plugins;

namespace BlackFlag.Engine.Plugins.Capabilities;

internal sealed class PluginCapabilityAccess : IPluginCapabilityAccess
{
    private readonly PluginCapabilityRegistry registry;
    private readonly IPluginSecurityPolicy securityPolicy;
    private readonly PluginId pluginId;
    private readonly HashSet<string> declared;

    public PluginCapabilityAccess(PluginCapabilityRegistry registry, IPluginSecurityPolicy securityPolicy, PluginManifest manifest)
    {
        this.registry = registry; this.securityPolicy = securityPolicy; pluginId = manifest.Id;
        declared = manifest.RequiredCapabilities.Concat(manifest.OptionalCapabilities).Select(static c => c.Value).ToHashSet(StringComparer.Ordinal);
    }

    public T? Retrieve<T>(PluginCapabilityKey<T> key) where T : class, IPluginCapability
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!declared.Contains(key.Id)) throw new InvalidOperationException($"Capability '{key.Id}' was not declared by the plugin manifest.");
        if (!registry.TryGetSensitivity(key.Id, out PluginSecuritySensitivity sensitivity)) return null;
        PluginSecurityAuthorization.Demand(securityPolicy, pluginId, PluginSecurityOperation.CapabilityAccess, key.Id, sensitivity);
        return registry.Resolve(key);
    }
}
