using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Tests;

public sealed class H2bTokenCompatibilityTests
{
    [Theory]
    [InlineData(1, 1u)]
    [InlineData(4, 1u)]
    [InlineData(4, 0x80000000u)]
    public void ValidRestrictedResponsesAreAccepted(int length, uint value) =>
        Validate(true, length, Bytes(value));

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void ZeroStillRejectsInsufficientRestrictions(int length) =>
        Assert.Throws<SecurityException>(() => Validate(true, length, new byte[4]));

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(int.MaxValue)]
    public void UnexpectedReturnedLengthsFailClosed(int length) =>
        Assert.Throws<SecurityException>(() => Validate(true, length, Bytes(1)));

    [Theory]
    [InlineData(2u)]
    [InlineData(255u)]
    [InlineData(257u)]
    public void InvalidOneByteBooleanOrModifiedTailFailsClosed(uint value) =>
        Assert.Throws<SecurityException>(() => Validate(true, 1, Bytes(value)));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void TruncatedOrInconsistentBufferFailsClosed(int capacity) =>
        Assert.Throws<SecurityException>(() => Validate(true, 4, new byte[capacity]));

    [Fact]
    public void ApiFailureCannotBeOverriddenByRestrictedBytes()
    {
        Win32Exception failure = Assert.Throws<Win32Exception>(() => Validate(false, 4, Bytes(1), 5));
        Assert.Equal(5, failure.NativeErrorCode);
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void InvalidHandleFailsTheActualCompatibilityQuery()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        using var invalid = new SafeAccessTokenHandle(nint.Zero);
        Assert.Throws<Win32Exception>(() => H2bTestChild.Call("NativeExecutionIdentity", "VerifyHasRestrictions", invalid));
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void CommonReaderRetainsItsFourByteMinimum()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        using WindowsIdentity current = WindowsIdentity.GetCurrent();
        // TokenSessionId remains on the unchanged DWORD reader, not the class-21 decoder.
        nint session = (nint)H2bTestChild.Call("NativeExecutionIdentity", "Read", current.AccessToken, 12)!;
        try { Assert.Equal(System.Diagnostics.Process.GetCurrentProcess().SessionId, Marshal.ReadInt32(session)); }
        finally { Marshal.FreeHGlobal(session); }
        using var invalid = new SafeAccessTokenHandle(nint.Zero);
        Assert.Throws<SecurityException>(() => H2bTestChild.Call("NativeExecutionIdentity", "Read", invalid, 12));
    }

    private static byte[] Bytes(uint value)
    {
        byte[] bytes = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        return bytes;
    }

    private static void Validate(bool success, int length, byte[] bytes, int error = 0) =>
        H2bTestChild.Call("NativeExecutionIdentity", "ValidateHasRestrictionsResult", success, length, bytes, error);
}
