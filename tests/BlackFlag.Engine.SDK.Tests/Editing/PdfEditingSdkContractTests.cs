using BlackFlag.Engine.SDK.Editing;

namespace BlackFlag.Engine.SDK.Tests.Editing;

public sealed class PdfEditingSdkContractTests
{
    [Fact]
    public void EditingContractsRemainSdkOwned()
    {
        var page =
            new PdfEditingSdkPage(
                Guid.NewGuid(),
                612,
                792,
                PdfEditingSdkPageRotation.None,
                true,
                0,
                false,
                false);

        Assert.True(page.IsSourcePage);
        Assert.Equal(0, page.SourceIndex);
        Assert.Equal(
            PdfEditingSdkPreservationMode.PreserveUntargetedStructure,
            PdfEditingSdkPreservationMode.PreserveUntargetedStructure);
    }

    [Fact]
    public void ChangeSetCarriesOnlySdkOperationDescriptions()
    {
        var operation =
            new PdfEditingSdkOperation(
                "Page",
                "Move",
                Guid.NewGuid());

        var changeSet =
            new PdfEditingSdkChangeSet(
                Guid.NewGuid(),
                [operation]);

        Assert.Same(
            operation,
            Assert.Single(changeSet.Operations));
    }

    [Fact]
    public void StructuralContractsRemainSdkOwned()
    {
        var box =
            new PdfEditingSdkPageBox(0, 0, 612, 792);

        var state =
            new PdfEditingSdkPageBoxState(
                PdfEditingSdkStructuralValueOrigin.Explicit,
                box);

        var summary =
            new PdfEditingSdkStructureSummary(
                1,
                2,
                1,
                3,
                1,
                4,
                2,
                1);

        Assert.Equal(612, state.Value.Right);
        Assert.Equal(3, summary.TotalFormFieldCount);
        Assert.Equal(
            PdfEditingSdkDocumentInformationField.Title,
            PdfEditingSdkDocumentInformationField.Title);
    }
}
