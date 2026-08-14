namespace BlackFlag.Engine.Security.Plugins;

/// <summary>Describes one plugin-originated request at a host-controlled boundary.</summary>
public sealed record PluginSecurityRequest
{
    public const int MaximumIdentifierLength = 128;

    public PluginSecurityRequest(string pluginId, PluginSecurityOperation operation, string resourceId, PluginSecuritySensitivity sensitivity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        if (pluginId.Length > MaximumIdentifierLength) throw new ArgumentOutOfRangeException(nameof(pluginId));
        if (resourceId.Length > MaximumIdentifierLength) throw new ArgumentOutOfRangeException(nameof(resourceId));
        PluginId = pluginId; Operation = operation; ResourceId = resourceId; Sensitivity = sensitivity;
    }

    public string PluginId { get; }
    public PluginSecurityOperation Operation { get; }
    public string ResourceId { get; }
    public PluginSecuritySensitivity Sensitivity { get; }
}
