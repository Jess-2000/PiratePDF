using System.Collections.Generic;
using System.IO;
using System.Threading;
using BlackFlag.Engine.Security.Containers;
using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Tests.Containers;

public sealed class DocumentEmbeddedInspectionEngineTests
{
    [Fact]
    public void RecursiveChildrenShareOneBudget()
    {
        var adapter = new FixtureContainerAdapter(
            new DocumentContainerEntry(
                new DocumentEmbeddedObject(
                    "child"u8.ToArray(),
                    "/root/child.bin"),
                compressedLength: 5));

        var budget = new DocumentSecurityInspectionBudget();

        DocumentEmbeddedInspectionResult result =
            new DocumentEmbeddedInspectionEngine(adapters: [adapter])
                .Inspect(
                    [new DocumentEmbeddedObject(new byte[] { 0x50, 0x4B, 0x03, 0x04 }, "/root.zip")],
                    budget);

        Assert.Equal(2, budget.EmbeddedObjects);
        Assert.Equal(9, budget.ExpandedBytes);
        Assert.Equal(2, result.Observations.Count);
    }

    [Fact]
    public void RecursiveDepthLimitAppliesAcrossAdapters()
    {
        var adapter = new SelfNestingAdapter();
        var budget =
            new DocumentSecurityInspectionBudget(
                new DocumentSecurityInspectionLimits
                {
                    MaximumContainerDepth = 2
                });

        Assert.Throws<InvalidDataException>(
            () => new DocumentEmbeddedInspectionEngine(adapters: [adapter])
                .Inspect(
                    [new DocumentEmbeddedObject(new byte[] { 0x50, 0x4B, 0x03, 0x04 }, "/root.zip")],
                    budget));
    }

    [Fact]
    public void CompressionRatioIsValidatedBeforeRecursiveInspection()
    {
        var budget =
            new DocumentSecurityInspectionBudget(
                new DocumentSecurityInspectionLimits
                {
                    MaximumCompressionRatio = 2
                });

        Assert.Throws<InvalidDataException>(
            () => new DocumentEmbeddedInspectionEngine()
                .Inspect(
                    [new DocumentEmbeddedObject(
                        new byte[101],
                        "/expanded.bin",
                        compressedLength: 50)],
                    budget));
    }

    [Fact]
    public void ExecutablePayloadProducesNormalizedObservation()
    {
        byte[] bytes = new byte[128];
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        bytes[0x3C] = 0x40;
        bytes[0x40] = (byte)'P';
        bytes[0x41] = (byte)'E';

        DocumentEmbeddedInspectionResult result =
            new DocumentEmbeddedInspectionEngine()
                .Inspect(
                    [new DocumentEmbeddedObject(bytes, "/attachment.exe")]);

        DocumentSecurityObservation observation = Assert.Single(result.Observations);
        Assert.Equal(DocumentSecurityObservationCategory.ExecutableContent, observation.Category);
        Assert.Equal("EmbeddedExecutableContent", observation.Kind);
    }

    private sealed class FixtureContainerAdapter : IDocumentContainerAdapter
    {
        private readonly DocumentContainerEntry[] entries;

        public FixtureContainerAdapter(params DocumentContainerEntry[] entries) =>
            this.entries = entries;

        public bool CanExpand(DocumentEmbeddedObjectClassification classification) =>
            classification.Kind == DocumentEmbeddedObjectKind.ZipContainer;

        public IEnumerable<DocumentContainerEntry> Expand(
            DocumentEmbeddedObject embeddedObject,
            CancellationToken cancellationToken = default) =>
            entries;
    }

    private sealed class SelfNestingAdapter : IDocumentContainerAdapter
    {
        public bool CanExpand(DocumentEmbeddedObjectClassification classification) =>
            classification.Kind == DocumentEmbeddedObjectKind.ZipContainer;

        public IEnumerable<DocumentContainerEntry> Expand(
            DocumentEmbeddedObject embeddedObject,
            CancellationToken cancellationToken = default)
        {
            yield return new DocumentContainerEntry(
                new DocumentEmbeddedObject(
                    new byte[] { 0x50, 0x4B, 0x03, 0x04 },
                    embeddedObject.LogicalPath + "/nested.zip"),
                compressedLength: 4);
        }
    }
}
