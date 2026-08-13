#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackFlag.Engine.Plugins.Activation;
using BlackFlag.Engine.Plugins.Discovery;

namespace BlackFlag.Engine.Plugins.Tests.Activation;
public sealed class PluginActivationPlannerTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "PiratePDF-BF1050C-" + Guid.NewGuid().ToString("N"));
    [Fact]
    public async Task DependenciesAreOrderedBeforeDependents()
    {
        Write("app", "org.sample.app", "2.0.0", """[{"id":"org.sample.core","minimumVersion":"1.0.0"}]"""); Write("core", "org.sample.core", "1.5.0");
        PluginActivationPlan plan = PluginActivationPlanner.Plan(await PluginDiscoveryService.DiscoverAsync([root]), new Version(1,0));
        Assert.Equal(["org.sample.core","org.sample.app"], plan.ActivationOrder.Select(static p => p.Manifest.Id.Value)); Assert.Empty(plan.Issues);
    }
    [Fact]
    public async Task MissingDependencyBlocksOnlyDependentPlugin()
    {
        Write("good", "org.sample.good", "1.0.0"); Write("bad", "org.sample.bad", "1.0.0", """[{"id":"org.sample.missing","minimumVersion":"1.0.0"}]""");
        PluginActivationPlan plan = PluginActivationPlanner.Plan(await PluginDiscoveryService.DiscoverAsync([root]), new Version(1,0));
        Assert.Equal("org.sample.good", Assert.Single(plan.ActivationOrder).Manifest.Id.Value); Assert.Equal(PluginActivationIssueKind.MissingDependency, Assert.Single(plan.Issues).Kind);
    }
    [Fact]
    public async Task IncompatibleDependencyVersionBlocksDependent()
    {
        Write("app", "org.sample.app", "1.0.0", """[{"id":"org.sample.core","minimumVersion":"2.0.0"}]"""); Write("core", "org.sample.core", "1.0.0");
        PluginActivationPlan plan = PluginActivationPlanner.Plan(await PluginDiscoveryService.DiscoverAsync([root]), new Version(1,0));
        Assert.Contains(plan.Issues, static issue => issue.Kind == PluginActivationIssueKind.DependencyVersionIncompatible && issue.PluginId.Value == "org.sample.app");
    }
    [Fact]
    public async Task CycleIsDetectedAndUnrelatedPluginRemainsReady()
    {
        Write("a", "org.sample.a", "1.0.0", """[{"id":"org.sample.b","minimumVersion":"1.0.0"}]"""); Write("b", "org.sample.b", "1.0.0", """[{"id":"org.sample.a","minimumVersion":"1.0.0"}]"""); Write("good", "org.sample.good", "1.0.0");
        PluginActivationPlan plan = PluginActivationPlanner.Plan(await PluginDiscoveryService.DiscoverAsync([root]), new Version(1,0));
        Assert.Equal("org.sample.good", Assert.Single(plan.ActivationOrder).Manifest.Id.Value); Assert.Equal(2, plan.Issues.Count(static issue => issue.Kind == PluginActivationIssueKind.DependencyCycle));
    }
    [Fact]
    public async Task SdkIncompatibleDependencyPropagatesBlockedState()
    {
        Write("app", "org.sample.app", "1.0.0", """[{"id":"org.sample.core","minimumVersion":"1.0.0"}]"""); Write("core", "org.sample.core", "1.0.0", null, "2.0.0");
        PluginActivationPlan plan = PluginActivationPlanner.Plan(await PluginDiscoveryService.DiscoverAsync([root]), new Version(1,0));
        Assert.Empty(plan.ActivationOrder); Assert.Contains(plan.Issues, static issue => issue.Kind == PluginActivationIssueKind.SdkIncompatible); Assert.Contains(plan.Issues, static issue => issue.Kind == PluginActivationIssueKind.DependencyBlocked);
    }
    [Fact]
    public async Task CancellationIsPropagated()
    {
        Write("a", "org.sample.a", "1.0.0"); PluginCatalog catalog = await PluginDiscoveryService.DiscoverAsync([root]); using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => PluginActivationPlanner.Plan(catalog, new Version(1,0), cancellationToken: cts.Token));
    }
    private void Write(string dir, string id, string version, string? dependencies=null, string minimumSdk="1.0.0")
    {
        string path=Directory.CreateDirectory(Path.Combine(root,dir)).FullName; string deps=dependencies is null ? "" : $",\"dependencies\":{dependencies}";
        File.WriteAllText(Path.Combine(path,"plugin.json"), $"{{\"manifestVersion\":1,\"id\":\"{id}\",\"name\":\"Sample\",\"version\":\"{version}\",\"minimumSdkVersion\":\"{minimumSdk}\",\"entryPoint\":\"Sample.Plugin\"{deps}}}");
    }
    public void Dispose(){ if(Directory.Exists(root)) Directory.Delete(root,true); GC.SuppressFinalize(this); }
}
