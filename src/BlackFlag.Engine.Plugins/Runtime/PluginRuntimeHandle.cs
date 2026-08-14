using BlackFlag.Engine.Plugins.Discovery;
using BlackFlag.Engine.SDK.Plugins;

namespace BlackFlag.Engine.Plugins.Runtime;

public sealed class PluginRuntimeHandle
{
    private PluginLoadContext? loadContext;
    private IBlackFlagPlugin? plugin;
    internal PluginRuntimeHandle(PluginDescriptor descriptor, PluginLoadContext loadContext, IBlackFlagPlugin plugin)
    { Descriptor = descriptor; this.loadContext = loadContext; this.plugin = plugin; LoadContextWeakReference = new WeakReference(loadContext); }
    public PluginDescriptor Descriptor { get; }
    public WeakReference LoadContextWeakReference { get; }
    internal IBlackFlagPlugin Plugin => plugin ?? throw new ObjectDisposedException(nameof(PluginRuntimeHandle));
    internal void Release() { plugin = null; PluginLoadContext? context = loadContext; loadContext = null; context?.Unload(); }
}
