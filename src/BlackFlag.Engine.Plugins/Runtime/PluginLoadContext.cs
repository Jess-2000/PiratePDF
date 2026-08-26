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
        packageRoot = Path.GetFullPath(Path.GetDirectoryName(entryAssemblyPath)!);
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
        if (!PluginPackagePath.Contains(packageRoot, full))
            throw new FileLoadException("Plugin dependency resolved outside its package directory.");
        return full;
    }
}

internal static class PluginPackagePath
{
    internal static bool Contains(string packageRoot, string candidatePath)
    {
        string fullRoot = Path.GetFullPath(packageRoot);
        string fullCandidate = Path.GetFullPath(candidatePath);
        string relative = Path.GetRelativePath(fullRoot, fullCandidate);

        return !Path.IsPathRooted(relative)
            && !string.Equals(relative, "..", StringComparison.Ordinal)
            && !relative.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal)
            && !relative.StartsWith(
                $"..{Path.AltDirectorySeparatorChar}",
                StringComparison.Ordinal);
    }
}
