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
}
