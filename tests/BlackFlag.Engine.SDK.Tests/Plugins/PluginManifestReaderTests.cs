using System.Text;
using BlackFlag.Engine.SDK.Plugins;

namespace BlackFlag.Engine.SDK.Tests.Plugins;

public sealed class PluginManifestReaderTests
{
    [Fact]
    public void ReadsValidManifestWithoutLoadingCode()
    {
        byte[] json = Encoding.UTF8.GetBytes("""
        {"manifestVersion":1,"id":"org.piratepdf.sample","name":"Sample","version":"1.2.3","minimumSdkVersion":"1.0.0","entryPoint":"Sample.Plugin","requiredCapabilities":["commands"],"optionalCapabilities":["rendering"]}
        """);
        PluginManifest manifest = PluginManifestReader.Read(json);
        Assert.Equal("org.piratepdf.sample", manifest.Id.Value);
        Assert.Equal(new Version(1, 2, 3), manifest.Version);
        Assert.Single(manifest.RequiredCapabilities);
        Assert.Single(manifest.OptionalCapabilities);
    }

    [Fact]
    public void RejectsOversizedManifestBeforeParsing()
    {
        byte[] json = new byte[129];
        PluginManifestException exception = Assert.Throws<PluginManifestException>(() =>
            PluginManifestReader.Read(json, new PluginManifestLimits { MaximumBytes = 128 }));
        Assert.Contains("size limit", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Org.Sample")]
    [InlineData("org_sample")]
    [InlineData(".sample")]
    [InlineData("sample.")]
    public void RejectsInvalidPluginIds(string id)
    {
        Assert.Throws<ArgumentException>(() => new PluginId(id));
    }

    [Fact]
    public void CompatibilityRequiresSameMajorAndMinimumVersion()
    {
        var manifest = new PluginManifest(new PluginId("org.sample"), "Sample", new Version(1, 0), new Version(2, 1), "Sample.Plugin");
        Assert.True(PluginCompatibility.IsCompatible(manifest, new Version(2, 4)));
        Assert.False(PluginCompatibility.IsCompatible(manifest, new Version(2, 0)));
        Assert.False(PluginCompatibility.IsCompatible(manifest, new Version(3, 0)));
    }
}
