using System.Reflection;
using System.Runtime.InteropServices;

namespace PiratePDF.Fuzz.Tests;

public sealed class H2bStationCreationContractTests
{
    [Theory]
    [InlineData(0u, 16538502u, "Service-0x0-fc5b86$")]
    [InlineData(0u, 999u, "Service-0x0-3e7$")]
    [InlineData(uint.MaxValue, uint.MaxValue, "Service-0xffffffff-ffffffff$")]
    public void GeneratedNameUsesFullAuthenticationLuid(uint high, uint low, string expected) =>
        Assert.Equal(expected, H2bTestChild.Call("WindowsExecutionDesktop", "AuthenticationStationName", high, low));

    [Fact]
    public void ManagedAndPointerEntryPointsShareExactUnicodeWindowsAbi()
    {
        Type manager = H2bTestChild.Type("WindowsExecutionDesktop");
        foreach (string name in new[] { "CreateWindowStationW", "CreateStationWithPointer" })
        {
            MethodInfo method = manager.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
            DllImportAttribute import = method.GetCustomAttribute<DllImportAttribute>()!;
            Assert.Equal("CreateWindowStationW", import.EntryPoint);
            Assert.Equal(CharSet.Unicode, import.CharSet);
            Assert.True(import.ExactSpelling);
            Assert.True(import.SetLastError);
            Assert.True(typeof(SafeHandle).IsAssignableFrom(method.ReturnType));
            Assert.Equal(DllImportSearchPath.System32, method.GetCustomAttribute<DefaultDllImportSearchPathsAttribute>()!.Paths);
        }
    }

    [Fact]
    public void SecurityAttributesMatchNativePointerAlignmentAndBoolWidth()
    {
        Type attributes = H2bTestChild.Type("WindowsCapturePipe+SecurityAttributes");
        Assert.Equal(nint.Size == 8 ? 24 : 12, Marshal.SizeOf(attributes));
        Assert.Equal(nint.Size, Marshal.OffsetOf(attributes, "Descriptor").ToInt32());
        Assert.Equal(nint.Size * 2, Marshal.OffsetOf(attributes, "Inherit").ToInt32());
    }
}
