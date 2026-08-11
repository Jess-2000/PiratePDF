
namespace BlackFlag.Engine.Rendering.Images;

/// <summary>
/// Selects a supported best-fit PDF representation without performing destructive conversion silently.
/// </summary>
public static class PdfImageCompatibilityConverter
{
    /// <summary>Chooses the preferred target for a recognized and decodable source.</summary>
    public static PdfImageCompatibilityDecision Choose(
        PdfImageSourceFormat sourceFormat,
        PdfImageSourceTraits traits,
        PdfImageConversionPolicy policy = PdfImageConversionPolicy.PreferLossless,
        bool encodedRepresentationCanBePreserved = false)
    {
        ArgumentNullException.ThrowIfNull(traits);

        if (encodedRepresentationCanBePreserved)
        {
            return new(
                sourceFormat,
                PdfImageCompatibilityTarget.PreserveEncoded,
                RequiresDecode: false,
                IsLossless: true,
                RequiresUserConfirmation: false,
                "The original encoded image can be preserved without transcoding.");
        }

        if (traits.IsVector)
        {
            return new(
                sourceFormat,
                PdfImageCompatibilityTarget.VectorImport,
                RequiresDecode: true,
                IsLossless: true,
                RequiresUserConfirmation: false,
                "The source should remain vector content rather than being rasterized.");
        }

        if (traits.IsBilevel)
        {
            return new(
                sourceFormat,
                PdfImageCompatibilityTarget.CcittBilevel,
                RequiresDecode: true,
                IsLossless: true,
                RequiresUserConfirmation: false,
                "The source is bilevel and is best represented using a lossless bilevel PDF encoding.");
        }

        if (traits.IsIndexed && !traits.HasAlpha)
        {
            return new(
                sourceFormat,
                PdfImageCompatibilityTarget.IndexedFlate,
                RequiresDecode: true,
                IsLossless: true,
                RequiresUserConfirmation: false,
                "The source palette can be preserved using an Indexed PDF image with lossless compression.");
        }

        if (traits.HasAlpha)
        {
            return new(
                sourceFormat,
                PdfImageCompatibilityTarget.FlateRgbWithSoftMask,
                RequiresDecode: true,
                IsLossless: true,
                RequiresUserConfirmation: false,
                "Transparency requires lossless color samples plus a PDF soft mask.");
        }

        if (traits.IsGray)
        {
            return new(
                sourceFormat,
                PdfImageCompatibilityTarget.FlateGray,
                RequiresDecode: true,
                IsLossless: true,
                RequiresUserConfirmation: false,
                "Grayscale samples can be preserved losslessly.");
        }

        bool jpegIsNatural =
            traits.IsPhotographic &&
            traits.BitsPerComponent <= 8 &&
            !traits.HasAlpha;

        if (jpegIsNatural &&
            (traits.SourceIsLossy ||
             policy is PdfImageConversionPolicy.Balanced or PdfImageConversionPolicy.PreferCompact))
        {
            return new(
                sourceFormat,
                PdfImageCompatibilityTarget.Jpeg,
                RequiresDecode: true,
                IsLossless: traits.SourceIsLossy,
                RequiresUserConfirmation:
                    !traits.SourceIsLossy &&
                    policy != PdfImageConversionPolicy.PreferCompact,
                traits.SourceIsLossy
                    ? "The photographic source is already lossy; JPEG is the closest supported compact representation."
                    : "JPEG would reduce size but introduces loss and should require confirmation.");
        }

        if (policy == PdfImageConversionPolicy.PreserveExactly &&
            traits.BitsPerComponent > 16)
        {
            return new(
                sourceFormat,
                PdfImageCompatibilityTarget.Unsupported,
                RequiresDecode: true,
                IsLossless: false,
                RequiresUserConfirmation: true,
                "The source precision cannot be preserved exactly by the currently supported PDF image representations.");
        }

        return new(
            sourceFormat,
            PdfImageCompatibilityTarget.FlateRgb,
            RequiresDecode: true,
            IsLossless: true,
            RequiresUserConfirmation: false,
            "Lossless RGB samples are the best supported fallback.");
    }
}
