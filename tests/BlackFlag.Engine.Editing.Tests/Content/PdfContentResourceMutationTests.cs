using System.Text;

using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Operations;

namespace BlackFlag.Engine.Editing.Tests.Content;

public sealed class PdfContentResourceMutationTests
{
    [Fact]
    public void ContentReplacementUsesDefensiveCopies()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var replacement = Encoding.ASCII.GetBytes("q 1 0 0 1 10 20 cm Q");

        var operation =
            session.Document.ReplacePageContent(
                page,
                replacement);

        replacement[0] = (byte)'X';
        var stored = page.GetContentBytes();

        Assert.Equal(PdfEditScope.Content, operation.Scope);
        Assert.Equal(PdfContentMutationKind.Replace, operation.Kind);
        Assert.Equal(0, operation.PreviousLength);
        Assert.Equal(stored.Length, operation.ReplacementLength);
        Assert.Equal((byte)'q', stored[0]);
        Assert.True(page.HasContent);
    }

    [Fact]
    public void ContentReplacementParticipatesInUndoAndRedo()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];

        _ = session.Document.ReplacePageContent(
            page,
            Encoding.ASCII.GetBytes("q Q"));

        using var transaction = session.BeginTransaction();
        _ = session.Document.ReplacePageContent(
            page,
            Encoding.ASCII.GetBytes("BT ET"));
        _ = transaction.Commit();

        Assert.Equal(
            "BT ET",
            Encoding.ASCII.GetString(page.GetContentBytes()));

        _ = session.History.Undo();

        Assert.Equal(
            "q Q",
            Encoding.ASCII.GetString(page.GetContentBytes()));

        _ = session.History.Redo();

        Assert.Equal(
            "BT ET",
            Encoding.ASCII.GetString(page.GetContentBytes()));
    }

    [Fact]
    public void ResourceSetAndRemoveParticipateInRollback()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var fontCategory = PdfName.FromLatin1("Font");
        var fontName = PdfName.FromLatin1("F1");
        var resource = new PdfDictionary();

        using var transaction = session.BeginTransaction();

        var setOperation =
            session.Document.SetPageResource(
                page,
                fontCategory,
                fontName,
                resource);

        Assert.True(
            page.ContainsResource(
                fontCategory,
                fontName));

        var removeOperation =
            session.Document.RemovePageResource(
                page,
                fontCategory,
                fontName);

        Assert.False(
            page.ContainsResource(
                fontCategory,
                fontName));

        transaction.Rollback();

        Assert.False(page.HasResources);
        Assert.Equal(PdfEditScope.Resource, setOperation.Scope);
        Assert.Equal(PdfResourceMutationKind.Set, setOperation.Kind);
        Assert.Equal(PdfResourceMutationKind.Remove, removeOperation.Kind);
    }

    [Fact]
    public void ReplacingExistingResourceRestoresItThroughUndo()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var category = PdfName.FromLatin1("XObject");
        var name = PdfName.FromLatin1("Im1");

        _ = session.Document.SetPageResource(
            page,
            category,
            name,
            new PdfDictionary());

        using var transaction = session.BeginTransaction();

        var operation =
            session.Document.SetPageResource(
                page,
                category,
                name,
                new PdfStream(
                    new PdfDictionary(),
                    [1, 2, 3]));

        _ = transaction.Commit();

        Assert.True(operation.ReplacedExisting);
        Assert.True(page.ContainsResource(category, name));

        _ = session.History.Undo();

        Assert.True(page.ContainsResource(category, name));

        _ = session.History.Redo();

        Assert.True(page.ContainsResource(category, name));
    }

    [Fact]
    public void RemovingMissingResourceIsRejectedWithoutRecordingChange()
    {
        var document = PdfDocument.CreateEmpty();
        document.Pages.Add(new PdfPage(612, 792));

        using var session = new PdfEditingSession(document);
        var page = session.Document.Pages[0];
        var category = PdfName.FromLatin1("Font");
        var name = PdfName.FromLatin1("Missing");

        using var transaction = session.BeginTransaction();

        Assert.Throws<KeyNotFoundException>(
            () =>
            {
                _ = session.Document.RemovePageResource(
                    page,
                    category,
                    name);
            });

        Assert.Equal(0, transaction.ChangeCount);
        transaction.Rollback();
    }
}
