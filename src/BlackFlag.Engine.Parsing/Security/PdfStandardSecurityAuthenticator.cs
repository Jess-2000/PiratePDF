using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Parsing.Security;

/// <summary>
/// Independently implements password authentication and content-key recovery for
/// PDF Standard security handlers revisions 2 through 6.
/// </summary>
public sealed class PdfStandardSecurityAuthenticator
{
    private static readonly PdfName FilterKey = new("Filter");
    private static readonly PdfName StandardName = new("Standard");
    private static readonly PdfName VersionKey = new("V");
    private static readonly PdfName RevisionKey = new("R");
    private static readonly PdfName LengthKey = new("Length");
    private static readonly PdfName OwnerKey = new("O");
    private static readonly PdfName UserKey = new("U");
    private static readonly PdfName OwnerEncryptionKey = new("OE");
    private static readonly PdfName UserEncryptionKey = new("UE");
    private static readonly PdfName PermissionsKey = new("P");
    private static readonly PdfName PermissionsBlockKey = new("Perms");
    private static readonly PdfName EncryptMetadataKey = new("EncryptMetadata");
    private static readonly PdfName CryptFiltersKey = new("CF");
    private static readonly PdfName StreamFilterKey = new("StmF");
    private static readonly PdfName StringFilterKey = new("StrF");
    private static readonly PdfName CryptFilterMethodKey = new("CFM");
    private static readonly PdfName IdentityName = new("Identity");

    private static readonly byte[] PasswordPadding =
    [
        0x28, 0xbf, 0x4e, 0x5e, 0x4e, 0x75, 0x8a, 0x41,
        0x64, 0x00, 0x4e, 0x56, 0xff, 0xfa, 0x01, 0x08,
        0x2e, 0x2e, 0x00, 0xb6, 0xd0, 0x68, 0x3e, 0x80,
        0x2f, 0x0c, 0xa9, 0xfe, 0x64, 0x53, 0x69, 0x7a
    ];

    /// <summary>
    /// Authenticates a password and, on success, returns a disposable session
    /// containing the recovered file key.
    /// </summary>
    /// <param name="encryptionDictionary">The resolved /Encrypt dictionary.</param>
    /// <param name="documentIdentifier">
    /// The first byte string in the trailer /ID array. Required for revisions
    /// 2 through 4 and ignored for revisions 5 and 6.
    /// </param>
    /// <param name="password">The password supplied by the caller.</param>
    /// <param name="cancellationToken">Cancellation for expensive revision-6 hashing.</param>
    public static PdfStandardSecurityAuthenticationResult Authenticate(
        PdfDictionary encryptionDictionary,
        ReadOnlyMemory<byte> documentIdentifier,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(encryptionDictionary);
        ArgumentNullException.ThrowIfNull(password);
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryReadConfiguration(
                encryptionDictionary,
                out Configuration? configuration,
                out string? failure))
        {
            return Failed(failure!);
        }

        byte[] passwordBytes = configuration!.Revision >= 5
            ? EncodeModernPassword(password)
            : EncodeLegacyPassword(password);

