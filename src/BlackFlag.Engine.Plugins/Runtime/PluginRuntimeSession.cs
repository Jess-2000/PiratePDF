using BlackFlag.Engine.Plugins.Extensions;

namespace BlackFlag.Engine.Plugins.Runtime;

public sealed class PluginRuntimeSession : IAsyncDisposable
{
    private readonly List<PluginRuntimeHandle> activePlugins;
    private readonly List<PluginRuntimeIssue> issues;
    private readonly PluginExtensionRegistry extensions;
    private bool disposed;
    internal PluginRuntimeSession(List<PluginRuntimeHandle> activePlugins, List<PluginRuntimeIssue> issues, PluginExtensionRegistry extensions)
    { this.activePlugins = activePlugins; this.issues = issues; this.extensions = extensions; }
    public IReadOnlyList<PluginRuntimeHandle> ActivePlugins => activePlugins;
    public IReadOnlyList<PluginRuntimeIssue> Issues => issues;

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        for (int i = activePlugins.Count - 1; i >= 0; i--)
        {
            PluginRuntimeHandle handle = activePlugins[i];
            try { await handle.Plugin.DeactivateAsync(CancellationToken.None).ConfigureAwait(false); }
            catch { issues.Add(new(PluginRuntimeIssueKind.DeactivationFailed, handle.Descriptor.Manifest.Id, "Plugin deactivation failed.")); }
            try
            {
                if (handle.Plugin is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                else if (handle.Plugin is IDisposable disposable) disposable.Dispose();
            }
            catch { issues.Add(new(PluginRuntimeIssueKind.DisposalFailed, handle.Descriptor.Manifest.Id, "Plugin disposal failed.")); }
            finally { extensions.RemoveOwnedBy(handle.Descriptor.Manifest.Id); handle.Release(); }
        }
        activePlugins.Clear();
    }
}
