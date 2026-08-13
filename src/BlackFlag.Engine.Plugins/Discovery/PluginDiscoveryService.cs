using BlackFlag.Engine.SDK.Plugins;

namespace BlackFlag.Engine.Plugins.Discovery;

/// <summary>Discovers validated plugin manifests from explicitly configured roots without loading assemblies.</summary>
public sealed class PluginDiscoveryService
{
    public static async Task<PluginCatalog> DiscoverAsync(
        IEnumerable<string> roots,
        PluginDiscoveryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roots);
        options ??= new PluginDiscoveryOptions();
        options.Validate();

        string[] normalizedRoots = NormalizeRoots(roots, options.MaximumRoots);
        var descriptors = new List<PluginDescriptor>();
        var issues = new List<PluginDiscoveryIssue>();
        var ids = new HashSet<PluginId>();

        foreach (string root in normalizedRoots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(root))
            {
                issues.Add(new(PluginDiscoveryIssueKind.RootUnavailable, root, "Plugin discovery root is unavailable."));
                continue;
            }

            string[] packagePaths;
            try
            {
                packagePaths = Directory.EnumerateDirectories(root)
                    .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
                    .Take(options.MaximumPackagesPerRoot + 1)
                    .ToArray();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                issues.Add(new(PluginDiscoveryIssueKind.RootUnavailable, root, "Plugin discovery root could not be enumerated."));
                continue;
            }

            if (packagePaths.Length > options.MaximumPackagesPerRoot)
            {
                issues.Add(new(PluginDiscoveryIssueKind.PackageLimitExceeded, root, "Plugin package count exceeds the configured root limit."));
                Array.Resize(ref packagePaths, options.MaximumPackagesPerRoot);
            }

            foreach (string packagePath in packagePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string manifestPath = Path.Combine(packagePath, options.ManifestFileName);
                if (!File.Exists(manifestPath)) continue;
                PluginManifest? manifest = await TryReadManifestAsync(manifestPath, options, issues, cancellationToken).ConfigureAwait(false);
                if (manifest is null) continue;
                if (!ids.Add(manifest.Id))
                {
                    issues.Add(new(PluginDiscoveryIssueKind.DuplicatePluginId, manifestPath, $"Plugin ID '{manifest.Id}' was discovered more than once."));
                    continue;
                }
                descriptors.Add(new(manifest, Path.GetFullPath(packagePath), Path.GetFullPath(manifestPath)));
            }
        }

        return new PluginCatalog(descriptors, issues);
    }

    private static string[] NormalizeRoots(IEnumerable<string> roots, int maximumRoots)
    {
        var normalized = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string root in roots)
        {
            if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Plugin discovery roots cannot contain empty paths.", nameof(roots));
            normalized.Add(Path.GetFullPath(root));
            if (normalized.Count > maximumRoots) throw new ArgumentException("Plugin discovery root count exceeds the configured limit.", nameof(roots));
        }
        return normalized.ToArray();
    }

    private static async Task<PluginManifest?> TryReadManifestAsync(
        string manifestPath, PluginDiscoveryOptions options, List<PluginDiscoveryIssue> issues, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (stream.Length <= 0 || stream.Length > options.ManifestLimits.MaximumBytes)
                throw new PluginManifestException("Plugin manifest is empty or exceeds the configured size limit.");
            byte[] bytes = GC.AllocateUninitializedArray<byte>(checked((int)stream.Length));
            await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
            return PluginManifestReader.Read(bytes, options.ManifestLimits);
        }
        catch (OperationCanceledException) { throw; }
        catch (PluginManifestException ex)
        {
            issues.Add(new(PluginDiscoveryIssueKind.ManifestInvalid, manifestPath, ex.Message));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            issues.Add(new(PluginDiscoveryIssueKind.ManifestUnreadable, manifestPath, "Plugin manifest could not be read."));
        }
        return null;
    }
}
