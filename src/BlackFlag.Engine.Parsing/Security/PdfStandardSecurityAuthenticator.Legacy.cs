using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace BlackFlag.Engine.Parsing.Security;

public sealed partial class PdfStandardSecurityAuthenticator
{
    private static PdfStandardSecurityAuthenticationResult AuthenticateLegacy(
        Configuration configuration,
        ReadOnlySpan<byte> documentIdentifier,
        ReadOnlySpan<byte> password)
    {
        if (documentIdentifier.IsEmpty)
        {
            return Failed(
                "Standard security-handler revisions 2 through 4 require the first trailer /ID value.");
        }

        byte[]? fileKey = null;
        byte[]? recoveredUserPassword = null;

        try
        {
            fileKey = ComputeLegacyFileKey(
                configuration, documentIdentifier, password);

            if (ValidateLegacyUser(configuration, documentIdentifier, fileKey))
            {
                return Success(
                    configuration,
                    PdfPasswordAuthenticationKind.User,
                    fileKey);
            }

            CryptographicOperations.ZeroMemory(fileKey);
            fileKey = null;

            recoveredUserPassword = RecoverLegacyUserPassword(
                configuration, password);
            fileKey = ComputeLegacyFileKey(
                configuration, documentIdentifier, recoveredUserPassword);

            if (ValidateLegacyUser(configuration, documentIdentifier, fileKey))
            {
                return Success(
                    configuration,
                    PdfPasswordAuthenticationKind.Owner,
                    fileKey);
            }

            return Failed("The supplied PDF password is not valid.");
        }
        finally
        {
            if (fileKey is not null)
            {
                CryptographicOperations.ZeroMemory(fileKey);
            }

            if (recoveredUserPassword is not null)
            {
                CryptographicOperations.ZeroMemory(recoveredUserPassword);
            }
        }
    }

