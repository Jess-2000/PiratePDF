using BlackFlag.Engine.SDK.Plugins;

namespace BlackFlag.Engine.Plugins.Discovery;

/// <summary>Defines deterministic bounds for non-executing plugin discovery.</summary>
public sealed record PluginDiscoveryOptions
{
    public const string DefaultManifestFileName = "plugin.json";
    public const int DefaultMaximumRoots = 16;
    public const int DefaultMaximumPackagesPerRoot = 1024;
    public const int HardMaximumRoots = 256;
    public const int HardMaximumPackagesPerRoot = 16384;

    public string ManifestFileName { get; init; } = DefaultManifestFileName;
    public int MaximumRoots { get; init; } = DefaultMaximumRoots;
    public int MaximumPackagesPerRoot { get; init; } = DefaultMaximumPackagesPerRoot;
    public PluginManifestLimits ManifestLimits { get; init; } = new();

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ManifestFileName) || Path.GetFileName(ManifestFileName) != ManifestFileName)
            throw new ArgumentException("Manifest file name must be a simple file name.", nameof(ManifestFileName));
        if (MaximumRoots is <= 0 or > HardMaximumRoots) throw new ArgumentOutOfRangeException(nameof(MaximumRoots));
        if (MaximumPackagesPerRoot is <= 0 or > HardMaximumPackagesPerRoot) throw new ArgumentOutOfRangeException(nameof(MaximumPackagesPerRoot));
        ArgumentNullException.ThrowIfNull(ManifestLimits);
    }
}
