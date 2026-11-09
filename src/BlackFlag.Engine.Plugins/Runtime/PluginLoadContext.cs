using System.Reflection;
using System.Runtime.Loader;
using BlackFlag.Engine.SDK.Plugins;

namespace BlackFlag.Engine.Plugins.Runtime;

internal sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver resolver;
    private readonly string packageRoot;
    private readonly string physicalPackageRoot;
    private readonly string sdkAssemblyName = typeof(IBlackFlagPlugin).Assembly.GetName().Name!;

    internal PluginLoadContext(string entryAssemblyPath) : base(isCollectible: true)
    {
        packageRoot = Path.GetFullPath(Path.GetDirectoryName(entryAssemblyPath)!);
        physicalPackageRoot = PluginPackagePath.ResolvePhysicalPath(packageRoot);
        resolver = new AssemblyDependencyResolver(ValidateResolvedPath(entryAssemblyPath));
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
        if (!PluginPackagePath.Contains(packageRoot, full) &&
            !PluginPackagePath.Contains(physicalPackageRoot, full))
            throw new FileLoadException("Plugin dependency resolved outside its package directory.");
        string physical = PluginPackagePath.ResolvePhysicalPath(full);
        if (!PluginPackagePath.Contains(physicalPackageRoot, physical))
            throw new FileLoadException("Plugin dependency resolved outside its physical package directory.");
        return physical;
    }
}

internal static class PluginPackagePath
{
    internal static string ResolvePhysicalPath(string path)
    {
        string full = Path.GetFullPath(path);
        // Restart after each link so links in the target's ancestors are also
        // checked. The shared budget bounds cycles across redirected directories.
        for (int redirects = 0; redirects <= 40; redirects++)
        {
            string current = Path.GetPathRoot(full)!;
            string[] parts = full[current.Length..].Split(
                Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            bool redirected = false;
            for (int index = 0; index < parts.Length; index++)
            {
                current = Path.Combine(current, parts[index]);
                FileAttributes attributes;
                try { attributes = File.GetAttributes(current); }
                catch (FileNotFoundException) { return full; }
                catch (DirectoryNotFoundException) { return full; }
                if ((attributes & FileAttributes.ReparsePoint) == 0) continue;

                FileSystemInfo info = (attributes & FileAttributes.Directory) != 0
                    ? new DirectoryInfo(current) : new FileInfo(current);
                FileSystemInfo target = info.ResolveLinkTarget(returnFinalTarget: false)
                    ?? throw new FileLoadException("Plugin dependency redirection could not be resolved.");
                current = target.FullName;
                for (int remaining = index + 1; remaining < parts.Length; remaining++)
                    current = Path.Combine(current, parts[remaining]);
                full = Path.GetFullPath(current);
                redirected = true;
                break;
            }
            if (!redirected) return full;
        }
        throw new FileLoadException("Plugin dependency exceeded the symbolic-link resolution limit.");
    }

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
