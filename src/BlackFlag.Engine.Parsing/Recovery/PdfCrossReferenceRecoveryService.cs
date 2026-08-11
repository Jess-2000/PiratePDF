
using BlackFlag.Engine.Abstractions.IO;
using BlackFlag.Engine.IO.RandomAccess;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Syntax.Lexing;
using BlackFlag.Engine.Syntax.Parsing;

namespace BlackFlag.Engine.Parsing.Recovery;

/// <summary>
/// Reconstructs a bounded in-memory object index when normal cross-reference
/// loading cannot establish a trustworthy document structure.
/// </summary>
/// <remarks>
/// Recovery may increase PiratePDF's ability to locate valid content, but it
/// must never increase authorization to access protected content.
/// </remarks>
public sealed class PdfCrossReferenceRecoveryService
{
    private static readonly PdfName RootKey = new("Root");
    private static readonly PdfName EncryptKey = new("Encrypt");
    private static readonly PdfName TypeKey = new("Type");
    private static readonly PdfName CatalogName = new("Catalog");
    private static readonly PdfName FilterKey = new("Filter");
    private static readonly PdfName StandardName = new("Standard");
    private static readonly PdfName RevisionKey = new("R");
    private static readonly PdfName OwnerKey = new("O");
    private static readonly PdfName UserKey = new("U");
    private static readonly PdfName PermissionsKey = new("P");

    private readonly IRandomAccessSource source;

    /// <summary>Initializes the recovery service.</summary>
    public PdfCrossReferenceRecoveryService(IRandomAccessSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        this.source = source;
    }

    /// <summary>
    /// Performs a bounded structural scan and builds a synthetic newest-wins
    /// object-location table.
    /// </summary>
    public PdfCrossReferenceRecoveryResult Recover(
        PdfRecoveryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= PdfRecoveryOptions.Default;
        options.Validate();

        if (options.Mode == PdfRecoveryMode.None)
        {
            throw new InvalidOperationException(
                "Structural recovery is disabled by the recovery options.");
        }

        if (source.Length > options.Limits.MaximumRecoveryScanBytes)
        {
            throw new InvalidDataException(
                $"Recovery source length {source.Length} exceeds the configured "
                + $"limit {options.Limits.MaximumRecoveryScanBytes}.");
        }

        if (source.Length > int.MaxValue)
        {
            throw new InvalidDataException(
                "The current recovery scanner requires a source no larger than Int32.MaxValue bytes.");
        }

        byte[] bytes = ReadSource(cancellationToken);
        var diagnostics = new List<PdfRecoveryDiagnostic>();
        List<PdfRecoveredObjectCandidate> objects = ScanObjects(
            bytes,
            options.Limits,
            diagnostics,
            cancellationToken);

        if (objects.Count == 0)
        {
            throw new InvalidDataException(
                "Structural recovery did not find any parser-valid indirect objects.");
        }

        List<PdfDictionary> trailers = ScanTrailers(
            bytes,
            objects,
            options.Limits,
            diagnostics,
            cancellationToken);

        PdfDictionary? trailer = trailers.Count == 0 ? null : trailers[^1];
        PdfObjectId? root = ResolveRoot(trailer, objects, diagnostics);

        if (root is null)
        {
            throw new InvalidDataException(
                "Structural recovery could not establish a valid PDF catalog.");
        }

        PdfRecoveredEncryptionState encryptionState =
            AssessEncryption(trailer, objects, diagnostics);

        PdfObjectLocation[] effective =
            BuildEffectiveLocations(objects, diagnostics);

        PdfObjectLocationTable locationTable =
            new([effective]);

        PdfRecoveryConfidence confidence =
            trailers.Count > 0
                ? PdfRecoveryConfidence.High
                : PdfRecoveryConfidence.Partial;

        diagnostics.Add(new PdfRecoveryDiagnostic(
            "RECOVERY_COMPLETED",
            $"Recovered {effective.Length} effective direct object locations."));

        return new PdfCrossReferenceRecoveryResult(
            locationTable,
            trailer,
            root,
            encryptionState,
            confidence,
            objects,
            diagnostics);
    }

