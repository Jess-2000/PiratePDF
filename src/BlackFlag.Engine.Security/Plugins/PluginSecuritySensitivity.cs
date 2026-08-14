namespace BlackFlag.Engine.Security.Plugins;

/// <summary>Classifies how tightly the host must authorize a plugin-visible resource.</summary>
public enum PluginSecuritySensitivity
{
    Standard,
    Privileged
}
