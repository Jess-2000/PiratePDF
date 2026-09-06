using System.Buffers.Binary;
using System.Text;
using BlackFlag.Engine.Fonts.Embedding;
using BlackFlag.Engine.Fonts.Sanitization;

namespace BlackFlag.Engine.Fonts;

/// <summary>
/// Reads catalog-facing family/style/coverage metadata exclusively from a
/// sanitized SFNT program.
/// </summary>
public static class PdfSanitizedExternalFontMetadataInspector
{
    public static PdfSanitizedExternalFontMetadata Inspect(
        PdfSanitizedExternalFontProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);

        ReadOnlySpan<byte> bytes =
            program.Bytes.Span;

        Dictionary<string, PdfTrueTypeTable> tables =
            PdfTrueTypeBinary.ReadTables(bytes);

        PdfTrueTypeTable cmap =
            PdfTrueTypeBinary.Require(
                tables,
                "cmap");

        PdfTrueTypeTable name =
            PdfTrueTypeBinary.Require(
                tables,
                "name");

        Func<int, int> glyphMapper =
            PdfTrueTypeCmap.CreateGlyphMapper(
                PdfTrueTypeBinary.Slice(
                    bytes,
                    cmap));

        NameMetadata names =
            ReadNames(
                PdfTrueTypeBinary.Slice(
                    bytes,
                    name));

        StyleMetadata style =
            tables.TryGetValue(
                    "OS/2",
                    out PdfTrueTypeTable os2)
                ? ReadStyle(
                    PdfTrueTypeBinary.Slice(
                        bytes,
                        os2))
                : StyleMetadata.Default;