    private byte[] ReadSource(CancellationToken cancellationToken)
    {
        byte[] bytes = new byte[checked((int)source.Length)];
        int total = 0;

        while (total < bytes.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int read = source.Read(total, bytes.AsSpan(total));
            if (read <= 0)
            {
                throw new EndOfStreamException(
                    "The random-access source ended before its declared length.");
            }

            total = checked(total + read);
        }

        return bytes;
    }

    private List<PdfRecoveredObjectCandidate> ScanObjects(
        byte[] bytes,
        PdfRecoveryLimits limits,
        List<PdfRecoveryDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var candidates = new List<PdfRecoveredObjectCandidate>();
        long offset = 0;

        while (offset < bytes.LongLength)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsPossibleObjectHeader(bytes, checked((int)offset)))
            {
                offset++;
                continue;
            }

            if (TryParseIndirectObject(
                    offset,
                    limits,
                    out PdfRecoveredObjectCandidate? candidate))
            {
                candidates.Add(candidate!);

                if (candidates.Count > limits.MaximumRecoveredObjects)
                {
                    throw new InvalidDataException(
                        "Recovered-object count exceeds the configured limit.");
                }

                diagnostics.Add(new PdfRecoveryDiagnostic(
                    "OBJECT_RECOVERED",
                    $"Recovered indirect object {candidate!.ObjectId}.",
                    candidate.Offset,
                    candidate.ObjectId));

                offset = Math.Max(offset + 1, candidate.EndOffset);
                continue;
            }

