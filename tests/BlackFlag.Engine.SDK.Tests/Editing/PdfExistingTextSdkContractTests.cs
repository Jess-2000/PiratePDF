using BlackFlag.Engine.SDK.Editing;

namespace BlackFlag.Engine.SDK.Tests.Editing;

public sealed class PdfExistingTextSdkContractTests
{
    [Fact]
    public void ExistingTextContractsRemainSdkOwned()
    {
        Type[] publicTypes =
        [
            typeof(IPdfExistingTextSdkSession),
            typeof(PdfExistingTextSdkTarget),
            typeof(PdfExistingTextSdkTypography),
            typeof(PdfExistingTextSdkTypographyUpdate),
            typeof(PdfExistingTextSdkStyleUpdate),
            typeof(PdfExistingTextSdkReplaceResult),
            typeof(PdfExistingTextSdkFindReplaceResult),
            typeof(PdfExistingTextSdkFontCandidate)
        ];

        Assert.All(publicTypes, type => Assert.Equal("BlackFlag.Engine.SDK", type.Assembly.GetName().Name));
    }

    [Fact]
    public void TargetTextSegmentsAreReadOnlySnapshot()
    {
        var target = new PdfExistingTextSdkTarget(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PdfExistingTextSdkSourceKind.PageContent,
            ["One", "Two"],
            "F1",
            12,
            new PdfExistingTextSdkTypography(12, 0, 0, 100, 0, 0),
            PdfExistingTextSdkEditability.EditableWithPositionPreservation,
            "Position preservation required.");

        Assert.Equal("OneTwo", target.Text);
        Assert.IsNotType<string[]>(target.TextSegments);
    }
}
