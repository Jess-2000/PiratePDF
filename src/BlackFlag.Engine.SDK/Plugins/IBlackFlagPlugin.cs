namespace BlackFlag.Engine.SDK.Plugins;

/// <summary>Defines the managed lifecycle contract implemented by an in-process BlackFlag plugin.</summary>
public interface IBlackFlagPlugin
{
    ValueTask InitializeAsync(PluginInitializationContext context, CancellationToken cancellationToken);
    ValueTask DeactivateAsync(CancellationToken cancellationToken);
}
