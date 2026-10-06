using System.Security.Cryptography;
using System.Text;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Parsing.Security;

/// <summary>
/// Independently implements password authentication and content-key recovery for
/// PDF Standard security handlers revisions 2 through 6.
/// </summary>
public sealed partial class PdfStandardSecurityAuthenticator
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
