namespace BlackFlag.Engine.SDK.Plugins;

/// <summary>Declares a version-bounded dependency on another plugin identity.</summary>
public sealed record PluginDependency
{
    public PluginDependency(PluginId id, Version minimumVersion, Version? maximumVersionExclusive = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(minimumVersion);
        if (maximumVersionExclusive is not null && PluginVersionComparison.Compare(maximumVersionExclusive, minimumVersion) <= 0)
            throw new ArgumentException("Maximum dependency version must be greater than the minimum version.", nameof(maximumVersionExclusive));
        Id = id;
        MinimumVersion = minimumVersion;
        MaximumVersionExclusive = maximumVersionExclusive;
    }

    public PluginId Id { get; }
    public Version MinimumVersion { get; }
    public Version? MaximumVersionExclusive { get; }

    public bool Accepts(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return PluginVersionComparison.Compare(version, MinimumVersion) >= 0
            && (MaximumVersionExclusive is null || PluginVersionComparison.Compare(version, MaximumVersionExclusive) < 0);
    }
}
