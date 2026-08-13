using BlackFlag.Engine.SDK.Plugins;
using BlackFlag.Engine.SDK.Plugins.Capabilities;
using BlackFlag.Engine.SDK.Plugins.Extensions;

namespace BlackFlag.Engine.Plugins.TestPlugin;

public sealed class SamplePlugin : IBlackFlagPlugin, IPluginExtension, IDisposable
{
    public ValueTask InitializeAsync(PluginInitializationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.PluginId.Value is "org.sample.extension" or "org.sample.extension.second" or "org.sample.fail")
            context.Extensions.Register(new PluginExtensionPoint<IPluginExtension>("test.extension"), this);

        if (context.PluginId.Value == "org.sample.capability")
            _ = context.Capabilities.Retrieve(new PluginCapabilityKey<IPluginCapability>("test.capability"))
                ?? throw new InvalidOperationException("Capability unavailable.");

        if (context.PluginId.Value == "org.sample.fail")
            throw new InvalidOperationException("Requested test failure.");

        return ValueTask.CompletedTask;
    }

    public ValueTask DeactivateAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public void Dispose()
    {
    }
}