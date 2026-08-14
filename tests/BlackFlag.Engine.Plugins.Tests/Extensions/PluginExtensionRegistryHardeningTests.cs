#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using BlackFlag.Engine.Plugins.Activation;
using BlackFlag.Engine.Plugins.Discovery;
using BlackFlag.Engine.Plugins.Runtime;
using BlackFlag.Engine.SDK.Plugins.Extensions;

namespace BlackFlag.Engine.Plugins.Tests.Extensions;

public sealed class PluginExtensionRegistryHardeningTests
{
    [Fact]
    public async Task DisposingOneSessionPreservesOtherOwnerOnSharedPoint()
    {
        string firstPackage = CreatePackage("org.sample.extension");
        string secondPackage = CreatePackage("org.sample.extension.second");
        var environment = new PluginRuntimeEnvironment();
        var point = new PluginExtensionPoint<IPluginExtension>("test.extension");

        PluginRuntimeSession? firstSession = null;
        PluginRuntimeSession? secondSession = null;

        try
        {
            firstSession = await ActivateAsync(firstPackage, environment);
            secondSession = await ActivateAsync(secondPackage, environment);

            Assert.Single(firstSession.ActivePlugins);
            Assert.Single(secondSession.ActivePlugins);
            Assert.Equal(2, environment.Extensions.GetExtensions(point).Count);

            await firstSession.DisposeAsync();

            Assert.Empty(firstSession.ActivePlugins);
            Assert.Single(secondSession.ActivePlugins);
            Assert.Single(environment.Extensions.GetExtensions(point));

            await secondSession.DisposeAsync();

            Assert.Empty(secondSession.ActivePlugins);
            Assert.Empty(environment.Extensions.GetExtensions(point));
        }
        finally
        {
            if (firstSession is not null)
                await firstSession.DisposeAsync();
            if (secondSession is not null)
                await secondSession.DisposeAsync();

            DeletePackageRoot(firstPackage);
            DeletePackageRoot(secondPackage);
        }
    }

    [Fact]
    public async Task RepeatedSessionDisposalIsIdempotentAndLeavesNoOwnedExtensions()
    {
        string package = CreatePackage("org.sample.extension");
        var environment = new PluginRuntimeEnvironment();
        var point = new PluginExtensionPoint<IPluginExtension>("test.extension");

        PluginRuntimeSession? session = null;

        try
        {
            session = await ActivateAsync(package, environment);

            Assert.Single(session.ActivePlugins);
            Assert.Single(environment.Extensions.GetExtensions(point));

            await session.DisposeAsync();
            await session.DisposeAsync();

            Assert.Empty(session.ActivePlugins);
            Assert.Empty(environment.Extensions.GetExtensions(point));
        }
        finally
        {
            if (session is not null)
                await session.DisposeAsync();

            DeletePackageRoot(package);
        }
    }

    private static async Task<PluginRuntimeSession> ActivateAsync(
        string package,
        PluginRuntimeEnvironment environment)
    {
        string root = Path.GetDirectoryName(package)!;
        PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([root]);
        PluginActivationPlan plan = PluginActivationPlanner.Plan(catalog, new Version(1, 0));

        return await PluginRuntimeHost.ActivateAsync(
            plan,
            new Version(1, 0),
            environment: environment);
    }

    private static string CreatePackage(string id)
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "blackflag-extension-hardening-" + Guid.NewGuid().ToString("N"));
        string package = Path.Combine(root, "plugin");
        Directory.CreateDirectory(package);

        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "BlackFlag.Engine.Plugins.TestPlugin.dll"),
            Path.Combine(package, "BlackFlag.Engine.Plugins.TestPlugin.dll"));

        File.WriteAllText(
            Path.Combine(package, "plugin.json"),
            $$"""
            {
            "manifestVersion": 1,
            "id": "{{id}}",
            "name": "Extension Hardening Test",
            "version": "1.0.0",
            "minimumSdkVersion": "1.0.0",
            "entryPoint": "BlackFlag.Engine.Plugins.TestPlugin.SamplePlugin, BlackFlag.Engine.Plugins.TestPlugin",
            "requiredCapabilities": []
            }
            """);

        return package;
    }

    private static void DeletePackageRoot(string package)
    {
        string? root = Path.GetDirectoryName(package);
        if (root is not null && Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}