using BlackFlag.Engine.Plugins.Capabilities;
using BlackFlag.Engine.Plugins.Extensions;

namespace BlackFlag.Engine.Plugins.Runtime;

/// <summary>Host-owned plugin services shared across one or more runtime activation sessions.</summary>
public sealed class PluginRuntimeEnvironment
{
    public PluginCapabilityRegistry Capabilities { get; } = new();
    public PluginExtensionRegistry Extensions { get; } = new();
}
