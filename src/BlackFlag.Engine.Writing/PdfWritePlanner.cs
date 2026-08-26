using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>Creates preservation-aware write plans from authoritative document state.</summary>
public static class PdfWritePlanner
{
    /// <summary>
    /// Validates the current logical document and captures the object-identity
    /// namespace needed by later serialization stages.
    /// </summary>
    public static PdfWritePlan Create(
        PdfDocument document,
        PdfWriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        var resolvedOptions = options ?? new PdfWriteOptions();
        resolvedOptions.Validate();

        PdfObjectIdentifier rootObject = document.Trailer.RootObject
            ?? throw new InvalidOperationException(
                "The PDF trailer root object must be assigned before writing can be planned.");

        EnsureStoredReference(document, rootObject, "root catalog");
        EnsureOptionalStoredReference(
            document,
            document.Trailer.InformationObject,
            "document information");
        EnsureOptionalStoredReference(
            document,
            document.Trailer.EncryptionObject,
            "encryption dictionary");

        PdfObjectIdentifier[] identifiers = document.Objects.Identifiers.ToArray();
        int nextObjectNumber = CalculateNextObjectNumber(identifiers);

        return new PdfWritePlan(
            document,
            resolvedOptions,
            identifiers,
            nextObjectNumber);
    }

    private static int CalculateNextObjectNumber(
        PdfObjectIdentifier[] identifiers)
    {
        if (identifiers.Length == 0)
        {
            return 1;
        }

        int highestObjectNumber = identifiers[^1].ObjectNumber;

        if (highestObjectNumber == int.MaxValue)
        {
            throw new InvalidOperationException(
                "The indirect-object namespace cannot allocate another object number.");
        }

        return highestObjectNumber + 1;
    }

    private static void EnsureOptionalStoredReference(
        PdfDocument document,
        PdfObjectIdentifier? identifier,
        string role)
    {
        if (identifier.HasValue)
        {
            EnsureStoredReference(document, identifier.Value, role);
        }
    }

    private static void EnsureStoredReference(
        PdfDocument document,
        PdfObjectIdentifier identifier,
        string role)
    {
        if (!document.Objects.Contains(identifier))
        {
            throw new InvalidOperationException(
                $"The trailer {role} reference '{identifier}' is not present in the document object store.");
        }
    }
}
