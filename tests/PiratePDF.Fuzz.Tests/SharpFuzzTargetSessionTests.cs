using PiratePDF.Fuzz.SharpFuzz;

namespace PiratePDF.Fuzz.Tests;

public sealed class SharpFuzzTargetSessionTests
{
    [Fact]
    public void ConstructionDoesNotResolveRegistryBeforeRuntimeCallback()
    {
        var session = new SharpFuzzTargetSession("unknown-target");
        Assert.Throws<ArgumentException>(() => session.Invoke([]));
    }

    [Theory]
    [InlineData(">")]
    [InlineData("4869>")]
    [InlineData("F>")]
    [InlineData("G>")]
    public void CallbackRetainsTargetOutcomesOnRepeatedInvocations(string input)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(input);
        var session = new SharpFuzzTargetSession("filter-asciihex");
        FuzzResult expected = TargetRegistry.Get("filter-asciihex").Run(bytes);
        Assert.Equal(expected, session.Invoke(bytes));
        Assert.Equal(expected, session.Invoke(bytes));
    }

    [Fact]
    public void CallbackKeepsAdmissionAndCancellationFailures()
    {
        var session = new SharpFuzzTargetSession("filter-asciihex");
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Invoke(new byte[16385]));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => session.Invoke(">"u8, cancellation.Token));
    }
}
