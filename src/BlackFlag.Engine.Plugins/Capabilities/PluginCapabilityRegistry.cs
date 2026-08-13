using BlackFlag.Engine.SDK.Plugins.Capabilities;
using BlackFlag.Engine.Security.Plugins;

namespace BlackFlag.Engine.Plugins.Capabilities;

/// <summary>Host-owned registry of explicitly exposed typed plugin capabilities.</summary>
public sealed class PluginCapabilityRegistry
{
    private readonly Dictionary<string, Binding> bindings = new(StringComparer.Ordinal);

    public void Register<T>(PluginCapabilityKey<T> key, T implementation, PluginSecuritySensitivity sensitivity = PluginSecuritySensitivity.Standard) where T : class, IPluginCapability
    {
        ArgumentNullException.ThrowIfNull(key); ArgumentNullException.ThrowIfNull(implementation);
        if (!bindings.TryAdd(key.Id, new(typeof(T), implementation, sensitivity))) throw new InvalidOperationException($"Capability '{key.Id}' is already registered.");
    }

    internal bool TryGetSensitivity(string id, out PluginSecuritySensitivity sensitivity)
    {
        if (bindings.TryGetValue(id, out Binding? binding)) { sensitivity = binding.Sensitivity; return true; }
        sensitivity = default; return false;
    }

    internal T? Resolve<T>(PluginCapabilityKey<T> key) where T : class, IPluginCapability
    {
        if (!bindings.TryGetValue(key.Id, out Binding? binding)) return null;
        if (binding.ContractType != typeof(T)) throw new InvalidOperationException($"Capability '{key.Id}' was requested with an incompatible contract type.");
        return (T)binding.Implementation;
    }

    private sealed record Binding(Type ContractType, object Implementation, PluginSecuritySensitivity Sensitivity);
}
