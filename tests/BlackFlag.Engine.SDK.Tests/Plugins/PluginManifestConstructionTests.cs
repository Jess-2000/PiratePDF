using System.Linq;
using BlackFlag.Engine.SDK.Plugins;

namespace BlackFlag.Engine.SDK.Tests.Plugins;

public sealed class PluginManifestConstructionTests
{
    [Fact]
    public void ConstructorRejectsCapabilityDeclaredAsRequiredAndOptional()
    {
        var capability = new PluginCapability("document.read");

        Assert.Throws<ArgumentException>(() => Create([capability], [capability]));
    }

    [Fact]
    public void ConstructorRejectsDuplicateCapabilitiesInsteadOfSilentlyNormalizingThem()
    {
        var capability = new PluginCapability("document.read");

        Assert.Throws<ArgumentException>(() => Create([capability, capability], []));
    }

    [Fact]
    public void ConstructorEnforcesDefaultCapabilityBound()
    {
        PluginCapability[] capabilities = Enumerable.Range(0, PluginManifestLimits.DefaultMaximumCapabilities + 1)
            .Select(static index => new PluginCapability($"capability.{index}"))
            .ToArray();

        Assert.Throws<ArgumentOutOfRangeException>(() => Create(capabilities, []));
    }

    private static PluginManifest Create(IEnumerable<PluginCapability> required, IEnumerable<PluginCapability> optional) =>
        new(new PluginId("org.piratepdf.test"), "Test", new Version(1, 0), new Version(1, 0), "Test.Plugin, Test.Plugin", required, optional);
}