    private static byte[] ComputeLegacyFileKey(
        Configuration configuration,
        ReadOnlySpan<byte> documentIdentifier,
        ReadOnlySpan<byte> password)
    {
        byte[] padded = PadLegacyPassword(password);
        int inputLength = 32 + configuration.Owner.Length + 4 +
            documentIdentifier.Length +
            (configuration.Revision >= 4 && !configuration.EncryptMetadata ? 4 : 0);
        byte[] input = new byte[inputLength];

        try
        {
            int offset = 0;
            padded.CopyTo(input, offset);
            offset += padded.Length;
            configuration.Owner.CopyTo(input, offset);
            offset += configuration.Owner.Length;
            BinaryPrimitives.WriteInt32LittleEndian(
                input.AsSpan(offset, 4), configuration.Permissions);
            offset += 4;
            documentIdentifier.CopyTo(input.AsSpan(offset));
            offset += documentIdentifier.Length;

            if (configuration.Revision >= 4 && !configuration.EncryptMetadata)
            {
                input.AsSpan(offset, 4).Fill(0xff);
            }

            // MD5 is required here for PDF legacy compatibility. Suppress analyzer warning.
#pragma warning disable CA5351 // Do Not Use Weak Cryptographic Algorithms
            byte[] digest = MD5.HashData(input);
#pragma warning restore CA5351 // Do Not Use Weak Cryptographic Algorithms
            try
            {
                if (configuration.Revision >= 3)
                {
                    for (int iteration = 0; iteration < 50; iteration++)
                    {
                        // MD5 is required here for PDF legacy compatibility. Suppress analyzer warning.
#pragma warning disable CA5351 // Do Not Use Weak Cryptographic Algorithms
                        byte[] next = MD5.HashData(
                            digest.AsSpan(0, configuration.KeyLengthBytes));
#pragma warning restore CA5351 // Do Not Use Weak Cryptographic Algorithms
                        CryptographicOperations.ZeroMemory(digest);
                        digest = next;
                    }
                }

                return digest.AsSpan(0, configuration.KeyLengthBytes).ToArray();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(digest);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(padded);
            CryptographicOperations.ZeroMemory(input);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5351:Do not use broken cryptographic algorithms", Justification = "MD5 is required by the PDF legacy security algorithm (for compatibility); its use is confined to this compatibility path.")]
    private static bool ValidateLegacyUser(
        Configuration configuration,
        ReadOnlySpan<byte> documentIdentifier,
        ReadOnlySpan<byte> fileKey)
    {
        byte[] expected;
        if (configuration.Revision == 2)
        {
            expected = PdfStandardSecurityCryptography.Rc4(
                PasswordPadding, fileKey);
            try
            {
                return CryptographicOperations.FixedTimeEquals(
                    expected, configuration.User);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(expected);
            }
        }

        byte[] seed = new byte[PasswordPadding.Length + documentIdentifier.Length];
        PasswordPadding.CopyTo(seed, 0);
        documentIdentifier.CopyTo(seed.AsSpan(PasswordPadding.Length));
        byte[] digest = MD5.HashData(seed);
        CryptographicOperations.ZeroMemory(seed);

        expected = null!;
        try
        {
            expected = PdfStandardSecurityCryptography.Rc4(digest, fileKey);
            for (int iteration = 1; iteration <= 19; iteration++)
            {
                byte[] iterationKey = XorKey(fileKey, iteration);
                byte[] next;
                try
                {
                    next = PdfStandardSecurityCryptography.Rc4(
                        expected, iterationKey);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(iterationKey);
                }

                CryptographicOperations.ZeroMemory(expected);
                expected = next;
            }

            return configuration.User.Length >= 16 &&
                CryptographicOperations.FixedTimeEquals(
                    expected.AsSpan(0, 16),
                    configuration.User.AsSpan(0, 16));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(digest);
            if (expected is not null)
            {
                CryptographicOperations.ZeroMemory(expected);
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Security",
        "CA5351:Do not use broken cryptographic algorithms",
        Justification = "MD5 is required by the PDF legacy security algorithm; its use is confined to this compatibility path.")]
    private static byte[] RecoverLegacyUserPassword(
        Configuration configuration,
        ReadOnlySpan<byte> ownerPassword)
    {
        byte[] paddedOwner = PadLegacyPassword(ownerPassword);
        byte[] digest = MD5.HashData(paddedOwner);
        CryptographicOperations.ZeroMemory(paddedOwner);

        try
        {
            if (configuration.Revision >= 3)
            {
                for (int iteration = 0; iteration < 50; iteration++)
                {
                    byte[] next = MD5.HashData(digest);
                    CryptographicOperations.ZeroMemory(digest);
                    digest = next;
                }
            }

            int keyLength = configuration.Revision == 2
                ? 5
                : configuration.KeyLengthBytes;
            byte[] key = digest.AsSpan(0, keyLength).ToArray();
            try
            {
                byte[] result = configuration.Owner.ToArray();
                if (configuration.Revision == 2)
                {
                    byte[] decrypted =
                        PdfStandardSecurityCryptography.Rc4(result, key);
                    CryptographicOperations.ZeroMemory(result);
                    return decrypted;
                }

                for (int iteration = 19; iteration >= 0; iteration--)
                {
                    byte[] iterationKey = XorKey(key, iteration);
                    byte[] next;
                    try
                    {
                        next = PdfStandardSecurityCryptography.Rc4(
                            result, iterationKey);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(iterationKey);
                    }

                    CryptographicOperations.ZeroMemory(result);
                    result = next;
                }

                return result;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(digest);
        }
    }

    private static byte[] PadLegacyPassword(ReadOnlySpan<byte> password)
    {
        byte[] padded = new byte[32];
        int copyLength = Math.Min(password.Length, padded.Length);
        password[..copyLength].CopyTo(padded);

        if (copyLength < padded.Length)
        {
            PasswordPadding.AsSpan(0, padded.Length - copyLength)
                .CopyTo(padded.AsSpan(copyLength));
        }

        return padded;
    }

    private static byte[] XorKey(ReadOnlySpan<byte> key, int value)
    {
        byte[] result = new byte[key.Length];
        for (int index = 0; index < key.Length; index++)
        {
            result[index] = (byte)(key[index] ^ value);
        }

        return result;
    }

    private static byte[] EncodeLegacyPassword(string password)
    {
        int length = Math.Min(password.Length, 32);
        byte[] bytes = new byte[length];
        Encoding.Latin1.GetBytes(password.AsSpan(0, length), bytes);
        return bytes;
    }

}
