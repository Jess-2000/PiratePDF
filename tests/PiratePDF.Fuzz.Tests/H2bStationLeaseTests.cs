using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;

namespace PiratePDF.Fuzz.Tests;

public sealed class H2bStationLeaseTests
{
    private const string Id = "00112233445566778899aabbccddeeff";
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 0, 0, 0, TimeSpan.Zero);
    private static Dictionary<string, object> Peer(bool helper) => new()
    {
        ["Pid"] = helper ? 2002 : 1001, ["CreationTime"] = helper ? 202 : 101,
        ["User"] = "S-1-5-21-1-2-3-1001", ["Logon"] = "S-1-5-5-0-123", ["Authentication"] = "000000000000007b",
        ["TokenId"] = helper ? "0000000000000022" : "0000000000000011", ["Session"] = 1,
        ["Integrity"] = helper ? 12288 : 8192, ["Elevated"] = helper, ["Administrator"] = helper,
        ["Primary"] = true, ["AppContainer"] = false, ["UIAccess"] = false
    };
    private static Dictionary<string, object> Authorization() => new()
    {
        ["Version"] = 1, ["Operation"] = "StationLeaseOnlyV1", ["Id"] = Id, ["RunId"] = "0123abcd", ["Policy"] = "station-lease-v1",
        ["StationRights"] = 0x20002, ["DesktopRights"] = 0x20081, ["CodeDigest"] = new string('a', 64),
        ["NotBefore"] = Now.AddSeconds(-1), ["Expires"] = Now.AddSeconds(20), ["Controller"] = Peer(false), ["Helper"] = Peer(true)
    };
    private static object Model(string name, object value) => JsonSerializer.Deserialize(JsonSerializer.Serialize(value), H2bTestChild.Type(name))!;
    private static void Validate(Dictionary<string, object> fields) => H2bTestChild.Call("NativeStationPolicy", "ValidateAuthorization", Model("StationAuthorization", fields), Id, Now);

    [Fact]
    public void FixedAuthorizationAcceptsExactBoundedRoles() => Validate(Authorization());
    [Theory]
    [InlineData("Operation", "Install")]
    [InlineData("Id", "ffffffffffffffffffffffffffffffff")]
    [InlineData("RunId", "0123ABCd")]
    [InlineData("Policy", "future")]
    [InlineData("CodeDigest", "caller-hash")]
    [InlineData("StationRights", 0xf037f)]
    [InlineData("DesktopRights", 0xf01ff)]
    public void AuthorizationCannotChooseOperationPolicyOrRights(string field, object value)
    {
        Dictionary<string, object> fields = Authorization(); fields[field] = value;
        Assert.Throws<SecurityException>(() => Validate(fields));
    }
    [Theory]
    [InlineData("Pid", 1001)]
    [InlineData("Session", 2)]
    [InlineData("Integrity", 8192)]
    [InlineData("Elevated", false)]
    [InlineData("Administrator", false)]
    [InlineData("Primary", false)]
    [InlineData("AppContainer", true)]
    [InlineData("UIAccess", true)]
    [InlineData("User", "S-1-5-21-1-2-3-1002")]
    public void UnexpectedHelperRolesRefuse(string field, object value)
    {
        Dictionary<string, object> fields = Authorization();
        ((Dictionary<string, object>)fields["Helper"])[field] = value;
        Assert.Throws<SecurityException>(() => Validate(fields));
    }
    [Theory]
    [InlineData("Pid", 3333)]
    [InlineData("CreationTime", 999)]
    [InlineData("TokenId", "changed")]
    [InlineData("Authentication", "changed")]
    [InlineData("Session", 2)]
    public void CopyingRequestDoesNotAuthorizeAnotherSameUserInstance(string field, object value)
    {
        Dictionary<string, object> actual = Peer(false); actual[field] = value;
        Assert.Throws<SecurityException>(() => H2bTestChild.Call("NativeStationPolicy", "MatchPeer", Model("StationPeer", Peer(false)), Model("StationPeer", actual)));
    }
    [Theory]
    [InlineData(-2, -1)]
    [InlineData(1, 20)]
    [InlineData(-1, 31)]
    public void StaleFutureAndOverlongAuthorizationRefuse(int start, int end)
    {
        Dictionary<string, object> fields = Authorization(); fields["NotBefore"] = Now.AddSeconds(start); fields["Expires"] = Now.AddSeconds(end);
        Assert.Throws<SecurityException>(() => Validate(fields));
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(4097)]
    [InlineData(int.MaxValue)]
    public void FramingRejectsOversizeAndInvalidLengths(int length) =>
        Assert.Throws<SecurityException>(() => H2bTestChild.Call("NativeStationTransport", "ValidateFrameLength", length));
    [Theory]
    [InlineData(2)]
    [InlineData(4096)]
    public void FramingAcceptsOnlyBoundedLengths(int length) => H2bTestChild.Call("NativeStationTransport", "ValidateFrameLength", length);

    [Fact]
    public void ProtocolRejectsReplayForeignOperationAndUnexpectedHandles()
    {
        foreach (object message in new[]
        {
            Model("StationMessage", new { Version=2, Operation="StationLeaseOnlyV1", Id, Kind="Request", State="" }),
            Model("StationMessage", new { Version=1, Operation="Install", Id, Kind="Request", State="" }),
            Model("StationMessage", new { Version=1, Operation="StationLeaseOnlyV1", Id="copied", Kind="Request", State="" }),
            Model("StationMessage", new { Version=1, Operation="StationLeaseOnlyV1", Id, Kind="Ack", State="Verified" }),
            Model("StationMessage", new { Version=1, Operation="StationLeaseOnlyV1", Id, Kind="Request", Station=123, State="" })
        }) Assert.Throws<SecurityException>(() => H2bTestChild.Call("NativeStationTransport", "ValidateMessage", message, Id, "Request"));
    }
    [Fact]
    public void OnlyFullTransferCanBeAcknowledgedAndNoRetryAfterFailure()
    {
        object state = H2bTestChild.New("StationTransferState");
        Assert.Throws<SecurityException>(() => Invoke(state, "Acknowledge"));
        Invoke(state, "Duplicated");
        Assert.Throws<SecurityException>(() => Invoke(state, "Acknowledge"));
        Invoke(state, "Fail", "SecondDuplicationFailure");
        Assert.Equal("UNKNOWN/BLOCKED:SecondDuplicationFailure", H2bTestChild.Value(state, "State"));
        Assert.Throws<SecurityException>(() => Invoke(state, "Duplicated"));
    }
    [Theory]
    [InlineData("Timeout")]
    [InlineData("Disconnect")]
    [InlineData("HelperTermination")]
    [InlineData("ControllerTermination")]
    [InlineData("ReceiverRefusal")]
    public void LostAcknowledgmentKeepsTransferredOwnershipUnknown(string reason)
    {
        object state = H2bTestChild.New("StationTransferState"); Invoke(state, "Duplicated"); Invoke(state, "Duplicated"); Invoke(state, "Fail", reason);
        Assert.Equal("UNKNOWN/BLOCKED:" + reason, H2bTestChild.Value(state, "State"));
    }
    [Theory]
    [InlineData(true, true, "Closed")]
    [InlineData(true, false, "UNKNOWN/BLOCKED:Cleanup")]
    [InlineData(false, true, "UNKNOWN/BLOCKED:Cleanup")]
    public void CooperativeCleanupRequiresBothOwners(bool local, bool peer, string expected)
    {
        object state = H2bTestChild.New("StationTransferState"); Invoke(state, "Duplicated"); Invoke(state, "Duplicated"); Invoke(state, "Acknowledge");
        Invoke(state, "Closed", local, peer); Assert.Equal(expected, H2bTestChild.Value(state, "State"));
        Assert.Throws<SecurityException>(() => Invoke(state, "Acknowledge"));
    }
    [Theory]
    [InlineData(0L, 12L)]
    [InlineData(12L, -1L)]
    [InlineData(12L, 12L)]
    public void InvalidAndReusedNumericHandlesNeverBecomeOwned(long station, long desktop) =>
        Assert.Throws<SecurityException>(() => H2bTestChild.Call("WindowsExecutionDesktop", "ValidateHandlePair", station, desktop));

    [Fact, Trait("Platform", "Windows")]
    public void EnvironmentIsAnAllowlistWithoutHooksOrRequesterPaths()
    {
        var environment = (Dictionary<string, string>)H2bTestChild.Call("NativeStationPolicy", "EnvironmentFor", "C:\\Windows")!;
        Assert.Equal(11, environment.Count);
        Assert.Equal("C:\\Windows\\System32", environment["PATH"]);
        foreach (string key in new[] { "DOTNET_STARTUP_HOOKS", "DOTNET_ADDITIONAL_DEPS", "DOTNET_SHARED_STORE", "CORECLR_PROFILER", "TEMP", "COMPLUS_Version" }) Assert.False(environment.ContainsKey(key));
        Assert.Equal("0", environment["DOTNET_EnableDiagnostics"]); Assert.Equal("0", environment["CORECLR_ENABLE_PROFILING"]);
    }
    [Theory]
    [InlineData("LoadFromAssemblyPath")]
    [InlineData("CreateProcessW")]
    [InlineData("ReadKey")]
    [InlineData("RunReceiverAsync")]
    [InlineData("TerminateProcess")]
    [InlineData("CompleteOwnedHelper")]
    [InlineData("ObserveStationLifetime")]
    public void ClosedArtifactInspectionRejectsUnrelatedEntrySurface(string member) =>
        Assert.Throws<SecurityException>(() => H2bTestChild.Call("StationArtifactInspection", "ValidateMetadata", new[] { "NativeStationTransaction" }, new[] { "RunHelperAsync", member }, Array.Empty<string>()));
    [Fact]
    public void ExactRuntimeConfigRefusesRollForwardAndAdditionalProbing()
    {
        string valid="{\"runtimeOptions\":{\"tfm\":\"net10.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"10.0.10\"},\"rollForward\":\"Disable\"}}";
        H2bTestChild.Call("StationArtifactInspection", "ValidateConfig", Encoding.UTF8.GetBytes(valid), "10.0.10");
        foreach (string invalid in new[] { valid.Replace("Disable", "LatestPatch", StringComparison.Ordinal), valid.Replace("10.0.10", "10.0.9", StringComparison.Ordinal),
            valid.Replace("\"tfm\"", "\"additionalProbingPaths\":[],\"tfm\"", StringComparison.Ordinal) })
            Assert.Throws<SecurityException>(() => H2bTestChild.Call("StationArtifactInspection", "ValidateConfig", Encoding.UTF8.GetBytes(invalid), "10.0.10"));
    }
    [Fact, Trait("Platform", "Windows")]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void ExactDescriptorsHaveNoControllerReopenAceOrImplicitOwnerWriteDac()
    {
        foreach (bool desktop in new[] { false, true })
        {
            var descriptor=new RawSecurityDescriptor((string)H2bTestChild.Call("NativeStationPolicy", "Descriptor", desktop)!);
            Assert.Equal("S-1-5-32-544", descriptor.Owner!.Value); Assert.True(descriptor.ControlFlags.HasFlag(ControlFlags.DiscretionaryAclProtected));
            Assert.Equal(2, descriptor.DiscretionaryAcl!.Count);
            var owner=Assert.IsType<CommonAce>(descriptor.DiscretionaryAcl[0]); Assert.Equal("S-1-3-4", owner.SecurityIdentifier.Value); Assert.Equal(0x20000, owner.AccessMask);
            var creator=Assert.IsType<CommonAce>(descriptor.DiscretionaryAcl[1]); Assert.Equal("S-1-5-32-544", creator.SecurityIdentifier.Value); Assert.Equal(desktop ? 0x20083 : 0x2000a, creator.AccessMask);
            Assert.Single(descriptor.SystemAcl!); H2bTestChild.Call("WindowsExecutionDesktop", "VerifyLeaseDescriptor", descriptor, desktop);
            descriptor.DiscretionaryAcl.InsertAce(2,new CommonAce(AceFlags.None,AceQualifier.AccessAllowed,0x20000,new("S-1-1-0"),false,null));
            Assert.Throws<SecurityException>(() => H2bTestChild.Call("WindowsExecutionDesktop", "VerifyLeaseDescriptor", descriptor, desktop));
        }
    }
    [Fact, Trait("Platform", "Windows")]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void WritableAndNullDaclCodePathsRefuse()
    {
        foreach (string sddl in new[] { "O:BAG:BAD:P(A;;GA;;;WD)", "O:BAG:BAD:NO_ACCESS_CONTROL", "O:WDG:BAD:P(A;;GR;;;WD)" })
            Assert.Throws<SecurityException>(() => H2bTestChild.Call("NativeStationTrust", "VerifyPathDescriptor", new RawSecurityDescriptor(sddl), true, true));
    }
    [Fact]
    public void TypedUserHandlesUseCorrectCloseOperationsAndCaptureFailure()
    {
        foreach ((string type, string close) in new[] { ("SafeStationHandle","CloseWindowStation"), ("SafeDesktopHandle","CloseDesktop") })
        {
            using var handle=(SafeHandle)H2bTestChild.New(type);
            Assert.True(handle.IsInvalid); handle.Dispose(); handle.Dispose(); Assert.True(handle.IsClosed);
            MethodInfo method=H2bTestChild.Type(type).GetMethod(close,BindingFlags.NonPublic|BindingFlags.Static)!;
            Assert.True(method.GetCustomAttribute<DllImportAttribute>()!.SetLastError);
            Assert.Equal(DllImportSearchPath.System32,method.GetCustomAttribute<DefaultDllImportSearchPathsAttribute>()!.Paths);
        }
    }
    [Fact, Trait("Platform", "Windows")]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void PrimaryTokenAccessCheckRespectsActualOwnerAndAdministratorRoles()
    {
        using var process=System.Diagnostics.Process.GetCurrentProcess();
        using var identity=System.Security.Principal.WindowsIdentity.GetCurrent();
        bool administrator=new System.Security.Principal.WindowsPrincipal(identity).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        foreach(bool desktop in new[] { false, true })
        {
            var descriptor=new RawSecurityDescriptor((string)H2bTestChild.Call("NativeStationPolicy","Descriptor",desktop)!);
            byte[] bytes=new byte[descriptor.BinaryLength]; descriptor.GetBinaryForm(bytes,0);
            if(administrator) Assert.Throws<SecurityException>(()=>H2bTestChild.Call("WindowsExecutionDesktop","VerifyNoReopenDescriptor",bytes,process.SafeHandle));
            else H2bTestChild.Call("WindowsExecutionDesktop","VerifyNoReopenDescriptor",bytes,process.SafeHandle);
        }
    }
    private static void Invoke(object value, string method, params object[] arguments)
    {
        try { value.GetType().GetMethod(method, BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(value,arguments); }
        catch(TargetInvocationException failure) when(failure.InnerException is not null)
        { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure.InnerException).Throw(); }
    }
}
