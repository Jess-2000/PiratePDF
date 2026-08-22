using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfPageTreeWriteProjectorTests
{
    private static readonly PdfName KidsName = PdfName.FromLatin1("Kids");
    private static readonly PdfName CountName = PdfName.FromLatin1("Count");
    private static readonly PdfName CustomName = PdfName.FromLatin1("FusionProPrivate");

    [Fact]
    public void CreateProjectsAuthoritativeOrderAndCount()
    {
        (PdfDocument document, PdfPage first, PdfPage second) =
            CreateBoundDocument();

        document.Pages.Move(1, 0);

        PdfDictionary projected = GetProjectedDictionary(document);
        PdfArray kids = projected.Get<PdfArray>(KidsName);

        Assert.Equal(2, projected.Get<PdfInteger>(CountName).Value);
        Assert.Equal(
            second.ObjectIdentifier,
            kids.Get<PdfIndirectReference>(0).Identifier);
        Assert.Equal(
            first.ObjectIdentifier,
            kids.Get<PdfIndirectReference>(1).Identifier);
    }

    [Fact]
    public void CreateProjectsRemovalWithoutDeletingDetachedObject()
    {
        (PdfDocument document, PdfPage first, PdfPage second) =
            CreateBoundDocument();
        PdfObjectIdentifier removedIdentifier =
            second.ObjectIdentifier!.Value;

        _ = document.Pages.Remove(second);

        PdfDictionary projected = GetProjectedDictionary(document);
        PdfArray kids = projected.Get<PdfArray>(KidsName);

        Assert.Equal(1, projected.Get<PdfInteger>(CountName).Value);
        Assert.Single(kids);
        Assert.Equal(
            first.ObjectIdentifier,
            kids.Get<PdfIndirectReference>(0).Identifier);
        Assert.True(document.Objects.Contains(removedIdentifier));
    }

    [Fact]
    public void CreatePreservesUntargetedPageTreeEntriesByReference()
    {
        (PdfDocument document, _, _) = CreateBoundDocument();
        PdfObjectIdentifier treeIdentifier =
            document.Pages.ObjectIdentifier!.Value;
        PdfDictionary original =
            document.Objects.Get<PdfDictionary>(treeIdentifier);
        var custom = PdfString.FromLatin1("preserve-me");
        original.Add(CustomName, custom);

        PdfDictionary projected = GetProjectedDictionary(document);

        Assert.Same(custom, projected.Get(CustomName));
        Assert.Same(custom, original.Get(CustomName));
    }

    [Fact]
    public void CreateRejectsUnboundPage()
    {
        (PdfDocument document, _, _) = CreateBoundDocument();
        document.Pages.Add(new PdfPage(612, 792));

        Assert.Throws<InvalidOperationException>(
            () => PdfPageTreeWriteProjector.Create(document));
    }

    [Fact]
    public void CreateRejectsMissingPageBackingObject()
    {
        (PdfDocument document, _, PdfPage second) =
            CreateBoundDocument();
        PdfObjectIdentifier identifier =
            second.ObjectIdentifier!.Value;
        _ = document.Objects.Remove(identifier);

        Assert.Throws<InvalidOperationException>(
            () => PdfPageTreeWriteProjector.Create(document));
    }

    [Fact]
    public void CreateRejectsNonDictionaryPageTreeObject()
    {
        (PdfDocument document, _, _) = CreateBoundDocument();
        PdfObjectIdentifier identifier =
            document.Pages.ObjectIdentifier!.Value;
        document.Objects.Set(
            identifier,
            PdfString.FromLatin1("invalid"));

        Assert.Throws<InvalidOperationException>(
            () => PdfPageTreeWriteProjector.Create(document));
    }

    private static PdfDictionary GetProjectedDictionary(
        PdfDocument document)
    {
        return Assert.IsType<PdfDictionary>(
            Assert.Single(
                PdfPageTreeWriteProjector.Create(document)
                    .Projections)
                .Value);
    }

    private static (PdfDocument Document, PdfPage First, PdfPage Second)
        CreateBoundDocument()
    {
        PdfDocument document = PdfDocument.CreateEmpty();
        var rootIdentifier = new PdfObjectIdentifier(1);
        var treeIdentifier = new PdfObjectIdentifier(2);
        var firstIdentifier = new PdfObjectIdentifier(3);
        var secondIdentifier = new PdfObjectIdentifier(4);

        document.BindCoreStructure(
            rootIdentifier,
            treeIdentifier);
        document.Objects.Add(
            rootIdentifier,
            new PdfDictionary());
        document.Objects.Add(
            treeIdentifier,
            new PdfDictionary());

        var first = new PdfPage(612, 792);
        first.BindObjectIdentifier(firstIdentifier);
        document.Pages.Add(first);
        document.Objects.Add(
            firstIdentifier,
            first.CreateDictionarySnapshot());

        var second = new PdfPage(612, 792);
        second.BindObjectIdentifier(secondIdentifier);
        document.Pages.Add(second);
        document.Objects.Add(
            secondIdentifier,
            second.CreateDictionarySnapshot());

        return (document, first, second);
    }
}
