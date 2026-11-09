using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Runner;

// Windows SDK 10.0.26100 fwpmtypes.h/fwptypes.h. Explicit union alignment is significant on x64.
internal static class WindowsNetworkMethods
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Blob { internal uint Size; internal nint Data; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct Display { [MarshalAs(UnmanagedType.LPWStr)] internal string? Name; [MarshalAs(UnmanagedType.LPWStr)] internal string? Description; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct Value { internal uint Type; internal nint Data; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct Condition { internal Guid Key; internal uint Match; internal Value Value; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct Action { internal uint Type; internal Guid Key; }
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct Context { [FieldOffset(0)] internal ulong Raw; [FieldOffset(0)] internal Guid Key; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct Filter
    {
        internal Guid Key;
        internal Display Display;
        internal uint Flags;
        internal nint Provider;
        internal Blob ProviderData;
        internal Guid Layer, Sublayer;
        internal Value Weight;
        internal uint Count;
        internal nint Conditions;
        internal Action Action;
        internal Context Context;
        internal nint Reserved;
        internal ulong Id;
        internal Value EffectiveWeight;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct Sublayer
    {
        internal Guid Key;
        internal Display Display;
        internal uint Flags;
        internal nint Provider;
        internal Blob ProviderData;
        internal ushort Weight;
    }

    [DllImport("fwpuclnt.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint FwpmEngineOpen0(string? server, uint service, nint identity, nint session, out SafeWfpHandle engine);
    [DllImport("fwpuclnt.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint FwpmEngineClose0(nint engine);
    [DllImport("fwpuclnt.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint FwpmTransactionBegin0(SafeWfpHandle engine, uint flags);
    [DllImport("fwpuclnt.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint FwpmTransactionCommit0(SafeWfpHandle engine);
    [DllImport("fwpuclnt.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint FwpmTransactionAbort0(SafeWfpHandle engine);
    [DllImport("fwpuclnt.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint FwpmSubLayerAdd0(SafeWfpHandle engine, in Sublayer layer, byte[] security);
    [DllImport("fwpuclnt.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint FwpmSubLayerDeleteByKey0(SafeWfpHandle engine, in Guid key);
    [DllImport("fwpuclnt.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint FwpmFilterAdd0(SafeWfpHandle engine, in Filter filter, byte[] security, out ulong id);
    [DllImport("fwpuclnt.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint FwpmFilterGetByKey0(SafeWfpHandle engine, in Guid key, out nint filter);
    [DllImport("fwpuclnt.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint FwpmFilterDeleteByKey0(SafeWfpHandle engine, in Guid key);
    [DllImport("fwpuclnt.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern void FwpmFreeMemory0(ref nint memory);
}

internal sealed class SafeWfpHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeWfpHandle() : base(true) { }
    protected override bool ReleaseHandle() => WindowsNetworkMethods.FwpmEngineClose0(handle) == 0;
}
