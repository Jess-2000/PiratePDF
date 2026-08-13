namespace BlackFlag.Engine.SDK.Plugins;

/// <summary>Immutable initialization data supplied before a plugin becomes active.</summary>
public sealed class PluginInitializationContext
{
    public PluginInitializationContext(PluginId pluginId, Version pluginVersion, Version hostSdkVersion)
    {
        ArgumentNullException.ThrowIfNull(pluginId);
        ArgumentNullException.ThrowIfNull(pluginVersion);
        ArgumentNullException.ThrowIfNull(hostSdkVersion);
        PluginId = pluginId;
        PluginVersion = pluginVersion;
        HostSdkVersion = hostSdkVersion;
    }
    public PluginId PluginId { get; }
    public Version PluginVersion { get; }
    public Version HostSdkVersion { get; }
}
