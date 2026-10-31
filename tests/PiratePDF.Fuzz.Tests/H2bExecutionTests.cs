namespace PiratePDF.Fuzz.Tests;

public sealed class H2bExecutionTests
{
    [Theory]
    [InlineData(0, 5, 100, 100, 33554432L, 2, 67108864L, 1024L, 100)]
    [InlineData(301, 5, 100, 100, 33554432L, 2, 67108864L, 1024L, 100)]
    [InlineData(20, 0, 100, 100, 33554432L, 2, 67108864L, 1024L, 100)]
    [InlineData(20, 5, 0, 100, 33554432L, 2, 67108864L, 1024L, 100)]
    [InlineData(20, 5, 100, 100, 33554432L, 0, 67108864L, 1024L, 100)]
    [InlineData(20, 5, 100, 100, 33554432L, 2, 67108864L, 67108865L, 100)]
    public void UnboundedOrInconsistentPolicyIsRejected(int execution, int cleanup, int interval, int scan,
        long memory, int processes, long storage, long file, int entries) => Assert.Throws<ArgumentOutOfRangeException>(() =>
        H2bTestChild.New("NativeExecutionPolicy", execution, cleanup, interval, scan, memory, processes, storage, file, entries));

    [Fact]
    public void MissingLiveSecurityProvidersRefuseWithoutClaimingExecution()
    {
        object result = H2bTestChild.Call("NativeExecutionEnvironment", "Inspect")!;
        Assert.Equal("Refused", H2bTestChild.Value(result, "Outcome"));
        Assert.Equal("Refused", H2bTestChild.Value(result, "Admission")!.ToString());
        Assert.Equal("NotChecked", H2bTestChild.Value(result, "Network")!.ToString());
        Assert.Equal("NotChecked", H2bTestChild.Value(result, "Storage")!.ToString());
        Assert.Null(H2bTestChild.Value(result, "ExitCode"));
        Assert.False((bool)H2bTestChild.Value(result, "EvidenceComplete")!);
    }
}
