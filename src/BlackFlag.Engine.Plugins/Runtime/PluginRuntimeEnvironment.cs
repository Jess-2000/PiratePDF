using BlackFlag.Engine.Plugins.Capabilities;
using BlackFlag.Engine.Plugins.Extensions;
using BlackFlag.Engine.Security.Plugins;

namespace BlackFlag.Engine.Plugins.Runtime;

/// <summary>Host-owned plugin services shared across one or more runtime activation sessions.</summary>
public sealed class PluginRuntimeEnvironment
{
    public PluginRuntimeEnvironment(IPluginSecurityPolicy? securityPolicy = null)
    {
        SecurityPolicy = securityPolicy ?? new BlackFlagPluginSecurityPolicy();
    }

    public PluginCapabilityRegistry Capabilities { get; } = new();
    public PluginExtensionRegistry Extensions { get; } = new();
    public PluginExtensionSecurityRegistry ExtensionSecurity { get; } = new();
    public IPluginSecurityPolicy SecurityPolicy { get; }
}
