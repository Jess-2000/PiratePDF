using System.Collections.ObjectModel;

namespace BlackFlag.Engine.Rendering.Fonts;

/// <summary>Stores renderer-neutral mappings parsed from a PDF CMap.</summary>
public sealed class PdfCMap
{
    private readonly ReadOnlyCollection<PdfCodeSpaceRange> codeSpaces;
    private readonly int[] codeSpaceLengths;
    private readonly Dictionary<PdfCharacterCode, int> cidMappings;
    private readonly Dictionary<PdfCharacterCode, string> unicodeMappings;

    internal PdfCMap(
        IEnumerable<PdfCodeSpaceRange> codeSpaces,
        IDictionary<PdfCharacterCode, int> cidMappings,
        IDictionary<PdfCharacterCode, string> unicodeMappings,
        int writingMode)
    {
        this.codeSpaces = Array.AsReadOnly(codeSpaces.ToArray());
        codeSpaceLengths = this.codeSpaces.Count == 0
            ? [1]
            : [.. this.codeSpaces
                .Select(static range => range.Length)
                .Distinct()
                .OrderDescending()];
        this.cidMappings = new Dictionary<PdfCharacterCode, int>(cidMappings);
        this.unicodeMappings = new Dictionary<PdfCharacterCode, string>(unicodeMappings);
        WritingMode = writingMode;
    }

    /// <summary>Gets the CMap writing mode, where zero is horizontal and one is vertical.</summary>
    public int WritingMode { get; }

    /// <summary>Gets declared codespace ranges.</summary>
    public IReadOnlyList<PdfCodeSpaceRange> CodeSpaces => codeSpaces;

    /// <summary>Attempts to resolve a source code to a CID.</summary>
    public bool TryGetCid(PdfCharacterCode code, out int cid) =>
        cidMappings.TryGetValue(code, out cid);

    /// <summary>Attempts to resolve a source code to Unicode text.</summary>
    public bool TryGetUnicode(PdfCharacterCode code, out string? unicodeText)
    {
        bool found = unicodeMappings.TryGetValue(code, out string? value);
        unicodeText = value;
        return found;
    }

    /// <summary>Returns the longest valid code starting at an offset.</summary>
    public PdfCharacterCode ReadCode(ReadOnlySpan<byte> bytes, int offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(offset, bytes.Length);

        foreach (int length in codeSpaceLengths)
        {
            if (offset + length > bytes.Length)
            {
                continue;
            }

            ReadOnlySpan<byte> candidate = bytes.Slice(offset, length);
            bool accepted = codeSpaces.Count == 0;
            if (!accepted)
            {
                foreach (PdfCodeSpaceRange range in codeSpaces)
                {
                    if (range.Contains(candidate))
                    {
                        accepted = true;
                        break;
                    }
                }
            }

            if (accepted)
            {
                return new PdfCharacterCode(candidate);
            }
        }

        throw new InvalidDataException(
            $"No CMap codespace accepts the character code beginning at byte offset {offset}.");
    }
}
