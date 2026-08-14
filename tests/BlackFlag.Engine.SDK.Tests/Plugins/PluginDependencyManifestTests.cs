using System.Text;
using BlackFlag.Engine.SDK.Plugins;

namespace BlackFlag.Engine.SDK.Tests.Plugins;

public sealed class PluginDependencyManifestTests
{
    [Fact]
    public void ReaderParsesBoundedDependencyRange()
    {
        PluginManifest manifest = PluginManifestReader.Read(Encoding.UTF8.GetBytes("""{"manifestVersion":1,"id":"org.sample.main","name":"Main","version":"2.0.0","minimumSdkVersion":"1.0.0","entryPoint":"Main.Plugin","dependencies":[{"id":"org.sample.base","minimumVersion":"1.2.0","maximumVersionExclusive":"2.0.0"}]}"""));
        PluginDependency dependency = Assert.Single(manifest.Dependencies);

        Assert.Equal("org.sample.base", dependency.Id.Value);
        Assert.True(dependency.Accepts(new Version(1, 2)));
        Assert.True(dependency.Accepts(new Version(1, 5)));
        Assert.False(dependency.Accepts(new Version(2, 0)));
        Assert.False(dependency.Accepts(new Version(2, 0, 0)));
    }

    [Fact]
    public void DependencyRangeTreatsOmittedVersionComponentsAsZero()
    {
        var dependency = new PluginDependency(new PluginId("org.sample.base"), new Version(1, 2, 0), new Version(2, 0, 0));

        Assert.True(dependency.Accepts(new Version(1, 2)));
        Assert.False(dependency.Accepts(new Version(1, 1, 9, 9)));
        Assert.False(dependency.Accepts(new Version(2, 0)));
    }

    [Fact]
    public void CompatibilityTreatsOmittedVersionComponentsAsZero()
    {
        var manifest = new PluginManifest(new PluginId("org.sample.main"), "Main", new Version(1, 0), new Version(1, 0, 0), "Main.Plugin");

        Assert.True(PluginCompatibility.IsCompatible(manifest, new Version(1, 0)));
        Assert.True(PluginCompatibility.IsCompatible(manifest, new Version(1, 0, 0)));
        Assert.False(PluginCompatibility.IsCompatible(manifest, new Version(2, 0)));
    }

    [Fact]
    public void ReaderRejectsDuplicateDependencyIds()
    {
        byte[] json = Encoding.UTF8.GetBytes("""{"manifestVersion":1,"id":"org.sample.main","name":"Main","version":"1.0.0","minimumSdkVersion":"1.0.0","entryPoint":"Main.Plugin","dependencies":[{"id":"org.sample.base","minimumVersion":"1.0.0"},{"id":"org.sample.base","minimumVersion":"1.1.0"}]}""");
        Assert.Throws<PluginManifestException>(() => PluginManifestReader.Read(json));
    }

    [Fact]
    public void ReaderRejectsSelfDependency()
    {
        byte[] json = Encoding.UTF8.GetBytes("""{"manifestVersion":1,"id":"org.sample.main","name":"Main","version":"1.0.0","minimumSdkVersion":"1.0.0","entryPoint":"Main.Plugin","dependencies":[{"id":"org.sample.main","minimumVersion":"1.0.0"}]}""");
        Assert.Throws<PluginManifestException>(() => PluginManifestReader.Read(json));
    }
}
