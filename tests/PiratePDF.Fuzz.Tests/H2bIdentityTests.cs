using System.Security;
using System.Security.Principal;

namespace PiratePDF.Fuzz.Tests;

public sealed class H2bIdentityTests
{
    [Theory]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    [InlineData("Administrator")]
    [InlineData("pph2b_short")]
    [InlineData("pph2b_zzzzzzzz")]
    public void InvalidAccountNamesAreRejectedBeforeCredentialUse(string account)
    {
        using var secret = new SecureString();
        Assert.Throws<ArgumentException>(() => H2bTestChild.Call("NativeExecutionIdentity", "Open", account, secret,
            "S-1-5-21-1-2-3-1001", "S-1-5-21-1-2-3-1002"));
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void ControllerCannotBeSelectedAsNativeIdentity()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        using WindowsIdentity current = WindowsIdentity.GetCurrent();
        string user = current.User!.Value;
        string domain = current.User.AccountDomainSid!.Value;
        using var secret = new SecureString();
        Assert.Throws<SecurityException>(() => H2bTestChild.Call("NativeExecutionIdentity", "Open", "pph2b_12345678", secret,
            user, domain + "-999999"));
    }
}
