using System.Buffers.Binary;
using System.Security;
using System.Security.Cryptography;

namespace PiratePDF.Fuzz.Runner;

/// <summary>One-use decoding of the fixed native attester's measurement, not execution authorization.</summary>
/// <remarks>The caller must still verify the live USER objects and all admission gates before release.</remarks>
internal sealed class NativeDesktopAttestation
{
    internal const int ReportLength = 560;
    private readonly byte[] challenge;
    private readonly uint processId;
    private readonly uint threadId;
    private readonly string station;
    private readonly string desktop;
    private int consumed;

    internal NativeDesktopAttestation(byte[] challenge, uint processId, uint threadId, string expectedDesktop)
    {
        if (challenge.Length != 16 || processId == 0 || threadId == 0) throw new ArgumentException("Invalid attestation identity.");
        string[] names = expectedDesktop.Split('\\');
        if (names.Length != 2 || !ValidName(names[0]) || !ValidName(names[1])) throw new ArgumentException("Invalid expected desktop.");
        this.challenge = (byte[])challenge.Clone();
        this.processId = processId;
        this.threadId = threadId;
        station = names[0];
        desktop = names[1];
    }

    // Consume even a rejected report: retries require a new process and fresh challenge.
    internal NativeDesktopMeasurement Accept(byte[] bytes)
    {
        if (Interlocked.Exchange(ref consumed, 1) != 0) throw new SecurityException("Attestation already consumed.");
        if (bytes.Length != ReportLength || !bytes.AsSpan(0, 8).SequenceEqual("PPH2BA01"u8) ||
            !CryptographicOperations.FixedTimeEquals(bytes.AsSpan(8, 16), challenge) ||
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(24)) != processId ||
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(28)) != threadId)
            throw new SecurityException("Invalid attestation frame or process identity.");
        ulong stationHandle = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(32));
        ulong desktopHandle = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(40));
        if (!ValidHandle(stationHandle) || !ValidHandle(desktopHandle) ||
            ReadName(bytes.AsSpan(48, 256)) != station || ReadName(bytes.AsSpan(304, 256)) != desktop)
            throw new SecurityException("Unexpected attested USER object.");
        return new(stationHandle, desktopHandle);
    }

    private static bool ValidHandle(ulong value) => value is > 0 and <= long.MaxValue;

    private static bool ValidName(string value) => value.Length is > 0 and < 128 &&
        value.All(character => character is >= ' ' and <= '~' && character != '\\');

    private static string ReadName(ReadOnlySpan<byte> bytes)
    {
        int end = -1;
        for (int index = 0; index < 128; index++)
        {
            ushort value = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(index * 2)..]);
            if (value == 0) { end = index; break; }
            if (value is < 32 or > 126 || value == '\\') throw new SecurityException("Invalid attested name.");
        }
        if (end <= 0 || bytes[(end * 2)..].ContainsAnyExcept((byte)0)) throw new SecurityException("Noncanonical attested name.");
        return System.Text.Encoding.Unicode.GetString(bytes[..(end * 2)]);
    }
}

internal readonly record struct NativeDesktopMeasurement(ulong StationHandle, ulong DesktopHandle);
