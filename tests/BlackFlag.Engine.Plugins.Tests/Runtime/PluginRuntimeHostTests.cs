using System;
using System.IO;
using System.Threading.Tasks;
using BlackFlag.Engine.Plugins.Activation;
using BlackFlag.Engine.Plugins.Discovery;
using BlackFlag.Engine.Plugins.Runtime;
using BlackFlag.Engine.SDK.Plugins.Capabilities;
using BlackFlag.Engine.SDK.Plugins.Extensions;
using BlackFlag.Engine.Security.Plugins;

namespace BlackFlag.Engine.Plugins.Tests.Runtime;

public sealed class PluginRuntimeHostTests
{
    [Fact]
    public async Task ValidPluginActivatesAndSessionUnloadsIt()
    {
        string root = CreatePackage("org.sample.good");
        try
        {
            PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([Path.GetDirectoryName(root)!]);
            PluginActivationPlan plan = PluginActivationPlanner.Plan(catalog, new Version(1,0));
            PluginRuntimeSession session = await PluginRuntimeHost.ActivateAsync(plan, new Version(1,0));
            PluginRuntimeHandle handle = Assert.Single(session.ActivePlugins);
            WeakReference weak = handle.LoadContextWeakReference;
            await session.DisposeAsync();
            Assert.Empty(session.ActivePlugins);
            Assert.True(weak.IsAlive); // Unload is requested; collection remains nondeterministic and is hardened later in BF-1050I.
            Assert.True(CanOpenForExclusiveWrite(Path.Combine(root, "BlackFlag.Engine.Plugins.TestPlugin.dll")));
        }
        finally { Directory.Delete(Path.GetDirectoryName(root)!, true); }
    }

    [Fact]
    public async Task InitializationFailureDoesNotActivatePlugin()
    {
        string root = CreatePackage("org.sample.fail");
        try
        {
            PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([Path.GetDirectoryName(root)!]);
            PluginActivationPlan plan = PluginActivationPlanner.Plan(catalog, new Version(1,0));
            await using PluginRuntimeSession session = await PluginRuntimeHost.ActivateAsync(plan, new Version(1,0));
            Assert.Empty(session.ActivePlugins);
            Assert.Contains(session.Issues, issue => issue.Kind == PluginRuntimeIssueKind.InitializationFailed);
        }
        finally { Directory.Delete(Path.GetDirectoryName(root)!, true); }
    }

    [Fact]
    public async Task InvalidEntryPointIsContained()
    {
        string root = CreatePackage("org.sample.bad", "NotAssemblyQualified");
        try
        {
            PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([Path.GetDirectoryName(root)!]);
            PluginActivationPlan plan = PluginActivationPlanner.Plan(catalog, new Version(1,0));
            await using PluginRuntimeSession session = await PluginRuntimeHost.ActivateAsync(plan, new Version(1,0));
            Assert.Empty(session.ActivePlugins);
            Assert.Contains(session.Issues, issue => issue.Kind == PluginRuntimeIssueKind.EntryPointInvalid);
        }
        finally { Directory.Delete(Path.GetDirectoryName(root)!, true); }
    }


    [Fact]
    public async Task SuccessfulInitializationCommitsOwnedExtensionAndDisposalRemovesIt()
    {
        string root = CreatePackage("org.sample.extension");
        var environment = new PluginRuntimeEnvironment();
        var point = new PluginExtensionPoint<IPluginExtension>("test.extension");
        try
        {
            PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([Path.GetDirectoryName(root)!]);
            PluginActivationPlan plan = PluginActivationPlanner.Plan(catalog, new Version(1,0));
            PluginRuntimeSession session = await PluginRuntimeHost.ActivateAsync(plan, new Version(1,0), environment: environment);
            Assert.Single(environment.Extensions.GetExtensions(point));
            await session.DisposeAsync();
            Assert.Empty(environment.Extensions.GetExtensions(point));
        }
        finally { Directory.Delete(Path.GetDirectoryName(root)!, true); }
    }

    [Fact]
    public async Task FailedInitializationRollsBackPendingExtensions()
    {
        string root = CreatePackage("org.sample.fail");
        var environment = new PluginRuntimeEnvironment();
        var point = new PluginExtensionPoint<IPluginExtension>("test.extension");
        try
        {
            PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([Path.GetDirectoryName(root)!]);
            PluginActivationPlan plan = PluginActivationPlanner.Plan(catalog, new Version(1,0));
            await using PluginRuntimeSession session = await PluginRuntimeHost.ActivateAsync(plan, new Version(1,0), environment: environment);
            Assert.Empty(environment.Extensions.GetExtensions(point));
        }
        finally { Directory.Delete(Path.GetDirectoryName(root)!, true); }
    }

    [Fact]
    public async Task RequiredCapabilityMustBeAvailableBeforeInitialization()
    {
        string root = CreatePackage("org.sample.capability", requiredCapabilities: "[\"test.capability\"]");
        try
        {
            PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([Path.GetDirectoryName(root)!]);
            PluginActivationPlan plan = PluginActivationPlanner.Plan(catalog, new Version(1,0));
            await using PluginRuntimeSession session = await PluginRuntimeHost.ActivateAsync(plan, new Version(1,0));
            Assert.Empty(session.ActivePlugins);
            Assert.Contains(session.Issues, issue => issue.Kind == PluginRuntimeIssueKind.RequiredCapabilityUnavailable);
        }
        finally { Directory.Delete(Path.GetDirectoryName(root)!, true); }
    }

