using BlackFlag.Engine.SDK.Plugins;
using BlackFlag.Engine.SDK.Plugins.Capabilities;
using BlackFlag.Engine.SDK.Plugins.Extensions;

namespace BlackFlag.Engine.ReferencePlugin;

/// <summary>Canonical SDK-only example of the supported BlackFlag plugin lifecycle.</summary>
public sealed class ReferencePlugin : IBlackFlagPlugin, IPluginExtension, IDisposable
{
    public static readonly PluginCapabilityKey<IPluginCapability> OptionalHostCapability = new("blackflag.reference.optional");
    public static readonly PluginExtensionPoint<IPluginExtension> ExtensionPoint = new("blackflag.reference.extension");

    private bool initialized;

    public ValueTask InitializeAsync(PluginInitializationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (initialized) throw new InvalidOperationException("The reference plugin is already initialized.");

        // Optional capabilities may be absent. Retrieval is constrained by the manifest declaration and host policy.
        _ = context.Capabilities.Retrieve(OptionalHostCapability);

        // Registration is transactional: the host commits only after InitializeAsync succeeds.
        context.Extensions.Register(ExtensionPoint, this);
        initialized = true;
        return ValueTask.CompletedTask;
    }

    public ValueTask DeactivateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        initialized = false;
        return ValueTask.CompletedTask;
    }

    public void Dispose() => initialized = false;
}
