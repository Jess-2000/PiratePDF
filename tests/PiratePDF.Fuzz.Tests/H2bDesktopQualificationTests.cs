using System.Security.AccessControl;

namespace PiratePDF.Fuzz.Tests;

public sealed class H2bDesktopQualificationTests
{
    private const string Controller = "S-1-5-21-1-2-3-1001";
    private const string Restriction = "S-1-5-21-1-2-3-1002";
    private const string Package = "S-1-15-2-1-2-3-4-5-6-7";

    [Theory]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    [InlineData(false, 2, 0x2000a)]
    [InlineData(true, 0x81, 0x20083)]
    public void QualificationSeparatesWorkerAndControllerRights(bool desktop, int workerMask, int controllerMask)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        string sddl = (string)H2bTestChild.Call("WindowsExecutionDesktop", "QualificationDescriptor",
            Controller, Restriction, Package, desktop)!;
        var descriptor = new RawSecurityDescriptor(sddl);
        Assert.Equal(Controller, descriptor.Owner!.Value);
        Assert.True(descriptor.ControlFlags.HasFlag(ControlFlags.DiscretionaryAclProtected));
        Assert.Equal(3, descriptor.DiscretionaryAcl!.Count);
        var worker = Assert.IsType<CommonAce>(descriptor.DiscretionaryAcl[2]);
        Assert.Equal(Package, worker.SecurityIdentifier.Value);
        Assert.Equal(workerMask, worker.AccessMask);
        Assert.Equal(0, worker.AccessMask & (0x40000 | 0x80000));
        Assert.Equal(controllerMask, Assert.IsType<CommonAce>(descriptor.DiscretionaryAcl[0]).AccessMask);
        Assert.Equal(Restriction, Assert.IsType<CommonAce>(descriptor.DiscretionaryAcl[1]).SecurityIdentifier.Value);
        if (!desktop) Assert.Equal(0, worker.AccessMask & (4 | 0x20 | 8 | 0x10 | 0x200 | 0x40));
        else Assert.Equal(0, worker.AccessMask & (8 | 0x10 | 0x20 | 0x100));
    }

    [Theory]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    [InlineData("S-1-15-2-1")]
    [InlineData("S-1-15-2-2")]
    [InlineData("S-1-1-0")]
    [InlineData("S-1-5-11")]
    public void BroadPrincipalsCannotReplaceSpecificPackage(string package) =>
        Assert.Throws<ArgumentException>(() => H2bTestChild.Call("WindowsExecutionDesktop", "QualificationDescriptor",
            Controller, Restriction, package, false));
}
