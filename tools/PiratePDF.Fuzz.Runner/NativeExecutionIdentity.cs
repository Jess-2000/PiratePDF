using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Credential-consuming boundary; never creates accounts or stores credentials in evidence.</summary>
[SupportedOSPlatform("windows")]
internal sealed class NativeExecutionIdentity : IDisposable
{
    private static readonly string[] RequiredAccountRights = ["SeBatchLogonRight", "SeDenyInteractiveLogonRight", "SeDenyRemoteInteractiveLogonRight", "SeDenyNetworkLogonRight"];
    internal SafeAccessTokenHandle Token { get; }
    internal string AccountSid { get; }
    internal string RestrictingSid { get; }

    private NativeExecutionIdentity(SafeAccessTokenHandle token, string account, string restriction)
    { Token = token; AccountSid = account; RestrictingSid = restriction; }

    internal static NativeExecutionIdentity Open(string account, SecureString password, string expectedSid, string restrictingSid)
    {
        if (!account.StartsWith("pph2b_", StringComparison.Ordinal) || account.Length != 14 ||
            !account.AsSpan(6).ToString().All(Uri.IsHexDigit)) throw new ArgumentException("Expected a dedicated H2b account.");
        var expected = new SecurityIdentifier(expectedSid);
        var restriction = new SecurityIdentifier(restrictingSid);
        SecurityIdentifier? domain = expected.AccountDomainSid;
        SecurityIdentifier? restrictionDomain = restriction.AccountDomainSid;
        if (domain is null || restrictionDomain is null || !domain.Equals(restrictionDomain) || expected.Equals(restriction))
            throw new ArgumentException("A distinct local restricting principal is required.");
        using WindowsIdentity current = WindowsIdentity.GetCurrent();
        SecurityIdentifier? controller = current.User;
        if (controller is null || expected.Equals(controller)) throw new SecurityException("The controller identity cannot be the native identity.");
        if (!expected.Equals(new NTAccount(Environment.MachineName, account).Translate(typeof(SecurityIdentifier))))
            throw new SecurityException("Dedicated account identity mismatch.");

        nint secret = Marshal.SecureStringToGlobalAllocUnicode(password);
        SafeAccessTokenHandle original;
        try
        {
            if (!WindowsIdentityMethods.LogonUserW(account, Environment.MachineName, secret, 4, 0, out original))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
        finally { Marshal.ZeroFreeGlobalAllocUnicode(secret); }
        using (original)
        {
            if (User(original) != expectedSid) throw new SecurityException("Logon token does not match the dedicated account.");
            using var originalIdentity = new WindowsIdentity(original.DangerousGetHandle());
            if (new WindowsPrincipal(originalIdentity).IsInRole(WindowsBuiltInRole.Administrator))
                throw new SecurityException("Native account is an administrator.");
            if (!WindowsIdentityMethods.LookupPrivilegeValueW(null, "SeChangeNotifyPrivilege", out long traverse))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            WindowsIdentityMethods.Privilege[] deleted = Privileges(original).Where(value =>
                ((long)value.High << 32 | value.Low) != traverse).ToArray();
            byte[] sid = new byte[restriction.BinaryLength];
            restriction.GetBinaryForm(sid, 0);
            nint sidBuffer = Marshal.AllocHGlobal(sid.Length);
            try
            {
                Marshal.Copy(sid, 0, sidBuffer, sid.Length);
                // Flags zero: full restriction, no WRITE_RESTRICTED or SANDBOX_INERT.
                if (!WindowsIdentityMethods.CreateRestrictedToken(original, 0, 0, nint.Zero, (uint)deleted.Length,
                    deleted, 1, [new() { Sid = sidBuffer }], out SafeAccessTokenHandle token))
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                try
                {
                    if (User(token) != expectedSid || Privileges(token).Any(value => ((long)value.High << 32 | value.Low) != traverse))
                        throw new SecurityException("Unexpected restricted-token identity or privileges.");
                    nint groups = Read(token, 11);
                    try
                    {
                        if (Marshal.ReadInt32(groups) != 1 || new SecurityIdentifier(Marshal.ReadIntPtr(groups, nint.Size)).Value != restrictingSid)
                            throw new SecurityException("Restricting SID mismatch.");
                    }
                    finally { Marshal.FreeHGlobal(groups); }
                    var identity = new NativeExecutionIdentity(token, expectedSid, restrictingSid);
                    identity.Verify(token);
                    return identity;
                }
                catch { token.Dispose(); throw; }
            }
            finally { Marshal.FreeHGlobal(sidBuffer); }
        }
    }

    internal static string LogonSid(SafeAccessTokenHandle token)
    {
        // WindowsIdentity.Groups deliberately omits SE_GROUP_LOGON_ID entries.
        nint buffer = Marshal.AllocHGlobal(256);
        try
        {
            if (!WindowsIdentityMethods.GetTokenInformation(token, 28, buffer, 256, out int returned))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            if (returned is < 32 or > 256 || Marshal.ReadInt32(buffer) != 1)
                throw new SecurityException("A single bounded logon SID is required.");
            nint pointer = Marshal.ReadIntPtr(buffer, nint.Size);
            long offset = pointer.ToInt64() - buffer.ToInt64();
            uint attributes = unchecked((uint)Marshal.ReadInt32(buffer, nint.Size * 2));
            if (offset < nint.Size * 3 || offset > returned - 20 || Marshal.ReadByte(pointer, 1) != 3 ||
                (attributes & 0xc0000000) != 0xc0000000)
                throw new SecurityException("Invalid logon SID structure.");
            string sid = new SecurityIdentifier(pointer).Value;
            if (!sid.StartsWith("S-1-5-5-", StringComparison.Ordinal)) throw new SecurityException("Unexpected logon SID authority.");
            return sid;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    internal void Verify(SafeAccessTokenHandle token)
    {
        if (User(token) != AccountSid) throw new SecurityException("Launched token user mismatch.");
        nint session = Read(token, 12);
        try
        {
            using var controllerProcess = System.Diagnostics.Process.GetCurrentProcess();
            if (Marshal.ReadInt32(session) != controllerProcess.SessionId)
                throw new SecurityException("Native/controller session mismatch; no session privilege escalation is permitted.");
        }
        finally { Marshal.FreeHGlobal(session); }
        VerifyHasRestrictions(token);
        foreach ((int information, int expected) in new[] { (8, 1), (15, 0), (24, 0), (26, 0) })
        {
            nint value = Read(token, information);
            try { if (Marshal.ReadInt32(value) != expected) throw new SecurityException("Unexpected token restriction/type/virtualization/UI state."); }
            finally { Marshal.FreeHGlobal(value); }
        }
        nint integrity = Read(token, 25);
        try
        {
            var sid = new SecurityIdentifier(Marshal.ReadIntPtr(integrity));
            if (!int.TryParse(sid.Value.AsSpan(sid.Value.LastIndexOf('-') + 1), out int level) || level is < 0x1000 or > 0x2000)
                throw new SecurityException("Native token must have low or medium integrity.");
        }
        finally { Marshal.FreeHGlobal(integrity); }
        nint groups = Read(token, 11);
        try
        {
            if (Marshal.ReadInt32(groups) != 1 || new SecurityIdentifier(Marshal.ReadIntPtr(groups, nint.Size)).Value != RestrictingSid)
                throw new SecurityException("Native restricting SID mismatch.");
        }
        finally { Marshal.FreeHGlobal(groups); }
        if (!WindowsIdentityMethods.LookupPrivilegeValueW(null, "SeChangeNotifyPrivilege", out long traverse)) throw new Win32Exception(Marshal.GetLastPInvokeError());
        if (Privileges(token).Any(value => ((long)value.High << 32 | value.Low) != traverse)) throw new SecurityException("Unexpected native privileges.");
    }

    internal static void VerifyLogonRights(string accountSid)
    {
        var attributes = new WindowsIdentityMethods.LsaAttributes { Length = (uint)Marshal.SizeOf<WindowsIdentityMethods.LsaAttributes>() };
        uint status = WindowsIdentityMethods.LsaOpenPolicy(nint.Zero, attributes, 0x800, out nint policy);
        if (status != 0) throw new Win32Exception((int)WindowsIdentityMethods.LsaNtStatusToWinError(status));
        var sid = new SecurityIdentifier(accountSid);
        byte[] bytes = new byte[sid.BinaryLength];
        sid.GetBinaryForm(bytes, 0);
        nint buffer = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, buffer, bytes.Length);
            status = WindowsIdentityMethods.LsaEnumerateAccountRights(policy, buffer, out nint rights, out uint count);
            if (status != 0) throw new Win32Exception((int)WindowsIdentityMethods.LsaNtStatusToWinError(status));
            try
            {
                if (count > 128) throw new SecurityException("Unbounded account-right list.");
                var names = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 0; index < count; index++)
                {
                    var value = Marshal.PtrToStructure<WindowsIdentityMethods.LsaString>(rights + index * Marshal.SizeOf<WindowsIdentityMethods.LsaString>());
                    if (value.Length > 512 || value.Length % 2 != 0 || value.Buffer == nint.Zero) throw new SecurityException("Invalid account-right name.");
                    names.Add(Marshal.PtrToStringUni(value.Buffer, value.Length / 2)!);
                }
                if (!RequiredAccountRights.All(names.Contains) ||
                    names.Contains("SeServiceLogonRight") || names.Contains("SeInteractiveLogonRight") || names.Contains("SeRemoteInteractiveLogonRight"))
                    throw new SecurityException("Dedicated account logon policy is not the approved batch-only policy.");
            }
            finally { _ = WindowsIdentityMethods.LsaFreeMemory(rights); }
        }
        finally { Marshal.FreeHGlobal(buffer); _ = WindowsIdentityMethods.LsaClose(policy); }
    }

