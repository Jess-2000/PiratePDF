using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace BlackFlag.Engine.Fonts.Sanitization;

/// <summary>
/// Immutable output of the trusted external-font sanitization boundary.
/// </summary>
public sealed class PdfSanitizedExternalFontProgram
{
    private readonly byte[] bytes;
    private readonly ReadOnlyCollection<string> tableTags;

    internal PdfSanitizedExternalFontProgram(
        byte[] bytes,
        PdfExternalFontTechnology technology,
        IEnumerable<string> tableTags)
    {
        this.bytes = bytes.ToArray();
        Technology = technology;
        this.tableTags = Array.AsReadOnly(tableTags.ToArray());
        Sha256 = Convert.ToHexString(SHA256.HashData(this.bytes));
    }

    public ReadOnlyMemory<byte> Bytes => bytes;
    public PdfExternalFontTechnology Technology { get; }
    public IReadOnlyList<string> TableTags => tableTags;
    public string Sha256 { get; }

    public Stream OpenRead() =>
        new MemoryStream(bytes, writable: false);
}
