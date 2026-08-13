using System.Reflection;
using BlackFlag.Engine.Plugins.Activation;
using BlackFlag.Engine.Plugins.Discovery;
using BlackFlag.Engine.SDK.Plugins;

namespace BlackFlag.Engine.Plugins.Runtime;

/// <summary>Loads and initializes a validated activation plan using collectible per-plugin load contexts.</summary>
public static class PluginRuntimeHost
{
    public static async Task<PluginRuntimeSession> ActivateAsync(PluginActivationPlan plan, Version hostSdkVersion, PluginRuntimeOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan); ArgumentNullException.ThrowIfNull(hostSdkVersion);
        options ??= new PluginRuntimeOptions(); options.Validate();
        var active = new List<PluginRuntimeHandle>(Math.Min(plan.ActivationOrder.Count, options.MaximumActivePlugins));
        var issues = new List<PluginRuntimeIssue>();
        var activeIds = new HashSet<PluginId>();
        foreach (PluginDescriptor descriptor in plan.ActivationOrder)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (active.Count >= options.MaximumActivePlugins) break;
            if (descriptor.Manifest.Dependencies.Any(dependency => !activeIds.Contains(dependency.Id)))
            { issues.Add(new(PluginRuntimeIssueKind.DependencyUnavailable, descriptor.Manifest.Id, "A required plugin dependency did not become active.")); continue; }
            PluginRuntimeHandle? handle = await TryActivateAsync(descriptor, hostSdkVersion, issues, cancellationToken).ConfigureAwait(false);
            if (handle is null) continue;
            active.Add(handle); activeIds.Add(descriptor.Manifest.Id);
        }
        return new PluginRuntimeSession(active, issues);
    }

    private static async Task<PluginRuntimeHandle?> TryActivateAsync(PluginDescriptor descriptor, Version hostSdkVersion, List<PluginRuntimeIssue> issues, CancellationToken cancellationToken)
    {
        PluginId id = descriptor.Manifest.Id;
        if (!PluginEntryPoint.TryParse(descriptor.Manifest.EntryPoint, out PluginEntryPoint entryPoint))
        { issues.Add(new(PluginRuntimeIssueKind.EntryPointInvalid, id, "Plugin entry point must be an assembly-qualified type name.")); return null; }
        string assemblyPath = Path.GetFullPath(Path.Combine(descriptor.PackagePath, entryPoint.AssemblyName + ".dll"));
        string packageRoot = Path.GetFullPath(descriptor.PackagePath + Path.DirectorySeparatorChar);
        if (!assemblyPath.StartsWith(packageRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(assemblyPath))
        { issues.Add(new(PluginRuntimeIssueKind.AssemblyUnavailable, id, "Plugin entry assembly is unavailable.")); return null; }
        PluginLoadContext? context = null; IBlackFlagPlugin? plugin = null;
        try
        {
            context = new PluginLoadContext(assemblyPath);
            Assembly assembly = context.LoadEntryAssembly(assemblyPath);
            Type? type = assembly.GetType(entryPoint.TypeName, throwOnError: false, ignoreCase: false);
            if (type is null || type.IsAbstract || !typeof(IBlackFlagPlugin).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) is null)
            { issues.Add(new(PluginRuntimeIssueKind.EntryPointInvalid, id, "Plugin entry point must be a concrete IBlackFlagPlugin with a public parameterless constructor.")); context.Unload(); return null; }
            try { plugin = (IBlackFlagPlugin)Activator.CreateInstance(type)!; }
            catch { issues.Add(new(PluginRuntimeIssueKind.EntryPointCreationFailed, id, "Plugin entry point could not be created.")); context.Unload(); return null; }
            try
            {
                await plugin.InitializeAsync(new PluginInitializationContext(id, descriptor.Manifest.Version, hostSdkVersion), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            { await CleanupFailedAsync(plugin).ConfigureAwait(false); context.Unload(); throw; }
            catch
            { issues.Add(new(PluginRuntimeIssueKind.InitializationFailed, id, "Plugin initialization failed.")); await CleanupFailedAsync(plugin).ConfigureAwait(false); context.Unload(); return null; }
            return new PluginRuntimeHandle(descriptor, context, plugin);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or BadImageFormatException or FileLoadException or TypeLoadException)
        { context?.Unload(); issues.Add(new(PluginRuntimeIssueKind.AssemblyLoadFailed, id, "Plugin assembly could not be loaded.")); return null; }
    }

    private static async ValueTask CleanupFailedAsync(IBlackFlagPlugin plugin)
    {
        try { await plugin.DeactivateAsync(CancellationToken.None).ConfigureAwait(false); } catch { }
        try { if (plugin is IAsyncDisposable a) await a.DisposeAsync().ConfigureAwait(false); else if (plugin is IDisposable d) d.Dispose(); } catch { }
    }
}
