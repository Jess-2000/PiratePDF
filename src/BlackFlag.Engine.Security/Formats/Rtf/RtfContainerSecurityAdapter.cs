using System.Text;
using BlackFlag.Engine.Security.Containers;
using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Formats.Rtf;

/// <summary>Passively inspects RTF control words, fields, and embedded object data.</summary>
public sealed class RtfContainerSecurityAdapter : IDocumentContainerSecurityAdapter
{
    private const string SourceFormat = "rtf";
    private const string ObjectDataControlWord = "\\objdata";

    private static readonly byte[][] KnownPayloadSignatures =
    [
        [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1],
        [(byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-'],
        [0x50, 0x4B, 0x03, 0x04],
        [0x4D, 0x5A]
    ];

    private readonly RtfSecurityAnalysisOptions options;

    public RtfContainerSecurityAdapter(RtfSecurityAnalysisOptions? options = null)
    {
        this.options = options ?? RtfSecurityAnalysisOptions.Default;
        this.options.Validate();
    }

    public bool CanInspect(DocumentEmbeddedObjectClassification classification)
    {
        ArgumentNullException.ThrowIfNull(classification);
        return classification.Kind == DocumentEmbeddedObjectKind.Rtf;
    }

    public DocumentContainerSecurityInspection Inspect(
        DocumentEmbeddedObject embeddedObject,
        DocumentSecurityInspectionBudget budget,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embeddedObject);
        ArgumentNullException.ThrowIfNull(budget);

        if (embeddedObject.Content.Length > options.MaximumDocumentBytes)
        {
            throw new InvalidDataException(
                $"RTF document exceeds the configured maximum of {options.MaximumDocumentBytes} bytes.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        string text = Encoding.Latin1.GetString(embeddedObject.Content.Span);
        var observations = new List<DocumentSecurityObservation>(capacity: 4);
        var children = new List<DocumentContainerEntry>();
        var keys = new HashSet<string>(StringComparer.Ordinal);

        InspectControlWords(text, embeddedObject.LogicalPath, observations, keys);
        ExtractObjectData(
            text,
            embeddedObject.LogicalPath,
            children,
            budget,
            cancellationToken);

        return new DocumentContainerSecurityInspection(observations, children);
    }

    private static void InspectControlWords(
        string text,
        string logicalPath,
        List<DocumentSecurityObservation> observations,
        HashSet<string> keys)
    {
        if (Contains(text, ObjectDataControlWord))
        {
            AddObservation(
                observations,
                keys,
                DocumentSecurityObservationCategory.EmbeddedContent,
                "EmbeddedObjectData",
                "The RTF document contains embedded object data.",
                logicalPath);
        }

        if (Contains(text, "\\objautlink") || Contains(text, "\\objupdate"))
        {
            AddObservation(
                observations,
                keys,
                DocumentSecurityObservationCategory.AutomaticAction,
                "AutomaticOleObject",
                "The RTF document contains an automatically linked or updated OLE object.",
                logicalPath);
        }

        if (Contains(text, "DDEAUTO") || Contains(text, " DDE "))
        {
            AddObservation(
                observations,
                keys,
                DocumentSecurityObservationCategory.ActiveContent,
                "DdeField",
                "The RTF document contains a DDE or DDEAUTO field instruction.",
                logicalPath);
        }

        if (Contains(text, "INCLUDEPICTURE") ||
            Contains(text, "INCLUDETEXT") ||
            Contains(text, "HYPERLINK"))
        {
            AddObservation(
                observations,
                keys,
                DocumentSecurityObservationCategory.ExternalReference,
                "ExternalFieldReference",
                "The RTF document contains a field capable of referencing external content.",
                logicalPath);
        }
    }

    private void ExtractObjectData(
        string text,
        string parentPath,
        List<DocumentContainerEntry> children,
        DocumentSecurityInspectionBudget budget,
        CancellationToken cancellationToken)
    {
        int searchFrom = 0;
        int objectIndex = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int marker = text.IndexOf(
                ObjectDataControlWord,
                searchFrom,
                StringComparison.OrdinalIgnoreCase);

            if (marker < 0)
            {
                return;
            }

            budget.ConsumeWork();
            int dataStart = marker + ObjectDataControlWord.Length;
            int groupEnd = FindGroupEnd(text, marker);

            if (groupEnd < 0)
            {
                throw new InvalidDataException("RTF embedded object group is unterminated.");
            }

            byte[] decoded = DecodeHexObjectData(text, dataStart, groupEnd);
            if (decoded.Length > options.MaximumEmbeddedObjectBytes)
            {
                throw new InvalidDataException(
                    $"RTF embedded object exceeds the configured maximum of {options.MaximumEmbeddedObjectBytes} bytes.");
            }

            ReadOnlyMemory<byte> payload = LocateKnownPayload(decoded);

            if (!payload.IsEmpty)
            {
                objectIndex++;
                children.Add(
                    new DocumentContainerEntry(
                        new DocumentEmbeddedObject(
                            payload,
                            $"{parentPath.TrimEnd('/')}/rtf-object-{objectIndex}.bin",
                            declaredFileName: $"rtf-object-{objectIndex}.bin"),
                        payload.Length));
            }

            searchFrom = groupEnd + 1;
        }
    }

    private static int FindGroupEnd(string text, int marker)
    {
        int start = text.LastIndexOf('{', marker);
        if (start < 0)
        {
            return -1;
        }

        int depth = 0;

        for (int index = start; index < text.Length; index++)
        {
            char value = text[index];

            if ((value == '{' || value == '}') &&
                (index == 0 || text[index - 1] != '\\'))
            {
                depth += value == '{' ? 1 : -1;
                if (depth == 0)
                {
                    return index;
                }
            }
        }

        return -1;
    }

    private static byte[] DecodeHexObjectData(string text, int start, int end)
    {
        int maximumDecodedLength = Math.Max(0, (end - start + 1) / 2);
        byte[] bytes = new byte[maximumDecodedLength];
        int byteCount = 0;
        int highNibble = -1;

        for (int index = start; index < end; index++)
        {
            char value = text[index];

            if (value == '\\')
            {
                while (index < end &&
                    text[index] != ' ' &&
                    text[index] != '\r' &&
                    text[index] != '\n' &&
                    text[index] != '}')
                {
                    index++;
                }

                highNibble = -1;
                continue;
            }

            int nibble = HexValue(value);
            if (nibble < 0)
            {
                continue;
            }

            if (highNibble < 0)
            {
                highNibble = nibble;
                continue;
            }

            bytes[byteCount++] =
                (byte)((highNibble << 4) | nibble);

            highNibble = -1;
        }

        if (byteCount == bytes.Length)
        {
            return bytes;
        }

        Array.Resize(ref bytes, byteCount);
        return bytes;
    }

    private static int HexValue(char value) =>
        value switch
        {
            >= '0' and <= '9' => value - '0',
            >= 'a' and <= 'f' => value - 'a' + 10,
            >= 'A' and <= 'F' => value - 'A' + 10,
            _ => -1
        };

    private static ReadOnlyMemory<byte> LocateKnownPayload(byte[] decoded)
    {
        ReadOnlySpan<byte> bytes = decoded;

        for (int signatureIndex = 0;
            signatureIndex < KnownPayloadSignatures.Length;
            signatureIndex++)
        {
            int payloadIndex =
                bytes.IndexOf(KnownPayloadSignatures[signatureIndex]);

            if (payloadIndex >= 0)
            {
                return decoded.AsMemory(payloadIndex);
            }
        }

        return decoded;
    }

    private static bool Contains(string text, string value) =>
        text.Contains(value, StringComparison.OrdinalIgnoreCase);

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
}
