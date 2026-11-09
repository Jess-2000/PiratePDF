using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Real Windows access checks under an impersonation duplicate of the restricted primary token.</summary>
[SupportedOSPlatform("windows")]
internal static class NativeFilesystemAdmission
{
    internal const uint ReadExecute = 0x1200a9;
    internal const uint Write = 0x116;
    internal const uint ChangeSecurity = 0xc0000;

    internal static void VerifyProtectedPath(string path)
    {
        NativeLaunchPolicy.ValidatePath(path);
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new SecurityException("Redirected ACL admission path.");
        byte[] bytes = Security(path);
        var descriptor = new RawSecurityDescriptor(bytes, 0);
        if (descriptor.Owner?.Value is not ("S-1-5-18" or "S-1-5-32-544") || descriptor.DiscretionaryAcl is null ||
            !descriptor.ControlFlags.HasFlag(ControlFlags.DiscretionaryAclProtected))
            throw new SecurityException("Admission resources must have protected Administrator/SYSTEM-owned DACLs.");
    }

    internal static bool Allows(SafeAccessTokenHandle primary, string path, uint desired)
        => AllowsDescriptor(primary, Security(path), desired);

    // Read-only counterfactual ACL diagnosis; never installs the supplied descriptor.
    internal static bool AllowsDescriptor(SafeAccessTokenHandle primary, byte[] descriptor, uint desired)
    {
        if (descriptor.Length is < 20 or > 65536) throw new SecurityException("Invalid access-check descriptor budget.");
        _ = new RawSecurityDescriptor(descriptor, 0);
        if (!DuplicateToken(primary, 2, out SafeAccessTokenHandle impersonation)) throw Error();
        using (impersonation)
        {
            var mapping = new Mapping { Read = 0x120089, Write = 0x120116, Execute = 0x1200a0, All = 0x1f01ff };
            nint privileges = Marshal.AllocHGlobal(4096);
            try
            {
                uint length = 4096;
                if (!AccessCheck(descriptor, impersonation, desired, mapping, privileges, ref length, out uint granted, out bool allowed)) throw Error();
                return allowed && (granted & desired) == desired;
            }
            finally { Marshal.FreeHGlobal(privileges); }
        }
    }

