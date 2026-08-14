using System.Text;

using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;

namespace BlackFlag.Engine.Editing.Tests.Preservation;

public sealed class PdfEditingPreservationContractTests
{
    [Fact]
    public void SessionExposesFusionProTargetedPreservationContract()
    {
        using var session =
            new PdfEditingSession(
                PdfDocument.CreateEmpty());

        var contract =
            session.PreservationContract;

        Assert.True(contract.PreservesUntargetedStructure);
        Assert.True(contract.PreservesExistingObjectIdentity);
        Assert.True(contract.AvoidsImplicitNormalization);
        Assert.True(contract.PreservesHistoryObjectIdentity);
        Assert.True(contract.SupportsTargetedFusionProPreservation);
        Assert.False(contract.GuaranteesByteLevelEquivalence);
    }

    [Fact]
    public void PageEditPreservesUntargetedDocumentLevelState()
    {
        var document =
            PdfDocument.CreateEmpty();

        var first =
            new PdfPage(
                612,
                792);

        var second =
            new PdfPage(
                612,
                792);

        document.Pages.Add(first);
        document.Pages.Add(second);

        var catalog =
            document.Catalog;
        var trailer =
            document.Trailer;
        var objectStore =
            document.Objects;

        var identifier =
            new PdfObjectIdentifier(
                42,
                0);

        var untargetedObject =
            PdfString.FromLatin1(
                "FusionPro-preserved");

        objectStore.Add(
            identifier,
            untargetedObject);

        using var session =
            new PdfEditingSession(document);

        _ = session.Document.SetPageRotation(
            session.Document.Pages[0],
            PdfPageRotation.Clockwise90);

        Assert.Same(catalog, document.Catalog);
        Assert.Same(trailer, document.Trailer);
        Assert.Same(objectStore, document.Objects);
        Assert.Same(
            untargetedObject,
            document.Objects.Get(identifier));
        Assert.Same(
            second,
            document.Pages[1]);
    }

    [Fact]
    public void ContentEditPreservesUntargetedPageIdentityAndState()
    {
        var document =
            PdfDocument.CreateEmpty();

        var targeted =
            new PdfPage(
                612,
                792);

        var untargeted =
            new PdfPage(
                612,
                792,
                PdfPageRotation.Clockwise180);

        document.Pages.Add(targeted);
        document.Pages.Add(untargeted);

        using var session =
            new PdfEditingSession(document);

        var untargetedHandle =
            session.Document.Pages[1];

        _ = session.Document.ReplacePageContent(
            session.Document.Pages[0],
            Encoding.ASCII.GetBytes(
                "q Q"));

        Assert.Same(
            untargeted,
            document.Pages[1]);
        Assert.Same(
            untargetedHandle,
            session.Document.Pages[1]);
        Assert.Equal(
            PdfPageRotation.Clockwise180,
            untargetedHandle.Rotation);
        Assert.False(untargetedHandle.HasContent);
        Assert.False(untargetedHandle.HasResources);
    }

    [Fact]
    public void ResourceUndoRestoresPriorLogicalEntry()
    {
        var document =
            PdfDocument.CreateEmpty();

        document.Pages.Add(
            new PdfPage(
                612,
                792));

        using var session =
            new PdfEditingSession(document);

        var page =
            session.Document.Pages[0];

        var category =
            PdfName.FromLatin1("Font");
        var name =
            PdfName.FromLatin1("F1");

        _ = session.Document.SetPageResource(
            page,
            category,
            name,
            new PdfDictionary());

        using (var transaction =
            session.BeginTransaction())
        {
            _ = session.Document.SetPageResource(
                page,
                category,
                name,
                new PdfDictionary());

            _ = transaction.Commit();
        }

        _ = session.History.Undo();

        Assert.True(
            page.ContainsResource(
                category,
                name));
    }

    [Fact]
    public void StructuralUndoRestoresSamePageHandle()
    {
        var document =
            PdfDocument.CreateEmpty();

        document.Pages.Add(
            new PdfPage(
                612,
                792));

        using var session =
            new PdfEditingSession(document);

        var original =
            session.Document.Pages[0];

        using (var transaction =
            session.BeginTransaction())
        {
            _ = session.Document.RemovePage(
                original);

            _ = transaction.Commit();
        }

        _ = session.History.Undo();

        Assert.Same(
            original,
            session.Document.Pages[0]);
    }
}
