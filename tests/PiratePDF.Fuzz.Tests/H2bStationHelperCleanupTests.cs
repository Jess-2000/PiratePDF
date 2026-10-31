namespace PiratePDF.Fuzz.Tests;

public sealed class H2bStationHelperCleanupTests
{
    [Fact]
    public void LaunchFailureRecordsNoChildWithoutInventingExitEvidence()
    {
        object result = H2bTestChild.Call("StationHelperCleanup", "NoLaunch", "LaunchFailure")!;
        Assert.False((bool)H2bTestChild.Value(result, "HelperCreated")!);
        Assert.False((bool)H2bTestChild.Value(result, "ExitVerified")!);
        Assert.False((bool)H2bTestChild.Value(result, "TerminationAttempted")!);
        Assert.Equal("LaunchFailure", H2bTestChild.Value(result, "OriginalFailure"));
    }
    [Theory]
    [InlineData(false, 0u, 0u, 0, "None", false, true)]
    [InlineData(false, 258u, 0u, 0, "None", true, true)]
    [InlineData(true, 258u, 0u, 0, "PostLaunchFailure", true, true)]
    [InlineData(true, 0u, 0u, 0, "PostLaunchFailure", false, true)]
    [InlineData(true, 258u, 258u, 5, "PostLaunchFailure", true, false)]
    [InlineData(true, uint.MaxValue, uint.MaxValue, 6, "PostLaunchFailure", true, false)]
    public void CompletionPreservesFailureAndUsesFiniteWaits(bool abort, uint initial, uint final, int terminateError,
        string failure, bool terminated, bool exited)
    {
        var waits = new List<uint>();
        int terminationCalls = 0;
        Func<uint, long> wait = milliseconds => { waits.Add(milliseconds); return waits.Count == 1 ? Pack(initial, initial == uint.MaxValue ? 6 : 0) : Pack(final, final == uint.MaxValue ? 6 : 0); };
        Func<long> terminate = () => { terminationCalls++; return Pack(terminateError == 0 ? 1u : 0u, terminateError); };
        Func<long> exit = () => Pack(0x400500b7, 0);
        object result = H2bTestChild.Call("StationHelperCleanup", "Complete", abort, failure, wait, terminate, exit)!;
        Assert.Equal(terminated ? 1 : 0, terminationCalls);
        Assert.Equal(abort ? 0u : 10000u, waits[0]);
        if (terminated) Assert.Equal(5000u, waits[1]);
        Assert.Equal(exited, H2bTestChild.Value(result, "ExitVerified"));
        Assert.Equal(failure == "None" && initial == 258 ? "Timeout" : failure, H2bTestChild.Value(result, "OriginalFailure"));
        Assert.Equal(terminateError, H2bTestChild.Value(result, "TerminationError"));
    }

    [Fact]
    public void ExitQueryFailureCannotBecomeVerifiedCleanup()
    {
        object result = H2bTestChild.Call("StationHelperCleanup", "Complete", true, "OriginalError",
            (Func<uint, long>)(_ => Pack(0, 0)), (Func<long>)(() => throw new InvalidOperationException()), (Func<long>)(() => Pack(0, 5)))!;
        Assert.False((bool)H2bTestChild.Value(result, "ExitVerified")!);
        Assert.Equal("OriginalError", H2bTestChild.Value(result, "OriginalFailure"));
        Assert.Equal(5, H2bTestChild.Value(result, "ExitError"));
    }

    [Theory]
    [InlineData("-File")]
    [InlineData("-Command")]
    [InlineData("-EncodedCommand")]
    [InlineData("-NonInteractive")]
    [InlineData("-f")]
    public void DisposableParentCannotOwnUncertainCodeLeases(string option) =>
        Assert.Throws<System.Security.SecurityException>(() => H2bTestChild.Call("StationHelperCleanup", "ValidateResidentArguments",
            (object)new[] { "powershell.exe", "-NoProfile", option }));

    [Fact]
    public void ExistingCleanInteractiveParentIsSupported() => H2bTestChild.Call("StationHelperCleanup", "ValidateResidentArguments",
        (object)new[] { "powershell.exe", "-NoLogo", "-NoProfile", "-NoExit" });

    [Fact]
    public void UnapprovedBootstrapPropertyCannotBroadenInspectionSurface() =>
        Assert.Throws<System.Security.SecurityException>(() => H2bTestChild.Call("StationArtifactInspection", "ValidateBootstrapMethods",
            (object)new[] { "LaunchStationHelper", "CreateProcessW", "set_UnapprovedProperty" }));

    [Fact]
    public void SourceOwnsHelperBeforePostLaunchWorkAndNeverLooksUpTerminationPid()
    {
        string root = ReplayFixture.FindRepository();
        string script = File.ReadAllText(Path.Combine(root, "tools/PiratePDF.Fuzz.Runner/Provision-NativeExecution.ps1"));
        Assert.Contains("CompleteOwnedHelper", script, StringComparison.Ordinal);
        Assert.DoesNotContain("$helperHandle.Dispose()", script, StringComparison.Ordinal);
        string interop = File.ReadAllText(Path.Combine(root, "tools/PiratePDF.NativeProvisioning/StationBootstrapInterop.cs"));
        Assert.Contains("TerminateProcess(ownedHelper", interop, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProcessById", interop, StringComparison.Ordinal);
    }

    private static long Pack(uint value, int error) => unchecked((long)(((ulong)(uint)error << 32) | value));
}
