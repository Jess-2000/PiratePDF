namespace BlackFlag.Engine.SDK.Plugins.Capabilities;

/// <summary>Strongly typed key for a manifest-declared plugin capability.</summary>
public sealed record PluginCapabilityKey<T> where T : class, IPluginCapability
{
    public PluginCapabilityKey(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (id.Length > PluginCapability.MaximumLength) throw new ArgumentOutOfRangeException(nameof(id));
        Id = id;
    }
    public string Id { get; }
}
