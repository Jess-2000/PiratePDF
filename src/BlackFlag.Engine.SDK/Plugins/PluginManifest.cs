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

        PluginCapability[] required = requiredCapabilities?.ToArray() ?? [];
        PluginCapability[] optional = optionalCapabilities?.ToArray() ?? [];
        PluginDependency[] dependencyArray = dependencies?.ToArray() ?? [];

        ValidateCapabilities(required, nameof(requiredCapabilities));
        ValidateCapabilities(optional, nameof(optionalCapabilities));
        if (required.Length + optional.Length > PluginManifestLimits.DefaultMaximumCapabilities)
            throw new ArgumentOutOfRangeException(nameof(requiredCapabilities), $"A plugin cannot declare more than {PluginManifestLimits.DefaultMaximumCapabilities} capabilities.");
        if (required.Intersect(optional).Any())
            throw new ArgumentException("A capability cannot be both required and optional.", nameof(optionalCapabilities));
        if (dependencyArray.Length > PluginManifestLimits.DefaultMaximumDependencies)
            throw new ArgumentOutOfRangeException(nameof(dependencies), $"A plugin cannot declare more than {PluginManifestLimits.DefaultMaximumDependencies} dependencies.");
        if (dependencyArray.Any(static dependency => dependency is null))
            throw new ArgumentException("Plugin dependencies cannot contain null entries.", nameof(dependencies));
        if (dependencyArray.Select(static dependency => dependency.Id).Distinct().Count() != dependencyArray.Length)
            throw new ArgumentException("Plugin dependencies must have unique plugin IDs.", nameof(dependencies));
        if (dependencyArray.Any(dependency => dependency.Id == id))
            throw new ArgumentException("A plugin cannot depend on itself.", nameof(dependencies));

        Id = id;
        Name = name;
        Version = version;
        MinimumSdkVersion = minimumSdkVersion;
        EntryPoint = entryPoint;
        this.requiredCapabilities = required;
        this.optionalCapabilities = optional;
        this.dependencies = dependencyArray;
    }

    public PluginId Id { get; }
    public string Name { get; }
    public Version Version { get; }
    public Version MinimumSdkVersion { get; }
    public string EntryPoint { get; }
    public IReadOnlyList<PluginCapability> RequiredCapabilities => requiredCapabilities;
    public IReadOnlyList<PluginCapability> OptionalCapabilities => optionalCapabilities;
    public IReadOnlyList<PluginDependency> Dependencies => dependencies;

    private static void ValidateCapabilities(PluginCapability[] capabilities, string parameterName)
    {
        if (capabilities.Any(static capability => capability is null))
            throw new ArgumentException("Plugin capabilities cannot contain null entries.", parameterName);
        if (capabilities.Distinct().Count() != capabilities.Length)
            throw new ArgumentException("Plugin capabilities must be unique.", parameterName);
    }
}
