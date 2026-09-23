using System.Text;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Fonts;
using BlackFlag.Engine.Fonts.Embedding;
using BlackFlag.Engine.Fonts.Sanitization;
using BlackFlag.Engine.Services.Resources;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Fonts;

/// <summary>
/// Builds a Type0/Identity-H + CIDFontType2 Unicode TrueType resource from a
/// sanitizer-backed glyph subset.
/// </summary>
public static class PdfExternalType0TrueTypeResourceProjector
{
    private static readonly PdfName TypeName = PdfName.FromLatin1("Type");
    private static readonly PdfName FontName = PdfName.FromLatin1("Font");
    private static readonly PdfName SubtypeName = PdfName.FromLatin1("Subtype");
    private static readonly PdfName Type0Name = PdfName.FromLatin1("Type0");
    private static readonly PdfName CidFontType2Name = PdfName.FromLatin1("CIDFontType2");
    private static readonly PdfName BaseFontName = PdfName.FromLatin1("BaseFont");
    private static readonly PdfName EncodingName = PdfName.FromLatin1("Encoding");
    private static readonly PdfName IdentityHName = PdfName.FromLatin1("Identity-H");
    private static readonly PdfName DescendantFontsName = PdfName.FromLatin1("DescendantFonts");
    private static readonly PdfName ToUnicodeName = PdfName.FromLatin1("ToUnicode");
    private static readonly PdfName CidSystemInfoName = PdfName.FromLatin1("CIDSystemInfo");
    private static readonly PdfName RegistryName = PdfName.FromLatin1("Registry");
    private static readonly PdfName OrderingName = PdfName.FromLatin1("Ordering");
    private static readonly PdfName SupplementName = PdfName.FromLatin1("Supplement");
    private static readonly PdfName FontDescriptorName = PdfName.FromLatin1("FontDescriptor");
    private static readonly PdfName CidToGidMapName = PdfName.FromLatin1("CIDToGIDMap");
    private static readonly PdfName WName = PdfName.FromLatin1("W");
    private static readonly PdfName DwName = PdfName.FromLatin1("DW");
    private static readonly PdfName FontFile2Name = PdfName.FromLatin1("FontFile2");
    private static readonly PdfName Length1Name = PdfName.FromLatin1("Length1");
    private static readonly PdfName FontBBoxName = PdfName.FromLatin1("FontBBox");
    private static readonly PdfName FlagsName = PdfName.FromLatin1("Flags");
    private static readonly PdfName ItalicAngleName = PdfName.FromLatin1("ItalicAngle");
    private static readonly PdfName AscentName = PdfName.FromLatin1("Ascent");
    private static readonly PdfName DescentName = PdfName.FromLatin1("Descent");
    private static readonly PdfName CapHeightName = PdfName.FromLatin1("CapHeight");
    private static readonly PdfName StemVName = PdfName.FromLatin1("StemV");
    private static readonly PdfName FontCategoryName = PdfName.FromLatin1("Font");

    public static PdfExternalType0TrueTypeResourceProjection Create(
        PdfWritePlan writePlan,
        PdfObjectIdentifier pageIdentifier,
        string resourceName,
        PdfExternalFontFace face,
        PdfSanitizedExternalFontProgram program,
        string requiredText,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writePlan);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentNullException.ThrowIfNull(face);
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(requiredText);

        cancellationToken.ThrowIfCancellationRequested();
        PdfExternalFontEmbeddingPolicy.EnsureCanEmbed(face, program, subset: true);

        PdfUnicodeTrueTypeSubsetResult subset =
            new PdfUnicodeTrueTypeGlyphSubsetter()
                .CreateSubset(
                    program,
                    requiredText,
                    cancellationToken:
                        cancellationToken);

        PdfExternalTrueTypeMetrics metrics =
            PdfExternalTrueTypeInspector.Inspect(
                subset.Program);

        PdfWriteObjectAllocator allocator =
            writePlan.CreateObjectAllocator();

