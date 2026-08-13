using BlackFlag.Engine.SDK.Plugins;
using BlackFlag.Engine.SDK.Plugins.Capabilities;

namespace BlackFlag.Engine.Plugins.Capabilities;

internal sealed class PluginCapabilityAccess : IPluginCapabilityAccess
{
    private readonly PluginCapabilityRegistry registry;
    private readonly HashSet<string> declared;
    public PluginCapabilityAccess(PluginCapabilityRegistry registry, PluginManifest manifest)
    {
        this.registry = registry;
        declared = manifest.RequiredCapabilities.Concat(manifest.OptionalCapabilities).Select(static c => c.Value).ToHashSet(StringComparer.Ordinal);
    }
    public T? Retrieve<T>(PluginCapabilityKey<T> key) where T : class, IPluginCapability
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!declared.Contains(key.Id)) throw new InvalidOperationException($"Capability '{key.Id}' was not declared by the plugin manifest.");
        return registry.Resolve(key);
    }
}
