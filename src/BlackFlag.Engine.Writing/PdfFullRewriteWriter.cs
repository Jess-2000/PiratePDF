using System.Globalization;
using System.Text;

using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing;

/// <summary>Writes a complete replacement PDF revision using a traditional cross-reference table.</summary>
public static class PdfFullRewriteWriter
{
    private static readonly byte[] BinaryComment =
    [
        (byte)'%', 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n'
    ];

    /// <summary>Writes a planned full rewrite to a new byte array.</summary>
    public static byte[] Write(PdfWritePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        using var destination = new MemoryStream();
        Write(destination, plan);
        return destination.ToArray();
    }

    /// <summary>Writes a planned full rewrite to a caller-owned destination.</summary>
    public static void Write(Stream destination, PdfWritePlan plan)
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
        WriteHeader(output, plan.Document.Version);

        var offsets = new Dictionary<PdfObjectIdentifier, long>();

        foreach (PdfObjectIdentifier identifier in plan.ExistingObjectIdentifiers)
        {
            PdfObject value = plan.Document.Objects.Get(identifier);
            offsets.Add(identifier, output.BytesWritten);
            PdfIndirectObjectSerializer.Write(output, identifier, value);
        }

        long crossReferenceOffset = output.BytesWritten;
        int highestObjectNumber = GetHighestObjectNumber(plan.ExistingObjectIdentifiers);

        PdfTraditionalCrossReferenceWriter.Write(
            output,
            offsets,
            highestObjectNumber);

        WriteTrailer(
            output,
            plan,
            highestObjectNumber + 1,
            crossReferenceOffset);
    }

    private static void ValidateSupportedPlan(PdfWritePlan plan)
    {
        if (plan.Mode != PdfWriteMode.FullRewrite)
        {
            throw new NotSupportedException(
                "BF-1053C supports complete full-rewrite output only. Incremental updates are deferred.");
        }

        if (plan.CrossReferenceMode == PdfCrossReferenceWriteMode.CrossReferenceStream)
        {
            throw new NotSupportedException(
                "BF-1053C supports traditional cross-reference tables only. Cross-reference streams are deferred.");
        }

        if (plan.EncryptionObject.HasValue)
        {
            throw new NotSupportedException(
                "Encrypted PDF output is deferred until encryption-aware Writing is implemented.");
        }

        foreach (PdfObjectIdentifier identifier in plan.ExistingObjectIdentifiers)
        {
            if (!plan.Document.Objects.Contains(identifier))
            {
                throw new InvalidOperationException(
                    $"The planned object '{identifier}' is no longer present in the document object store.");
            }
        }
    }

    private static int GetHighestObjectNumber(
        IReadOnlyList<PdfObjectIdentifier> identifiers)
    {
        return identifiers.Count == 0
            ? 0
            : identifiers[^1].ObjectNumber;
    }

    private static void WriteHeader(Stream destination, PdfVersion version)
    {
        WriteAscii(
            destination,
            string.Create(
                CultureInfo.InvariantCulture,
                $"%PDF-{version.Major}.{version.Minor}\n"));
        destination.Write(BinaryComment);
    }

    private static void WriteTrailer(
        Stream destination,
        PdfWritePlan plan,
        int size,
        long crossReferenceOffset)
    {
        var trailer = new PdfDictionary();
        trailer.Add(PdfNames.Size, new PdfInteger(size));
        trailer.Add(PdfNames.Root, new PdfIndirectReference(plan.RootObject));

        if (plan.InformationObject is { } informationObject)
        {
            trailer.Add(PdfNames.Info, new PdfIndirectReference(informationObject));
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
