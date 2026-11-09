using System.Security;
using System.Security.AccessControl;

namespace PiratePDF.Fuzz.Tests;

public sealed class H2bFixtureOwnerPolicyTests
{
    private const string Owner = "S-1-5-21-1-2-3-1001";
    private const string Logon = "S-1-5-5-10-20";
    private const string Package = "S-1-15-2-1-2-3-4-5-6-7";

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void NativeLogonQueryIncludesSidOmittedByManagedGroupsAndRejectsInvalidHandle()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        string sid = (string)H2bTestChild.Call("NativeExecutionIdentity", "LogonSid", identity.AccessToken)!;
        Assert.StartsWith("S-1-5-5-", sid, StringComparison.Ordinal);
        using var invalid = new Microsoft.Win32.SafeHandles.SafeAccessTokenHandle(nint.Zero);
        Assert.Throws<System.ComponentModel.Win32Exception>(() => H2bTestChild.Call("NativeExecutionIdentity", "LogonSid", invalid));
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void FreshFixtureHasOnlyFourNarrowRolesAndNoOwnerWriteControl()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        byte[] before = Descriptor("O:" + Owner + "G:" + Owner + "D:(A;;GA;;;AU)");
        byte[] original = (byte[])before.Clone();
        byte[] bytes = (byte[])H2bTestChild.Call("NativeFilesystemAdmission", "RestrictFixtureOwner", before, Owner, Owner, Logon, Package)!;
        var actual = new RawSecurityDescriptor(bytes, 0);
        Assert.True(actual.ControlFlags.HasFlag(ControlFlags.DiscretionaryAclProtected));
        Assert.Equal(4, actual.DiscretionaryAcl!.Count);
        int[] masks = new int[4];
        for (int index = 0; index < masks.Length; index++)
        {
            var ace = Assert.IsType<CommonAce>(actual.DiscretionaryAcl[index]);
            masks[index] = ace.AccessMask;
            Assert.NotEqual("S-1-5-11", ace.SecurityIdentifier.Value);
        }
        Assert.Equal(new[] { 0x20000, 0x1600a9, 0x1200a9, 0x1200a9 }, masks);
        Assert.Equal(original, before);
    }

    [Theory]
    [InlineData("wrong-owner")]
    [InlineData("broad-logon")]
    [InlineData("same-logon")]
    [InlineData("broad-package")]
    [InlineData("existing-owner-policy")]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void AmbiguousOrExistingPolicyRejects(string fault)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        byte[] bytes = Descriptor("O:" + Owner + "G:" + Owner + "D:(A;;GR;;;" +
            (fault == "existing-owner-policy" ? "S-1-3-4" : Owner) + ")");
        string owner = fault == "wrong-owner" ? "S-1-5-21-1-2-3-1002" : Owner;
        string logon = fault == "broad-logon" ? "S-1-5-11" : Logon;
        string restriction = fault == "same-logon" ? Logon : Owner;
        string package = fault == "broad-package" ? "S-1-15-2-1" : Package;
        Assert.Throws<SecurityException>(() => H2bTestChild.Call("NativeFilesystemAdmission", "RestrictFixtureOwner", bytes, owner, restriction, logon, package));
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static byte[] Descriptor(string sddl)
    {
        var descriptor = new RawSecurityDescriptor(sddl);
        byte[] bytes = new byte[descriptor.BinaryLength]; descriptor.GetBinaryForm(bytes, 0);
        return bytes;
    }
}
