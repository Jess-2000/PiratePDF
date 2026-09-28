using System.Buffers.Binary;
using BlackFlag.Engine.Fonts.Embedding;

namespace BlackFlag.Engine.Fonts;

// Created once from sanitizer-owned bytes. Keep the raw declaration separate
// from provider policy and from the operation that will consume the program.
internal readonly record struct PdfExternalFontProgramRights(
    ushort? Version,
    ushort RawFlags,
    PdfExternalFontEmbeddingRights Level,
    bool NoSubsetting,
    bool BitmapEmbeddingOnly)
{
    internal static PdfExternalFontProgramRights Read(ReadOnlySpan<byte> bytes)
    {
        var tables = PdfTrueTypeBinary.ReadTables(bytes);
        if (!tables.TryGetValue("OS/2", out PdfTrueTypeTable table))
            return new(null, 0, PdfExternalFontEmbeddingRights.Unknown, false, false);

        ReadOnlySpan<byte> os2 = PdfTrueTypeBinary.Slice(bytes, table);
        ushort version = BinaryPrimitives.ReadUInt16BigEndian(os2);
        ushort flags = BinaryPrimitives.ReadUInt16BigEndian(os2.Slice(8, 2));
        int level = flags & 0x000E;
        // OpenType OS/2 fsType: v0/v1 ignore high bits; v0-v2 permit
        // least-restrictive precedence. v3+ requires exclusive base levels.
        bool conflict = version >= 3 && (level & (level - 1)) != 0;
        PdfExternalFontEmbeddingRights rights = version > 5 || conflict
            ? PdfExternalFontEmbeddingRights.Unknown
            : (level & 8) != 0 ? PdfExternalFontEmbeddingRights.Editable
            : (level & 4) != 0 ? PdfExternalFontEmbeddingRights.PreviewAndPrint
            : (level & 2) != 0 ? PdfExternalFontEmbeddingRights.Restricted
            : PdfExternalFontEmbeddingRights.Allowed;
        return new(version, flags, rights,
            version >= 2 && (flags & 0x0100) != 0,
            version >= 2 && (flags & 0x0200) != 0);
    }

    internal void EnsureCanEmbed(bool subset)
    {
        if (Level is not (PdfExternalFontEmbeddingRights.Allowed or PdfExternalFontEmbeddingRights.Editable))
            throw new NotSupportedException(Level == PdfExternalFontEmbeddingRights.PreviewAndPrint
                ? "The font program permits preview/print embedding, not the supported editable-document operation."
                : "The font program does not establish permission for editable-document embedding.");
        if (BitmapEmbeddingOnly)
            throw new NotSupportedException("The font program permits bitmap embedding only, not outline embedding.");
        if (subset && NoSubsetting)
            throw new NotSupportedException("The font program prohibits subsetting.");
    }
}
