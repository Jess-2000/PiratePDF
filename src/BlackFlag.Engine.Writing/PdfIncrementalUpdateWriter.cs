using System.Globalization;
using System.Text;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing;

/// <summary>Appends a traditional incremental update while preserving the preceding revision bytes.</summary>
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

        long crossReferenceOffset = output.BytesWritten;
        PdfIncrementalTraditionalCrossReferenceWriter.Write(output, offsets);
        WriteTrailer(output, plan, CalculateTrailerSize(plan), crossReferenceOffset);
    }

    private static void ValidateSupportedPlan(PdfIncrementalWritePlan plan)
    {
        if (plan.WritePlan.Mode != PdfWriteMode.IncrementalUpdate)
        {
            throw new InvalidOperationException(
                "PdfIncrementalUpdateWriter requires an incremental write plan.");
        }

        if (plan.CrossReferenceMode != PdfCrossReferenceWriteMode.TraditionalTable)
        {
            throw new NotSupportedException(
                "BF-1053F emits traditional incremental cross-reference tables only. Incremental cross-reference streams are deferred.");
        }

        if (plan.WritePlan.EncryptionObject.HasValue)
        {
            throw new NotSupportedException(
                "Encrypted incremental output is deferred until encryption-aware Writing is implemented.");
        }
    }

    private static int CalculateTrailerSize(PdfIncrementalWritePlan plan)
    {
        int highestObjectNumber = 0;

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
        WriteAscii(
            destination,
            string.Create(
                CultureInfo.InvariantCulture,
                $"\nstartxref\n{crossReferenceOffset}\n%%EOF\n"));
    }

    private static void WriteAscii(Stream destination, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        destination.Write(bytes);
    }
}
