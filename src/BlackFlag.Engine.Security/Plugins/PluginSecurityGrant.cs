namespace BlackFlag.Engine.Security.Plugins;

/// <summary>Grants one plugin access to one exact privileged host resource operation.</summary>
public sealed record PluginSecurityGrant
{
    public PluginSecurityGrant(string pluginId, PluginSecurityOperation operation, string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        if (pluginId.Length > PluginSecurityRequest.MaximumIdentifierLength) throw new ArgumentOutOfRangeException(nameof(pluginId));
        if (resourceId.Length > PluginSecurityRequest.MaximumIdentifierLength) throw new ArgumentOutOfRangeException(nameof(resourceId));
        PluginId = pluginId; Operation = operation; ResourceId = resourceId;
    }
    public string PluginId { get; }
    public PluginSecurityOperation Operation { get; }
    public string ResourceId { get; }
}
