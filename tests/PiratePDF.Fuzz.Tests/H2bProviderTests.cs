#nullable enable annotations
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Tests;

public sealed class H2bProviderTests
{
    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void AdministrativeFlagsCannotBypassIncompleteSecurityQualification()
    {
        string json = System.Text.Json.JsonSerializer.Serialize(new
        {
            Version = 1, RunId = "abcdef12", Account = "pph2b_abcdef12", NetworkOwner = Guid.NewGuid(), DiskId = Guid.NewGuid(),
            ControllerRoot = @"C:\controlled", RepositoryRoot = @"C:\repository", MountRoot = @"V:\",
            BackingPath = @"C:\controlled\owned.vhdx", Executable = @"C:\controlled\launch\host.exe", FixtureAssembly = @"C:\controlled\launch\probe.dll",
            Images = new[] { new { Path = @"C:\controlled\launch\host.exe", Sha256 = new string('0', 64), MaximumBytes = 1024 },
                new { Path = @"C:\controlled\launch\probe.dll", Sha256 = new string('0', 64), MaximumBytes = 1024 } },
            RuntimeResources = new[] { @"C:\controlled\runtime" }, ExcludedResources = new[] { @"C:\repository" },
            QualificationApproved = true, FilesystemQualified = true, NetworkQualified = true, StorageQualified = true, DesktopQualified = true
        });
        Assert.NotNull(H2bTestChild.Call("NativeExecutionEnvironment", "ParseConfiguration", json));
        SecurityException failure = Assert.Throws<SecurityException>(() => H2bTestChild.Call("NativeExecutionEnvironment", "ParsePlan", json));
        Assert.Contains("administrative flags cannot enable execution", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void DeniedRoleChecksRejectEvenOneGrantedAccessBit()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        string path = Path.GetTempFileName();
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            // ACCESS_SYSTEM_SECURITY is deliberately unrelated to the granted file-read bit.
            Assert.True((bool)H2bTestChild.Call("NativeFilesystemAdmission", "HasAnyAccess", identity.AccessToken, path, 0x1000001u)!);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task PreCancelledContainedExecutionDoesNotOpenAProvisionedEnvironment()
    {
        using var password = new SecureString();
        object policy = H2bTestChild.New("NativeExecutionPolicy", 1, 1, 10, 10, 33554432L, 2, 67108864L, 1024L, 16);
        object request = H2bTestChild.New("NativeExecutionRequest", @"C:\absent\plan.json", new string('0', 64), password, policy, "exit0");
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var task = (Task)H2bTestChild.Call("NativeProcessSupervisor", "RunContainedAsync", request, cancelled.Token)!;
        await task;
        object result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        Assert.Equal("PreCancelled", H2bTestChild.Value(result, "Outcome"));
        Assert.True((bool)H2bTestChild.Value(result, "Cancelled")!);
        Assert.Equal("NotChecked", H2bTestChild.Value(result, "Job")!.ToString());
    }

    [Theory]
    [InlineData("Filter", 200)]
    [InlineData("Condition", 40)]
    [InlineData("Sublayer", 72)]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void WfpInteropMatchesWindowsX64Layout(string name, int size)
    {
        Assert.Equal(8, IntPtr.Size);
        Assert.Equal(size, Marshal.SizeOf(H2bTestChild.Type("WindowsNetworkMethods+" + name)));
    }

    [Fact]
    public void FilterKeysAreStableUniqueAndOwnerScoped()
    {
        Guid owner = Guid.NewGuid();
        Guid[] keys = Enumerable.Range(0, 8).Select(index => (Guid)H2bTestChild.Call("WindowsNetworkEnforcement", "FilterKey", owner, index)!).ToArray();
        Assert.Equal(8, keys.Distinct().Count());
        Assert.Equal(keys[0], H2bTestChild.Call("WindowsNetworkEnforcement", "FilterKey", owner, 0));
        Assert.DoesNotContain((Guid)H2bTestChild.Call("WindowsNetworkEnforcement", "FilterKey", Guid.NewGuid(), 0)!, keys);
        Assert.Throws<ArgumentOutOfRangeException>(() => H2bTestChild.Call("WindowsNetworkEnforcement", "FilterKey", owner, 8));
    }

    [Theory]
    [InlineData("S-1-5-21-1-2-3-1001", "S-1-5-21-1-2-3-1001")]
    [InlineData("S-1-5-21-1-2-3-1001", "S-1-5-21-4-5-6-1002")]
    [InlineData("S-1-1-0", "S-1-5-21-1-2-3-1002")]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void NetworkDescriptorRefusesNonDedicatedIdentity(string user, string restriction) =>
        Assert.Throws<SecurityException>(() => H2bTestChild.Call("WindowsNetworkEnforcement", "IdentityDescriptor", user, restriction));

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void NetworkDescriptorContainsExactlyBothAccessCheckPrincipals()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        byte[] bytes = (byte[])H2bTestChild.Call("WindowsNetworkEnforcement", "IdentityDescriptor", "S-1-5-21-1-2-3-1001", "S-1-5-21-1-2-3-1002")!;
        var descriptor = new RawSecurityDescriptor(bytes, 0);
        Assert.Equal(2, descriptor.DiscretionaryAcl!.Count);
        foreach (CommonAce ace in descriptor.DiscretionaryAcl.Cast<CommonAce>()) Assert.Equal(1, ace.AccessMask);
    }

    [Theory]
    [InlineData(@"C:\owned-other\file", typeof(IOException))]
    [InlineData(@"C:\owned\..\foreign\file", typeof(ArgumentException))]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void StorageContainmentRejectsPrefixAndParentEscapes(string path, Type exception) =>
        Assert.Throws(exception, () => H2bTestChild.Call("NativeStorageBoundary", "RequireWithin", @"C:\owned", path));

    [Fact]
    public void ConfigurationWithoutQualificationNeverAdmitsExecution() =>
        Assert.Throws<SecurityException>(() => H2bTestChild.Call("NativeExecutionEnvironment", "ParsePlan", "{}"));

    [Fact]
    public void OversizedConfigurationIsRejectedBeforeDeserialization() =>
        Assert.Throws<SecurityException>(() => H2bTestChild.Call("NativeExecutionEnvironment", "ParseConfiguration", new string(' ', 65537)));

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task CapturePipeDrainsBeyondRetentionAndReachesEof()
    {
        using var pipe = (IDisposable)H2bTestChild.New("WindowsCapturePipe");
        var reader = (Stream)H2bTestChild.Value(pipe, "Reader")!;
        var handle = (SafeFileHandle)H2bTestChild.Value(pipe, "Writer")!;
        using var writer = new FileStream(handle, FileAccess.Write, 4096, false);
        object capture = H2bTestChild.New("BoundedCapture");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var drain = (Task)capture.GetType().GetMethod("DrainAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(capture, [reader, deadline.Token])!;
        byte[] payload = new byte[262144];
        await writer.WriteAsync(payload, deadline.Token);
        writer.Dispose();
        await drain.WaitAsync(deadline.Token);
        Assert.Equal(65536, H2bTestChild.Value(capture, "Count"));
        Assert.True((bool)H2bTestChild.Value(capture, "Truncated")!);
        Assert.Equal(65536, ((ReadOnlyMemory<byte>)H2bTestChild.Value(capture, "Bytes")!).Length);
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task StagedCorpusIsAnIndependentCopyAndExistingDestinationIsPreserved()
    {
        string root = Path.Combine(Path.GetTempPath(), "H2b-stage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string source = Path.Combine(root, "source"), destination = Path.Combine(root, "copy");
            byte[] original = [1, 2, 3];
            await File.WriteAllBytesAsync(source, original);
            string hash = Convert.ToHexString(SHA256.HashData(original));
            await (Task)H2bTestChild.Call("NativeWorkspace", "StageCopyAsync", source, destination, hash, 3, CancellationToken.None)!;
            await File.WriteAllBytesAsync(destination, [9]);
            Assert.Equal(original, await File.ReadAllBytesAsync(source));
            await Assert.ThrowsAsync<IOException>(async () => await (Task)H2bTestChild.Call("NativeWorkspace", "StageCopyAsync", source, destination, hash, 3, CancellationToken.None)!);
            Assert.Equal(new byte[] { 9 }, await File.ReadAllBytesAsync(destination));
        }
        finally { Directory.Delete(root, true); }
    }
}
