using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Xml;
using BlackFlag.Engine.Security.Containers;
using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Formats.OpenDocument;

/// <summary>
/// Extracts passive security facts and recursively inspectable payloads from
/// OpenDocument Text packages.
/// </summary>
public sealed class OpenDocumentTextContainerSecurityAdapter :
    IDocumentContainerSecurityAdapter
{
    private const string SourceFormat = "opendocument-text";
    private const string XLinkNamespace = "http://www.w3.org/1999/xlink";

    private readonly OpenDocumentTextSecurityAnalysisOptions options;

    public OpenDocumentTextContainerSecurityAdapter(
        OpenDocumentTextSecurityAnalysisOptions? options = null)
    {
        this.options = options ?? OpenDocumentTextSecurityAnalysisOptions.Default;
        this.options.Validate();
    }

    public bool CanInspect(DocumentEmbeddedObjectClassification classification)
    {
        ArgumentNullException.ThrowIfNull(classification);

        return classification.Kind ==
            DocumentEmbeddedObjectKind.OpenDocumentPackage;
    }

    public DocumentContainerSecurityInspection Inspect(
        DocumentEmbeddedObject embeddedObject,
        DocumentSecurityInspectionBudget budget,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embeddedObject);
        ArgumentNullException.ThrowIfNull(budget);

        if (!IsTextPackage(embeddedObject))
        {
            return new DocumentContainerSecurityInspection();
        }

        var observations = new List<DocumentSecurityObservation>();
        var embeddedObjects = new List<DocumentContainerEntry>();
        var observationKeys = new HashSet<string>(StringComparer.Ordinal);

        using Stream packageStream = CreateReadOnlyStream(embeddedObject.Content);
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: false);

        if (archive.Entries.Count > options.MaximumArchiveEntries)
        {
            throw new InvalidDataException(
                $"ODT package contains {archive.Entries.Count} entries, exceeding the configured maximum of {options.MaximumArchiveEntries}.");
        }

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            budget.ConsumeWork();

            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            string partName = NormalizePartName(entry.FullName);

            if (IsSuspiciousPartName(entry.FullName))
            {
                AddObservation(
                    observations,
                    observationKeys,
                    DocumentSecurityObservationCategory.StructuralAnomaly,
                    "SuspiciousPackagePath",
                    $"The ODT package contains a suspicious part path '{entry.FullName}'.",
                    partName);
            }

            if (IsScriptPart(partName))
            {
                AddObservation(
                    observations,
                    observationKeys,
                    DocumentSecurityObservationCategory.ScriptContent,
                    "EmbeddedScript",
                    "The ODT package contains embedded script or macro content.",
                    partName);

                embeddedObjects.Add(
                    ReadEmbeddedEntry(
                        entry,
                        embeddedObject.LogicalPath,
                        partName,
                        budget,
                        cancellationToken));

                continue;
            }

            if (IsEmbeddedBinaryPart(partName))
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

            if (!IsInspectableXmlPart(partName))
            {
                continue;
            }

            byte[] xml = ReadXmlEntry(entry, budget, cancellationToken);

            InspectXml(
                xml,
                partName,
                observations,
                observationKeys,
                cancellationToken);
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
        budget.ValidateCompressionRatio(entry.CompressedLength, entry.Length);
        budget.ConsumeEmbeddedObject(entry.Length);

        byte[] content = ReadEntryBytes(entry, cancellationToken);

        return new DocumentContainerEntry(
            new DocumentEmbeddedObject(
                content,
                CombineLogicalPath(parentLogicalPath, partName),
                entry.CompressedLength,
                declaredFileName: Path.GetFileName(partName)),
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
                $"ODT XML part '{entry.FullName}' is {entry.Length} bytes, exceeding the configured maximum of {options.MaximumXmlPartBytes}.");
        }

        budget.ValidateCompressionRatio(entry.CompressedLength, entry.Length);
        budget.ConsumeExpandedBytes(entry.Length);

        return ReadEntryBytes(entry, cancellationToken);
    }

    private static byte[] ReadEntryBytes(
        ZipArchiveEntry entry,
        CancellationToken cancellationToken)
    {
        if (entry.Length > int.MaxValue)
        {
            throw new InvalidDataException(
                $"ODT part '{entry.FullName}' is too large to inspect in memory.");
        }

        byte[] content = new byte[checked((int)entry.Length)];
        using Stream stream = entry.Open();

        int offset = 0;
        while (offset < content.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int read = stream.Read(content, offset, content.Length - offset);
            if (read == 0)
            {
                throw new InvalidDataException(
                    $"ODT part '{entry.FullName}' ended before its declared expanded length.");
            }

            offset += read;
        }

        if (stream.ReadByte() != -1)
        {
            throw new InvalidDataException(
                $"ODT part '{entry.FullName}' exceeds its declared expanded length.");
        }

        return content;
    }

    private static void InspectXml(
        ReadOnlyMemory<byte> xml,
        string partName,
        List<DocumentSecurityObservation> observations,
        HashSet<string> observationKeys,
        CancellationToken cancellationToken)
    {
        using XmlReader reader = CreateXmlReader(xml);

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            if (string.Equals(reader.LocalName, "script", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(reader.LocalName, "event-listener", StringComparison.OrdinalIgnoreCase))
            {
                AddObservation(
                    observations,
                    observationKeys,
                    DocumentSecurityObservationCategory.ActiveContent,
                    "ScriptEvent",
                    "The ODT document contains script or event-driven active content.",
                    partName);
            }

            if (!reader.HasAttributes)
            {
                continue;
            }

            while (reader.MoveToNextAttribute())
            {
                if (!string.Equals(reader.LocalName, "href", StringComparison.Ordinal) ||
                    (!string.IsNullOrEmpty(reader.NamespaceURI) &&
                    !string.Equals(reader.NamespaceURI, XLinkNamespace, StringComparison.Ordinal)))
                {
                    continue;
                }

                string target = reader.Value;

                if (IsExternalReference(target))
                {
                    AddObservation(
                        observations,
                        observationKeys,
                        DocumentSecurityObservationCategory.ExternalReference,
                        "ExternalReference",
                        $"The ODT document contains an external reference targeting '{target}'.",
                        partName,
                        properties: new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            ["Target"] = target
                        });
                }
            }

            reader.MoveToElement();
        }
    }

    private static bool IsExternalReference(string target)
    {
        if (string.IsNullOrWhiteSpace(target) ||
            target[0] == '#')
        {
            return false;
        }

        if (target.StartsWith("./", StringComparison.Ordinal) ||
            target.StartsWith("../", StringComparison.Ordinal) ||
            target.StartsWith('/'))
        {
            return false;
        }

        return Uri.TryCreate(target, UriKind.Absolute, out Uri? uri) &&
            !string.Equals(uri.Scheme, "data", StringComparison.OrdinalIgnoreCase);
    }

    private static XmlReader CreateXmlReader(ReadOnlyMemory<byte> xml)
    {
        Stream stream = CreateReadOnlyStream(xml);

        return XmlReader.Create(
            stream,
            new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                CloseInput = true
            });
    }

    private static MemoryStream CreateReadOnlyStream(ReadOnlyMemory<byte> content)
    {
        if (MemoryMarshal.TryGetArray(content, out ArraySegment<byte> segment) &&
            segment.Array is not null)
        {
            return new MemoryStream(
                segment.Array,
                segment.Offset,
                segment.Count,
                writable: false,
                publiclyVisible: false);
        }

        return new MemoryStream(content.ToArray(), writable: false);
    }

    private static bool IsTextPackage(DocumentEmbeddedObject embeddedObject)
    {
        string extension = Path.GetExtension(embeddedObject.DeclaredFileName ?? string.Empty);

        if (string.Equals(extension, ".odt", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return embeddedObject.DeclaredMediaType?.Contains(
            "vnd.oasis.opendocument.text",
            StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsInspectableXmlPart(string partName) =>
        partName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);

    private static bool IsScriptPart(string partName) =>
        partName.StartsWith("Scripts/", StringComparison.OrdinalIgnoreCase) ||
        partName.StartsWith("Basic/", StringComparison.OrdinalIgnoreCase);

    private static bool IsEmbeddedBinaryPart(string partName)
    {
        if (partName.StartsWith("ObjectReplacements/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!partName.StartsWith("Object ", StringComparison.OrdinalIgnoreCase) &&
            !partName.StartsWith("Object/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !partName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSuspiciousPartName(string rawPartName)
    {
        string normalized = rawPartName.Replace('\\', '/');

        return normalized.StartsWith('/') ||
            normalized.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Any(segment => string.Equals(segment, "..", StringComparison.Ordinal));
    }

    private static string NormalizePartName(string partName) =>
        partName.Replace('\\', '/').TrimStart('/');

    private static string CombineLogicalPath(string parent, string partName)
    {
        string normalizedParent = parent.TrimEnd('/');

        return string.IsNullOrEmpty(normalizedParent)
            ? $"/{partName}"
            : $"{normalizedParent}/{partName}";
    }

    private static void ValidateEntryLength(ZipArchiveEntry entry)
    {
        if (entry.Length < 0 || entry.CompressedLength < 0)
        {
            throw new InvalidDataException(
                $"ODT part '{entry.FullName}' has an invalid ZIP length.");
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
        string key = $"{category}|{kind}|{logicalPath}|{objectIdentifier}";

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
