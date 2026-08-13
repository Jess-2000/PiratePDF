using System;
using System.IO;
using BlackFlag.Engine.Security.Containers;
using BlackFlag.Engine.Security.Inspection;

namespace BlackFlag.Engine.Security.Tests.Inspection;

public sealed class DocumentSecurityAdversarialHardeningTests
{
    [Fact]
    public void ExpandedByteCounterCannotWrapAtInt64Boundary()
    {
        var budget =
            new DocumentSecurityInspectionBudget(
                new DocumentSecurityInspectionLimits
                {
                    MaximumExpandedBytes = long.MaxValue
                });

        budget.ConsumeExpandedBytes(long.MaxValue);

        Assert.Throws<InvalidDataException>(
            () => budget.ConsumeExpandedBytes(1));

        Assert.Equal(long.MaxValue, budget.ExpandedBytes);
    }

    [Fact]
    public void WorkCounterCannotWrapAtInt64Boundary()
    {
        var budget =
            new DocumentSecurityInspectionBudget(
                new DocumentSecurityInspectionLimits
                {
                    MaximumInspectionWorkUnits = long.MaxValue
                });

        budget.ConsumeWork(long.MaxValue);

        Assert.Throws<InvalidDataException>(
            () => budget.ConsumeWork(1));

        Assert.Equal(long.MaxValue, budget.WorkUnits);
    }

    [Fact]
    public void RejectedEmbeddedBytesDoNotConsumeObjectCount()
    {
        var budget =
            new DocumentSecurityInspectionBudget(
                new DocumentSecurityInspectionLimits
                {
                    MaximumExpandedBytes = 4,
                    MaximumSingleEmbeddedObjectBytes = 8
                });

        Assert.Throws<InvalidDataException>(
            () => budget.ConsumeEmbeddedObject(5));

        Assert.Equal(0, budget.EmbeddedObjects);
        Assert.Equal(0, budget.ExpandedBytes);
    }

    [Fact]
    public void BoundaryConsumptionSucceedsExactlyOnce()
    {
        var budget =
            new DocumentSecurityInspectionBudget(
                new DocumentSecurityInspectionLimits
                {
                    MaximumEmbeddedObjects = 1,
                    MaximumExpandedBytes = 4,
                    MaximumSingleEmbeddedObjectBytes = 4,
                    MaximumObservations = 1,
                    MaximumInspectionWorkUnits = 1
                });

        budget.ConsumeEmbeddedObject(4);
        budget.ConsumeObservation();
        budget.ConsumeWork();

        Assert.Equal(1, budget.EmbeddedObjects);
        Assert.Equal(4, budget.ExpandedBytes);
        Assert.Equal(1, budget.Observations);
        Assert.Equal(1, budget.WorkUnits);

        Assert.Throws<InvalidDataException>(
            () => budget.ConsumeEmbeddedObject(0));
        Assert.Throws<InvalidDataException>(
            budget.ConsumeObservation);
        Assert.Throws<InvalidDataException>(
            () => budget.ConsumeWork());
    }

    [Fact]
    public void SignatureClassifierToleratesDeterministicAdversarialBytes()
    {
        var classifier = new DocumentEmbeddedObjectClassifier();
        var random = new Random(1049);
        byte[] bytes = new byte[257];

        for (int iteration = 0; iteration < 4096; iteration++)
        {
            int length = iteration % bytes.Length;
            random.NextBytes(bytes);

            var embeddedObject =
                new DocumentEmbeddedObject(
                    bytes.AsMemory(0, length),
                    "/fuzz/payload.bin",
                    declaredMediaType: "application/octet-stream",
                    declaredFileName: "payload.bin");

            DocumentEmbeddedObjectClassification classification =
                classifier.Classify(embeddedObject);

            Assert.NotNull(classification);
            Assert.False(
                string.IsNullOrWhiteSpace(
                    classification.FormatIdentifier));
        }
    }

    [Fact]
    public void TruncatedPortableExecutableHeadersRemainPassive()
    {
        var classifier = new DocumentEmbeddedObjectClassifier();

        for (int length = 1; length < 64; length++)
        {
            byte[] bytes = new byte[length];
            bytes[0] = (byte)'M';

            if (length > 1)
            {
                bytes[1] = (byte)'Z';
            }

            DocumentEmbeddedObjectClassification classification =
                classifier.Classify(
                    new DocumentEmbeddedObject(
                        bytes,
                        "/fuzz/truncated.exe",
                        declaredFileName: "truncated.exe"));

            Assert.False(classification.IsExecutable);
        }
    }
}
