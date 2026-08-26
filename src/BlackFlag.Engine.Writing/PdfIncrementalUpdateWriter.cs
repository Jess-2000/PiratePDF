using System.Globalization;
using System.Text;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing;

/// <summary>Appends a preservation-aware incremental PDF revision.</summary>
public static class PdfIncrementalUpdateWriter
{
    /// <summary>Writes the complete source revision followed by the planned incremental update.</summary>
    public static byte[] Write(PdfIncrementalWritePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        using var destination = new MemoryStream();
        Write(destination, plan);
        return destination.ToArray();
    }

    /// <summary>Writes the complete source revision and incremental update to a caller-owned destination.</summary>
    public static void Write(
        Stream destination,
        PdfIncrementalWritePlan plan)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(plan);

        if (!destination.CanWrite)
        {
            throw new ArgumentException(
                "The PDF file destination must be writable.",
                nameof(destination));
        }

        ValidateSupportedPlan(plan);

        using var output = new PdfCountingWriteStream(destination);
        plan.SourceRevision.CopyTo(output);

        if (output.BytesWritten != plan.SourceRevision.Length)
        {
            throw new InvalidOperationException(
                "The original source revision copied a byte count different from its declared length.");
        }

        WriteAscii(output, "\n");

        var offsets = new Dictionary<PdfObjectIdentifier, long>();

        foreach (PdfObjectIdentifier identifier in plan.ChangedObjectIdentifiers)
        {
            if (!plan.WritePlan.Document.Objects.TryGet(identifier, out PdfObject? value) ||
                value is null)
            {
                throw new InvalidOperationException(
                    $"Changed object '{identifier}' is no longer present in the document object store.");
            }

            offsets.Add(identifier, output.BytesWritten);
            PdfIndirectObjectSerializer.Write(output, identifier, value);
        }

        switch (plan.CrossReferenceMode)
        {
            case PdfCrossReferenceWriteMode.TraditionalTable:
                WriteTraditionalFinalization(output, plan, offsets);
                break;

            case PdfCrossReferenceWriteMode.CrossReferenceStream:
                WriteCrossReferenceStreamFinalization(output, plan, offsets);
                break;

            default:
                throw new InvalidOperationException(
                    "The resolved incremental cross-reference write mode is not supported.");
        }
    }

    private static void ValidateSupportedPlan(PdfIncrementalWritePlan plan)
    {
        if (plan.WritePlan.Mode != PdfWriteMode.IncrementalUpdate)
        {
            throw new InvalidOperationException(
                "PdfIncrementalUpdateWriter requires an incremental write plan.");
        }

        if (plan.WritePlan.EncryptionObject.HasValue)
        {
            throw new NotSupportedException(
                "Encrypted incremental output is deferred until encryption-aware Writing is implemented.");
        }

        if (plan.CrossReferenceMode == PdfCrossReferenceWriteMode.CrossReferenceStream &&
            plan.WritePlan.Document.Version < new Document.Model.PdfVersion(1, 5))
        {
            throw new NotSupportedException(
                "Cross-reference streams require PDF 1.5 or later.");
        }

        if (plan.CrossReferenceMode is not
            (PdfCrossReferenceWriteMode.TraditionalTable or
            PdfCrossReferenceWriteMode.CrossReferenceStream))
        {
            throw new InvalidOperationException(
                "The resolved incremental cross-reference write mode is not supported.");
        }

        foreach (PdfObjectIdentifier identifier in plan.ChangedObjectIdentifiers)
        {
            if (!plan.WritePlan.Document.Objects.Contains(identifier))
            {
                throw new InvalidOperationException(
                    $"Changed object '{identifier}' is no longer present in the document object store.");
            }
        }
    }

    private static void WriteTraditionalFinalization(
        PdfCountingWriteStream destination,
        PdfIncrementalWritePlan plan,
        IReadOnlyDictionary<PdfObjectIdentifier, long> offsets)
    {
        long crossReferenceOffset = destination.BytesWritten;
        PdfIncrementalTraditionalCrossReferenceWriter.Write(
            destination,
            offsets);
        WriteTrailer(
            destination,
            plan,
            CalculateTrailerSize(plan, additionalObjectNumber: null),
            crossReferenceOffset);
    }

    private static void WriteCrossReferenceStreamFinalization(
        PdfCountingWriteStream destination,
        PdfIncrementalWritePlan plan,
        IReadOnlyDictionary<PdfObjectIdentifier, long> offsets)
    {
        PdfObjectIdentifier crossReferenceIdentifier =
            plan.WritePlan
                .CreateObjectAllocator()
                .ReserveNext();

        long crossReferenceOffset = destination.BytesWritten;
        int size = CalculateTrailerSize(
            plan,
            crossReferenceIdentifier.ObjectNumber);

        PdfIncrementalCrossReferenceStreamWriter.Write(
            destination,
            plan,
            offsets,
            crossReferenceIdentifier,
            crossReferenceOffset,
            size);

        WriteStartXrefAndEof(destination, crossReferenceOffset);
    }

    private static int CalculateTrailerSize(
        PdfIncrementalWritePlan plan,
        int? additionalObjectNumber)
    {
        int highestObjectNumber = additionalObjectNumber ?? 0;

        foreach (PdfObjectIdentifier identifier in plan.WritePlan.Document.Objects.Identifiers)
        {
            highestObjectNumber = Math.Max(
                highestObjectNumber,
                identifier.ObjectNumber);
        }

        return checked(highestObjectNumber + 1);
    }

    private static void WriteTrailer(
        Stream destination,
        PdfIncrementalWritePlan plan,
        int size,
        long crossReferenceOffset)
    {
        var trailer = new PdfDictionary
        {
            { PdfNames.Size, new PdfInteger(size) },
            { PdfNames.Root, new PdfIndirectReference(plan.WritePlan.RootObject) },
            {
                PdfNames.Previous,
                new PdfInteger(plan.SourceRevision.CrossReferenceOffset)
            }
        };

        if (plan.WritePlan.InformationObject is { } informationObject)
        {
            trailer.Add(
                PdfNames.Info,
                new PdfIndirectReference(informationObject));
        }

        WriteAscii(destination, "trailer\n");
        PdfObjectSerializer.Write(destination, trailer);
        WriteAscii(destination, "\n");
        WriteStartXrefAndEof(destination, crossReferenceOffset);
    }

    private static void WriteStartXrefAndEof(
        Stream destination,
        long crossReferenceOffset)
    {
        WriteAscii(
            destination,
            string.Create(
                CultureInfo.InvariantCulture,
                $"startxref\n{crossReferenceOffset}\n%%EOF\n"));
    }

    private static void WriteAscii(Stream destination, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        destination.Write(bytes);
    }
}
