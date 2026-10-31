using System.Buffers.Binary;

namespace PiratePDF.Fuzz.Runner;

internal static class CampaignGateTransport
{
    internal static async Task<byte[]> ReadAsync(Stream stream, int maximum, CancellationToken token)
    {
        byte[] prefix = new byte[4];
        await stream.ReadExactlyAsync(prefix, token).ConfigureAwait(false);
        int length = BinaryPrimitives.ReadInt32LittleEndian(prefix);
        if (length < 1 || length > maximum) throw new IOException("Invalid gate frame length.");
        byte[] bytes = new byte[length];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        return bytes;
    }

    internal static async Task WriteAsync(Stream stream, ReadOnlyMemory<byte> bytes, int maximum, CancellationToken token)
    {
        if (bytes.Length < 1 || bytes.Length > maximum) throw new IOException("Invalid gate frame length.");
        byte[] prefix = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(prefix, bytes.Length);
        await stream.WriteAsync(prefix, token).ConfigureAwait(false);
        await stream.WriteAsync(bytes, token).ConfigureAwait(false);
        await stream.FlushAsync(token).ConfigureAwait(false);
    }
}
