
using BlackFlag.Engine.Rendering.Images;

namespace BlackFlag.Engine.Rendering.Tests.Images;

public sealed class PdfImageCompatibilityTests
{
    [Fact]
    public void RecognizesJpegXlContainerBySignature()
    {
        byte[] bytes =
        [
            0x00, 0x00, 0x00, 0x0C,
            0x4A, 0x58, 0x4C, 0x20,
            0x0D, 0x0A, 0x87, 0x0A
        ];

        Assert.Equal(
            PdfImageSourceFormat.JpegXl,
            PdfImageFormatRecognizer.Recognize(bytes));
    }

    [Fact]
    public void TransparentRasterChoosesLosslessSoftMaskRepresentation()
    {
        PdfImageCompatibilityDecision decision =
            PdfImageCompatibilityConverter.Choose(
                PdfImageSourceFormat.Png,
                new PdfImageSourceTraits
                {
                    HasAlpha = true,
                    BitsPerComponent = 8
                });

        Assert.Equal(
            PdfImageCompatibilityTarget.FlateRgbWithSoftMask,
            decision.Target);
        Assert.True(decision.IsLossless);
        Assert.False(decision.RequiresUserConfirmation);
    }

    [Fact]
    public void VectorSourceIsNotSilentlyRasterized()
    {
        PdfImageCompatibilityDecision decision =
            PdfImageCompatibilityConverter.Choose(
                PdfImageSourceFormat.Svg,
                new PdfImageSourceTraits
                {
                    IsVector = true
                });

        Assert.Equal(
            PdfImageCompatibilityTarget.VectorImport,
            decision.Target);
        Assert.False(decision.RequiresUserConfirmation);
    }

    [Fact]
    public void LossyPhotoCanChooseJpegWithoutAnotherLossClassification()
    {
        PdfImageCompatibilityDecision decision =
            PdfImageCompatibilityConverter.Choose(
                PdfImageSourceFormat.WebP,
                new PdfImageSourceTraits
                {
                    IsPhotographic = true,
                    SourceIsLossy = true,
                    BitsPerComponent = 8
                });

        Assert.Equal(
            PdfImageCompatibilityTarget.Jpeg,
            decision.Target);
        Assert.False(decision.RequiresUserConfirmation);
    }
}
