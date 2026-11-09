namespace PiratePDF.Fuzz.Tests;

public sealed class H2bContainmentTests
{
    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void EmptyJobHasReadBackVerifiedMemoryProcessAndCloseLimits()
    {
        using var job = (IDisposable)H2bTestChild.New("WindowsJob", 33554432L, 2);
        Assert.True((bool)H2bTestChild.CallInstance(job, "IsEmpty")!);
        // This proves configuration/readback only, not resource enforcement on a running restricted child.
    }

    [Fact]
    public void NetworkSpecificationIncludesBothFamiliesAndAllRequiredAleOperations()
    {
        string[] layers = (string[])H2bTestChild.Call("WindowsNetworkPolicy", "RequiredLayers")!;
        Assert.Equal(8, layers.Distinct(StringComparer.Ordinal).Count());
        foreach (string operation in new[] { "AUTH_CONNECT", "AUTH_RECV_ACCEPT", "AUTH_LISTEN", "RESOURCE_ASSIGNMENT" })
        foreach (string family in new[] { "V4", "V6" })
            Assert.Contains("FWPM_LAYER_ALE_" + operation + "_" + family, layers);
    }
}