    // Controlled fixture only: suppress implicit owner control while preserving
    // the original controller logon session's ability to restore the DACL.
    // Callers must AccessCheck both actual tokens before installing this descriptor.
    internal static byte[] RestrictFixtureOwner(byte[] original, string ownerSid, string restrictingSid, string logonSid, string packageSid)
    {
        if (original.Length is < 20 or > 65536 || !logonSid.StartsWith("S-1-5-5-", StringComparison.Ordinal) ||
            logonSid == restrictingSid) throw new SecurityException("Invalid fixture restoration identity.");
        var owner = new SecurityIdentifier(ownerSid);
        var logon = new SecurityIdentifier(logonSid);
        var restriction = new SecurityIdentifier(restrictingSid);
        var package = new SecurityIdentifier(packageSid);
        if (!package.Value.StartsWith("S-1-15-2-", StringComparison.Ordinal) || package.BinaryLength != 40 || package == restriction)
            throw new SecurityException("A distinct specific package identity is required.");
        var descriptor = new RawSecurityDescriptor(original, 0);
        if (descriptor.Owner != owner || descriptor.DiscretionaryAcl is null)
            throw new SecurityException("A controller-owned fixture with an explicit DACL is required.");
        var ownerRights = new SecurityIdentifier("S-1-3-4");
        foreach (GenericAce ace in descriptor.DiscretionaryAcl)
            if (ace is KnownAce known && known.SecurityIdentifier == ownerRights)
                throw new SecurityException("Existing owner-rights policy must not be overwritten.");
        // The fresh fixture needs four exact roles, not its parent's inherited modify grants.
        descriptor.DiscretionaryAcl = new RawAcl(2, 4);
        descriptor.DiscretionaryAcl.InsertAce(0, new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 0x20000, ownerRights, false, null));
        descriptor.DiscretionaryAcl.InsertAce(1, new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 0x1600a9, logon, false, null));
        descriptor.DiscretionaryAcl.InsertAce(2, new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 0x1200a9, restriction, false, null));
        descriptor.DiscretionaryAcl.InsertAce(3, new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 0x1200a9, package, false, null));
        descriptor.SetFlags(descriptor.ControlFlags | ControlFlags.DiscretionaryAclProtected);
        byte[] bytes = new byte[descriptor.BinaryLength]; descriptor.GetBinaryForm(bytes, 0);
        return bytes;
    }

    internal static void Verify(SafeAccessTokenHandle token, string path, bool writable)
    {
        VerifyProtectedPath(path);
        if (!Allows(token, path, ReadExecute) || HasAnyAccess(token, path, ChangeSecurity) ||
            (writable ? !Allows(token, path, Write) : HasAnyAccess(token, path, Write | 0x10040)))
            throw new SecurityException("Restricted filesystem access differs from the approved role.");
        if (writable && !HasOwnerRestriction(Security(path)))
            throw new SecurityException("Writable descendants must inherit owner-rights restrictions.");
    }

    internal static bool HasOwnerRestriction(byte[] bytes)
    {
        var descriptor = new RawSecurityDescriptor(bytes, 0);
        if (descriptor.DiscretionaryAcl is null) return false;
        foreach (GenericAce ace in descriptor.DiscretionaryAcl)
            if (ace is CommonAce common && common.AceQualifier == AceQualifier.AccessDenied &&
                common.SecurityIdentifier.Value == "S-1-3-4" && (common.AccessMask & ChangeSecurity) == ChangeSecurity &&
                (common.AceFlags & (AceFlags.ContainerInherit | AceFlags.ObjectInherit)) == (AceFlags.ContainerInherit | AceFlags.ObjectInherit)) return true;
        return false;
    }

    internal static void VerifyDenied(SafeAccessTokenHandle token, string path)
    {
        if (HasAnyAccess(token, path, 1 | Write | ChangeSecurity | 0x10040))
            throw new SecurityException("An excluded resource remains accessible to the native identity.");
    }

    internal static void VerifyRuntime(SafeAccessTokenHandle token, string path)
    {
        NativeLaunchPolicy.ValidatePath(path);
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new SecurityException("Redirected runtime resource.");
        var descriptor = new RawSecurityDescriptor(Security(path), 0);
        if (descriptor.Owner?.Value is not ("S-1-5-18" or "S-1-5-32-544" or
            "S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464") || descriptor.DiscretionaryAcl is null ||
            !Allows(token, path, ReadExecute) || HasAnyAccess(token, path, Write | ChangeSecurity | 0x10040))
            throw new SecurityException("Unapproved or writable Windows runtime resource.");
    }

    private static bool HasAnyAccess(SafeAccessTokenHandle token, string path, uint rights)
    {
        // An aggregate AccessCheck succeeds only when ALL requested rights are granted.
        // Denial admission must reject even one writable or security-changing capability.
        for (uint bit = 1; bit != 0; bit <<= 1)
            if ((rights & bit) != 0 && Allows(token, path, bit)) return true;
        return false;
    }

    private static byte[] Security(string path)
    {
        // AccessCheck requires both owner and primary group, even when only DACL rights are requested.
        uint code = GetNamedSecurityInfoW(path, 1, 7, out _, out _, out _, out _, out nint pointer);
        if (code != 0) throw new Win32Exception(unchecked((int)code));
        try
        {
            uint length = GetSecurityDescriptorLength(pointer);
            if (length is < 20 or > 65536) throw new SecurityException("Invalid security descriptor size.");
            byte[] result = new byte[length];
            Marshal.Copy(pointer, result, 0, result.Length);
            return result;
        }
        finally { LocalFree(pointer); }
    }

    private static Win32Exception Error() => new(Marshal.GetLastPInvokeError());
    [StructLayout(LayoutKind.Sequential)] private struct Mapping { internal uint Read, Write, Execute, All; }
    [DllImport("advapi32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint GetNamedSecurityInfoW(string name, int type, uint information, out nint owner, out nint group,
        out nint dacl, out nint sacl, out nint descriptor);
    [DllImport("advapi32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint GetSecurityDescriptorLength(nint descriptor);
    [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DuplicateToken(SafeAccessTokenHandle token, int level, out SafeAccessTokenHandle duplicate);
    [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool AccessCheck(byte[] security, SafeAccessTokenHandle token, uint desired,
        in Mapping mapping, nint privileges, ref uint length, out uint granted, [MarshalAs(UnmanagedType.Bool)] out bool allowed);
    [DllImport("kernel32.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern nint LocalFree(nint memory);
}
