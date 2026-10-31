using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Persistent per-account WFP filters. Install/remove are explicit privileged operations, never admission side effects.</summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsNetworkEnforcement : IDisposable
{
    private readonly SafeWfpHandle engine;
    private readonly Guid owner;
    private readonly string account, restriction;
    private static readonly Guid UserCondition = new("af043a0a-b34d-4f86-979c-c90371af6e66");
    private static readonly Guid[] Layers =
    [
        new("c38d57d1-05a7-4c33-904f-7fbceee60e82"), new("4a72393b-319f-44bc-84c3-ba54dcb3b6b4"),
        new("e1cd9fe7-f4b5-4273-96c0-592e487b8650"), new("a3b42c97-9f04-4672-b87e-cee9c483257f"),
        new("88bb5dad-76d7-4227-9c71-df0a3ed7be7e"), new("7ac9de24-17dd-4814-b4bd-a9fbc95a321b"),
        new("1247d66d-0b60-4a15-8d44-7155d0f53a0c"), new("55a650e1-5f0a-4eca-a653-88f53b26aa8c")
    ];

    internal WindowsNetworkEnforcement(Guid owner, string account, string restriction)
    {
        if (!Environment.Is64BitProcess || owner == Guid.Empty) throw new ArgumentException("A unique x64 WFP owner is required.");
        this.owner = owner;
        this.account = new SecurityIdentifier(account).Value;
        this.restriction = new SecurityIdentifier(restriction).Value;
        if (this.account == this.restriction) throw new ArgumentException("Distinct account/restricting identities are required.");
        Check(WindowsNetworkMethods.FwpmEngineOpen0(null, 10, nint.Zero, nint.Zero, out engine));
    }

    internal static byte[] IdentityDescriptor(string account, string restriction)
    {
        var user = new SecurityIdentifier(account);
        var restricted = new SecurityIdentifier(restriction);
        if (user.AccountDomainSid is null || restricted.AccountDomainSid is null ||
            !user.AccountDomainSid.Equals(restricted.AccountDomainSid) || user.Equals(restricted))
            throw new SecurityException("A dedicated local user and distinct restricting principal are required.");
        // Both access-check passes must match; never use Everyone, executable paths, or a condition excluding loopback.
        return Descriptor("D:(A;;CC;;;" + user.Value + ")(A;;CC;;;" + restricted.Value + ")");
    }

    private static byte[] Descriptor(string sddl)
    {
        var descriptor = new RawSecurityDescriptor(sddl);
        byte[] bytes = new byte[descriptor.BinaryLength];
        descriptor.GetBinaryForm(bytes, 0);
        return bytes;
    }

    internal void Install()
    {
        RequireAdministrator();
        byte[] objectSecurity = Descriptor("O:BAG:BAD:P(A;;GA;;;SY)(A;;GA;;;BA)");
        byte[] identity = IdentityDescriptor(account, restriction);
        Check(WindowsNetworkMethods.FwpmTransactionBegin0(engine, 0));
        bool committed = false;
        try
        {
            var layer = new WindowsNetworkMethods.Sublayer
            { Key = owner, Display = new() { Name = "PiratePDF H2b " + owner.ToString("N") }, Flags = 1, Weight = ushort.MaxValue };
            Check(WindowsNetworkMethods.FwpmSubLayerAdd0(engine, layer, objectSecurity));
            using var allocation = new FilterAllocation(identity);
            for (int index = 0; index < Layers.Length; index++)
            {
                var filter = new WindowsNetworkMethods.Filter
                {
                    Key = FilterKey(owner, index), Display = layer.Display, Flags = 1 | 8,
                    Layer = Layers[index], Sublayer = owner, Weight = new() { Type = 1, Data = 15 },
                    Count = 1, Conditions = allocation.Condition, Action = new() { Type = 0x1001 }
                };
                Check(WindowsNetworkMethods.FwpmFilterAdd0(engine, filter, objectSecurity, out _));
            }
            Check(WindowsNetworkMethods.FwpmTransactionCommit0(engine));
            committed = true;
        }
        finally { if (!committed) Check(WindowsNetworkMethods.FwpmTransactionAbort0(engine)); }
        Verify();
    }

    internal void Verify()
    {
        byte[] expected = IdentityDescriptor(account, restriction);
        for (int index = 0; index < Layers.Length; index++)
        {
            Guid key = FilterKey(owner, index);
            Check(WindowsNetworkMethods.FwpmFilterGetByKey0(engine, key, out nint pointer));
            try
            {
                var filter = Marshal.PtrToStructure<WindowsNetworkMethods.Filter>(pointer);
                if (filter.Key != key || filter.Layer != Layers[index] || filter.Sublayer != owner || filter.Flags != 9 ||
                    filter.Action.Type != 0x1001 || filter.Count != 1 || filter.Conditions == nint.Zero || filter.Provider != nint.Zero ||
                    filter.Weight.Type != 1 || filter.Weight.Data != 15)
                    throw new SecurityException("WFP policy identity/action mismatch.");
                var condition = Marshal.PtrToStructure<WindowsNetworkMethods.Condition>(filter.Conditions);
                if (condition.Key != UserCondition || condition.Match != 0 || condition.Value.Type != 14 || condition.Value.Data == nint.Zero)
                    throw new SecurityException("WFP account condition mismatch.");
                var blob = Marshal.PtrToStructure<WindowsNetworkMethods.Blob>(condition.Value.Data);
                if (blob.Size != expected.Length || blob.Data == nint.Zero) throw new SecurityException("WFP descriptor size mismatch.");
                byte[] actual = new byte[expected.Length];
                Marshal.Copy(blob.Data, actual, 0, actual.Length);
                if (!actual.AsSpan().SequenceEqual(expected)) throw new SecurityException("WFP account descriptor mismatch.");
            }
            finally { WindowsNetworkMethods.FwpmFreeMemory0(ref pointer); }
        }
    }

    internal void Remove()
    {
        RequireAdministrator();
        Verify(); // No deletion of missing, foreign, partially matching or ambiguous policy.
        Check(WindowsNetworkMethods.FwpmTransactionBegin0(engine, 0));
        bool committed = false;
        try
        {
            for (int index = 0; index < Layers.Length; index++)
            { Guid key = FilterKey(owner, index); Check(WindowsNetworkMethods.FwpmFilterDeleteByKey0(engine, key)); }
            Check(WindowsNetworkMethods.FwpmSubLayerDeleteByKey0(engine, owner));
            Check(WindowsNetworkMethods.FwpmTransactionCommit0(engine));
            committed = true;
        }
        finally { if (!committed) Check(WindowsNetworkMethods.FwpmTransactionAbort0(engine)); }
    }

    internal static Guid FilterKey(Guid owner, int index)
    {
        if (owner == Guid.Empty || index is < 0 or >= 8) throw new ArgumentOutOfRangeException(nameof(index));
        byte[] input = System.Text.Encoding.ASCII.GetBytes("PiratePDF-H2b-WFP-v1:" + owner.ToString("N") + ":" + index);
        return new Guid(System.Security.Cryptography.SHA256.HashData(input).AsSpan(0, 16));
    }

    private static void RequireAdministrator()
    {
        using WindowsIdentity current = WindowsIdentity.GetCurrent();
        if (!new WindowsPrincipal(current).IsInRole(WindowsBuiltInRole.Administrator)) throw new UnauthorizedAccessException("Explicit Administrator provisioning is required.");
    }

    private static void Check(uint code) { if (code != 0) throw new Win32Exception(unchecked((int)code), "WFP operation refused."); }
    public void Dispose() => engine.Dispose(); // Persistent filters survive controller interruption; disposal never removes policy.

    private sealed class FilterAllocation : IDisposable
    {
        private readonly nint bytes, blob;
        internal nint Condition { get; }
        internal FilterAllocation(byte[] descriptor)
        {
            bytes = Marshal.AllocHGlobal(descriptor.Length);
            Marshal.Copy(descriptor, 0, bytes, descriptor.Length);
            blob = Marshal.AllocHGlobal(Marshal.SizeOf<WindowsNetworkMethods.Blob>());
            Marshal.StructureToPtr(new WindowsNetworkMethods.Blob { Size = (uint)descriptor.Length, Data = bytes }, blob, false);
            Condition = Marshal.AllocHGlobal(Marshal.SizeOf<WindowsNetworkMethods.Condition>());
            Marshal.StructureToPtr(new WindowsNetworkMethods.Condition { Key = UserCondition, Value = new() { Type = 14, Data = blob } }, Condition, false);
        }
        public void Dispose() { Marshal.FreeHGlobal(Condition); Marshal.FreeHGlobal(blob); Marshal.FreeHGlobal(bytes); }
    }
}