        return new PdfSanitizedExternalFontMetadata(
            names.FamilyName,
            names.FaceName,
            style.Weight,
            style.Stretch,
            style.Slant,
            style.EmbeddingRights,
            glyphMapper);
    }

    private static NameMetadata ReadNames(
        ReadOnlySpan<byte> table)
    {
        if (table.Length < 6)
        {
            throw new InvalidDataException(
                "The sanitized font name table is truncated.");
        }

        int count =
            BinaryPrimitives.ReadUInt16BigEndian(
                table.Slice(2, 2));

        int stringOffset =
            BinaryPrimitives.ReadUInt16BigEndian(
                table.Slice(4, 2));

        int recordsEnd =
            checked(
                6 +
                count * 12);

        if (recordsEnd > table.Length ||
            stringOffset < recordsEnd ||
            stringOffset > table.Length)
        {
            throw new InvalidDataException(
                "The sanitized font name table boundaries are invalid.");
        }

        NameCandidate? family = null;
        NameCandidate? subfamily = null;
        NameCandidate? fullName = null;

        for (int index = 0;
            index < count;
            index++)
        {
            int record =
                6 +
                index * 12;

            ushort platform =
                U16(
                    table,
                    record);

            ushort encoding =
                U16(
                    table,
                    record + 2);

            ushort language =
                U16(
                    table,
                    record + 4);

            ushort nameId =
                U16(
                    table,
                    record + 6);

            int length =
                U16(
                    table,
                    record + 8);

            int offset =
                U16(
                    table,
                    record + 10);

            int absolute =
                checked(
                    stringOffset +
                    offset);

            if (absolute > table.Length ||
                length > table.Length - absolute)
            {
                throw new InvalidDataException(
                    "A sanitized font name record lies outside its table.");
            }

            string? value =
                DecodeName(
                    platform,
                    encoding,
                    table.Slice(
                        absolute,
                        length));

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            int score =
                ScoreName(
                    platform,
                    language);

            var candidate =
                new NameCandidate(
                    value,
                    score);

            switch (nameId)
            {
                case 1:
                    family =
                        Select(
                            family,
                            candidate);
                    break;

                case 2:
                    subfamily =
                        Select(
                            subfamily,
                            candidate);
                    break;

                case 4:
                    fullName =
                        Select(
                            fullName,
                            candidate);
                    break;
            }
        }

        string familyName =
            family?.Value ??
            fullName?.Value ??
            throw new InvalidDataException(
                "The sanitized font has no usable family name.");

        string faceName =
            fullName?.Value ??
            JoinFaceName(
                familyName,
                subfamily?.Value);

        return new NameMetadata(
            familyName,
            faceName);
    }

    private static StyleMetadata ReadStyle(
        ReadOnlySpan<byte> os2)
    {
        if (os2.Length < 10)
        {
            return StyleMetadata.Default;
        }

        int weight =
            Math.Clamp(
                (int)U16(
                    os2,
                    4),
                1,
                1000);

        int stretch =
            Math.Clamp(
                (int)U16(
                    os2,
                    6),
                1,
                9);

        int fsType =
            U16(
                os2,
                8);

        PdfExternalFontEmbeddingRights embeddingRights =
            (fsType & 0x0002) != 0 ||
            (fsType & 0x0200) != 0
                ? PdfExternalFontEmbeddingRights.Restricted
                : (fsType & 0x0100) != 0
                    ? PdfExternalFontEmbeddingRights.Allowed
                    : (fsType & 0x0004) != 0
                        ? PdfExternalFontEmbeddingRights.Allowed
                        : (fsType & 0x0008) != 0
                            ? PdfExternalFontEmbeddingRights.SubsetOnly
                            : PdfExternalFontEmbeddingRights.Allowed;

        PdfExternalFontSlant slant =
            ReadSlant(
                os2);

        return new StyleMetadata(
            weight,
            stretch,
            slant,
            embeddingRights);
    }

    private static PdfExternalFontSlant ReadSlant(
        ReadOnlySpan<byte> os2)
    {
        if (os2.Length < 64)
        {
            return PdfExternalFontSlant.Upright;
        }

        int selection =
            U16(
                os2,
                62);

        return (selection & 0x0001) != 0
            ? PdfExternalFontSlant.Italic
            : (selection & 0x0200) != 0
                ? PdfExternalFontSlant.Oblique
                : PdfExternalFontSlant.Upright;
    }

    private static string? DecodeName(
        ushort platform,
        ushort encoding,
        ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return null;
        }

        string value =
            platform switch
            {
                0 =>
                    DecodeBigEndianUnicode(
                        bytes),

                3 when encoding is
                    0 or 1 or 10 =>
                    DecodeBigEndianUnicode(
                        bytes),

                1 =>
                    Encoding.Latin1.GetString(
                        bytes),

                _ =>
                    string.Empty
            };

        value =
            value.Replace(
                    "\0",
                    string.Empty,
                    StringComparison.Ordinal)
                .Trim();

        return value.Length == 0
            ? null
            : value;
    }

    private static string DecodeBigEndianUnicode(
        ReadOnlySpan<byte> bytes)
    {
        if ((bytes.Length & 1) != 0)
        {
            throw new InvalidDataException(
                "A sanitized Unicode font name has an odd byte length.");
        }

        return Encoding.BigEndianUnicode.GetString(
            bytes);
    }

    private static int ScoreName(
        ushort platform,
        ushort language) =>
        platform switch
        {
            3 when language == 0x0409 => 400,
            0 => 300,
            3 => 200,
            1 => 100,
            _ => 0
        };

    private static NameCandidate Select(
        NameCandidate? current,
        NameCandidate candidate) =>
        current is null ||
        candidate.Score >
            current.Value.Score
            ? candidate
            : current.Value;

    private static string JoinFaceName(
        string family,
        string? subfamily) =>
        string.IsNullOrWhiteSpace(subfamily) ||
        string.Equals(
            subfamily,
            "Regular",
            StringComparison.OrdinalIgnoreCase)
            ? family
            : $"{family} {subfamily}";

    private static ushort U16(
        ReadOnlySpan<byte> data,
        int offset)
    {
        if (offset < 0 ||
            offset > data.Length - 2)
        {
            throw new InvalidDataException(
                "A sanitized font metadata read exceeded its table.");
        }

        return BinaryPrimitives.ReadUInt16BigEndian(
            data.Slice(
                offset,
                2));
    }

    private readonly record struct NameCandidate(
        string Value,
        int Score);

    private readonly record struct NameMetadata(
        string FamilyName,
        string FaceName);

    private readonly record struct StyleMetadata(
        int Weight,
        int Stretch,
        PdfExternalFontSlant Slant,
        PdfExternalFontEmbeddingRights EmbeddingRights)
    {
        public static StyleMetadata Default { get; } =
            new(
                400,
                5,
                PdfExternalFontSlant.Upright,
                PdfExternalFontEmbeddingRights.Unknown);
    }
}
