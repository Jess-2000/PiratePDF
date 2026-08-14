namespace BlackFlag.Engine.Security.Plugins;

/// <summary>Contains the host security decision for a plugin-originated request.</summary>
public sealed record PluginSecurityDecision(bool Allowed, string Message)
{
    public static PluginSecurityDecision Allow(string message = "Allowed by plugin security policy.") => new(true, message);
    public static PluginSecurityDecision Deny(string message) => new(false, message);
}
