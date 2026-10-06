using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace BlackFlag.Engine.Parsing.Security;

public sealed partial class PdfStandardSecurityAuthenticator
{
    private static PdfStandardSecurityAuthenticationResult AuthenticateAes256(
        Configuration configuration,
        ReadOnlySpan<byte> password,
        bool revision6,
        CancellationToken cancellationToken)
    {
        if (configuration.User.Length != 48 ||
            configuration.Owner.Length != 48 ||
            configuration.UserEncryptionKey?.Length != 32 ||
            configuration.OwnerEncryptionKey?.Length != 32 ||
            configuration.PermissionsBlock?.Length != 16)
        {
            return Failed(
                "The AES-256 Standard security dictionary contains invalid key material lengths.");
        }

        ReadOnlySpan<byte> user = configuration.User;
        ReadOnlySpan<byte> owner = configuration.Owner;

        byte[] userHash = ComputeAes256Hash(
            password, user.Slice(32, 8), ReadOnlySpan<byte>.Empty,
            revision6, cancellationToken);

        try
        {
            if (CryptographicOperations.FixedTimeEquals(
                    userHash, user[..32]))
            {
                byte[] keyHash = ComputeAes256Hash(
                    password, user.Slice(40, 8), ReadOnlySpan<byte>.Empty,
                    revision6, cancellationToken);
                byte[] fileKey;
                try
                {
                    fileKey = PdfStandardSecurityCryptography.DecryptAesCbcNoPadding(
                        configuration.UserEncryptionKey,
                        keyHash,
                        new byte[16]);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(keyHash);
                }

                return ValidateAes256PermissionsAndCreateSession(
                    configuration,
                    PdfPasswordAuthenticationKind.User,
                    fileKey);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(userHash);
        }

        byte[] ownerHash = ComputeAes256Hash(
            password, owner.Slice(32, 8), user,
            revision6, cancellationToken);

        try
        {
            if (!CryptographicOperations.FixedTimeEquals(
                    ownerHash, owner[..32]))
            {
                return Failed("The supplied PDF password is not valid.");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ownerHash);
        }

        byte[] ownerKeyHash = ComputeAes256Hash(
            password, owner.Slice(40, 8), user,
            revision6, cancellationToken);
        byte[] ownerFileKey;
        try
        {
            ownerFileKey = PdfStandardSecurityCryptography.DecryptAesCbcNoPadding(
                configuration.OwnerEncryptionKey,
                ownerKeyHash,
                new byte[16]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ownerKeyHash);
        }

        return ValidateAes256PermissionsAndCreateSession(
            configuration,
            PdfPasswordAuthenticationKind.Owner,
            ownerFileKey);
    }

    private static PdfStandardSecurityAuthenticationResult
        ValidateAes256PermissionsAndCreateSession(
            Configuration configuration,
            PdfPasswordAuthenticationKind kind,
            byte[] fileKey)
    {
        try
        {
            if (fileKey.Length != 32)
            {
                return Failed("The recovered AES-256 file key is invalid.");
            }

            byte[] permissions =
                PdfStandardSecurityCryptography.DecryptAesEcbNoPadding(
                    configuration.PermissionsBlock!, fileKey);
            try
            {
                if (permissions.Length != 16 ||
                    permissions[9] != (byte)'a' ||
                    permissions[10] != (byte)'d' ||
                    permissions[11] != (byte)'b')
                {
                    return Failed(
                        "The encrypted PDF permissions block failed validation.");
                }

                int storedPermissions =
                    BinaryPrimitives.ReadInt32LittleEndian(permissions);
                bool reservedPermissionsAreValid =
                    permissions[4] == 0xff &&
                    permissions[5] == 0xff &&
                    permissions[6] == 0xff &&
                    permissions[7] == 0xff;
                bool metadataMarkerIsValid =
                    permissions[8] is (byte)'T' or (byte)'F';
                bool storedEncryptMetadata = permissions[8] == (byte)'T';

                if (!reservedPermissionsAreValid ||
                    !metadataMarkerIsValid ||
                    storedPermissions != configuration.Permissions ||
                    storedEncryptMetadata != configuration.EncryptMetadata)
                {
                    return Failed(
                        "The encrypted PDF permissions do not match the encryption dictionary.");
                }

                return Success(configuration, kind, fileKey);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(permissions);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(fileKey);
        }
    }

    private static byte[] ComputeAes256Hash(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> userKey,
        bool revision6,
        CancellationToken cancellationToken)
    {
        if (revision6)
        {
            return PdfStandardSecurityCryptography.HashRevision6(
                password, salt, userKey, cancellationToken);
        }

        byte[] input = new byte[password.Length + salt.Length + userKey.Length];
        password.CopyTo(input);
        salt.CopyTo(input.AsSpan(password.Length));
        userKey.CopyTo(input.AsSpan(password.Length + salt.Length));
        try
        {
            return SHA256.HashData(input);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(input);
        }
    }

    private static byte[] EncodeModernPassword(string password)
    {
        const int MaximumPasswordBytes = 127;

        Span<byte> buffer = stackalloc byte[MaximumPasswordBytes];
        Encoder encoder = Encoding.UTF8.GetEncoder();
        encoder.Convert(
            password.AsSpan(),
            buffer,
            flush: true,
            out _,
            out int bytesUsed,
            out _);

        return buffer[..bytesUsed].ToArray();
    }

}
