using System.Collections.Concurrent;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Parsing.Resources;

public sealed partial class PdfResourceResolver
{
    private PdfResolvedFont CreateFont(
        PdfResourceIdentity identity,
        PdfObject source)
    {
        PdfDictionary dictionary = RequireDictionary(source, PdfResourceKind.Font);
        string subtype = RequireName(dictionary, "Subtype", PdfResourceKind.Font);
        string baseFont = GetOptionalName(dictionary, "BaseFont") ?? subtype;
        string faceName = RemoveSubsetPrefix(baseFont, out bool isSubset);

        PdfDictionary? descriptor = ResolveOptionalDictionary(dictionary, "FontDescriptor");
        string familyName = GetOptionalString(descriptor, "FontFamily") ?? faceName;
        int weight = GetFontWeight(descriptor, faceName);
        int stretch = GetFontStretch(descriptor);
        PdfFontSlant slant = GetFontSlant(descriptor, faceName);
        PdfFontTechnology technology = GetFontTechnology(subtype, descriptor);
        PdfObject? encoding = GetOptionalRaw(dictionary, "Encoding");
        PdfStream? toUnicode = ResolveOptionalStream(dictionary, "ToUnicode");
        PdfArray? descendantFonts = ResolveOptionalArray(dictionary, "DescendantFonts");

        if (descendantFonts is not null && descendantFonts.Count > options.MaximumFontDescendantCount)
        {
            throw CreateException(
                PdfResourceResolutionFailure.LimitExceeded,
                PdfResourceKind.Font,
                resourceName: null,
                $"Font descendant count {descendantFonts.Count} exceeds the configured maximum of "
                + $"{options.MaximumFontDescendantCount}.");
        }

        PdfStream? embeddedFont = ResolveEmbeddedFontProgram(descriptor);
        if (embeddedFont is not null && embeddedFont.Data.Length > options.MaximumEmbeddedFontBytes)
        {
            throw CreateException(
                PdfResourceResolutionFailure.LimitExceeded,
                PdfResourceKind.Font,
                resourceName: null,
                $"Embedded font stream size {embeddedFont.Data.Length} exceeds the configured maximum of "
                + $"{options.MaximumEmbeddedFontBytes} bytes.");
        }

        return new PdfResolvedFont(
            identity,
            dictionary,
            subtype,
            familyName,
            faceName,
            weight,
            stretch,
            slant,
            technology,
            PdfFontEmbeddingRights.Unknown,
            isSubset,
            encoding,
            toUnicode,
            descriptor,
            descendantFonts,
            embeddedFont,
            Array.Empty<PdfFontVariationAxis>());
    }

    private static string RemoveSubsetPrefix(string value, out bool isSubset)
    {
        isSubset = value.Length > 7
            && value[6] == '+'
            && value.AsSpan(0, 6).ToString().All(character => character is >= 'A' and <= 'Z');

        return isSubset ? value[7..] : value;
    }

    private static int GetFontWeight(PdfDictionary? descriptor, string faceName)
    {
        double? explicitWeight = descriptor is null ? null : GetOptionalNumber(descriptor, "FontWeight");
        if (explicitWeight.HasValue)
        {
            return Math.Clamp((int)Math.Round(explicitWeight.Value, MidpointRounding.AwayFromZero), 1, 1000);
        }

        if (faceName.Contains("Black", StringComparison.OrdinalIgnoreCase))
        {
            return 900;
        }

        if (faceName.Contains("Bold", StringComparison.OrdinalIgnoreCase))
        {
            return 700;
        }

        if (faceName.Contains("Light", StringComparison.OrdinalIgnoreCase))
        {
            return 300;
        }

        return 400;
    }

    private static int GetFontStretch(PdfDictionary? descriptor)
    {
        string? stretch = GetOptionalName(descriptor, "FontStretch");
        return stretch switch
        {
            "UltraCondensed" => 1,
            "ExtraCondensed" => 2,
            "Condensed" => 3,
            "SemiCondensed" => 4,
            "Normal" or null => 5,
            "SemiExpanded" => 6,
            "Expanded" => 7,
            "ExtraExpanded" => 8,
            "UltraExpanded" => 9,
            _ => 5
        };
    }

    private static PdfFontSlant GetFontSlant(PdfDictionary? descriptor, string faceName)
    {
        double italicAngle = descriptor is null ? 0 : GetOptionalNumber(descriptor, "ItalicAngle") ?? 0;
        if (faceName.Contains("Oblique", StringComparison.OrdinalIgnoreCase))
        {
            return PdfFontSlant.Oblique;
        }

        return italicAngle != 0 || faceName.Contains("Italic", StringComparison.OrdinalIgnoreCase)
            ? PdfFontSlant.Italic
            : PdfFontSlant.Upright;
    }

    private static PdfFontTechnology GetFontTechnology(string subtype, PdfDictionary? descriptor)
    {
        if (descriptor is not null)
        {
            PdfObject? fontFile3 = GetOptionalRaw(descriptor, "FontFile3");
            if (fontFile3 is PdfStream fontFile3Stream)
            {
                string? embeddedSubtype = GetOptionalName(fontFile3Stream.Dictionary, "Subtype");
                if (string.Equals(embeddedSubtype, "OpenType", StringComparison.Ordinal))
                {
                    return PdfFontTechnology.OpenType;
                }

                if (embeddedSubtype is "Type1C" or "CIDFontType0C")
                {
                    return PdfFontTechnology.Cff;
                }
            }
        }

        return subtype switch
        {
            "Type1" or "MMType1" => PdfFontTechnology.Type1,
            "TrueType" or "CIDFontType2" => PdfFontTechnology.TrueType,
            "Type0" => PdfFontTechnology.Composite,
            "Type3" => PdfFontTechnology.Type3,
            "CIDFontType0" => PdfFontTechnology.Cff,
            _ => PdfFontTechnology.Unknown
        };
    }

}
