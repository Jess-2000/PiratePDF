namespace BlackFlag.Engine.Security.Plugins;

/// <summary>Authorizes plugin-originated requests before host resources are exposed or registered.</summary>
public interface IPluginSecurityPolicy
{
    PluginSecurityDecision Evaluate(PluginSecurityRequest request);
}