        PdfObjectIdentifier programIdentifier = allocator.ReserveNext();
        PdfObjectIdentifier cidToGidIdentifier = allocator.ReserveNext();
        PdfObjectIdentifier toUnicodeIdentifier = allocator.ReserveNext();
        PdfObjectIdentifier descriptorIdentifier = allocator.ReserveNext();
        PdfObjectIdentifier descendantIdentifier = allocator.ReserveNext();
        PdfObjectIdentifier type0Identifier = allocator.ReserveNext();

        string baseFont =
            CreateSubsetBaseFontName(
                face.FaceName,
                subset.Program.Sha256);

        ReadOnlyMemory<byte> fontBytes = subset.Program.Bytes;
        PdfStream programStream =
            new(
                new PdfDictionary
                {
                    {
                        Length1Name,
                        new PdfInteger(
                            fontBytes.Length)
                    }
                },
                fontBytes.Span);

        PdfStream cidToGid =
            new(
                new PdfDictionary(),
                BuildCidToGidMap(
                    subset.GlyphByCid));

        PdfStream toUnicode =
            new(
                [],
                PdfCidToUnicodeCMapBuilder.Build(
                    subset.UnicodeByCid));

        PdfDictionary descriptor =
            CreateDescriptor(
                baseFont,
                face,
                metrics,
                programIdentifier);

        PdfDictionary descendant =
            CreateDescendantFont(
                baseFont,
                descriptorIdentifier,
                cidToGidIdentifier,
                subset.WidthByCid);

        PdfDictionary type0 =
            new()
            {
                { TypeName, FontName },
                { SubtypeName, Type0Name },
                { BaseFontName, PdfName.FromLatin1(baseFont) },
                { EncodingName, IdentityHName },
                {
                    DescendantFontsName,
                    new PdfArray(
                    [
                        new PdfIndirectReference(
                            descendantIdentifier)
                    ])
                },
                {
                    ToUnicodeName,
                    new PdfIndirectReference(
                        toUnicodeIdentifier)
                }
            };

        PdfPageResourceMutationProjection resourceMutation =
            PdfPageResourceMutationProjector.Create(
                writePlan,
                pageIdentifier,
                FontCategoryName,
                resourceName,
                new PdfIndirectReference(
                    type0Identifier));

