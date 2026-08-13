using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Parsing.Recovery;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Parsing.Security;

/// <summary>
/// Establishes the safe pre-authentication state for PDF Standard security
/// handlers. This component never decrypts, reconstructs, or bypasses protected
/// content.
/// </summary>
public sealed class PdfEncryptionSecurityHandler
{
    private static readonly PdfName EncryptKey = new("Encrypt");
    private static readonly PdfName FilterKey = new("Filter");
    private static readonly PdfName StandardName = new("Standard");
    private static readonly PdfName VersionKey = new("V");
    private static readonly PdfName RevisionKey = new("R");
    private static readonly PdfName OwnerKey = new("O");
    private static readonly PdfName UserKey = new("U");
    private static readonly PdfName PermissionsKey = new("P");

    private readonly IPdfIndirectObjectResolver resolver;

    /// <summary>Initializes a PDF encryption security handler.</summary>
    public PdfEncryptionSecurityHandler(IPdfIndirectObjectResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        this.resolver = resolver;
    }

    /// <summary>
    /// Inspects trailer encryption state and returns a normalized, typed
    /// pre-authentication result.
    /// </summary>
    public PdfEncryptionSecurityAssessment Assess(
        PdfDictionary? trailer,
        PdfRecoveredEncryptionState recoveredEncryptionState =
            PdfRecoveredEncryptionState.None,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (recoveredEncryptionState == PdfRecoveredEncryptionState.Damaged)
        {
            return Damaged(
                "Recovered PDF encryption metadata is damaged or cannot be trusted.",
                "Recovery will not reconstruct, bypass, or ignore encryption.");
        }

        if (trailer is null)
        {
            return recoveredEncryptionState ==
                PdfRecoveredEncryptionState.AuthenticationRequired
                    ? PasswordRequired(revision: null, version: null)
                    : None();
        }

        if (!trailer.TryGetValue(EncryptKey, out PdfObject? encrypt))
        {
            return recoveredEncryptionState ==
                PdfRecoveredEncryptionState.AuthenticationRequired
                    ? PasswordRequired(revision: null, version: null)
                    : None();
        }

        PdfDictionary? dictionary =
            ResolveEncryptionDictionary(encrypt, cancellationToken);

        if (dictionary is null)
        {
            return Damaged(
                "The PDF encryption dictionary is missing or cannot be resolved.",
                "Protected content will not be exposed.");
        }

        return AssessStandardDictionary(dictionary);
    }

    private PdfDictionary? ResolveEncryptionDictionary(
        PdfObject encrypt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return encrypt switch
        {
            PdfDictionary dictionary => dictionary,
            PdfIndirectReference reference =>
                Resolve(reference),
            _ => null
        };
    }

    private PdfDictionary? Resolve(PdfIndirectReference reference)
    {
        var objectId = new PdfObjectId(
            reference.ObjectNumber,
            reference.GenerationNumber);

        return resolver.TryResolve(objectId, out PdfObject? value)
            ? value as PdfDictionary
            : null;
    }

    private static PdfEncryptionSecurityAssessment AssessStandardDictionary(
        PdfDictionary dictionary)
    {
        if (!dictionary.TryGetValue(FilterKey, out PdfObject? filter) ||
            filter is not PdfName filterName ||
            filterName != StandardName)
        {
            return Damaged(
                "The PDF declares an unsupported or malformed encryption filter.",
                "Only trustworthy Standard security-handler metadata may request authentication.");
        }

        if (!TryGetInteger(dictionary, RevisionKey, out long revision) ||
            !TryGetInteger(dictionary, PermissionsKey, out _) ||
            !IsPdfString(dictionary, OwnerKey) ||
            !IsPdfString(dictionary, UserKey))
        {
            return Damaged(
                "The PDF Standard security dictionary is incomplete or malformed.",
                "Authentication cannot safely proceed.");
        }

        long version = 0;
        if (dictionary.TryGetValue(VersionKey, out PdfObject? versionObject))
        {
            if (versionObject is not PdfInteger versionInteger)
            {
                return Damaged(
                    "The PDF Standard security-handler version is malformed.",
                    "Authentication cannot safely proceed.");
            }

            version = versionInteger.Value;
        }

        if (revision is < 2 or > 6 ||
            version is < 0 or > 5)
        {
            return Damaged(
                "The PDF uses an unsupported Standard security-handler revision or version.",
                $"Security handler V={version}, R={revision} will not be bypassed.");
        }

        return PasswordRequired(revision, version);
    }

    private static bool TryGetInteger(
        PdfDictionary dictionary,
        PdfName key,
        out long value)
    {
        if (dictionary.TryGetValue(key, out PdfObject? item) &&
            item is PdfInteger integer)
        {
            value = integer.Value;
            return true;
        }

        value = 0;
        return false;
    }

    private static bool IsPdfString(
        PdfDictionary dictionary,
        PdfName key) =>
        dictionary.TryGetValue(key, out PdfObject? value) &&
        value is PdfLiteralString or PdfHexString;

    private static PdfEncryptionSecurityAssessment None() =>
        new(PdfEncryptionSecurityState.None, observation: null);

    private static PdfEncryptionSecurityAssessment PasswordRequired(
        long? revision,
        long? version)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SecurityHandler"] = "Standard"
        };

        if (revision.HasValue)
        {
            properties["Revision"] = revision.Value.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }

        if (version.HasValue)
        {
            properties["Version"] = version.Value.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }

        return new(
            PdfEncryptionSecurityState.PasswordRequired,
            new DocumentSecurityObservation(
                DocumentSecurityObservationCategory.EncryptionCondition,
                "PasswordRequired",
                "The PDF is protected by a Standard security handler and requires authentication before protected content can be exposed.",
                "pdf",
                new DocumentSecuritySourceLocation
                {
                    LogicalPath = "/Trailer/Encrypt"
                },
                properties));
    }

    private static PdfEncryptionSecurityAssessment Damaged(
        string description,
        string detail) =>
        new(
            PdfEncryptionSecurityState.Damaged,
            new DocumentSecurityObservation(
                DocumentSecurityObservationCategory.EncryptionCondition,
                "DamagedEncryption",
                description,
                "pdf",
                new DocumentSecuritySourceLocation
                {
                    LogicalPath = "/Trailer/Encrypt"
                },
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["SecurityBoundary"] = detail
                }));
}
