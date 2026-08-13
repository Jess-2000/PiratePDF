namespace BlackFlag.Engine.SDK.Plugins;

/// <summary>Defines hard bounds for plugin manifest processing.</summary>
public sealed record PluginManifestLimits
{
    public const int DefaultMaximumBytes = 64 * 1024;
    public const int DefaultMaximumCapabilities = 64;
    public int MaximumBytes { get; init; } = DefaultMaximumBytes;
    public int MaximumCapabilities { get; init; } = DefaultMaximumCapabilities;

    internal void Validate()
    {
        if (MaximumBytes is <= 0 or > 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(MaximumBytes));
        if (MaximumCapabilities is <= 0 or > 1024) throw new ArgumentOutOfRangeException(nameof(MaximumCapabilities));
    }
}