    [Fact]
    public async Task DeclaredCapabilityResolvesThroughTypedHostRegistry()
    {
        string root = CreatePackage("org.sample.capability", requiredCapabilities: "[\"test.capability\"]");
        var environment = new PluginRuntimeEnvironment();
        environment.Capabilities.Register(new PluginCapabilityKey<IPluginCapability>("test.capability"), new TestCapability());
        try
        {
            PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([Path.GetDirectoryName(root)!]);
            PluginActivationPlan plan = PluginActivationPlanner.Plan(catalog, new Version(1,0));
            await using PluginRuntimeSession session = await PluginRuntimeHost.ActivateAsync(plan, new Version(1,0), environment: environment);
            Assert.Single(session.ActivePlugins);
        }
        finally { Directory.Delete(Path.GetDirectoryName(root)!, true); }
    }

    [Fact]
    public async Task PrivilegedRequiredCapabilityIsDeniedWithoutExplicitGrant()
    {
        string root = CreatePackage("org.sample.capability", requiredCapabilities: "[\"test.capability\"]");
        var environment = new PluginRuntimeEnvironment();
        environment.Capabilities.Register(new PluginCapabilityKey<IPluginCapability>("test.capability"), new TestCapability(), PluginSecuritySensitivity.Privileged);
        try
        {
            PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([Path.GetDirectoryName(root)!]);
            PluginActivationPlan plan = PluginActivationPlanner.Plan(catalog, new Version(1,0));
            await using PluginRuntimeSession session = await PluginRuntimeHost.ActivateAsync(plan, new Version(1,0), environment: environment);
            Assert.Empty(session.ActivePlugins);
            Assert.Contains(session.Issues, issue => issue.Kind == PluginRuntimeIssueKind.SecurityPolicyDenied);
        }
        finally { Directory.Delete(Path.GetDirectoryName(root)!, true); }
    }

    [Fact]
    public async Task ExplicitGrantAllowsPrivilegedCapability()
    {
        string root = CreatePackage("org.sample.capability", requiredCapabilities: "[\"test.capability\"]");
        var policy = new BlackFlagPluginSecurityPolicy([new PluginSecurityGrant("org.sample.capability", PluginSecurityOperation.CapabilityAccess, "test.capability")]);
        var environment = new PluginRuntimeEnvironment(policy);
        environment.Capabilities.Register(new PluginCapabilityKey<IPluginCapability>("test.capability"), new TestCapability(), PluginSecuritySensitivity.Privileged);
        try
        {
            PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([Path.GetDirectoryName(root)!]);
            PluginActivationPlan plan = PluginActivationPlanner.Plan(catalog, new Version(1,0));
            await using PluginRuntimeSession session = await PluginRuntimeHost.ActivateAsync(plan, new Version(1,0), environment: environment);
            Assert.Single(session.ActivePlugins);
        }
        finally { Directory.Delete(Path.GetDirectoryName(root)!, true); }
    }

    [Fact]
    public async Task PrivilegedExtensionPointIsDeniedAndRolledBackWithoutGrant()
    {
        string root = CreatePackage("org.sample.extension");
        var environment = new PluginRuntimeEnvironment();
        var point = new PluginExtensionPoint<IPluginExtension>("test.extension");
        environment.ExtensionSecurity.SetSensitivity(point, PluginSecuritySensitivity.Privileged);
        try
        {
            PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([Path.GetDirectoryName(root)!]);
            PluginActivationPlan plan = PluginActivationPlanner.Plan(catalog, new Version(1,0));
            await using PluginRuntimeSession session = await PluginRuntimeHost.ActivateAsync(plan, new Version(1,0), environment: environment);
            Assert.Empty(session.ActivePlugins);
            Assert.Empty(environment.Extensions.GetExtensions(point));
            Assert.Contains(session.Issues, issue => issue.Kind == PluginRuntimeIssueKind.SecurityPolicyDenied);
        }
        finally { Directory.Delete(Path.GetDirectoryName(root)!, true); }
    }

    private static bool CanOpenForExclusiveWrite(string path)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        return stream.CanWrite;
    }

    private static string CreatePackage(string id, string entryPoint = "BlackFlag.Engine.Plugins.TestPlugin.SamplePlugin, BlackFlag.Engine.Plugins.TestPlugin", string requiredCapabilities = "[]")
    {
        string root = Path.Combine(Path.GetTempPath(), "blackflag-runtime-" + Guid.NewGuid().ToString("N"));
        string package = Path.Combine(root, "plugin"); Directory.CreateDirectory(package);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "BlackFlag.Engine.Plugins.TestPlugin.dll"), Path.Combine(package, "BlackFlag.Engine.Plugins.TestPlugin.dll"));
        File.WriteAllText(Path.Combine(package, "plugin.json"), $$"""{"manifestVersion":1,"id":"{{id}}","name":"Runtime Test","version":"1.0.0","minimumSdkVersion":"1.0.0","entryPoint":"{{entryPoint}}","requiredCapabilities":{{requiredCapabilities}}}""");
        return package;
    }
    private sealed class TestCapability : IPluginCapability { }
}
