using BlackFlag.Engine.SDK.Plugins.Capabilities;
using BlackFlag.Engine.SDK.Plugins.Extensions;

namespace BlackFlag.Engine.SDK.Plugins;

/// <summary>Immutable initialization data and constrained host access supplied before a plugin becomes active.</summary>
public sealed class PluginInitializationContext
{
    public PluginInitializationContext(PluginId pluginId, Version pluginVersion, Version hostSdkVersion, IPluginCapabilityAccess capabilities, IPluginExtensionRegistration extensions)
    {
        ArgumentNullException.ThrowIfNull(pluginId); ArgumentNullException.ThrowIfNull(pluginVersion); ArgumentNullException.ThrowIfNull(hostSdkVersion);
        ArgumentNullException.ThrowIfNull(capabilities); ArgumentNullException.ThrowIfNull(extensions);
        PluginId = pluginId; PluginVersion = pluginVersion; HostSdkVersion = hostSdkVersion; Capabilities = capabilities; Extensions = extensions;
    }
    public PluginId PluginId { get; }
    public Version PluginVersion { get; }
    public Version HostSdkVersion { get; }
    public IPluginCapabilityAccess Capabilities { get; }
    public IPluginExtensionRegistration Extensions { get; }
}