    private static string User(SafeAccessTokenHandle token)
    {
        nint buffer = Read(token, 1);
        try { return new SecurityIdentifier(Marshal.ReadIntPtr(buffer)).Value; }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static WindowsIdentityMethods.Privilege[] Privileges(SafeAccessTokenHandle token)
    {
        nint buffer = Read(token, 3);
        try
        {
            int count = Marshal.ReadInt32(buffer);
            if (count is < 0 or > 128) throw new SecurityException("Unbounded privilege list.");
            var values = new WindowsIdentityMethods.Privilege[count];
            for (int index = 0; index < count; index++)
                values[index] = Marshal.PtrToStructure<WindowsIdentityMethods.Privilege>(buffer + 4 + index * 12);
            return values;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static void VerifyHasRestrictions(SafeAccessTokenHandle token)
    {
        nint buffer = Marshal.AllocHGlobal(4);
        try
        {
            // Observed native reproduction: token-verifier-20261016-214134/native-probe.log.
            // Explicit compatibility policy, not an officially supported cross-version contract:
            // https://learn.microsoft.com/en-us/answers/questions/6000453/gettokeninformation-tokenhasrestrictions-21-return
            Marshal.WriteInt32(buffer, 0);
            bool success = WindowsIdentityMethods.GetTokenInformation(token, 21, buffer, 4, out int length);
            int error = Marshal.GetLastPInvokeError();
            byte[] bytes = new byte[4];
            Marshal.Copy(buffer, bytes, 0, bytes.Length);
            ValidateHasRestrictionsResult(success, length, bytes, error);
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static void ValidateHasRestrictionsResult(bool success, int length, byte[] bytes, int error)
    {
        if (!success) throw new Win32Exception(error);
        if (bytes.Length != 4 || length is not (1 or 4))
            throw new SecurityException("Invalid TokenHasRestrictions response length.");
        bool restricted;
        if (length == 1)
        {
            if (bytes[0] > 1 || bytes[1] != 0 || bytes[2] != 0 || bytes[3] != 0)
                throw new SecurityException("Invalid one-byte TokenHasRestrictions response.");
            restricted = bytes[0] == 1;
        }
        else
        {
            restricted = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(bytes) != 0;
        }
        if (!restricted) throw new SecurityException("Native token must have restrictions.");
    }

    private static nint Read(SafeAccessTokenHandle token, int informationClass)
    {
        WindowsIdentityMethods.GetTokenInformation(token, informationClass, nint.Zero, 0, out int size);
        if (size is < 4 or > 65536) throw new SecurityException("Invalid token information size.");
        nint buffer = Marshal.AllocHGlobal(size);
        if (WindowsIdentityMethods.GetTokenInformation(token, informationClass, buffer, size, out _)) return buffer;
        Marshal.FreeHGlobal(buffer);
        throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    public void Dispose() => Token.Dispose();
}
