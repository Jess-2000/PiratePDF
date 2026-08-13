namespace BlackFlag.Engine.Plugins.Security;

internal sealed class PluginSecurityPolicyDeniedException : InvalidOperationException
{
    public PluginSecurityPolicyDeniedException(string message) : base(message) { }
}
