using System.Reflection;
using System.Runtime.Loader;
using BlackFlag.Engine.SDK.Plugins;

namespace BlackFlag.Engine.Plugins.Runtime;

internal sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver resolver;
    private readonly string packageRoot;
    private readonly string sdkAssemblyName = typeof(IBlackFlagPlugin).Assembly.GetName().Name!;

    internal PluginLoadContext(string entryAssemblyPath) : base(isCollectible: true)
    {
        resolver = new AssemblyDependencyResolver(entryAssemblyPath);
        packageRoot = Path.GetFullPath(Path.GetDirectoryName(entryAssemblyPath)! + Path.DirectorySeparatorChar);
    }

    internal Assembly LoadEntryAssembly(string entryAssemblyPath) => LoadManagedAssembly(ValidateResolvedPath(entryAssemblyPath));

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (StringComparer.Ordinal.Equals(assemblyName.Name, sdkAssemblyName)) return typeof(IBlackFlagPlugin).Assembly;
        string? path = resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadManagedAssembly(ValidateResolvedPath(path));
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        string? path = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? nint.Zero : LoadUnmanagedDllFromPath(ValidateResolvedPath(path));
    }

    private Assembly LoadManagedAssembly(string path)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        return LoadFromStream(stream);
    }

    private string ValidateResolvedPath(string path)
    {
        string full = Path.GetFullPath(path);
        if (!full.StartsWith(packageRoot, StringComparison.OrdinalIgnoreCase))
            throw new FileLoadException("Plugin dependency resolved outside its package directory.");
        return full;
    }
}
