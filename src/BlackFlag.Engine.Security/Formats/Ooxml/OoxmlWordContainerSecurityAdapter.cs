using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Xml;
using BlackFlag.Engine.Security.Containers;
using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Formats.Ooxml;

/// <summary>
/// Extracts security facts and recursively inspectable embedded payloads from
/// Word OOXML packages without activating document behavior.
/// </summary>
public sealed class OoxmlWordContainerSecurityAdapter :
    IDocumentContainerSecurityAdapter
{
    private const string SourceFormat = "ooxml-word";

    private readonly OoxmlWordSecurityAnalysisOptions options;

    public OoxmlWordContainerSecurityAdapter(
        OoxmlWordSecurityAnalysisOptions? options = null)
    {
        this.options = options ?? OoxmlWordSecurityAnalysisOptions.Default;
        this.options.Validate();
    }

    public bool CanInspect(
        DocumentEmbeddedObjectClassification classification)
    {
        ArgumentNullException.ThrowIfNull(classification);

        return classification.Kind ==
            DocumentEmbeddedObjectKind.OoxmlPackage;
    }

    public DocumentContainerSecurityInspection Inspect(
        DocumentEmbeddedObject embeddedObject,
        DocumentSecurityInspectionBudget budget,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embeddedObject);
        ArgumentNullException.ThrowIfNull(budget);

        if (!IsWordPackage(embeddedObject))
        {
            return new DocumentContainerSecurityInspection();
        }

        var observations = new List<DocumentSecurityObservation>();
        var embeddedObjects = new List<DocumentContainerEntry>();
        var observationKeys = new HashSet<string>(StringComparer.Ordinal);

        using Stream packageStream =
            CreateReadOnlyStream(embeddedObject.Content);

        using var archive =
            new ZipArchive(
                packageStream,
                ZipArchiveMode.Read,
                leaveOpen: false);

        if (archive.Entries.Count > options.MaximumArchiveEntries)
        {
            throw new InvalidDataException(
                $"OOXML package contains {archive.Entries.Count} entries, exceeding the configured maximum of {options.MaximumArchiveEntries}.");
        }

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            budget.ConsumeWork();

            if (IsDirectory(entry))
            {
                continue;
            }

            string partName = NormalizePartName(entry.FullName);

            if (IsSuspiciousPartName(partName))
            {
                AddObservation(
                    observations,
                    observationKeys,
                    DocumentSecurityObservationCategory.StructuralAnomaly,
                    "SuspiciousPackagePath",
                    $"The OOXML package contains a suspicious part path '{partName}'.",
                    partName);
            }

            if (IsVbaProjectPart(partName))
            {
                AddObservation(
                    observations,
                    observationKeys,
                    DocumentSecurityObservationCategory.ScriptContent,
                    "VbaProject",
                    "The Word document contains a VBA macro project.",
                    partName);
            }

            if (IsActiveXPart(partName))
            {
                AddObservation(
                    observations,
                    observationKeys,
                    DocumentSecurityObservationCategory.ActiveContent,
                    "ActiveXControl",
                    "The Word document contains ActiveX content.",
                    partName);
            }

            bool isEmbeddedPayload =
                IsEmbeddedPayloadPart(partName) ||
                IsVbaProjectPart(partName) ||
                IsActiveXBinaryPart(partName);

            if (isEmbeddedPayload)
            {
                embeddedObjects.Add(
                    ReadEmbeddedEntry(
                        entry,
                        embeddedObject.LogicalPath,
                        partName,
                        budget,
                        cancellationToken));

                continue;
            }

            if (IsRelationshipPart(partName))
            {
                byte[] xml =
                    ReadXmlEntry(
                        entry,
                        budget,
                        cancellationToken);

                InspectRelationships(
                    xml,
                    partName,
                    observations,
                    observationKeys,
                    cancellationToken);

                continue;
            }

            if (string.Equals(
                    partName,
                    "[Content_Types].xml",
                    StringComparison.OrdinalIgnoreCase))
            {
                byte[] xml =
                    ReadXmlEntry(
                        entry,
                        budget,
                        cancellationToken);

                InspectContentTypes(
                    xml,
                    observations,
                    observationKeys,
                    cancellationToken);

                continue;
            }

            if (IsWordXmlPart(partName))
            {
                byte[] xml =
                    ReadXmlEntry(
                        entry,
                        budget,
                        cancellationToken);

                InspectWordFields(
                    xml,
                    partName,
                    observations,
                    observationKeys,
                    cancellationToken);
            }
        }

        return new DocumentContainerSecurityInspection(
            observations,
            embeddedObjects);
    }

    private static DocumentContainerEntry ReadEmbeddedEntry(
        ZipArchiveEntry entry,
        string parentLogicalPath,
        string partName,
        DocumentSecurityInspectionBudget budget,
        CancellationToken cancellationToken)
    {
        ValidateEntryLength(entry);
        budget.ValidateCompressionRatio(
            entry.CompressedLength,
            entry.Length);

        // Reserve the object and its expanded bytes before decompression so a
        // hostile package cannot allocate past the shared document budget.
        budget.ConsumeEmbeddedObject(entry.Length);

        byte[] content =
            ReadEntryBytes(
                entry,
                cancellationToken);

        string logicalPath =
            CombineLogicalPath(
                parentLogicalPath,
                partName);

        return new DocumentContainerEntry(
            new DocumentEmbeddedObject(
                content,
                logicalPath,
                entry.CompressedLength,
                GetPartMediaType(partName),
                Path.GetFileName(partName)),
            entry.CompressedLength,
            budgetAccounted: true);
    }

    private byte[] ReadXmlEntry(
        ZipArchiveEntry entry,
        DocumentSecurityInspectionBudget budget,
        CancellationToken cancellationToken)
    {
        ValidateEntryLength(entry);

        if (entry.Length > options.MaximumXmlPartBytes)
        {
            throw new InvalidDataException(
                $"OOXML XML part '{entry.FullName}' is {entry.Length} bytes, exceeding the configured maximum of {options.MaximumXmlPartBytes}.");
        }

        budget.ValidateCompressionRatio(
            entry.CompressedLength,
            entry.Length);

        budget.ConsumeExpandedBytes(entry.Length);

        return ReadEntryBytes(
            entry,
            cancellationToken);
    }

    private static byte[] ReadEntryBytes(
        ZipArchiveEntry entry,
        CancellationToken cancellationToken)
    {
        if (entry.Length > int.MaxValue)
        {
            throw new InvalidDataException(
                $"OOXML part '{entry.FullName}' is too large to inspect in memory.");
        }

        byte[] content = new byte[checked((int)entry.Length)];

        using Stream stream = entry.Open();

        int offset = 0;
        while (offset < content.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int read =
                stream.Read(
                    content,
                    offset,
                    content.Length - offset);

            if (read == 0)
            {
                throw new InvalidDataException(
                    $"OOXML part '{entry.FullName}' ended before its declared expanded length.");
            }

            offset += read;
        }

        if (stream.ReadByte() != -1)
        {
            throw new InvalidDataException(
                $"OOXML part '{entry.FullName}' exceeds its declared expanded length.");
        }

        return content;
    }

    private static void InspectRelationships(
        ReadOnlyMemory<byte> xml,
        string partName,
        List<DocumentSecurityObservation> observations,
        HashSet<string> observationKeys,
        CancellationToken cancellationToken)
    {
        using XmlReader reader =
            CreateXmlReader(xml);

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.NodeType != XmlNodeType.Element ||
                !string.Equals(
                    reader.LocalName,
                    "Relationship",
                    StringComparison.Ordinal))
            {
                continue;
            }

            string id =
                reader.GetAttribute("Id") ??
                string.Empty;

            string relationshipType =
                reader.GetAttribute("Type") ??
                string.Empty;

            string target =
                reader.GetAttribute("Target") ??
                string.Empty;

            string targetMode =
                reader.GetAttribute("TargetMode") ??
                string.Empty;

            if (string.Equals(
                    targetMode,
                    "External",
                    StringComparison.OrdinalIgnoreCase))
            {
                AddObservation(
                    observations,
                    observationKeys,
                    DocumentSecurityObservationCategory.ExternalReference,
                    "ExternalRelationship",
                    $"The Word document contains an external relationship targeting '{target}'.",
                    partName,
                    id,
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["RelationshipType"] = relationshipType,
                        ["Target"] = target,
                        ["TargetMode"] = targetMode
                    });
            }

            if (relationshipType.EndsWith(
                    "/vbaProject",
                    StringComparison.OrdinalIgnoreCase))
            {
                AddObservation(
                    observations,
                    observationKeys,
                    DocumentSecurityObservationCategory.ScriptContent,
                    "VbaProject",
                    "The Word document contains a relationship to a VBA macro project.",
                    partName,
                    id);
            }

            if (relationshipType.Contains(
                    "/activeX",
                    StringComparison.OrdinalIgnoreCase))
            {
                AddObservation(
                    observations,
                    observationKeys,
                    DocumentSecurityObservationCategory.ActiveContent,
                    "ActiveXControl",
                    "The Word document contains a relationship to ActiveX content.",
                    partName,
                    id);
            }

            if (relationshipType.EndsWith(
                    "/oleObject",
                    StringComparison.OrdinalIgnoreCase))
            {
                AddObservation(
                    observations,
                    observationKeys,
                    DocumentSecurityObservationCategory.EmbeddedContent,
                    "EmbeddedOleObject",
                    "The Word document contains an OLE object relationship.",
                    partName,
                    id);
            }

            if (relationshipType.EndsWith(
                    "/package",
                    StringComparison.OrdinalIgnoreCase))
            {
                AddObservation(
                    observations,
                    observationKeys,
                    DocumentSecurityObservationCategory.EmbeddedContent,
                    "EmbeddedPackage",
                    "The Word document contains an embedded package relationship.",
                    partName,
                    id);
            }
        }
    }

    private static void InspectContentTypes(
        ReadOnlyMemory<byte> xml,
        List<DocumentSecurityObservation> observations,
        HashSet<string> observationKeys,
        CancellationToken cancellationToken)
    {
        using XmlReader reader =
            CreateXmlReader(xml);

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.NodeType != XmlNodeType.Element ||
                !string.Equals(
                    reader.LocalName,
                    "Override",
                    StringComparison.Ordinal))
            {
                continue;
            }

            string partName =
                NormalizePartName(
                    reader.GetAttribute("PartName") ??
                    string.Empty);

            string contentType =
                reader.GetAttribute("ContentType") ??
                string.Empty;

            if (contentType.Contains(
                    "vbaProject",
                    StringComparison.OrdinalIgnoreCase))
            {
                AddObservation(
                    observations,
                    observationKeys,
                    DocumentSecurityObservationCategory.ScriptContent,
                    "VbaProject",
                    "The Word document declares a VBA macro project.",
                    partName);
            }

            if (contentType.Contains(
                    "activeX",
                    StringComparison.OrdinalIgnoreCase))
            {
                AddObservation(
                    observations,
                    observationKeys,
                    DocumentSecurityObservationCategory.ActiveContent,
                    "ActiveXControl",
                    "The Word document declares ActiveX content.",
                    partName);
            }
        }
    }

    private static void InspectWordFields(
        ReadOnlyMemory<byte> xml,
        string partName,
        List<DocumentSecurityObservation> observations,
        HashSet<string> observationKeys,
        CancellationToken cancellationToken)
    {
        using XmlReader reader =
            CreateXmlReader(xml);

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            string? instruction = null;

            if (string.Equals(
                    reader.LocalName,
                    "fldSimple",
                    StringComparison.Ordinal))
            {
                instruction =
                    GetAttributeByLocalName(
                        reader,
                        "instr");
            }
            else if (string.Equals(
                        reader.LocalName,
                        "instrText",
                        StringComparison.Ordinal))
            {
                instruction =
                    reader.ReadElementContentAsString();
            }

            if (!IsDdeInstruction(instruction))
            {
                continue;
            }

            AddObservation(
                observations,
                observationKeys,
                DocumentSecurityObservationCategory.ActiveContent,
                "DdeField",
                "The Word document contains a DDE or DDEAUTO field instruction.",
                partName,
                properties: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["FieldInstruction"] = instruction!.Trim()
                });
        }
    }

    private static XmlReader CreateXmlReader(
        ReadOnlyMemory<byte> xml)
    {
        Stream stream =
            CreateReadOnlyStream(xml);

        var settings =
            new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                CloseInput = true
            };

        return XmlReader.Create(
            stream,
            settings);
    }

    private static MemoryStream CreateReadOnlyStream(
        ReadOnlyMemory<byte> content)
    {
        if (MemoryMarshal.TryGetArray(
                content,
                out ArraySegment<byte> segment) &&
            segment.Array is not null)
        {
            return new MemoryStream(
                segment.Array,
                segment.Offset,
                segment.Count,
                writable: false,
                publiclyVisible: false);
        }

        return new MemoryStream(
            content.ToArray(),
            writable: false);
    }

    private static string? GetAttributeByLocalName(
        XmlReader reader,
        string localName)
    {
        if (!reader.HasAttributes)
        {
            return null;
        }

        while (reader.MoveToNextAttribute())
        {
            if (string.Equals(
                    reader.LocalName,
                    localName,
                    StringComparison.Ordinal))
            {
                string value = reader.Value;
                reader.MoveToElement();
                return value;
            }
        }

        reader.MoveToElement();
        return null;
    }

    private static bool IsDdeInstruction(string? instruction)
    {
        if (string.IsNullOrWhiteSpace(instruction))
        {
            return false;
        }

        ReadOnlySpan<char> value =
            instruction.AsSpan().TrimStart();

        int separator = -1;

        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] is ' ' or '\t' or '\r' or '\n')
            {
                separator = index;
                break;
            }
        }

        ReadOnlySpan<char> command =
            separator < 0
                ? value
                : value[..separator];

        return command.Equals(
                "DDE",
                StringComparison.OrdinalIgnoreCase) ||
            command.Equals(
                "DDEAUTO",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWordPackage(
        DocumentEmbeddedObject embeddedObject)
    {
        string extension =
            Path.GetExtension(
                embeddedObject.DeclaredFileName ??
                string.Empty);

        if (string.Equals(
                extension,
                ".docx",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                extension,
                ".docm",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string mediaType =
            embeddedObject.DeclaredMediaType ??
            string.Empty;

        return mediaType.Contains(
                "wordprocessingml.document",
                StringComparison.OrdinalIgnoreCase) ||
            mediaType.Contains(
                "ms-word.document.macroEnabled",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDirectory(ZipArchiveEntry entry) =>
        string.IsNullOrEmpty(entry.Name);

    private static bool IsRelationshipPart(string partName) =>
        partName.EndsWith(
            ".rels",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsWordXmlPart(string partName) =>
        partName.StartsWith(
            "word/",
            StringComparison.OrdinalIgnoreCase) &&
        partName.EndsWith(
            ".xml",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsVbaProjectPart(string partName) =>
        string.Equals(
            partName,
            "word/vbaProject.bin",
            StringComparison.OrdinalIgnoreCase) ||
        partName.EndsWith(
            "/vbaProject.bin",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsActiveXPart(string partName) =>
        partName.StartsWith(
            "word/activeX/",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsActiveXBinaryPart(string partName) =>
        IsActiveXPart(partName) &&
        partName.EndsWith(
            ".bin",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsEmbeddedPayloadPart(string partName) =>
        partName.StartsWith(
            "word/embeddings/",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsSuspiciousPartName(string partName) =>
        partName.StartsWith('/') ||
        partName.Contains(
            "../",
            StringComparison.Ordinal) ||
        partName.Contains(
            "..\\",
            StringComparison.Ordinal);

    private static string NormalizePartName(string partName) =>
        partName
            .Replace(
                '\\',
                '/')
            .TrimStart('/');

    private static string CombineLogicalPath(
        string parent,
        string partName)
    {
        string normalizedParent =
            parent.TrimEnd('/');

        return string.IsNullOrEmpty(normalizedParent)
            ? $"/{partName}"
            : $"{normalizedParent}/{partName}";
    }

    private static string? GetPartMediaType(string partName)
    {
        string extension =
            Path.GetExtension(partName);

        return extension switch
        {
            ".docx" =>
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".docm" =>
                "application/vnd.ms-word.document.macroEnabled.12",
            ".pdf" =>
                "application/pdf",
            ".rtf" =>
                "application/rtf",
            _ => null
        };
    }

    private static void ValidateEntryLength(
        ZipArchiveEntry entry)
    {
        if (entry.Length < 0 ||
            entry.CompressedLength < 0)
        {
            throw new InvalidDataException(
                $"OOXML part '{entry.FullName}' has an invalid ZIP length.");
        }
    }

    private static void AddObservation(
        List<DocumentSecurityObservation> observations,
        HashSet<string> observationKeys,
        DocumentSecurityObservationCategory category,
        string kind,
        string description,
        string logicalPath,
        string? objectIdentifier = null,
        IReadOnlyDictionary<string, string>? properties = null)
    {
        string key =
            $"{category}|{kind}|{logicalPath}|{objectIdentifier}";

        if (!observationKeys.Add(key))
        {
            return;
        }

        observations.Add(
            new DocumentSecurityObservation(
                category,
                kind,
                description,
                SourceFormat,
                new DocumentSecuritySourceLocation
                {
                    LogicalPath = logicalPath,
                    ObjectIdentifier = objectIdentifier
                },
                properties));
    }
}
