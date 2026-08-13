namespace BlackFlag.Engine.SDK.Plugins.Capabilities;

/// <summary>Resolves only capabilities declared by the current plugin manifest and supplied by the host.</summary>
public interface IPluginCapabilityAccess
{
    T? Retrieve<T>(PluginCapabilityKey<T> key) where T : class, IPluginCapability;
}
