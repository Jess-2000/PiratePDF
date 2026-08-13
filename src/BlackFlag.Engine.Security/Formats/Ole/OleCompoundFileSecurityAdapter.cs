using System.Buffers.Binary;
using System.Text;
using BlackFlag.Engine.Security.Containers;
using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Formats.Ole;

/// <summary>
/// Passively inspects OLE Compound File Binary containers used by legacy DOC
/// files and embedded OLE payloads.
/// </summary>
public sealed class OleCompoundFileSecurityAdapter :
    IDocumentContainerSecurityAdapter
{
    private const string SourceFormat = "ole";

    private static readonly byte[] OleSignature =
        [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    private static readonly byte[][] KnownPayloadSignatures =
    [
        OleSignature,
        [(byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-'],
        [0x50, 0x4B, 0x03, 0x04],
        [0x4D, 0x5A]
    ];

    private static readonly (byte[] Ascii, byte[] Utf16)[] DdePatterns =
    [
        (Encoding.ASCII.GetBytes("DDEAUTO"), Encoding.Unicode.GetBytes("DDEAUTO")),
        (Encoding.ASCII.GetBytes("DDE "), Encoding.Unicode.GetBytes("DDE "))
    ];

    private static readonly (byte[] Ascii, byte[] Utf16)[] ExternalReferencePatterns =
    [
        (Encoding.ASCII.GetBytes("http://"), Encoding.Unicode.GetBytes("http://")),
        (Encoding.ASCII.GetBytes("https://"), Encoding.Unicode.GetBytes("https://")),
        (Encoding.ASCII.GetBytes("file://"), Encoding.Unicode.GetBytes("file://"))
    ];

    private readonly OleCompoundFileSecurityAnalysisOptions options;

    public OleCompoundFileSecurityAdapter(
        OleCompoundFileSecurityAnalysisOptions? options = null)
    {
        this.options = options ?? OleCompoundFileSecurityAnalysisOptions.Default;
        this.options.Validate();
    }

    public bool CanInspect(DocumentEmbeddedObjectClassification classification)
    {
        ArgumentNullException.ThrowIfNull(classification);
        return classification.Kind == DocumentEmbeddedObjectKind.OleCompoundDocument;
    }

    public DocumentContainerSecurityInspection Inspect(
        DocumentEmbeddedObject embeddedObject,
        DocumentSecurityInspectionBudget budget,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embeddedObject);
        ArgumentNullException.ThrowIfNull(budget);

        if (!HasMinimumOleHeader(embeddedObject.Content.Span))
        {
            return new DocumentContainerSecurityInspection();
        }

        var observations = new List<DocumentSecurityObservation>(capacity: 4);
        var children = new List<DocumentContainerEntry>();
        var keys = new HashSet<string>(StringComparer.Ordinal);

        var reader = new CompoundFileReader(
            embeddedObject.Content,
            options.MaximumDirectoryEntries,
            options.MaximumStreamBytes);

        foreach (CompoundFileEntry entry in reader.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            budget.ConsumeWork();

            string name = entry.Name;
            string entryPath = EntryPath(embeddedObject.LogicalPath, entry);

            if (IsMacroEntry(name))
            {
                AddObservation(
                    observations,
                    keys,
                    DocumentSecurityObservationCategory.ScriptContent,
                    "LegacyMacroContent",
                    "The OLE container contains a VBA or legacy macro-related entry.",
                    entryPath);
            }

            if (IsActiveXEntry(name))
            {
                AddObservation(
                    observations,
                    keys,
                    DocumentSecurityObservationCategory.ActiveContent,
                    "ActiveXContent",
                    "The OLE container contains ActiveX-related content.",
                    entryPath);
            }

            if (IsEmbeddedObjectEntry(name))
            {
                AddObservation(
                    observations,
                    keys,
                    DocumentSecurityObservationCategory.EmbeddedContent,
                    "EmbeddedOleObject",
                    "The OLE container contains an embedded-object entry.",
                    entryPath);
            }

            if (entry.Type != CompoundFileEntryType.Stream)
            {
                continue;
            }

            bool inspectWordBehavior =
                IsWordTextStream(name) &&
                entry.StreamSize <= options.MaximumTextScanBytes;

            bool inspectNativePayload =
                IsNativePayloadStream(name) &&
                entry.StreamSize <= options.MaximumStreamBytes;

            if (!inspectWordBehavior && !inspectNativePayload)
            {
                continue;
            }

            byte[] bytes = reader.ReadStream(entry, cancellationToken);

            if (inspectWordBehavior)
            {
                InspectWordBehavior(
                    bytes,
                    entryPath,
                    observations,
                    keys);
            }

            if (inspectNativePayload)
            {
                ReadOnlyMemory<byte>? payload = FindEmbeddedPayload(bytes);

                if (payload is not null)
                {
                    children.Add(
                        new DocumentContainerEntry(
                            new DocumentEmbeddedObject(
                                payload.Value,
                                entryPath + "/payload",
                                declaredFileName: "embedded.bin"),
                            payload.Value.Length));
                }
            }
        }

        return new DocumentContainerSecurityInspection(observations, children);
    }

    private static bool HasMinimumOleHeader(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 512 &&
        bytes[..OleSignature.Length].SequenceEqual(OleSignature);

    private static void InspectWordBehavior(
        ReadOnlySpan<byte> bytes,
        string logicalPath,
        List<DocumentSecurityObservation> observations,
        HashSet<string> keys)
    {
        if (ContainsAnyAsciiOrUtf16(bytes, DdePatterns))
        {
            AddObservation(
                observations,
                keys,
                DocumentSecurityObservationCategory.ActiveContent,
                "DdeField",
                "The legacy Word document contains DDE or DDEAUTO field data.",
                logicalPath);
        }

        if (ContainsAnyAsciiOrUtf16(bytes, ExternalReferencePatterns))
        {
            AddObservation(
                observations,
                keys,
                DocumentSecurityObservationCategory.ExternalReference,
                "ExternalReference",
                "The legacy Word document contains an external-reference string.",
                logicalPath);
        }
    }

    private static bool ContainsAnyAsciiOrUtf16(
        ReadOnlySpan<byte> bytes,
        (byte[] Ascii, byte[] Utf16)[] patterns)
    {
        for (int patternIndex = 0;
            patternIndex < patterns.Length;
            patternIndex++)
        {
            (byte[] ascii, byte[] utf16) = patterns[patternIndex];

            if (bytes.IndexOf(ascii) >= 0 ||
                bytes.IndexOf(utf16) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static ReadOnlyMemory<byte>? FindEmbeddedPayload(byte[] bytes)
    {
        ReadOnlySpan<byte> content = bytes;

        for (int signatureIndex = 0;
            signatureIndex < KnownPayloadSignatures.Length;
            signatureIndex++)
        {
            int payloadIndex =
                content.IndexOf(KnownPayloadSignatures[signatureIndex]);

            if (payloadIndex >= 0)
            {
                return bytes.AsMemory(payloadIndex);
            }
        }

        return null;
    }

    private static bool IsMacroEntry(string name) =>
        name.Contains("VBA", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Macros", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "PROJECT", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "PROJECTwm", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("WordBasic", StringComparison.OrdinalIgnoreCase);

    private static bool IsActiveXEntry(string name) =>
        name.Contains("ActiveX", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("OCX", StringComparison.OrdinalIgnoreCase);

    private static bool IsEmbeddedObjectEntry(string name) =>
        name.Contains("ObjectPool", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Ole10Native", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "Package", StringComparison.OrdinalIgnoreCase);

    private static bool IsNativePayloadStream(string name) =>
        name.Contains("Ole10Native", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "Package", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "CONTENTS", StringComparison.OrdinalIgnoreCase);

    private static bool IsWordTextStream(string name) =>
        string.Equals(name, "WordDocument", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "0Table", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "1Table", StringComparison.OrdinalIgnoreCase);

    private static string EntryPath(
        string parent,
        CompoundFileEntry entry) =>
        $"{parent.TrimEnd('/')}/ole/{entry.Index}:{entry.Name}";

    private static void AddObservation(
        List<DocumentSecurityObservation> observations,
        HashSet<string> keys,
        DocumentSecurityObservationCategory category,
        string kind,
        string description,
        string logicalPath)
    {
        string key = $"{category}|{kind}|{logicalPath}";
        if (!keys.Add(key))
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
                    LogicalPath = logicalPath
                }));
    }

    private enum CompoundFileEntryType : byte
    {
        Unknown = 0,
        Storage = 1,
        Stream = 2,
        Root = 5
    }

    private sealed record CompoundFileEntry(
        int Index,
        string Name,
        CompoundFileEntryType Type,
        uint StartSector,
        long StreamSize);

    private sealed class CompoundFileReader
    {
        private const uint FreeSector = 0xFFFFFFFF;
        private const uint EndOfChain = 0xFFFFFFFE;
        private const uint FatSector = 0xFFFFFFFD;
        private const uint DifatSector = 0xFFFFFFFC;

        private readonly ReadOnlyMemory<byte> content;
        private readonly int sectorSize;
        private readonly int miniSectorSize;
        private readonly uint[] fat;
        private readonly uint[] miniFat;
        private readonly byte[] miniStream;
        private readonly long maximumStreamBytes;

        public CompoundFileReader(
            ReadOnlyMemory<byte> content,
            int maximumDirectoryEntries,
            long maximumStreamBytes)
        {
            this.content = content;
            this.maximumStreamBytes = maximumStreamBytes;

            ReadOnlySpan<byte> bytes = content.Span;

            if (bytes.Length < 512 ||
                !bytes[..8].SequenceEqual(OleSignature))
            {
                throw new InvalidDataException("OLE compound-file signature is invalid.");
            }

            ushort byteOrder = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(28, 2));
            if (byteOrder != 0xFFFE)
            {
                throw new InvalidDataException("OLE compound-file byte order is unsupported.");
            }

            ushort sectorShift = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(30, 2));
            ushort miniSectorShift = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(32, 2));

            sectorSize = 1 << sectorShift;
            miniSectorSize = 1 << miniSectorShift;

            if ((sectorSize != 512 && sectorSize != 4096) ||
                miniSectorSize != 64 ||
                bytes.Length < sectorSize)
            {
                throw new InvalidDataException("OLE compound-file sector geometry is invalid.");
            }

            uint fatSectorCount = ReadUInt32(bytes, 44);
            uint firstDirectorySector = ReadUInt32(bytes, 48);
            uint firstMiniFatSector = ReadUInt32(bytes, 60);
            uint miniFatSectorCount = ReadUInt32(bytes, 64);
            uint firstDifatSector = ReadUInt32(bytes, 68);
            uint difatSectorCount = ReadUInt32(bytes, 72);

            List<uint> fatSectorIds = ReadDifat(
                bytes,
                fatSectorCount,
                firstDifatSector,
                difatSectorCount);

            fat = BuildFat(fatSectorIds);

            byte[] directoryBytes = ReadRegularChain(
                firstDirectorySector,
                maximumDirectoryEntries * 128L);

            Entries = ParseDirectory(directoryBytes, maximumDirectoryEntries);

            CompoundFileEntry? root = null;

            for (int entryIndex = 0;
                entryIndex < Entries.Count;
                entryIndex++)
            {
                CompoundFileEntry candidate = Entries[entryIndex];

                if (candidate.Type == CompoundFileEntryType.Root)
                {
                    root = candidate;
                    break;
                }
            }

            if (miniFatSectorCount > 0 && firstMiniFatSector < DifatSector)
            {
                byte[] miniFatBytes = ReadRegularChain(
                    firstMiniFatSector,
                    checked((long)miniFatSectorCount * sectorSize));

                miniFat = new uint[miniFatBytes.Length / sizeof(uint)];
                for (int index = 0; index < miniFat.Length; index++)
                {
                    miniFat[index] = ReadUInt32(miniFatBytes, index * sizeof(uint));
                }
            }
            else
            {
                miniFat = [];
            }

            if (root is not null && root.StreamSize > 0)
            {
                miniStream = ReadRegularStream(root.StartSector, root.StreamSize);
            }
            else
            {
                miniStream = [];
            }
        }

        public List<CompoundFileEntry> Entries { get; }

        public byte[] ReadStream(
            CompoundFileEntry entry,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.Type != CompoundFileEntryType.Stream)
            {
                throw new InvalidDataException("OLE directory entry is not a stream.");
            }

            if (entry.StreamSize > maximumStreamBytes)
            {
                throw new InvalidDataException(
                    $"OLE stream '{entry.Name}' exceeds the configured stream limit.");
            }

            return entry.StreamSize < 4096 && miniFat.Length > 0
                ? ReadMiniStream(entry.StartSector, entry.StreamSize)
                : ReadRegularStream(entry.StartSector, entry.StreamSize);
        }

        private List<uint> ReadDifat(
            ReadOnlySpan<byte> header,
            uint fatSectorCount,
            uint firstDifatSector,
            uint difatSectorCount)
        {
            var result = new List<uint>();

            for (int index = 0; index < 109 && result.Count < fatSectorCount; index++)
            {
                uint value = ReadUInt32(header, 76 + (index * sizeof(uint)));
                if (value < DifatSector)
                {
                    result.Add(value);
                }
            }

            uint current = firstDifatSector;
            var visited = new HashSet<uint>();

            for (uint chainIndex = 0;
                chainIndex < difatSectorCount && current < DifatSector;
                chainIndex++)
            {
                if (!visited.Add(current))
                {
                    throw new InvalidDataException("OLE DIFAT chain contains a cycle.");
                }

                ReadOnlySpan<byte> sector = GetSector(current);
                int entriesPerSector = (sectorSize / sizeof(uint)) - 1;

                for (int index = 0;
                    index < entriesPerSector && result.Count < fatSectorCount;
                    index++)
                {
                    uint value = ReadUInt32(sector, index * sizeof(uint));
                    if (value < DifatSector)
                    {
                        result.Add(value);
                    }
                }

                current = ReadUInt32(sector, entriesPerSector * sizeof(uint));
            }

            if (result.Count < fatSectorCount)
            {
                throw new InvalidDataException("OLE FAT sector list is incomplete.");
            }

            return result;
        }

        private uint[] BuildFat(List<uint> fatSectorIds)
        {
            int valuesPerSector = sectorSize / sizeof(uint);
            uint[] values =
                new uint[checked(fatSectorIds.Count * valuesPerSector)];

            int valueIndex = 0;

            for (int sectorIndex = 0;
                sectorIndex < fatSectorIds.Count;
                sectorIndex++)
            {
                ReadOnlySpan<byte> sector =
                    GetSector(fatSectorIds[sectorIndex]);

                for (int offset = 0;
                    offset < sector.Length;
                    offset += sizeof(uint))
                {
                    values[valueIndex++] =
                        ReadUInt32(sector, offset);
                }
            }

            return values;
        }

        private byte[] ReadRegularStream(uint startSector, long streamSize)
        {
            if (streamSize < 0 || streamSize > maximumStreamBytes)
            {
                throw new InvalidDataException("OLE stream size exceeds the configured limit.");
            }

            byte[] chain = ReadRegularChain(startSector, streamSize);

            if (chain.Length != streamSize)
            {
                throw new InvalidDataException(
                    "OLE stream ended before its declared size.");
            }

            return chain;
        }

        private byte[] ReadRegularChain(uint startSector, long maximumBytes)
        {
            if (startSector >= DifatSector || maximumBytes <= 0)
            {
                return [];
            }

            if (maximumBytes > int.MaxValue)
            {
                throw new InvalidDataException(
                    "OLE stream is too large to materialize.");
            }

            byte[] result = new byte[checked((int)maximumBytes)];
            int written = 0;
            uint current = startSector;
            var visited = new HashSet<uint>();

            while (written < result.Length && current < DifatSector)
            {
                if (!visited.Add(current))
                {
                    throw new InvalidDataException(
                        "OLE FAT chain contains a cycle.");
                }

                if (current >= fat.Length)
                {
                    throw new InvalidDataException(
                        "OLE FAT chain references an invalid sector.");
                }

                ReadOnlySpan<byte> sector = GetSector(current);
                int count = Math.Min(
                    sector.Length,
                    result.Length - written);

                sector[..count].CopyTo(result.AsSpan(written, count));
                written += count;

                current = fat[current];

                if (current == EndOfChain)
                {
                    break;
                }

                if (current == FreeSector ||
                    current == FatSector ||
                    current == DifatSector)
                {
                    throw new InvalidDataException(
                        "OLE FAT chain contains an invalid sector marker.");
                }
            }

            if (written == result.Length)
            {
                return result;
            }

            Array.Resize(ref result, written);
            return result;
        }

        private byte[] ReadMiniStream(uint startMiniSector, long streamSize)
        {
            if (streamSize > int.MaxValue)
            {
                throw new InvalidDataException("OLE mini stream is too large to materialize.");
            }

            byte[] result = new byte[checked((int)streamSize)];
            int written = 0;
            uint current = startMiniSector;
            var visited = new HashSet<uint>();

            while (written < result.Length && current < DifatSector)
            {
                if (!visited.Add(current))
                {
                    throw new InvalidDataException("OLE mini-FAT chain contains a cycle.");
                }

                if (current >= miniFat.Length)
                {
                    throw new InvalidDataException("OLE mini-FAT chain references an invalid sector.");
                }

                long sourceOffset = checked((long)current * miniSectorSize);
                if (sourceOffset < 0 ||
                    sourceOffset + miniSectorSize > miniStream.Length)
                {
                    throw new InvalidDataException("OLE mini stream references data outside the root mini stream.");
                }

                int count = Math.Min(miniSectorSize, result.Length - written);
                miniStream.AsSpan(checked((int)sourceOffset), count)
                    .CopyTo(result.AsSpan(written, count));

                written += count;
                current = miniFat[current];

                if (current == EndOfChain)
                {
                    break;
                }
            }

            if (written != result.Length)
            {
                throw new InvalidDataException("OLE mini stream ended before its declared size.");
            }

            return result;
        }

        private static List<CompoundFileEntry> ParseDirectory(
            ReadOnlySpan<byte> bytes,
            int maximumEntries)
        {
            int count = Math.Min(bytes.Length / 128, maximumEntries);
            var entries = new List<CompoundFileEntry>(count);

            for (int index = 0; index < count; index++)
            {
                ReadOnlySpan<byte> entry = bytes.Slice(index * 128, 128);
                ushort nameBytes = BinaryPrimitives.ReadUInt16LittleEndian(entry.Slice(64, 2));
                CompoundFileEntryType type = (CompoundFileEntryType)entry[66];

                if (type == CompoundFileEntryType.Unknown)
                {
                    continue;
                }

                if (nameBytes < 2 || nameBytes > 64 || (nameBytes & 1) != 0)
                {
                    throw new InvalidDataException("OLE directory entry has an invalid name length.");
                }

                string name = Encoding.Unicode.GetString(entry[..(nameBytes - 2)]);
                uint startSector = ReadUInt32(entry, 116);
                long streamSize = BinaryPrimitives.ReadInt64LittleEndian(entry.Slice(120, 8));

                if (streamSize < 0)
                {
                    throw new InvalidDataException("OLE directory stream size is invalid.");
                }

                entries.Add(
                    new CompoundFileEntry(
                        index,
                        name,
                        type,
                        startSector,
                        streamSize));
            }

            if (bytes.Length / 128 > maximumEntries)
            {
                throw new InvalidDataException(
                    $"OLE directory exceeds the configured maximum of {maximumEntries} entries.");
            }

            return entries;
        }

        private ReadOnlySpan<byte> GetSector(uint sectorId)
        {
            long offset = checked((long)(sectorId + 1) * sectorSize);

            if (offset < 0 || offset + sectorSize > content.Length)
            {
                throw new InvalidDataException("OLE sector reference is outside the file.");
            }

            return content.Span.Slice(checked((int)offset), sectorSize);
        }

        private static uint ReadUInt32(ReadOnlySpan<byte> bytes, int offset) =>
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(offset, sizeof(uint)));
    }
}