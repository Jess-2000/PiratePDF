using System.Buffers.Binary;

namespace BlackFlag.Engine.Security.Containers;

/// <summary>
/// Performs conservative signature-first classification of embedded payloads.
/// Declared names and media types are hints only and never override contradictory bytes.
/// </summary>
public sealed class DocumentEmbeddedObjectClassifier : IDocumentEmbeddedObjectClassifier
{
    private static ReadOnlySpan<byte> PdfSignature => "%PDF-"u8;
    private static ReadOnlySpan<byte> OleSignature => [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
    private static ReadOnlySpan<byte> ElfSignature => [0x7F, (byte)'E', (byte)'L', (byte)'F'];
    private static ReadOnlySpan<byte> PngSignature => [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];
    private static ReadOnlySpan<byte> JpegSignature => [0xFF, 0xD8, 0xFF];
    private static ReadOnlySpan<byte> RtfSignature => "{\\rtf"u8;
    private static ReadOnlySpan<byte> ZipLocalFileSignature => [0x50, 0x4B, 0x03, 0x04];
    private static ReadOnlySpan<byte> ZipEmptyArchiveSignature => [0x50, 0x4B, 0x05, 0x06];
    private static ReadOnlySpan<byte> ZipSpannedArchiveSignature => [0x50, 0x4B, 0x07, 0x08];
    private static ReadOnlySpan<byte> PeSignature => "PE\0\0"u8;
    private static ReadOnlySpan<byte> ShebangSignature => "#!"u8;

    /// <inheritdoc />
    public DocumentEmbeddedObjectClassification Classify(
        DocumentEmbeddedObject embeddedObject)
    {
        ArgumentNullException.ThrowIfNull(embeddedObject);

        ReadOnlySpan<byte> bytes = embeddedObject.Content.Span;

        if (bytes.IsEmpty)
        {
            return Unknown(embeddedObject);
        }

        switch (bytes[0])
        {
            case (byte)'%':
                if (bytes.StartsWith(PdfSignature))
                {
                    return Pdf();
                }

                break;

            case 0xD0:
                if (bytes.StartsWith(OleSignature))
                {
                    return Ole();
                }

                break;

            case (byte)'P':
                if (IsZip(bytes))
                {
                    return ClassifyZip(embeddedObject);
                }

                break;

            case 0x89:
                if (bytes.StartsWith(PngSignature))
                {
                    return Png();
                }

                break;

            case 0xFF:
                if (bytes.StartsWith(JpegSignature))
                {
                    return Jpeg();
                }

                break;

            case 0x7F:
                if (bytes.StartsWith(ElfSignature))
                {
                    return Executable();
                }

                break;

            case (byte)'M':
                if (IsPortableExecutable(bytes))
                {
                    return Executable();
                }

                break;
        }

        if (StartsWithIgnoringAsciiWhitespace(bytes, RtfSignature))
        {
            return Rtf();
        }

        if (IsMarkdownFileName(embeddedObject.DeclaredFileName) ||
            ContainsMediaType(embeddedObject.DeclaredMediaType, "markdown"))
        {
            return Markdown(embeddedObject.DeclaredMediaType);
        }

        if (LooksLikeScript(embeddedObject, bytes))
        {
            return Script(embeddedObject.DeclaredMediaType);
        }

        return Unknown(embeddedObject);
    }

    private static DocumentEmbeddedObjectClassification ClassifyZip(
        DocumentEmbeddedObject embeddedObject)
    {
        string? fileName = embeddedObject.DeclaredFileName;
        string? mediaType = embeddedObject.DeclaredMediaType;

        return GetZipContainerKind(fileName, mediaType) switch
        {
            DocumentEmbeddedObjectKind.OoxmlPackage =>
                Classification(
                    DocumentEmbeddedObjectKind.OoxmlPackage,
                    "ooxml",
                    isContainer: true,
                    isExecutable: false,
                    mediaType),

            DocumentEmbeddedObjectKind.OpenDocumentPackage =>
                Classification(
                    DocumentEmbeddedObjectKind.OpenDocumentPackage,
                    "opendocument",
                    isContainer: true,
                    isExecutable: false,
                    mediaType),

            _ =>
                Classification(
                    DocumentEmbeddedObjectKind.ZipContainer,
                    "zip",
                    isContainer: true,
                    isExecutable: false,
                    mediaType ?? "application/zip")
        };
    }

    private static DocumentEmbeddedObjectKind GetZipContainerKind(
        string? fileName,
        string? mediaType)
    {
        if (IsOoxmlFileName(fileName) ||
            ContainsMediaType(mediaType, "openxmlformats-officedocument") ||
            ContainsMediaType(mediaType, "ms-word.document.macroEnabled"))
        {
            return DocumentEmbeddedObjectKind.OoxmlPackage;
        }

        if (IsOpenDocumentFileName(fileName) ||
            ContainsMediaType(mediaType, "oasis.opendocument"))
        {
            return DocumentEmbeddedObjectKind.OpenDocumentPackage;
        }

        return DocumentEmbeddedObjectKind.ZipContainer;
    }

    private static bool IsZip(ReadOnlySpan<byte> bytes) =>
        bytes.StartsWith(ZipLocalFileSignature) ||
        bytes.StartsWith(ZipEmptyArchiveSignature) ||
        bytes.StartsWith(ZipSpannedArchiveSignature);

    private static bool IsPortableExecutable(ReadOnlySpan<byte> bytes)
    {
        const int DosHeaderPeOffset = 0x3C;
        const int MinimumDosHeaderLength = DosHeaderPeOffset + sizeof(int);

        if (bytes.Length < MinimumDosHeaderLength ||
            bytes[0] != (byte)'M' ||
            bytes[1] != (byte)'Z')
        {
            return false;
        }

        int peOffset =
            BinaryPrimitives.ReadInt32LittleEndian(
                bytes.Slice(
                    DosHeaderPeOffset,
                    sizeof(int)));

        return peOffset >= 0 &&
            peOffset <= bytes.Length - PeSignature.Length &&
            bytes.Slice(
                peOffset,
                PeSignature.Length)
                .SequenceEqual(PeSignature);
    }

    private static bool StartsWithIgnoringAsciiWhitespace(
        ReadOnlySpan<byte> bytes,
        ReadOnlySpan<byte> signature)
    {
        int index = 0;

        while (index < bytes.Length &&
            IsAsciiWhitespace(bytes[index]))
        {
            index++;
        }

        return bytes[index..].StartsWith(signature);
    }

    private static bool IsAsciiWhitespace(byte value) =>
        value is (byte)' ' or
                (byte)'\t' or
                (byte)'\r' or
                (byte)'\n' or
                0x0C;

    private static bool LooksLikeScript(
        DocumentEmbeddedObject embeddedObject,
        ReadOnlySpan<byte> bytes)
    {
        string? fileName = embeddedObject.DeclaredFileName;
        string? mediaType = embeddedObject.DeclaredMediaType;

        if (IsScriptFileName(fileName))
        {
            return true;
        }

        if (ContainsMediaType(mediaType, "javascript") ||
            ContainsMediaType(mediaType, "script"))
        {
            return true;
        }

        return bytes.StartsWith(ShebangSignature);
    }

    private static bool IsOoxmlFileName(string? fileName) =>
        GetExtension(fileName) switch
        {
            ".docx" or
            ".docm" or
            ".xlsx" or
            ".xlsm" or
            ".pptx" or
            ".pptm" => true,
            _ => false
        };

    private static bool IsOpenDocumentFileName(string? fileName) =>
        GetExtension(fileName) switch
        {
            ".odt" or
            ".ods" or
            ".odp" => true,
            _ => false
        };

    private static bool IsMarkdownFileName(string? fileName) =>
        GetExtension(fileName) switch
        {
            ".md" or ".markdown" => true,
            _ => false
        };

    private static bool IsScriptFileName(string? fileName) =>
        GetExtension(fileName) switch
        {
            ".js" or
            ".vbs" or
            ".ps1" or
            ".bat" or
            ".cmd" or
            ".sh" or
            ".py" => true,
            _ => false
        };

    private static string GetExtension(string? fileName) =>
        string.IsNullOrEmpty(fileName)
            ? string.Empty
            : Path.GetExtension(fileName).ToLowerInvariant();

    private static bool ContainsMediaType(
        string? mediaType,
        string value) =>
        mediaType?.Contains(
            value,
            StringComparison.OrdinalIgnoreCase) == true;

    private static DocumentEmbeddedObjectClassification Pdf() =>
        Classification(
            DocumentEmbeddedObjectKind.Pdf,
            "pdf",
            isContainer: true,
            isExecutable: false,
            "application/pdf");

    private static DocumentEmbeddedObjectClassification Ole() =>
        Classification(
            DocumentEmbeddedObjectKind.OleCompoundDocument,
            "ole",
            isContainer: true,
            isExecutable: false,
            "application/x-ole-storage");

    private static DocumentEmbeddedObjectClassification Png() =>
        Classification(
            DocumentEmbeddedObjectKind.Image,
            "png",
            isContainer: false,
            isExecutable: false,
            "image/png");

    private static DocumentEmbeddedObjectClassification Jpeg() =>
        Classification(
            DocumentEmbeddedObjectKind.Image,
            "jpeg",
            isContainer: false,
            isExecutable: false,
            "image/jpeg");

    private static DocumentEmbeddedObjectClassification Executable() =>
        Classification(
            DocumentEmbeddedObjectKind.Executable,
            "executable",
            isContainer: false,
            isExecutable: true,
            "application/octet-stream");

    private static DocumentEmbeddedObjectClassification Rtf() =>
        Classification(
            DocumentEmbeddedObjectKind.Rtf,
            "rtf",
            isContainer: true,
            isExecutable: false,
            "application/rtf");

    private static DocumentEmbeddedObjectClassification Markdown(
        string? mediaType) =>
        Classification(
            DocumentEmbeddedObjectKind.Markdown,
            "markdown",
            isContainer: true,
            isExecutable: false,
            mediaType ?? "text/markdown");

    private static DocumentEmbeddedObjectClassification Script(
        string? mediaType) =>
        Classification(
            DocumentEmbeddedObjectKind.Script,
            "script",
            isContainer: false,
            isExecutable: true,
            mediaType);

    private static DocumentEmbeddedObjectClassification Unknown(
        DocumentEmbeddedObject embeddedObject) =>
        Classification(
            DocumentEmbeddedObjectKind.Unknown,
            "unknown",
            isContainer: false,
            isExecutable: false,
            embeddedObject.DeclaredMediaType);

    private static DocumentEmbeddedObjectClassification Classification(
        DocumentEmbeddedObjectKind kind,
        string formatIdentifier,
        bool isContainer,
        bool isExecutable,
        string? mediaType) =>
        new(
            kind,
            formatIdentifier,
            isContainer,
            isExecutable,
            mediaType);
}
