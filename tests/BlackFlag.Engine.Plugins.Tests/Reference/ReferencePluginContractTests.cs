using System;
using System.IO;
using System.Threading.Tasks;
using BlackFlag.Engine.Plugins.Activation;
using BlackFlag.Engine.Plugins.Discovery;
using BlackFlag.Engine.Plugins.Runtime;
using BlackFlag.Engine.SDK.Plugins.Extensions;

namespace BlackFlag.Engine.Plugins.Tests.Reference;

public sealed class ReferencePluginContractTests
{
    [Fact]
    public async Task ReferencePluginUsesSupportedDiscoveryActivationAndCleanupPath()
    {
        string root = Path.Combine(Path.GetTempPath(), "blackflag-reference-" + Guid.NewGuid().ToString("N"));
        string package = Path.Combine(root, "reference");
        Directory.CreateDirectory(package);
        try
        {
            File.Copy(Path.Combine(AppContext.BaseDirectory, "BlackFlag.Engine.ReferencePlugin.dll"), Path.Combine(package, "BlackFlag.Engine.ReferencePlugin.dll"));
            File.WriteAllText(Path.Combine(package, "plugin.json"), Manifest);

            PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([root]);
            PluginActivationPlan plan = PluginActivationPlanner.Plan(catalog, new Version(1, 0));
            var environment = new PluginRuntimeEnvironment();
            var point = new PluginExtensionPoint<IPluginExtension>("blackflag.reference.extension");

            PluginRuntimeSession session = await PluginRuntimeHost.ActivateAsync(plan, new Version(1, 0), environment: environment);
            Assert.Single(session.ActivePlugins);
            Assert.Single(environment.Extensions.GetExtensions(point));

            await session.DisposeAsync();
            Assert.Empty(environment.Extensions.GetExtensions(point));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private const string Manifest = /*lang=json,strict*/ """
        {
        "manifestVersion": 1,
        "id": "org.piratepdf.blackflag.reference",
        "name": "BlackFlag Reference Plugin",
        "version": "1.0.0",
        "minimumSdkVersion": "1.0.0",
        "entryPoint": "BlackFlag.Engine.ReferencePlugin.ReferencePlugin, BlackFlag.Engine.ReferencePlugin",
        "requiredCapabilities": [],
        "optionalCapabilities": ["blackflag.reference.optional"],
        "dependencies": []
        }
        """;
}
