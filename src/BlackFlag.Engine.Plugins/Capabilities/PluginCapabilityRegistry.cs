using BlackFlag.Engine.SDK.Plugins.Capabilities;

namespace BlackFlag.Engine.Plugins.Capabilities;

/// <summary>Host-owned registry of explicitly exposed typed plugin capabilities.</summary>
public sealed class PluginCapabilityRegistry
{
    private readonly Dictionary<string, Binding> bindings = new(StringComparer.Ordinal);
    public void Register<T>(PluginCapabilityKey<T> key, T implementation) where T : class, IPluginCapability
    {
        ArgumentNullException.ThrowIfNull(key); ArgumentNullException.ThrowIfNull(implementation);
        if (!bindings.TryAdd(key.Id, new(typeof(T), implementation))) throw new InvalidOperationException($"Capability '{key.Id}' is already registered.");
    }
    internal bool Contains(string id) => bindings.ContainsKey(id);
    internal T? Resolve<T>(PluginCapabilityKey<T> key) where T : class, IPluginCapability
    {
        if (!bindings.TryGetValue(key.Id, out var binding)) return null;
        if (binding.ContractType != typeof(T)) throw new InvalidOperationException($"Capability '{key.Id}' was requested with an incompatible contract type.");
        return (T)binding.Implementation;
    }
    private sealed record Binding(Type ContractType, object Implementation);
}
