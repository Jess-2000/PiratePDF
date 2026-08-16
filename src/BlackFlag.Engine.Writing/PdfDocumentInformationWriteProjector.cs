using System.Text;

using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Projects explicitly targeted logical document-information fields onto an
/// existing PDF information dictionary without rebuilding untargeted entries.
/// </summary>
public static class PdfDocumentInformationWriteProjector
{
    private static readonly PdfName TitleName = PdfName.FromLatin1("Title");
    private static readonly PdfName AuthorName = PdfName.FromLatin1("Author");
    private static readonly PdfName SubjectName = PdfName.FromLatin1("Subject");
    private static readonly PdfName KeywordsName = PdfName.FromLatin1("Keywords");
    private static readonly PdfName CreatorName = PdfName.FromLatin1("Creator");
    private static readonly PdfName ProducerName = PdfName.FromLatin1("Producer");
    private static readonly PdfName CreationDateName = PdfName.FromLatin1("CreationDate");
    private static readonly PdfName ModificationDateName = PdfName.FromLatin1("ModDate");
    private static readonly Encoding BigEndianUnicode =
        new UnicodeEncoding(
            bigEndian: true,
            byteOrderMark: false,
            throwOnInvalidBytes: true);

    /// <summary>
    /// Creates one projection for the existing indirect information dictionary.
    /// Only the supplied fields are changed or removed.
    /// </summary>
    public static PdfObjectWriteProjectionSet Create(
        PdfDocument document,
        IEnumerable<PdfDocumentInformationField> fields)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(fields);

        PdfDocumentInformationField[] targets =
        [
            .. fields
                .Distinct()
                .OrderBy(field => field)
        ];

        if (targets.Length == 0)
        {
            throw new ArgumentException(
                "At least one document-information field must be targeted.",
                nameof(fields));
        }

        for (int index = 0; index < targets.Length; index++)
        {
            if (!Enum.IsDefined(targets[index]))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fields),
                    targets[index],
                    "The document-information field is not supported.");
            }
        }

        PdfObjectIdentifier identifier =
            document.Trailer.InformationObject ??
            throw new NotSupportedException(
                "Creating a new document-information indirect object is deferred until write-plan allocation supports reserved projected objects.");

        if (!document.Objects.TryGet(identifier, out PdfObject? current) ||
            current is null)
        {
            throw new InvalidOperationException(
                $"The document-information object '{identifier}' is not present in the object store.");
        }

        if (current is not PdfDictionary currentDictionary)
        {
            throw new InvalidOperationException(
                $"The document-information object '{identifier}' must be a PDF dictionary.");
        }

        PdfDictionary projectedDictionary = currentDictionary.CreateCopy();

        for (int index = 0; index < targets.Length; index++)
        {
            PdfDocumentInformationField field = targets[index];
            PdfName name = GetName(field);
            string? value = document.Information.GetValue(field);

            if (value is null)
            {
                _ = projectedDictionary.Remove(name);
                continue;
            }

            projectedDictionary.Set(
                name,
                CreateTextString(value));
        }

        return new PdfObjectWriteProjectionSet(
        [
            new PdfObjectWriteProjection(
                identifier,
                projectedDictionary)
        ]);
    }

    private static PdfName GetName(
        PdfDocumentInformationField field) => field switch
        {
            PdfDocumentInformationField.Title => TitleName,
            PdfDocumentInformationField.Author => AuthorName,
            PdfDocumentInformationField.Subject => SubjectName,
            PdfDocumentInformationField.Keywords => KeywordsName,
            PdfDocumentInformationField.Creator => CreatorName,
            PdfDocumentInformationField.Producer => ProducerName,
            PdfDocumentInformationField.CreationDate => CreationDateName,
            PdfDocumentInformationField.ModificationDate => ModificationDateName,
            _ => throw new ArgumentOutOfRangeException(
                nameof(field),
                field,
                "The document-information field is not supported.")
        };

    private static PdfString CreateTextString(string value)
    {
        bool isAscii = true;

        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] > 0x7F)
            {
                isAscii = false;
                break;
            }
        }

        if (isAscii)
        {
            return PdfString.FromLatin1(value);
        }

        byte[] textBytes = BigEndianUnicode.GetBytes(value);
        byte[] encoded = new byte[textBytes.Length + 2];
        encoded[0] = 0xFE;
        encoded[1] = 0xFF;
        textBytes.CopyTo(encoded, 2);

        return new PdfString(
            encoded,
            PdfStringFormat.Hexadecimal);
    }
}
