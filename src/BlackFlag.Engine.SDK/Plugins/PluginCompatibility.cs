namespace BlackFlag.Engine.SDK.Plugins;

/// <summary>Provides deterministic SDK compatibility checks for validated manifests.</summary>
public static class PluginCompatibility
{
    public static bool IsCompatible(PluginManifest manifest, Version hostSdkVersion)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(hostSdkVersion);
        return hostSdkVersion.Major == manifest.MinimumSdkVersion.Major && hostSdkVersion >= manifest.MinimumSdkVersion;
    }
}
