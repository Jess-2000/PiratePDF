using System.IO;
using BlackFlag.Engine.Security.Inspection;

namespace BlackFlag.Engine.Security.Tests.Inspection;

public sealed class DocumentSecurityInspectionBudgetTests
{
    [Fact]
    public void BudgetIsSharedAcrossEmbeddedObjects()
    {
        var budget =
            new DocumentSecurityInspectionBudget(
                new DocumentSecurityInspectionLimits
                {
                    MaximumExpandedBytes = 100,
                    MaximumSingleEmbeddedObjectBytes = 75
                });

        budget.ConsumeEmbeddedObject(40);
        budget.ConsumeEmbeddedObject(50);

        Assert.Equal(90, budget.ExpandedBytes);
        Assert.Equal(2, budget.EmbeddedObjects);

        Assert.Throws<InvalidDataException>(
            () => budget.ConsumeEmbeddedObject(20));
    }

    [Fact]
    public void CompressionBombRatioIsRejected()
    {
        var budget =
            new DocumentSecurityInspectionBudget(
                new DocumentSecurityInspectionLimits
                {
                    MaximumCompressionRatio = 10
                });

        Assert.Throws<InvalidDataException>(
            () => budget.ValidateCompressionRatio(
                compressedLength: 10,
                expandedLength: 101));
    }

    [Fact]
    public void ContainerDepthIsBounded()
    {
        var budget =
            new DocumentSecurityInspectionBudget(
                new DocumentSecurityInspectionLimits
                {
                    MaximumContainerDepth = 3
                });

        budget.ValidateContainerDepth(3);

        Assert.Throws<InvalidDataException>(
            () => budget.ValidateContainerDepth(4));
    }

    [Fact]
    public void WorkUnitsRemainUnchangedAfterRejectedConsumption()
    {
        var budget =
            new DocumentSecurityInspectionBudget(
                new DocumentSecurityInspectionLimits
                {
                    MaximumInspectionWorkUnits = 5
                });

        budget.ConsumeWork(5);

        Assert.Throws<InvalidDataException>(
            () => budget.ConsumeWork());

        Assert.Equal(5, budget.WorkUnits);
    }
}
