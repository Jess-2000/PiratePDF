namespace BlackFlag.Engine.SDK.Plugins.Extensions;

/// <summary>Strongly typed identifier for a host-defined extension point.</summary>
public sealed record PluginExtensionPoint<T> where T : class, IPluginExtension
{
    public const int MaximumIdLength = 96;
    public PluginExtensionPoint(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (id.Length > MaximumIdLength) throw new ArgumentOutOfRangeException(nameof(id));
        Id = id;
    }
    public string Id { get; }
}
