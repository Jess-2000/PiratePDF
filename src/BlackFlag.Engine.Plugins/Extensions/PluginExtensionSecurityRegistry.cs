using BlackFlag.Engine.SDK.Plugins.Extensions;
using BlackFlag.Engine.Security.Plugins;

namespace BlackFlag.Engine.Plugins.Extensions;

/// <summary>Host-owned security classification for typed plugin extension points.</summary>
public sealed class PluginExtensionSecurityRegistry
{
    private readonly Dictionary<string, PluginSecuritySensitivity> sensitivities = new(StringComparer.Ordinal);

    public void SetSensitivity<T>(PluginExtensionPoint<T> extensionPoint, PluginSecuritySensitivity sensitivity) where T : class, IPluginExtension
    {
        ArgumentNullException.ThrowIfNull(extensionPoint);
        sensitivities[extensionPoint.Id] = sensitivity;
    }

    internal PluginSecuritySensitivity GetSensitivity(string extensionPointId) =>
        sensitivities.TryGetValue(extensionPointId, out PluginSecuritySensitivity sensitivity) ? sensitivity : PluginSecuritySensitivity.Standard;
}
