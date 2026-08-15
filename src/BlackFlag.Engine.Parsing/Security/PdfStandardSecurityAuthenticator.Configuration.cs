using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using BlackFlag.Engine.Objects;

namespace BlackFlag.Engine.Parsing.Security;

public sealed partial class PdfStandardSecurityAuthenticator
{
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

}
