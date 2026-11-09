using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;

namespace PiratePDF.Fuzz.Tests;

public sealed class H2bAccessDiagnosisTests
{
    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void SpecificIdentityGrantDoesNotRequireAuthenticatedUsersAce()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        using WindowsIdentity current = WindowsIdentity.GetCurrent();
        string sid = current.User!.Value;
        var descriptor = new RawSecurityDescriptor("O:" + sid + "G:" + sid + "D:(A;;0x1200a9;;;" + sid + ")");
        byte[] bytes = new byte[descriptor.BinaryLength]; descriptor.GetBinaryForm(bytes, 0);
        Assert.True((bool)H2bTestChild.Call("NativeFilesystemAdmission", "AllowsDescriptor", current.AccessToken, bytes, 0x1200a9U)!);
        Assert.False((bool)H2bTestChild.Call("NativeFilesystemAdmission", "AllowsDescriptor", current.AccessToken, bytes, 0xc0000U)!);
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void CounterfactualDescriptorBudgetRejectsBeforeNativeAccessCheck()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        using WindowsIdentity current = WindowsIdentity.GetCurrent();
        var token = current.AccessToken;
        Assert.Throws<SecurityException>(() => H2bTestChild.Call("NativeFilesystemAdmission", "AllowsDescriptor", token, new byte[19], 1U));
        Assert.Throws<SecurityException>(() => H2bTestChild.Call("NativeFilesystemAdmission", "AllowsDescriptor", token, new byte[65537], 1U));
    }
}
