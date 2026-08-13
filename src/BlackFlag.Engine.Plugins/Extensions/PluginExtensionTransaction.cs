using BlackFlag.Engine.SDK.Plugins;
using BlackFlag.Engine.SDK.Plugins.Extensions;

namespace BlackFlag.Engine.Plugins.Extensions;

internal sealed class PluginExtensionTransaction : IPluginExtensionRegistration
{
    private readonly PluginId owner;
    private readonly int maximumRegistrations;
    private readonly List<PendingExtension> pending = [];
    private bool completed;
    public PluginExtensionTransaction(PluginId owner, int maximumRegistrations) { this.owner = owner; this.maximumRegistrations = maximumRegistrations; }
    public void Register<T>(PluginExtensionPoint<T> extensionPoint, T extension) where T : class, IPluginExtension
    {
        if (completed) throw new InvalidOperationException("Plugin extension registration is closed.");
        ArgumentNullException.ThrowIfNull(extensionPoint); ArgumentNullException.ThrowIfNull(extension);
        if (pending.Count >= maximumRegistrations) throw new InvalidOperationException("Plugin exceeded its extension registration limit.");
        pending.Add(new(owner, extensionPoint.Id, typeof(T), extension));
    }
    public void Commit(PluginExtensionRegistry registry) { if (completed) throw new InvalidOperationException("Plugin extension registration is closed."); registry.Commit(pending); completed = true; pending.Clear(); }
    public void Rollback() { completed = true; pending.Clear(); }
}
