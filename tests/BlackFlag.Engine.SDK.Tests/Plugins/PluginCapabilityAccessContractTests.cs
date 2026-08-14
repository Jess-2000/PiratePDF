#nullable enable

using BlackFlag.Engine.SDK.Plugins.Capabilities;

namespace BlackFlag.Engine.SDK.Tests.Plugins;

public sealed class PluginCapabilityAccessContractTests
{
    [Fact]
    public void PublicContractExposesTypedRetrieve()
    {
        IPluginCapabilityAccess access = new StubCapabilityAccess();
        var key = new PluginCapabilityKey<TestCapability>("test.capability");

        TestCapability? capability = access.Retrieve(key);

        Assert.NotNull(capability);
    }

    private sealed class TestCapability : IPluginCapability;

    private sealed class StubCapabilityAccess : IPluginCapabilityAccess
    {
        public T? Retrieve<T>(PluginCapabilityKey<T> key) where T : class, IPluginCapability
        {
            ArgumentNullException.ThrowIfNull(key);
            return typeof(T) == typeof(TestCapability) ? (T)(IPluginCapability)new TestCapability() : null;
        }
    }
}
