using System.Text;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfNewPageWritePlannerTests
{
    private static readonly PdfName KidsName = PdfName.FromLatin1("Kids");
    private static readonly PdfName ParentName = PdfName.FromLatin1("Parent");

    [Fact]
    public void FullRewriteAllocatesBindsAndSerializesNewPage()
    {
        PdfDocument document = CreateBoundDocument(PdfVersion.Pdf17);
        var page = new PdfPage(612, 792);
        document.Pages.Add(page);

        PdfWritePlan basePlan = PdfWritePlanner.Create(document);
        int expectedObjectNumber = basePlan.NextObjectNumber;

        PdfWritePlan plan =
            PdfNewPageFullRewritePlanner.Create(basePlan);

        PdfObjectIdentifier pageIdentifier =
            Assert.IsType<PdfObjectIdentifier>(
                page.ObjectIdentifier);

        Assert.Equal(expectedObjectNumber, pageIdentifier.ObjectNumber);
        Assert.Contains(pageIdentifier, plan.ExistingObjectIdentifiers);

        PdfDictionary pageDictionary =
            document.Objects.Get<PdfDictionary>(pageIdentifier);
        Assert.Equal(
            document.Pages.ObjectIdentifier,
            pageDictionary
                .Get<PdfIndirectReference>(ParentName)
                .Identifier);

        string output =
            Encoding.Latin1.GetString(
                PdfFullRewriteWriter.Write(plan));

        Assert.Contains(
            $"{pageIdentifier.ObjectNumber} 0 obj",
            output,
            StringComparison.Ordinal);
        Assert.Contains(
            $"/Parent {document.Pages.ObjectIdentifier!.Value.ObjectNumber} 0 R",
            output,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FullRewriteAllocatesMultiplePagesInAuthoritativeOrder()
    {
        PdfDocument document = CreateBoundDocument(PdfVersion.Pdf17);
        var first = new PdfPage(612, 792);
        var second = new PdfPage(300, 400);
        document.Pages.Add(first);
        document.Pages.Add(second);

        PdfWritePlan basePlan = PdfWritePlanner.Create(document);
        PdfWritePlan plan =
            PdfNewPageFullRewritePlanner.Create(basePlan);

        PdfObjectIdentifier firstIdentifier =
            first.ObjectIdentifier!.Value;
        PdfObjectIdentifier secondIdentifier =
            second.ObjectIdentifier!.Value;

        Assert.Equal(basePlan.NextObjectNumber, firstIdentifier.ObjectNumber);
        Assert.Equal(
            firstIdentifier.ObjectNumber + 1,
            secondIdentifier.ObjectNumber);
        Assert.Contains(firstIdentifier, plan.ExistingObjectIdentifiers);
        Assert.Contains(secondIdentifier, plan.ExistingObjectIdentifiers);

        PdfDictionary tree =
            document.Objects.Get<PdfDictionary>(
                document.Pages.ObjectIdentifier!.Value);
        PdfArray kids = tree.Get<PdfArray>(KidsName);

        Assert.Equal(
            firstIdentifier,
            kids.Get<PdfIndirectReference>(0).Identifier);
        Assert.Equal(
            secondIdentifier,
            kids.Get<PdfIndirectReference>(1).Identifier);
    }

    [Fact]
    public void IncrementalPlanningTargetsNewPageAndPageTree()
    {
        PdfDocument document = CreateBoundDocument(PdfVersion.Pdf17);
        var existing = new PdfPage(612, 792);
        BindExistingPage(document, existing, new PdfObjectIdentifier(3));
        document.Pages.Add(existing);

        var added = new PdfPage(612, 792);
        document.Pages.Add(added);

        PdfWritePlan basePlan =
            PdfWritePlanner.Create(
                document,
                new PdfWriteOptions
                {
                    Mode = PdfWriteMode.IncrementalUpdate,
                    CrossReferenceMode =
                        PdfCrossReferenceWriteMode.TraditionalTable
                });

        PdfIncrementalWritePlan plan =
            PdfNewPageIncrementalWritePlanner.Create(
                basePlan,
                CreateSource());

        PdfObjectIdentifier newIdentifier =
            added.ObjectIdentifier!.Value;
        PdfObjectIdentifier treeIdentifier =
            document.Pages.ObjectIdentifier!.Value;

        Assert.Contains(newIdentifier, plan.ChangedObjectIdentifiers);
        Assert.Contains(treeIdentifier, plan.ChangedObjectIdentifiers);
        Assert.DoesNotContain(
            existing.ObjectIdentifier!.Value,
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void IncrementalPlanningFailureLeavesNewPageUnboundAndUnstored()
    {
        PdfDocument document = CreateBoundDocument(PdfVersion.Pdf14);
        var page = new PdfPage(612, 792);
        document.Pages.Add(page);

        PdfWritePlan basePlan =
            PdfWritePlanner.Create(
                document,
                new PdfWriteOptions
                {
                    Mode = PdfWriteMode.IncrementalUpdate,
                    CrossReferenceMode =
                        PdfCrossReferenceWriteMode.CrossReferenceStream
                });
        var expectedIdentifier =
            new PdfObjectIdentifier(basePlan.NextObjectNumber);

        Assert.Throws<NotSupportedException>(
            () => PdfNewPageIncrementalWritePlanner.Create(
                basePlan,
                CreateSource()));

        Assert.Null(page.ObjectIdentifier);
        Assert.False(document.Objects.Contains(expectedIdentifier));
    }

    [Fact]
    public void FullRewritePlanningFailureLeavesNewPageUnboundAndUnstored()
    {
        PdfDocument document = CreateBoundDocument(PdfVersion.Pdf17);
        var page = new PdfPage(612, 792);
        document.Pages.Add(page);

        PdfWritePlan basePlan = PdfWritePlanner.Create(document);
        var expectedIdentifier =
            new PdfObjectIdentifier(basePlan.NextObjectNumber);

        _ = document.Objects.Remove(
            document.Trailer.RootObject!.Value);

        Assert.Throws<InvalidOperationException>(
            () => PdfNewPageFullRewritePlanner.Create(basePlan));

        Assert.Null(page.ObjectIdentifier);
        Assert.False(document.Objects.Contains(expectedIdentifier));
    }

    [Fact]
    public void PlanningRejectsDocumentWithoutNewPages()
    {
        PdfDocument document = CreateBoundDocument(PdfVersion.Pdf17);

        Assert.Throws<InvalidOperationException>(
            () => PdfNewPageFullRewritePlanner.Create(
                PdfWritePlanner.Create(document)));
    }

    private static PdfDocument CreateBoundDocument(
        PdfVersion version)
    {
        PdfDocument document = PdfDocument.CreateEmpty(version);
        var rootIdentifier = new PdfObjectIdentifier(1);
        var treeIdentifier = new PdfObjectIdentifier(2);

        document.BindCoreStructure(
            rootIdentifier,
            treeIdentifier);
        document.Objects.Add(
            rootIdentifier,
            new PdfDictionary());
        document.Objects.Add(
            treeIdentifier,
            document.Pages.CreateDictionarySnapshot());
        document.Trailer.SetRootObject(rootIdentifier);

        return document;
    }

    private static void BindExistingPage(
        PdfDocument document,
        PdfPage page,
        PdfObjectIdentifier identifier)
    {
        page.BindObjectIdentifier(identifier);
        document.Objects.Add(
            identifier,
            page.CreateDictionarySnapshot());
    }

    private static PdfMemorySourceRevision CreateSource() =>
        new(
            Encoding.ASCII.GetBytes("%PDF-1.7\nxref\n%%EOF\n"),
            9,
            PdfCrossReferenceWriteMode.TraditionalTable);
}
