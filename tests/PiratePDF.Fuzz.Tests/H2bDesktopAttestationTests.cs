using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;

namespace PiratePDF.Fuzz.Tests;

public sealed class H2bDesktopAttestationTests
{
    private static readonly byte[] Challenge = Enumerable.Range(1, 16).Select(value => (byte)value).ToArray();
    private const string Expected = "Service-0x0-123$\\pph2b_12345678";

    [Fact]
    public void CanonicalFrameDecodesOnlyOnce()
    {
        object validator = Validator();
        object measurement = Accept(validator, Frame());
        Assert.Equal(0x100UL, H2bTestChild.Value(measurement, "StationHandle"));
        Assert.Equal(0x200UL, H2bTestChild.Value(measurement, "DesktopHandle"));
        Assert.Throws<SecurityException>(() => Accept(validator, Frame()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(559)]
    [InlineData(561)]
    public void WrongLengthRejects(int length) =>
        Assert.Throws<SecurityException>(() => Accept(Validator(), new byte[length]));

    [Theory]
    [InlineData(0)] // magic
    [InlineData(8)] // challenge
    [InlineData(24)] // PID
    [InlineData(28)] // TID
    [InlineData(48)] // station
    [InlineData(304)] // desktop
    public void WrongIdentityRejects(int offset)
    {
        byte[] bytes = Frame(); bytes[offset] ^= 1;
        Assert.Throws<SecurityException>(() => Accept(Validator(), bytes));
    }

    [Fact]
    public void RejectedAttemptCannotBeReplayed()
    {
        object validator = Validator();
        Assert.Throws<SecurityException>(() => Accept(validator, []));
        Assert.Throws<SecurityException>(() => Accept(validator, Frame()));
    }

    [Fact]
    public void ParentChallengeIsCopied()
    {
        byte[] challenge = (byte[])Challenge.Clone();
        object validator = H2bTestChild.New("NativeDesktopAttestation", challenge, 123u, 456u, Expected);
        challenge[0] ^= 1;
        _ = Accept(validator, Frame());
    }

    [Theory]
    [InlineData(32, 0UL)]
    [InlineData(40, 0UL)]
    [InlineData(32, ulong.MaxValue)]
    [InlineData(40, ulong.MaxValue)]
    public void InvalidHandleValuesReject(int offset, ulong value)
    {
        byte[] bytes = Frame(); BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(offset), value);
        Assert.Throws<SecurityException>(() => Accept(Validator(), bytes));
    }

    [Theory]
    [InlineData(49, 1)] // non-ASCII UTF16
    [InlineData(80, 1)] // nonzero station padding after NUL
    [InlineData(350, 1)] // nonzero desktop padding after NUL
    [InlineData(48, 0)] // empty name followed by data
    [InlineData(48, 92)] // path separator
    public void NoncanonicalNamesReject(int offset, byte value)
    {
        byte[] bytes = Frame(); bytes[offset] = value;
        Assert.Throws<SecurityException>(() => Accept(Validator(), bytes));
    }

    [Fact]
    public void UnterminatedNameRejects()
    {
        byte[] bytes = Frame();
        for (int index = 48; index < 304; index += 2) { bytes[index] = (byte)'A'; bytes[index + 1] = 0; }
        Assert.Throws<SecurityException>(() => Accept(Validator(), bytes));
    }

    [Theory]
    [InlineData(15, 123u, 456u, Expected)]
    [InlineData(16, 0u, 456u, Expected)]
    [InlineData(16, 123u, 0u, Expected)]
    [InlineData(16, 123u, 456u, "WinSta0")]
    [InlineData(16, 123u, 456u, "a\\b\\c")]
    [InlineData(16, 123u, 456u, "a\\")]
    public void InvalidExpectedIdentityRejects(int nonceLength, uint pid, uint tid, string path) =>
        Assert.Throws<ArgumentException>(() => H2bTestChild.New("NativeDesktopAttestation", new byte[nonceLength], pid, tid, path));

    private static object Validator() => H2bTestChild.New("NativeDesktopAttestation", Challenge, 123u, 456u, Expected);

    private static object Accept(object validator, byte[] bytes)
    {
        try { return validator.GetType().GetMethod("Accept", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(validator, [bytes])!; }
        catch (TargetInvocationException failure) when (failure.InnerException is not null)
        { ExceptionDispatchInfo.Capture(failure.InnerException).Throw(); throw; }
    }

    private static byte[] Frame()
    {
        byte[] bytes = new byte[560];
        "PPH2BA01"u8.CopyTo(bytes); Challenge.CopyTo(bytes, 8);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24), 123);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(28), 456);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(32), 0x100);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(40), 0x200);
        string[] names = Expected.Split('\\');
        Encoding.Unicode.GetBytes(names[0]).CopyTo(bytes, 48);
        Encoding.Unicode.GetBytes(names[1]).CopyTo(bytes, 304);
        return bytes;
    }
}
