using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Tests;

/// <summary>Real OS counterexample: a restricting SID is not a filesystem namespace boundary.</summary>
public sealed class H2bSecurityBoundaryTests
{
    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void FullRestrictedTokenCanWriteTestOwnedOutsideFileWithNullDacl()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        string path = Path.GetTempFileName();
        var file = new FileInfo(path);
        FileSecurity original = file.GetAccessControl();
        nint sidBuffer = nint.Zero, emptyAcl = nint.Zero;
        try
        {
            using WindowsIdentity current = WindowsIdentity.GetCurrent();
            // Preserve an existing restriction when the test host is already restricted:
            // CreateRestrictedToken intersects the old and supplied restricting lists.
            SecurityIdentifier restricting = FirstRestriction(current.AccessToken) ?? new SecurityIdentifier("S-1-5-21-111-222-333-12345");
            byte[] sid = new byte[restricting.BinaryLength];
            restricting.GetBinaryForm(sid, 0);
            sidBuffer = Marshal.AllocHGlobal(sid.Length);
            Marshal.Copy(sid, 0, sidBuffer, sid.Length);
            SidAttributes[] restricted = [new() { Sid = sidBuffer }];
            // DISABLE_MAX_PRIVILEGE only: neither WRITE_RESTRICTED nor SANDBOX_INERT.
            if (!CreateRestrictedToken(current.AccessToken, 1, 0, nint.Zero, 0, nint.Zero, 1, restricted, out SafeAccessTokenHandle token))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            using (token)
            {
                Assert.True(IsTokenRestricted(token));
                Assert.Equal(restricting, FirstRestriction(token));
                var acl = new RawAcl(2, 0);
                byte[] bytes = new byte[acl.BinaryLength];
                acl.GetBinaryForm(bytes, 0);
                emptyAcl = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, emptyAcl, bytes.Length);
                SetDacl(path, emptyAcl);
                // Negative control confirms the write is checked under the restricted token.
                Assert.Throws<UnauthorizedAccessException>(() =>
                {
                    if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
                    WindowsIdentity.RunImpersonated(token, () => File.WriteAllBytes(path, new byte[] { 1 }));
                });
                SetDacl(path, nint.Zero);
                // No process launch, network traffic, credentials, or external resource is used.
                WindowsIdentity.RunImpersonated(token, () => File.WriteAllBytes(path, new byte[] { 2 }));
                Assert.Equal(new byte[] { 2 }, File.ReadAllBytes(path));
            }
        }
        finally
        {
            if (emptyAcl != nint.Zero) Marshal.FreeHGlobal(emptyAcl);
            if (sidBuffer != nint.Zero) Marshal.FreeHGlobal(sidBuffer);
            // Restore the original descriptor before deleting only this test-owned file.
            var restore = new FileSecurity();
            restore.SetSecurityDescriptorBinaryForm(original.GetSecurityDescriptorBinaryForm(), AccessControlSections.Access);
            file.SetAccessControl(restore);
            File.Delete(path);
        }
    }

    private static SecurityIdentifier FirstRestriction(SafeAccessTokenHandle token)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        _ = GetTokenInformation(token, 11, nint.Zero, 0, out int size);
        if (size is < 4 or > 65536) throw new InvalidOperationException("Invalid restricting-SID buffer size.");
        nint buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (!GetTokenInformation(token, 11, buffer, size, out _)) throw new Win32Exception(Marshal.GetLastPInvokeError());
            int count = Marshal.ReadInt32(buffer);
            if (count is < 0 or > 128) throw new InvalidOperationException("Invalid restricting-SID count.");
            return count == 0 ? null : new SecurityIdentifier(Marshal.ReadIntPtr(buffer, nint.Size));
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static void SetDacl(string path, nint acl)
    {
        uint code = SetNamedSecurityInfoW(path, 1, 4 | 0x80000000, nint.Zero, nint.Zero, acl, nint.Zero);
        if (code != 0) throw new Win32Exception(unchecked((int)code));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SidAttributes { internal nint Sid; internal uint Attributes; }
    [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateRestrictedToken(SafeAccessTokenHandle existing, uint flags, uint disabledCount, nint disabled,
        uint deletedCount, nint deleted, uint restrictedCount, [In] SidAttributes[] restricted, out SafeAccessTokenHandle token);
    [DllImport("advapi32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsTokenRestricted(SafeAccessTokenHandle token);
    [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetTokenInformation(SafeAccessTokenHandle token, int informationClass, nint buffer, int size, out int required);
    [DllImport("advapi32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint SetNamedSecurityInfoW(string path, int objectType, uint information, nint owner, nint group, nint dacl, nint sacl);
}
