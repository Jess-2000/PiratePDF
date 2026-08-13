namespace BlackFlag.Engine.SDK.Plugins;

/// <summary>Describes a validated plugin without loading executable code.</summary>
public sealed class PluginManifest
{
    private readonly PluginCapability[] requiredCapabilities;
    private readonly PluginCapability[] optionalCapabilities;
    private readonly PluginDependency[] dependencies;

    public PluginManifest(PluginId id, string name, Version version, Version minimumSdkVersion, string entryPoint,
        IEnumerable<PluginCapability>? requiredCapabilities = null, IEnumerable<PluginCapability>? optionalCapabilities = null,
        IEnumerable<PluginDependency>? dependencies = null)
    {
        ArgumentNullException.ThrowIfNull(id);
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
        this.dependencies = dependencies?.ToArray() ?? [];
        if (this.dependencies.Select(static dependency => dependency.Id).Distinct().Count() != this.dependencies.Length)
            throw new ArgumentException("Plugin dependencies must have unique plugin IDs.", nameof(dependencies));
        if (this.dependencies.Any(dependency => dependency.Id == id))
            throw new ArgumentException("A plugin cannot depend on itself.", nameof(dependencies));
    }

    public PluginId Id { get; }
    public string Name { get; }
    public Version Version { get; }
    public Version MinimumSdkVersion { get; }
    public string EntryPoint { get; }
    public IReadOnlyList<PluginCapability> RequiredCapabilities => requiredCapabilities;
    public IReadOnlyList<PluginCapability> OptionalCapabilities => optionalCapabilities;
    public IReadOnlyList<PluginDependency> Dependencies => dependencies;
}
