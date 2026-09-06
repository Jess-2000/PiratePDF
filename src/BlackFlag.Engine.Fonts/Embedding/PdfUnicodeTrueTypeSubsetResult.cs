using System.Collections.ObjectModel;
using System.Text;
using BlackFlag.Engine.Fonts.Sanitization;

namespace BlackFlag.Engine.Fonts.Embedding;

/// <summary>
/// Contains a sanitized Unicode TrueType subset plus deterministic PDF
/// CID-to-Unicode and CID-to-GID mappings.
/// </summary>
public sealed class PdfUnicodeTrueTypeSubsetResult
{
    private readonly ReadOnlyDictionary<int, ushort> cidByUnicode;
    private readonly ReadOnlyDictionary<ushort, int> unicodeByCid;
    private readonly ReadOnlyDictionary<ushort, int> glyphByCid;
    private readonly ReadOnlyDictionary<ushort, int> widthByCid;

    internal PdfUnicodeTrueTypeSubsetResult(
        PdfSanitizedExternalFontProgram program,
        IReadOnlyDictionary<int, ushort> cidByUnicode,
        IReadOnlyDictionary<ushort, int> unicodeByCid,
        IReadOnlyDictionary<ushort, int> glyphByCid,
        IReadOnlyDictionary<ushort, int> widthByCid)
    {
        Program = program;
        this.cidByUnicode =
            new ReadOnlyDictionary<int, ushort>(
                new Dictionary<int, ushort>(cidByUnicode));
        this.unicodeByCid =
            new ReadOnlyDictionary<ushort, int>(
                new Dictionary<ushort, int>(unicodeByCid));
        this.glyphByCid =
            new ReadOnlyDictionary<ushort, int>(
                new Dictionary<ushort, int>(glyphByCid));
        this.widthByCid =
            new ReadOnlyDictionary<ushort, int>(
                new Dictionary<ushort, int>(widthByCid));
    }

    public PdfSanitizedExternalFontProgram Program { get; }
    public IReadOnlyDictionary<int, ushort> CidByUnicode => cidByUnicode;
    public IReadOnlyDictionary<ushort, int> UnicodeByCid => unicodeByCid;
    public IReadOnlyDictionary<ushort, int> GlyphByCid => glyphByCid;
    public IReadOnlyDictionary<ushort, int> WidthByCid => widthByCid;

    /// <summary>Encodes Unicode text as two-byte big-endian Identity-H CIDs.</summary>
    public byte[] EncodeText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var result =
            new List<byte>(
                checked(text.Length * 2));

        foreach (Rune rune in text.EnumerateRunes())
        {
            if (!cidByUnicode.TryGetValue(
                    rune.Value,
                    out ushort cid))
            {
                throw new NotSupportedException(
                    $"Unicode scalar U+{rune.Value:X} is not present in this font subset.");
            }

            result.Add(
                checked((byte)(cid >> 8)));
            result.Add(
                checked((byte)(cid & 0xFF)));
        }

        return [.. result];
    }
}
