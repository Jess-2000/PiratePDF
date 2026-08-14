namespace BlackFlag.Engine.SDK.Plugins.Extensions;

/// <summary>Registers typed extensions transactionally during plugin initialization.</summary>
public interface IPluginExtensionRegistration
{
    void Register<T>(PluginExtensionPoint<T> extensionPoint, T extension) where T : class, IPluginExtension;
}
