namespace PiratePDF.Fuzz.Tests;

public sealed class FuzzTargetTests
{
    [Theory]
    [InlineData("")]
    [InlineData("Upper")]
    [InlineData("space name")]
    [InlineData("path/name")]
    [InlineData("café")]
    public void InvalidIdsRejectAtConstruction(string id) =>
        Assert.Throws<ArgumentException>(() => Create(static (input, _) => FuzzResult.Success(input), id: id));

    [Fact]
    public void NullAndOverlongIdsRejectAtConstruction()
    {
        Assert.Throws<ArgumentNullException>(() => Create(static (input, _) => FuzzResult.Success(input), id: null));
        Assert.Throws<ArgumentException>(() => Create(static (input, _) => FuzzResult.Success(input), id: new string('a', 65)));
        Assert.Equal(64, Create(static (input, _) => FuzzResult.Success(input), id: new string('a', 64)).Id.Length);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void VersionAndInputLimitMustBePositive(int version, int maximum) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new FuzzTarget("valid", version, maximum,
            FuzzIsolation.SupervisedManagedWorker, static (input, _) => FuzzResult.Success(input)));

    [Fact]
    public void InvalidIsolationAndNullDelegateRejectAtConstruction()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FuzzTarget("valid", 1, 1,
            (FuzzIsolation)999, static (input, _) => FuzzResult.Success(input)));
        Assert.Throws<ArgumentNullException>(() => Create(null));
    }

    [Fact]
    public void ExactLimitInvokesOnceAndLimitPlusOneNeverInvokes()
    {
        int calls = 0;
        var expected = FuzzResult.Success([1, 2, 3, 4]);
        FuzzTarget target = Create((input, _) =>
        {
            calls++;
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, input.ToArray());
            return expected;
        });
        Assert.Same(expected, target.Run([1, 2, 3, 4]));
        Assert.Throws<ArgumentOutOfRangeException>(() => target.Run([1, 2, 3, 4, 5]));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void EmptyInputIsAdmitted()
    {
        int calls = 0;
        FuzzTarget target = Create((input, _) => { calls++; Assert.True(input.IsEmpty); return FuzzResult.Success(input); });
        Assert.Equal("success", target.Run([]).Outcome);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void PreCancellationPreservesTokenAndPreventsInvocation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        int calls = 0;
        FuzzTarget target = Create((input, _) => { calls++; return FuzzResult.Success(input); });
        OperationCanceledException error = Assert.Throws<OperationCanceledException>(() => target.Run([], cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancellationDuringInvocationCannotPublishAResult(bool rejection)
    {
        using var cancellation = new CancellationTokenSource();
        FuzzTarget target = Create((input, token) =>
        {
            Assert.Equal(cancellation.Token, token);
            cancellation.Cancel();
            return rejection ? FuzzResult.Rejected("InvalidData") : FuzzResult.Success(input);
        });
        OperationCanceledException error = Assert.Throws<OperationCanceledException>(() => target.Run([], cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
    }

    [Fact]
    public void UnexpectedExceptionsEscapeUnchanged()
    {
        // Synthetic exceptions test propagation without allocating excessively or crashing a process.
        Exception[] failures = [new InvalidOperationException(), new IndexOutOfRangeException(),
            new OverflowException(), new OutOfMemoryException(), new OperationCanceledException()];
        foreach (Exception failure in failures)
        {
            FuzzTarget target = Create((_, _) => throw failure);
            Assert.Same(failure, Record.Exception(() => target.Run([])));
        }
    }

    [Fact]
    public void NullAdapterResultIsAHarnessFailure() =>
        Assert.Throws<InvalidOperationException>(() => Create(static (_, _) => null).Run([]));

    private static FuzzTarget Create(ByteTarget execute, string id = "target") =>
        new(id, 1, 4, FuzzIsolation.SupervisedManagedWorker, execute);
}
