using System.Security.Cryptography;

namespace BlackFlag.Engine.Parsing.Security;

/// <summary>
/// Holds authenticated Standard security-handler key material and decrypts
/// encrypted PDF string or stream bytes for a specific indirect object.
/// </summary>
public sealed class PdfStandardSecuritySession : IDisposable
{
    private static readonly byte[] AesSalt = [0x73, 0x41, 0x6c, 0x54];

    private readonly byte[] fileKey;
    private readonly PdfStandardCipher stringCipher;
    private readonly PdfStandardCipher streamCipher;
    private bool disposed;

    internal PdfStandardSecuritySession(
        byte[] fileKey,
        PdfPasswordAuthenticationKind authenticationKind,
        PdfStandardSecurityPermissions permissions,
        PdfStandardCipher stringCipher,
        PdfStandardCipher streamCipher)
    {
        this.fileKey = fileKey;
        AuthenticationKind = authenticationKind;
        Permissions = permissions;
        this.stringCipher = stringCipher;
        this.streamCipher = streamCipher;
    }

    /// <summary>Gets whether the user or owner credential authenticated.</summary>
    public PdfPasswordAuthenticationKind AuthenticationKind { get; }

    /// <summary>Gets the effective document permissions.</summary>
    public PdfStandardSecurityPermissions Permissions { get; }

    /// <summary>Decrypts encrypted bytes belonging to a PDF string object.</summary>
    public byte[] DecryptString(
        ReadOnlySpan<byte> encrypted,
        int objectNumber,
        int generationNumber)
    {
        ThrowIfDisposed();
        return Decrypt(encrypted, objectNumber, generationNumber, stringCipher);
    }

    /// <summary>Decrypts encrypted bytes belonging to a PDF stream object.</summary>
    public byte[] DecryptStream(
        ReadOnlySpan<byte> encrypted,
        int objectNumber,
        int generationNumber)
    {
        ThrowIfDisposed();
        return Decrypt(encrypted, objectNumber, generationNumber, streamCipher);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(fileKey);
        disposed = true;
    }

    private byte[] Decrypt(
        ReadOnlySpan<byte> encrypted,
        int objectNumber,
        int generationNumber,
        PdfStandardCipher cipher)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(objectNumber);
        ArgumentOutOfRangeException.ThrowIfNegative(generationNumber);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(generationNumber, ushort.MaxValue, nameof(generationNumber));

        return cipher switch
        {
            PdfStandardCipher.Identity => encrypted.ToArray(),
            PdfStandardCipher.Rc4 => DecryptRc4(
                encrypted, objectNumber, generationNumber),
            PdfStandardCipher.Aes128 => DecryptAes128(
                encrypted, objectNumber, generationNumber),
            PdfStandardCipher.Aes256 => DecryptAes256(encrypted),
            _ => throw new InvalidOperationException(
                "The authenticated PDF uses an unsupported crypt filter.")
        };
    }

    private byte[] DecryptRc4(
        ReadOnlySpan<byte> encrypted,
        int objectNumber,
        int generationNumber)
    {
        byte[] key = CreateObjectKey(objectNumber, generationNumber, useAesSalt: false);
        try
        {
            return PdfStandardSecurityCryptography.Rc4(encrypted, key);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private byte[] DecryptAes128(
        ReadOnlySpan<byte> encrypted,
        int objectNumber,
        int generationNumber)
    {
        byte[] key = CreateObjectKey(objectNumber, generationNumber, useAesSalt: true);
        try
        {
            return PdfStandardSecurityCryptography.DecryptAesCbcWithPrefixedIv(
                encrypted, key);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private byte[] DecryptAes256(ReadOnlySpan<byte> encrypted) =>
        PdfStandardSecurityCryptography.DecryptAesCbcWithPrefixedIv(
            encrypted, fileKey);

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Security",
        "CA5351:Do not use broken cryptographic algorithms",
        Justification = "MD5 is required by the PDF legacy object-key derivation algorithm; its use is confined to this compatibility path.")]
    private byte[] CreateObjectKey(
        int objectNumber,
        int generationNumber,
        bool useAesSalt)
    {
        const int ObjectAndGenerationBytes = 5;
        const int MaximumLegacyObjectKeyBytes = 16;

        int saltLength = useAesSalt ? AesSalt.Length : 0;
        int inputLength = checked(fileKey.Length + ObjectAndGenerationBytes + saltLength);
        Span<byte> input = stackalloc byte[inputLength];

        fileKey.AsSpan().CopyTo(input);
        int offset = fileKey.Length;

        // ISO 32000 legacy object-key derivation stores the object number as
        // three low-order little-endian bytes and the generation as two.
        input[offset++] = (byte)(objectNumber & 0xff);
        input[offset++] = (byte)((objectNumber >> 8) & 0xff);
        input[offset++] = (byte)((objectNumber >> 16) & 0xff);
        input[offset++] = (byte)(generationNumber & 0xff);
        input[offset++] = (byte)((generationNumber >> 8) & 0xff);

        if (useAesSalt)
        {
            AesSalt.AsSpan().CopyTo(input[offset..]);
        }

        Span<byte> digest = stackalloc byte[16];
        MD5.HashData(input, digest);

        int keyLength = Math.Min(
            fileKey.Length + ObjectAndGenerationBytes,
            MaximumLegacyObjectKeyBytes);
        return digest[..keyLength].ToArray();
    }

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(disposed, this);
}

internal enum PdfStandardCipher
{
    Identity,
    Rc4,
    Aes128,
    Aes256,
    Unsupported
}