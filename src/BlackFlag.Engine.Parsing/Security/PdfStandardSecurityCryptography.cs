using System.Buffers;
using System.Security.Cryptography;

namespace BlackFlag.Engine.Parsing.Security;

internal static class PdfStandardSecurityCryptography
{
    internal static byte[] Rc4(ReadOnlySpan<byte> input, ReadOnlySpan<byte> key)
    {
        if (key.IsEmpty)
        {
            throw new ArgumentException("An RC4 key cannot be empty.", nameof(key));
        }

        Span<byte> state = stackalloc byte[256];
        for (int index = 0; index < state.Length; index++)
        {
            state[index] = (byte)index;
        }

        int j = 0;
        for (int index = 0; index < state.Length; index++)
        {
            j = (j + state[index] + key[index % key.Length]) & 0xff;
            (state[index], state[j]) = (state[j], state[index]);
        }

        byte[] output = new byte[input.Length];
        int i = 0;
        j = 0;

        for (int offset = 0; offset < input.Length; offset++)
        {
            i = (i + 1) & 0xff;
            j = (j + state[i]) & 0xff;
            (state[i], state[j]) = (state[j], state[i]);
            output[offset] = (byte)(input[offset] ^ state[(state[i] + state[j]) & 0xff]);
        }

        CryptographicOperations.ZeroMemory(state);
        return output;
    }

    internal static byte[] DecryptAesCbcWithPrefixedIv(
        ReadOnlySpan<byte> encrypted,
        ReadOnlySpan<byte> key)
    {
        if (encrypted.Length < 32 ||
            (encrypted.Length - 16) % 16 != 0)
        {
            throw new InvalidDataException(
                "Encrypted AES PDF data must contain a 16-byte IV and complete cipher blocks.");
        }

        return TransformAesCbc(
            encrypted[16..],
            key,
            encrypted[..16],
            decrypt: true,
            PaddingMode.PKCS7);
    }

    internal static byte[] DecryptAesCbcNoPadding(
        ReadOnlySpan<byte> encrypted,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> iv) =>
        TransformAesCbc(
            encrypted,
            key,
            iv,
            decrypt: true,
            PaddingMode.None);

    internal static byte[] EncryptAesCbcNoPadding(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> iv) =>
        TransformAesCbc(
            plaintext,
            key,
            iv,
            decrypt: false,
            PaddingMode.None);

    internal static byte[] DecryptAesEcbNoPadding(
        ReadOnlySpan<byte> encrypted,
        ReadOnlySpan<byte> key)
    {
        using Aes aes = Aes.Create();
        aes.Key = key.ToArray();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using ICryptoTransform transform = aes.CreateDecryptor();
        return transform.TransformFinalBlock(encrypted.ToArray(), 0, encrypted.Length);
    }

    internal static byte[] HashRevision6(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> userKey,
        CancellationToken cancellationToken)
    {
        int seedLength = password.Length + salt.Length + userKey.Length;
        byte[] seed = new byte[seedLength];
        password.CopyTo(seed);
        salt.CopyTo(seed.AsSpan(password.Length));
        userKey.CopyTo(seed.AsSpan(password.Length + salt.Length));

        byte[] k = SHA256.HashData(seed);
        CryptographicOperations.ZeroMemory(seed);

        byte[]? repeated = null;
        byte[]? encrypted = null;

        try
        {
            int round = 0;
            while (round < 64 ||
                   encrypted is not null &&
                   encrypted[^1] > round - 32)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int blockLength = password.Length + k.Length + userKey.Length;
                int repeatedLength = checked(blockLength * 64);
                repeated = ArrayPool<byte>.Shared.Rent(repeatedLength);
                Span<byte> repeatedSpan = repeated.AsSpan(0, repeatedLength);

                int offset = 0;
                for (int repetition = 0; repetition < 64; repetition++)
                {
                    password.CopyTo(repeatedSpan[offset..]);
                    offset += password.Length;
                    k.CopyTo(repeatedSpan[offset..]);
                    offset += k.Length;
                    userKey.CopyTo(repeatedSpan[offset..]);
                    offset += userKey.Length;
                }

                byte[] aesKey = k.AsSpan(0, 16).ToArray();
                byte[] iv = k.AsSpan(16, 16).ToArray();
                try
                {
                    encrypted = EncryptAesCbcNoPadding(
                        repeatedSpan, aesKey, iv);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(aesKey);
                    CryptographicOperations.ZeroMemory(iv);
                    CryptographicOperations.ZeroMemory(repeatedSpan);
                    ArrayPool<byte>.Shared.Return(repeated);
                    repeated = null;
                }

                int selector = 0;
                for (int index = 0; index < 16; index++)
                {
                    selector = (selector * 256 + encrypted[index]) % 3;
                }

                byte[] next = selector switch
                {
                    0 => SHA256.HashData(encrypted),
                    1 => SHA384.HashData(encrypted),
                    _ => SHA512.HashData(encrypted)
                };

                CryptographicOperations.ZeroMemory(k);
                k = next;
                round++;
            }

            return k.AsSpan(0, 32).ToArray();
        }
        finally
        {
            if (repeated is not null)
            {
                ArrayPool<byte>.Shared.Return(repeated, clearArray: true);
            }

            if (encrypted is not null)
            {
                CryptographicOperations.ZeroMemory(encrypted);
            }

            CryptographicOperations.ZeroMemory(k);
        }
    }

    private static byte[] TransformAesCbc(
        ReadOnlySpan<byte> input,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> iv,
        bool decrypt,
        PaddingMode padding)
    {
        using Aes aes = Aes.Create();
        aes.Key = key.ToArray();
        aes.IV = iv.ToArray();
        aes.Mode = CipherMode.CBC;
        aes.Padding = padding;
        using ICryptoTransform transform =
            decrypt ? aes.CreateDecryptor() : aes.CreateEncryptor();
        return transform.TransformFinalBlock(input.ToArray(), 0, input.Length);
    }
}
