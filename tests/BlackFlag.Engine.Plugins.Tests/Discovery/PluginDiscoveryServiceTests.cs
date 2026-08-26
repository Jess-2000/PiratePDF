using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackFlag.Engine.Plugins.Discovery;
namespace BlackFlag.Engine.Plugins.Tests.Discovery;

public sealed class PluginDiscoveryServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "PiratePDF-BF1050B-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task DiscoversPackagesInDeterministicOrderWithoutLoadingAssemblies()
    {
        WriteManifest("z-package", "org.sample.z");
        WriteManifest("a-package", "org.sample.a");
        await File.WriteAllBytesAsync(Path.Combine(root, "a-package", "should-not-load.dll"), [0x4d, 0x5a]);

        PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([root]);

        Assert.Equal(["org.sample.a", "org.sample.z"], catalog.Plugins.Select(static p => p.Manifest.Id.Value));
        Assert.Empty(catalog.Issues);
    }

    [Fact]
    public async Task CatalogExposesStableReadOnlyCollections()
    {
        WriteManifest("a-package", "org.sample.a");
        WriteManifest("duplicate-package", "org.sample.a");

        PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([root]);

        AssertReadOnly(catalog.Plugins);
        AssertReadOnly(catalog.Issues);
    }



    [Fact]
    public async Task DuplicateIdsKeepFirstDeterministicPackageAndReportIssue()
    {
        WriteManifest("b-package", "org.sample.same");
        WriteManifest("a-package", "org.sample.same");

        PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([root]);

        PluginDescriptor plugin = Assert.Single(catalog.Plugins);
        Assert.EndsWith("a-package", plugin.PackagePath, StringComparison.Ordinal);
        Assert.Equal(PluginDiscoveryIssueKind.DuplicatePluginId, Assert.Single(catalog.Issues).Kind);
    }

    [Fact]
    public async Task InvalidManifestDoesNotBlockUnrelatedPlugin()
    {
        WriteManifest("good", "org.sample.good");
        string bad = Directory.CreateDirectory(Path.Combine(root, "bad")).FullName;
        await File.WriteAllTextAsync(Path.Combine(bad, "plugin.json"), "{not-json}");

        PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([root]);

        Assert.Single(catalog.Plugins);
        Assert.Equal(PluginDiscoveryIssueKind.ManifestInvalid, Assert.Single(catalog.Issues).Kind);
    }

    [Fact]
    public async Task PackageLimitIsBoundedAndReported()
    {
        WriteManifest("a", "org.sample.a");
        WriteManifest("b", "org.sample.b");

        PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([root], new PluginDiscoveryOptions { MaximumPackagesPerRoot = 1 });

        Assert.Single(catalog.Plugins);
        Assert.Equal(PluginDiscoveryIssueKind.PackageLimitExceeded, Assert.Single(catalog.Issues).Kind);
    }

    [Fact]
    public async Task CancellationIsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => PluginDiscoveryService.DiscoverAsync([root], cancellationToken: cancellation.Token));
    }

    private void WriteManifest(string directoryName, string id)
    {
        string directory = Directory.CreateDirectory(Path.Combine(root, directoryName)).FullName;
        File.WriteAllText(Path.Combine(directory, "plugin.json"), $$"""
        {"manifestVersion":1,"id":"{{id}}","name":"Sample","version":"1.0.0","minimumSdkVersion":"1.0.0","entryPoint":"Sample.Plugin"}
        """);
    }

    private static void AssertReadOnly<T>(IReadOnlyList<T> values)
    {
        Assert.NotEmpty(values);
        Assert.IsNotType<T[]>(values);
        IList<T> list = Assert.IsAssignableFrom<IList<T>>(values);
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Clear());
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        GC.SuppressFinalize(this);
    }
}