        return new PdfExternalType0TrueTypeResourceProjection(
            resourceMutation.ResourceName,
            type0Identifier,
            descendantIdentifier,
            descriptorIdentifier,
            programIdentifier,
            cidToGidIdentifier,
            toUnicodeIdentifier,
            pageIdentifier,
            subset,
            new PdfObjectWriteProjectionSet(
            [
                resourceMutation.PageProjection,
                new PdfObjectWriteProjection(
                    programIdentifier,
                    programStream),
                new PdfObjectWriteProjection(
                    cidToGidIdentifier,
                    cidToGid),
                new PdfObjectWriteProjection(
                    toUnicodeIdentifier,
                    toUnicode),
                new PdfObjectWriteProjection(
                    descriptorIdentifier,
                    descriptor),
                new PdfObjectWriteProjection(
                    descendantIdentifier,
                    descendant),
                new PdfObjectWriteProjection(
                    type0Identifier,
                    type0)
            ]));
    }

    private static PdfDictionary CreateDescendantFont(
        string baseFont,
        PdfObjectIdentifier descriptorIdentifier,
        PdfObjectIdentifier cidToGidIdentifier,
        IReadOnlyDictionary<ushort, int> widthByCid)
    {
        var widths =
            new PdfArray();

        foreach (KeyValuePair<ushort, int> pair
                in widthByCid.OrderBy(
                    static pair =>
                        pair.Key))
        {
            widths.Add(
                new PdfInteger(
                    pair.Key));

            widths.Add(
                new PdfArray(
                [
                    new PdfInteger(
                        pair.Value)
                ]));
        }

        return new PdfDictionary
        {
            { TypeName, FontName },
            { SubtypeName, CidFontType2Name },
            { BaseFontName, PdfName.FromLatin1(baseFont) },
            {
                CidSystemInfoName,
                new PdfDictionary
                {
                    { RegistryName, PdfString.FromLatin1("Adobe") },
                    { OrderingName, PdfString.FromLatin1("Identity") },
                    { SupplementName, new PdfInteger(0) }
                }
            },
            {
                FontDescriptorName,
                new PdfIndirectReference(
                    descriptorIdentifier)
            },
            {
                CidToGidMapName,
                new PdfIndirectReference(
                    cidToGidIdentifier)
            },
            { DwName, new PdfInteger(1000) },
            { WName, widths }
        };
    }

    private static PdfDictionary CreateDescriptor(
        string baseFont,
        PdfExternalFontFace face,
        PdfExternalTrueTypeMetrics metrics,
        PdfObjectIdentifier programIdentifier)
    {
        int flags =
            32 |
            (face.Slant ==
                PdfExternalFontSlant.Upright
                ? 0
                : 64);

        double italicAngle =
            face.Slant ==
                PdfExternalFontSlant.Upright
                ? 0
                : -12;

        int stemV =
            face.Weight >= 700
                ? 120
                : 80;

        return new PdfDictionary
        {
            { TypeName, PdfName.FromLatin1("FontDescriptor") },
            { PdfName.FromLatin1("FontName"), PdfName.FromLatin1(baseFont) },
            { FlagsName, new PdfInteger(flags) },
            {
                FontBBoxName,
                new PdfArray(
                [
                    new PdfInteger(metrics.XMin),
                    new PdfInteger(metrics.YMin),
                    new PdfInteger(metrics.XMax),
                    new PdfInteger(metrics.YMax)
                ])
            },
            { ItalicAngleName, new PdfReal(italicAngle) },
            { AscentName, new PdfInteger(metrics.Ascent) },
            { DescentName, new PdfInteger(metrics.Descent) },
            { CapHeightName, new PdfInteger(metrics.Ascent) },
            { StemVName, new PdfInteger(stemV) },
            {
                FontFile2Name,
                new PdfIndirectReference(
                    programIdentifier)
            }
        };
    }

    private static byte[] BuildCidToGidMap(
        IReadOnlyDictionary<ushort, int> glyphByCid)
    {
        ushort maximumCid =
            glyphByCid.Count == 0
                ? (ushort)0
                : glyphByCid.Keys.Max();

        byte[] result =
            new byte[
                checked(
                    (maximumCid + 1) *
                    2)];

        foreach (KeyValuePair<ushort, int> pair
                in glyphByCid)
        {
            if (pair.Value is < 0 or > ushort.MaxValue)
            {
                throw new NotSupportedException(
                    $"Glyph ID {pair.Value} cannot be represented in a CIDToGIDMap stream.");
            }

            int offset =
                pair.Key * 2;

            result[offset] =
                checked(
                    (byte)(
                        pair.Value >>
                        8));

            result[offset + 1] =
                checked(
                    (byte)(
                        pair.Value &
                        0xFF));
        }

        return result;
    }

    private static string CreateSubsetBaseFontName(
        string faceName,
        string sha256)
    {
        string normalized =
            NormalizeBaseFontName(
                faceName);

        string prefix =
            string.Concat(
                sha256
                    .Take(6)
                    .Select(
                        static character =>
                            (char)(
                                'A' +
                                ((character is >= '0' and <= '9'
                                    ? character - '0'
                                    : character - 'A' + 10) %
                                26))));

        return $"{prefix}+{normalized}";
    }

    private static string NormalizeBaseFontName(
        string value)
    {
        var builder =
            new StringBuilder();

        foreach (char character in value)
        {
            if (character is >= 'A' and <= 'Z' or
                >= 'a' and <= 'z' or
                >= '0' and <= '9' or '-' or '_')
            {
                _ = builder.Append(
                    character);
            }
            else if (char.IsWhiteSpace(
                        character))
            {
                _ = builder.Append('-');
            }
        }

        if (builder.Length == 0)
        {
            throw new InvalidOperationException(
                "The selected external font face has no PDF-safe base font name.");
        }

        return builder.ToString();
    }
}