        try
        {
            return configuration!.Revision switch
            {
                2 or 3 or 4 => AuthenticateLegacy(
                    configuration,
                    documentIdentifier.Span,
                    passwordBytes),
                5 => AuthenticateAes256(
                    configuration,
                    passwordBytes,
                    revision6: false,
                    cancellationToken),
                6 => AuthenticateAes256(
                    configuration,
                    passwordBytes,
                    revision6: true,
                    cancellationToken),
                _ => Failed("Unsupported Standard security-handler revision.")
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

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

    private static PdfStandardSecurityAuthenticationResult Success(
        Configuration configuration,
        PdfPasswordAuthenticationKind kind,
        byte[] fileKey)
    {
        var permissions = new PdfStandardSecurityPermissions(
            configuration.Permissions,
            kind == PdfPasswordAuthenticationKind.Owner);
        var session = new PdfStandardSecuritySession(
            fileKey.ToArray(),
            kind,
            permissions,
            configuration.StringCipher,
            configuration.StreamCipher);
        return new(kind, session, failureReason: null);
    }

    private static PdfStandardSecurityAuthenticationResult Failed(
        string reason) =>
        new(PdfPasswordAuthenticationKind.Failed, session: null, reason);

    private static bool TryReadConfiguration(
        PdfDictionary dictionary,
        out Configuration? configuration,
        out string? failure)
    {
        configuration = null;
        failure = null;

        if (!dictionary.TryGetValue(FilterKey, out PdfObject? filter) ||
            filter is not PdfName filterName ||
            filterName != StandardName ||
            !TryGetInteger(dictionary, RevisionKey, out int revision) ||
            !TryGetInteger(dictionary, PermissionsKey, out int permissions) ||
            !TryGetBytes(dictionary, OwnerKey, out byte[]? owner) ||
            !TryGetBytes(dictionary, UserKey, out byte[]? user))
        {
            failure = "The Standard security dictionary is incomplete or malformed.";
            return false;
        }

        int version = 0;
        if (dictionary.TryGetValue(VersionKey, out PdfObject? versionValue) &&
            (versionValue is not PdfInteger versionInteger ||
            versionInteger.Value is < int.MinValue or > int.MaxValue))
        {
            failure = "The Standard security-handler version is malformed.";
            return false;
        }
        else if (versionValue is PdfInteger validVersion)
        {
            version = (int)validVersion.Value;
        }

        if (revision is < 2 or > 6 || version is < 0 or > 5)
        {
            failure = "The Standard security-handler revision or version is unsupported.";
            return false;
        }

        if (!IsSupportedRevisionVersionPair(revision, version))
        {
            failure = "The Standard security-handler revision and version are inconsistent.";
            return false;
        }

        int expectedOwnerUserLength = revision >= 5 ? 48 : 32;
        if (owner!.Length != expectedOwnerUserLength ||
            user!.Length != expectedOwnerUserLength)
        {
            failure = "The Standard security dictionary contains invalid /O or /U lengths.";
            return false;
        }

        bool encryptMetadata = true;
        if (dictionary.TryGetValue(EncryptMetadataKey, out PdfObject? metadataValue))
        {
            if (metadataValue is not PdfBoolean metadataBoolean)
            {
                failure = "/EncryptMetadata must be a boolean.";
                return false;
            }

            encryptMetadata = metadataBoolean.Value;
        }

        int keyLengthBytes;
        if (revision == 2)
        {
            keyLengthBytes = 5;
        }
        else if (revision >= 5)
        {
            keyLengthBytes = 32;
        }
        else
        {
            long lengthBits = 40;
            if (dictionary.TryGetValue(LengthKey, out PdfObject? lengthValue))
            {
                if (lengthValue is not PdfInteger lengthInteger)
                {
                    failure = "/Length must be an integer.";
                    return false;
                }

                lengthBits = lengthInteger.Value;
            }

            if (lengthBits is < 40 or > 128 || lengthBits % 8 != 0)
            {
                failure = "The legacy Standard security-handler key length is invalid.";
                return false;
            }

            keyLengthBytes = (int)(lengthBits / 8);
        }

        if (!TryResolveCiphers(
                dictionary, version, revision,
                out PdfStandardCipher stringCipher,
                out PdfStandardCipher streamCipher,
                out failure))
        {
            return false;
        }

        TryGetBytes(dictionary, UserEncryptionKey, out byte[]? ue);
        TryGetBytes(dictionary, OwnerEncryptionKey, out byte[]? oe);
        TryGetBytes(dictionary, PermissionsBlockKey, out byte[]? perms);

        configuration = new Configuration(
            revision,
            version,
            keyLengthBytes,
            permissions,
            encryptMetadata,
            owner,
            user,
            oe,
            ue,
            perms,
            stringCipher,
            streamCipher);
        return true;
    }

    private static bool TryResolveCiphers(
        PdfDictionary dictionary,
        int version,
        int revision,
        out PdfStandardCipher stringCipher,
        out PdfStandardCipher streamCipher,
        out string? failure)
    {
        streamCipher = default;

        failure = null;
        if (version < 4)
        {
            stringCipher = PdfStandardCipher.Rc4;
            streamCipher = PdfStandardCipher.Rc4;
            return true;
        }

        if (!TryResolveCryptFilter(
                dictionary, StringFilterKey, revision,
                out stringCipher) ||
            !TryResolveCryptFilter(
                dictionary, StreamFilterKey, revision,
                out streamCipher))
        {
            failure = "The Standard security-handler crypt filter is malformed or unsupported.";
            return false;
        }

        return true;
    }

    private static bool TryResolveCryptFilter(
        PdfDictionary dictionary,
        PdfName selectorKey,
        int revision,
        out PdfStandardCipher cipher)
    {
        cipher = PdfStandardCipher.Unsupported;

        PdfName selected = IdentityName;
        if (dictionary.TryGetValue(selectorKey, out PdfObject? selectedValue))
        {
            if (selectedValue is not PdfName selectedName)
            {
                return false;
            }

            selected = selectedName;
        }

        if (selected == IdentityName)
        {
            cipher = PdfStandardCipher.Identity;
            return true;
        }

        if (!dictionary.TryGetValue(CryptFiltersKey, out PdfObject? filtersValue) ||
            filtersValue is not PdfDictionary filters ||
            !filters.TryGetValue(selected, out PdfObject? filterValue) ||
            filterValue is not PdfDictionary filter ||
            !filter.TryGetValue(CryptFilterMethodKey, out PdfObject? methodValue) ||
            methodValue is not PdfName method)
        {
            return false;
        }

        cipher = method.Value switch
        {
            "V2" => PdfStandardCipher.Rc4,
            "AESV2" when revision == 4 => PdfStandardCipher.Aes128,
            "AESV3" when revision >= 5 => PdfStandardCipher.Aes256,
            "None" => PdfStandardCipher.Identity,
            _ => PdfStandardCipher.Unsupported
        };

        return cipher != PdfStandardCipher.Unsupported;
    }

    private static byte[] EncodeLegacyPassword(string password)
    {
        int length = Math.Min(password.Length, 32);
        byte[] bytes = new byte[length];
        Encoding.Latin1.GetBytes(password.AsSpan(0, length), bytes);
        return bytes;
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

    private static bool IsSupportedRevisionVersionPair(
        int revision,
        int version) =>
        revision switch
        {
            2 => version is 1 or 2,
            3 => version is 2 or 3,
            4 => version == 4,
            5 or 6 => version == 5,
            _ => false
        };

    private static bool TryGetInteger(
        PdfDictionary dictionary,
        PdfName key,
        out int value)
    {
        if (dictionary.TryGetValue(key, out PdfObject? item) &&
            item is PdfInteger integer &&
            integer.Value is >= int.MinValue and <= int.MaxValue)
        {
            value = (int)integer.Value;
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryGetBytes(
        PdfDictionary dictionary,
        PdfName key,
        out byte[]? value)
    {
        if (!dictionary.TryGetValue(key, out PdfObject? item))
        {
            value = null;
            return false;
        }

        switch (item)
        {
            case PdfHexString hex:
                value = hex.Value.ToArray();
                return true;
            case PdfLiteralString literal:
                value = Encoding.Latin1.GetBytes(literal.Value);
                return true;
            default:
                value = null;
                return false;
        }
    }

    private sealed record Configuration(
        int Revision,
        int Version,
        int KeyLengthBytes,
        int Permissions,
        bool EncryptMetadata,
        byte[] Owner,
        byte[] User,
        byte[]? OwnerEncryptionKey,
        byte[]? UserEncryptionKey,
        byte[]? PermissionsBlock,
        PdfStandardCipher StringCipher,
        PdfStandardCipher StreamCipher);
}