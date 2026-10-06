using BlackFlag.Engine.Services.Text;

namespace BlackFlag.Engine.Services.Tests.Text;

public sealed class PdfExistingTextStyleServiceTests
{
    [Fact]
    public void SuperscriptDefaultsAreStable()
    {
        var update =
            new PdfExistingTextStyleUpdate
            {
                ScriptStyle =
                    PdfExistingTextScriptStyle.Superscript
            };

        update.Validate();

        Assert.Equal(0.65, update.SuperscriptScale);
        Assert.Equal(0.35, update.SuperscriptRise);
    }

    [Fact]
    public void SubscriptDefaultsAreStable()
    {
        var update =
            new PdfExistingTextStyleUpdate
            {
                ScriptStyle =
                    PdfExistingTextScriptStyle.Subscript
            };

        update.Validate();

        Assert.Equal(0.65, update.SubscriptScale);
        Assert.Equal(0.20, update.SubscriptDrop);
    }

    [Fact]
    public void DeviceRgbRequiresNormalizedComponents()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                PdfExistingTextColor.Rgb(
                    1.1,
                    0,
                    0));
    }

    [Fact]
    public void ExplicitUnderlineRequiresPositiveAdvance()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new PdfExistingTextDecoration(
                    PdfExistingTextDecorationKind.Underline,
                    0));
    }
    [Fact]
    public void ExistingTextColorComponentsRejectMutationThroughListCast()
    {
        PdfExistingTextColor color = PdfExistingTextColor.Rgb(0.25, 0.5, 0.75);
        var components = Assert.IsType<IList<double>>(color.Components, exactMatch: false);

        Assert.Throws<NotSupportedException>(() => components[0] = double.NaN);
        Assert.Equal(0.25, color.Components[0]);
    }

}
