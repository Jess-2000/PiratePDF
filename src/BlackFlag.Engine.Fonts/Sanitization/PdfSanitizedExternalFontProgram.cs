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
        EmbeddingRestrictions = PdfExternalFontProgramRights.Read(this.bytes);
    }

    /// <summary>Returns an independent snapshot of the trusted font bytes.</summary>
    public ReadOnlyMemory<byte> Bytes => bytes.ToArray();

    // Assembly-owned readers borrow this span only while inspecting the program.
    // Never forward it to public callbacks or return a recoverable memory/array alias.
    internal ReadOnlySpan<byte> TrustedBytes => bytes;
    internal PdfExternalFontProgramRights EmbeddingRestrictions { get; }
    public PdfExternalFontTechnology Technology { get; }
    public IReadOnlyList<string> TableTags => tableTags;
    public string Sha256 { get; }

    public Stream OpenRead() =>
        // MemoryStream copy operations can hand their buffer to a caller-owned destination.
        new MemoryStream(bytes.ToArray(), writable: false);
}
