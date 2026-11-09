namespace PiratePDF.Fuzz.Tests;

public sealed class H2bStationLifetimeTests
{
    [Theory]
    [InlineData(0u, 2, 1u, 0, "NamespaceAbsentAtProbe", false)]
    [InlineData(0u, 5, 1u, 0, "AccessDeniedNotAbsence", false)]
    [InlineData(0u, 0, 1u, 0, "AmbiguousOpenFailure", false)]
    [InlineData(0u, 123, 1u, 0, "OpenFailure", false)]
    [InlineData(1u, 0, 1u, 0, "NamedObjectPresent", true)]
    [InlineData(1u, 0, 0u, 5, "ProbeCloseFailure", false)]
    public void OneBoundedObservationNeverClaimsDestruction(uint open, int openError, uint close, int closeError, string state, bool closed)
    {
        int probes = 0, closes = 0;
        object result = Observe(true, _ => Pack(0), () => Pack(0), () => { probes++; return Pack(open, openError); }, () => { closes++; return Pack(close, closeError); });
        Assert.Equal(state, H2bTestChild.Value(result, "State"));
        Assert.True((bool)H2bTestChild.Value(result, "KnownTransactionOwnersClosed")!);
        Assert.Equal(1, probes);
        Assert.Equal(open == 1 ? 1 : 0, closes);
        Assert.Equal(closed, H2bTestChild.Value(result, "ProbeClosed"));
        Assert.False((bool)H2bTestChild.Value(result, "DestructionProved")!);
        Assert.Equal("UNKNOWN/BLOCKED", H2bTestChild.Value(result, "FinalObjectLifetime"));
    }

    [Theory]
    [InlineData(258u, 0, "ControllerTimeout")]
    [InlineData(uint.MaxValue, 6, "ControllerWaitFailure")]
    public void TimeoutOrFailedWaitNeverProbes(uint status, int error, string state)
    {
        object result = Observe(true, timeout => { Assert.Equal(5000u, timeout); return Pack(status, error); },
            () => throw new InvalidOperationException(), () => throw new InvalidOperationException(), () => throw new InvalidOperationException());
        Assert.Equal(state, H2bTestChild.Value(result, "State"));
        Assert.Equal(error, H2bTestChild.Value(result, "ControllerWaitError"));
        Assert.Equal(0, H2bTestChild.Value(result, "ProbeAttempts"));
        Assert.False((bool)H2bTestChild.Value(result, "KnownTransactionOwnersClosed")!);
    }

    [Fact]
    public void UnknownTransferredOwnershipPreventsAnyObservation()
    {
        object result = Observe(false, _ => throw new InvalidOperationException(), () => throw new InvalidOperationException(),
            () => throw new InvalidOperationException(), () => throw new InvalidOperationException());
        Assert.Equal("HelperUnverified", H2bTestChild.Value(result, "State"));
        Assert.Equal(0, H2bTestChild.Value(result, "ProbeAttempts"));
    }

    [Theory]
    [InlineData(2u, 0)]
    [InlineData(0u, 5)]
    public void ReceiverFailureCannotBecomeOwnerClosure(uint exit, int error)
    {
        object result = Observe(true, _ => Pack(0), () => Pack(exit, error), () => throw new InvalidOperationException(), () => throw new InvalidOperationException());
        Assert.Equal("ControllerFailure", H2bTestChild.Value(result, "State"));
        Assert.Equal(exit, H2bTestChild.Value(result, "ControllerExit"));
        Assert.Equal(error, H2bTestChild.Value(result, "ControllerExitError"));
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("0011AABB")]
    [InlineData("0011223344")]
    public void ObserverCannotChooseAnArbitraryObjectName(string run) => Assert.Throws<System.Security.SecurityException>(() =>
        H2bTestChild.Call("StationLifetimeEvidence", "Observe", run, true, (Func<uint, long>)(_ => Pack(0)),
            (Func<long>)(() => Pack(0)), (Func<long>)(() => Pack(0)), (Func<long>)(() => Pack(1))));

    private static object Observe(bool helper, Func<uint, long> wait, Func<long> exited, Func<long> opened, Func<long> closed) =>
        H2bTestChild.Call("StationLifetimeEvidence", "Observe", "0123abcd", helper, wait, exited, opened, closed)!;
    private static long Pack(uint value, int error = 0) => unchecked((long)(((ulong)(uint)error << 32) | value));
}
