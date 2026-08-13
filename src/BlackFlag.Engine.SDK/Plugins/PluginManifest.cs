namespace BlackFlag.Engine.SDK.Plugins;

/// <summary>Describes a validated plugin without loading executable code.</summary>
public sealed class PluginManifest
{
    private readonly PluginCapability[] requiredCapabilities;
    private readonly PluginCapability[] optionalCapabilities;

    public PluginManifest(PluginId id, string name, Version version, Version minimumSdkVersion, string entryPoint,
        IEnumerable<PluginCapability>? requiredCapabilities = null, IEnumerable<PluginCapability>? optionalCapabilities = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(minimumSdkVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryPoint);
        Id = id;
        Name = name;
        Version = version;
        MinimumSdkVersion = minimumSdkVersion;
        EntryPoint = entryPoint;
        this.requiredCapabilities = requiredCapabilities?.Distinct().ToArray() ?? [];
        this.optionalCapabilities = optionalCapabilities?.Distinct().ToArray() ?? [];
    }

    public PluginId Id { get; }
    public string Name { get; }
    public Version Version { get; }
    public Version MinimumSdkVersion { get; }
    public string EntryPoint { get; }
    public IReadOnlyList<PluginCapability> RequiredCapabilities => requiredCapabilities;
    public IReadOnlyList<PluginCapability> OptionalCapabilities => optionalCapabilities;
}