            offset++;
        }

        return candidates;
    }

    private bool TryParseIndirectObject(
        long offset,
        PdfRecoveryLimits limits,
        out PdfRecoveredObjectCandidate? candidate)
    {
        try
        {
            var cursor = new RandomAccessCursor(source, offset);
            var lexer = new PdfLexer(cursor);
            var reader = new PdfTokenReader(lexer);
            var parser = new PdfObjectParser(reader, source);

            PdfIndirectObject indirect = parser.ParseIndirectObject();

            if (indirect.ObjectNumber > limits.MaximumObjectNumber)
            {
                candidate = null;
                return false;
            }

            candidate = new PdfRecoveredObjectCandidate(
                new PdfObjectId(
                    indirect.ObjectNumber,
                    indirect.GenerationNumber),
                offset,
                reader.Position.Offset,
                indirect.Value);

            return true;
        }
        catch (Exception exception) when (
            exception is FormatException or
            EndOfStreamException or
            PdfUnexpectedTokenException or
            ArgumentException or
            OverflowException or
            InvalidDataException)
        {
            candidate = null;
            return false;
        }
    }

    private static bool IsPossibleObjectHeader(byte[] bytes, int offset)
    {
        if (offset < 0 || offset >= bytes.Length || !IsDigit(bytes[offset]))
        {
            return false;
        }

        if (offset > 0 && !IsDelimiterOrWhitespace(bytes[offset - 1]))
        {
            return false;
        }

        int cursor = offset;
        SkipDigits(bytes, ref cursor);
        if (!SkipRequiredWhitespace(bytes, ref cursor))
        {
            return false;
        }

        if (cursor >= bytes.Length || !IsDigit(bytes[cursor]))
        {
            return false;
        }

        SkipDigits(bytes, ref cursor);
        if (!SkipRequiredWhitespace(bytes, ref cursor))
        {
            return false;
        }

        return HasKeyword(bytes, cursor, "obj");
    }

    private List<PdfDictionary> ScanTrailers(
        byte[] bytes,
        List<PdfRecoveredObjectCandidate> objects,
        PdfRecoveryLimits limits,
        List<PdfRecoveryDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var trailers = new List<PdfDictionary>();
        int objectIndex = 0;

        for (int offset = 0; offset <= bytes.Length - 7; offset++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            while (objectIndex < objects.Count &&
                offset >= objects[objectIndex].EndOffset)
            {
                objectIndex++;
            }

            if (objectIndex < objects.Count &&
                offset >= objects[objectIndex].Offset &&
                offset < objects[objectIndex].EndOffset)
            {
                offset = checked((int)Math.Min(
                    int.MaxValue,
                    objects[objectIndex].EndOffset - 1));
                continue;
            }

            if (!HasKeyword(bytes, offset, "trailer") ||
                (offset > 0 && !IsDelimiterOrWhitespace(bytes[offset - 1])) ||
                (offset + 7 < bytes.Length &&
                !IsDelimiterOrWhitespace(bytes[offset + 7])))
            {
                continue;
            }

            if (TryParseDictionary(offset + 7, out PdfDictionary? dictionary))
            {
                trailers.Add(dictionary!);
                diagnostics.Add(new PdfRecoveryDiagnostic(
                    "TRAILER_RECOVERED",
                    "Recovered a parser-valid trailer dictionary.",
                    offset));

                if (trailers.Count >= limits.MaximumTrailerCandidates)
                {
                    break;
                }
            }
        }

        return trailers;
    }

    private bool TryParseDictionary(long offset, out PdfDictionary? dictionary)
    {
        try
        {
            var cursor = new RandomAccessCursor(source, offset);
            var lexer = new PdfLexer(cursor);
            var reader = new PdfTokenReader(lexer);
            var parser = new PdfObjectParser(reader, source);

            dictionary = parser.Parse() as PdfDictionary;
            return dictionary is not null;
        }
        catch (Exception exception) when (
            exception is FormatException or
            EndOfStreamException or
            PdfUnexpectedTokenException or
            ArgumentException or
            OverflowException)
        {
            dictionary = null;
            return false;
        }
    }

    private static PdfObjectId? ResolveRoot(
        PdfDictionary? trailer,
        IReadOnlyList<PdfRecoveredObjectCandidate> objects,
        List<PdfRecoveryDiagnostic> diagnostics)
    {
        if (trailer is not null &&
            trailer.TryGetValue(RootKey, out PdfObject rootValue) &&
            rootValue is PdfIndirectReference rootReference)
        {
            var requested = new PdfObjectId(
                rootReference.ObjectNumber,
                rootReference.GenerationNumber);

            PdfRecoveredObjectCandidate? candidate = objects
                .LastOrDefault(item => item.ObjectId == requested);

            if (candidate is not null && IsCatalog(candidate.Value))
            {
                return requested;
            }
        }

        PdfRecoveredObjectCandidate[] catalogs =
            objects.Where(item => IsCatalog(item.Value)).ToArray();

        if (catalogs.Length == 1)
        {
            diagnostics.Add(new PdfRecoveryDiagnostic(
                "ROOT_INFERRED",
                $"Inferred catalog object {catalogs[0].ObjectId} because a "
                + "trustworthy trailer /Root was unavailable.",
                catalogs[0].Offset,
                catalogs[0].ObjectId));

            return catalogs[0].ObjectId;
        }

        return null;
    }

    private static bool IsCatalog(PdfObject value) =>
        value is PdfDictionary dictionary &&
        dictionary.TryGetValue(TypeKey, out PdfObject type) &&
        type is PdfName name &&
        name == CatalogName;

    private static PdfRecoveredEncryptionState AssessEncryption(
        PdfDictionary? trailer,
        IReadOnlyList<PdfRecoveredObjectCandidate> objects,
        List<PdfRecoveryDiagnostic> diagnostics)
    {
        if (trailer is null ||
            !trailer.TryGetValue(EncryptKey, out PdfObject encrypt))
        {
            return PdfRecoveredEncryptionState.None;
        }

        PdfDictionary? dictionary = encrypt switch
        {
            PdfDictionary direct => direct,
            PdfIndirectReference reference => objects
                .LastOrDefault(item =>
                    item.ObjectId.ObjectNumber == reference.ObjectNumber &&
                    item.ObjectId.GenerationNumber == reference.GenerationNumber)
                ?.Value as PdfDictionary,
            _ => null
        };

        if (dictionary is null || !LooksLikeStandardEncryption(dictionary))
        {
            diagnostics.Add(new PdfRecoveryDiagnostic(
                "ENCRYPTION_DAMAGED",
                "Encryption metadata is missing, malformed, or not trustworthy. "
                + "Recovery will not reconstruct or bypass encryption."));

            return PdfRecoveredEncryptionState.Damaged;
        }

        diagnostics.Add(new PdfRecoveryDiagnostic(
            "PASSWORD_REQUIRED",
            "Structurally valid Standard security metadata was recovered. "
            + "Authentication must occur through the normal security handler "
            + "before protected content is exposed."));

        return PdfRecoveredEncryptionState.AuthenticationRequired;
    }

    private static bool LooksLikeStandardEncryption(PdfDictionary dictionary)
    {
        if (!dictionary.TryGetValue(FilterKey, out PdfObject filter) ||
            filter is not PdfName filterName ||
            filterName != StandardName)
        {
            return false;
        }

        return dictionary.TryGetValue(RevisionKey, out PdfObject revision) &&
            revision is PdfInteger &&
            dictionary.TryGetValue(OwnerKey, out PdfObject owner) &&
            IsPdfString(owner) &&
            dictionary.TryGetValue(UserKey, out PdfObject user) &&
            IsPdfString(user) &&
            dictionary.TryGetValue(PermissionsKey, out PdfObject permissions) &&
            permissions is PdfInteger;
    }

    private static bool IsPdfString(PdfObject value) =>
        value is PdfLiteralString or PdfHexString;

    private static PdfObjectLocation[] BuildEffectiveLocations(
        IReadOnlyList<PdfRecoveredObjectCandidate> objects,
        List<PdfRecoveryDiagnostic> diagnostics)
    {
        var effective = new Dictionary<int, PdfRecoveredObjectCandidate>();

        foreach (PdfRecoveredObjectCandidate candidate in objects)
        {
            if (effective.TryGetValue(
                    candidate.ObjectId.ObjectNumber,
                    out PdfRecoveredObjectCandidate? previous))
            {
                diagnostics.Add(new PdfRecoveryDiagnostic(
                    "OBJECT_DUPLICATE",
                    $"Object number {candidate.ObjectId.ObjectNumber} has "
                    + "multiple physical revisions; the later physical candidate "
                    + "is used by the synthetic recovery index.",
                    candidate.Offset,
                    candidate.ObjectId));
            }

            effective[candidate.ObjectId.ObjectNumber] = candidate;
        }

        return effective.Values
            .OrderBy(item => item.ObjectId.ObjectNumber)
            .Select(item => PdfObjectLocation.CreateDirect(
                item.ObjectId,
                item.Offset))
            .ToArray();
    }

    private static bool HasKeyword(byte[] bytes, int offset, string keyword)
    {
        if (offset < 0 || offset + keyword.Length > bytes.Length)
        {
            return false;
        }

        for (int index = 0; index < keyword.Length; index++)
        {
            if (bytes[offset + index] != keyword[index])
            {
                return false;
            }
        }

        return true;
    }

    private static void SkipDigits(byte[] bytes, ref int cursor)
    {
        while (cursor < bytes.Length && IsDigit(bytes[cursor]))
        {
            cursor++;
        }
    }

    private static bool SkipRequiredWhitespace(byte[] bytes, ref int cursor)
    {
        int start = cursor;
        while (cursor < bytes.Length && IsWhitespace(bytes[cursor]))
        {
            cursor++;
        }

        return cursor > start;
    }

    private static bool IsDigit(byte value) => value is >= (byte)'0' and <= (byte)'9';

    private static bool IsWhitespace(byte value) =>
        value is 0 or 9 or 10 or 12 or 13 or 32;

    private static bool IsDelimiterOrWhitespace(byte value) =>
        IsWhitespace(value) ||
        value is (byte)'(' or (byte)')' or
            (byte)'<' or (byte)'>' or
            (byte)'[' or (byte)']' or
            (byte)'{' or (byte)'}' or
            (byte)'/' or (byte)'%';
}
