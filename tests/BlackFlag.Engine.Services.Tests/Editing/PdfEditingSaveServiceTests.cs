using System.IO;
using System.Text;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Services.Editing;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Editing;

public sealed class PdfEditingSaveServiceTests
{
    private static readonly PdfName TypeName =
        PdfName.FromLatin1("Type");
    private static readonly PdfName PagesName =
        PdfName.FromLatin1("Pages");
    private static readonly PdfName PageName =
        PdfName.FromLatin1("Page");
    private static readonly PdfName KidsName =
        PdfName.FromLatin1("Kids");
    private static readonly PdfName CountName =
        PdfName.FromLatin1("Count");
    private static readonly PdfName ParentName =
        PdfName.FromLatin1("Parent");

    [Fact]
    public void FullRewriteSerializesCommittedPageTransaction()
    {
        PdfDocument document =
            CreateDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.SetPageRotation(
                session.Document.Pages[0],
                PdfPageRotation.Clockwise90);
            changeSet = transaction.Commit();
        }

        byte[] output =
            PdfEditingSaveService.Write(
                session,
                changeSet);

        string text =
            Encoding.Latin1.GetString(output);

        Assert.StartsWith(
            "%PDF-1.7",
            text);
        Assert.Contains(
            "/Rotate 90",
            text);
        Assert.EndsWith(
            "%%EOF\n",
            text);
    }

    [Fact]
    public void IncrementalSavePreservesOriginalBytesAndResolvesSourceXrefMode()
    {
        PdfDocument document =
            CreateDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.SetPageRotation(
                session.Document.Pages[0],
                PdfPageRotation.Clockwise180);
            changeSet = transaction.Commit();
        }

        byte[] sourceBytes =
            Encoding.ASCII.GetBytes(
                "%PDF-1.7\n% original\nxref\n%%EOF\n");
        var source =
            new PdfMemorySourceRevision(
                sourceBytes,
                20,
                PdfCrossReferenceWriteMode.TraditionalTable);

        byte[] output =
            PdfEditingSaveService.Write(
                session,
                changeSet,
                new PdfWriteOptions
                {
                    Mode =
                        PdfWriteMode.IncrementalUpdate,
                    CrossReferenceMode =
                        PdfCrossReferenceWriteMode.PreserveSource
                },
                source);

        Assert.True(
            output.AsSpan(
                    0,
                    sourceBytes.Length)
                .SequenceEqual(
                    sourceBytes));
        Assert.Contains(
            "/Rotate 180",
            Encoding.Latin1.GetString(output));
    }

    [Fact]
    public void IncrementalSaveRequiresSourceRevision()
    {
        PdfDocument document =
            CreateDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.SetPageRotation(
                session.Document.Pages[0],
                PdfPageRotation.Clockwise270);
            changeSet = transaction.Commit();
        }

        Assert.Throws<ArgumentNullException>(
            () => PdfEditingSaveService.Write(
                session,
                changeSet,
                new PdfWriteOptions
                {
                    Mode =
                        PdfWriteMode.IncrementalUpdate
                }));
    }

    [Fact]
    public void StreamOverloadLeavesCallerOwnedDestinationOpen()
    {
        PdfDocument document =
            CreateDocument();
        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.SetPageRotation(
                session.Document.Pages[0],
                PdfPageRotation.Clockwise90);
            changeSet = transaction.Commit();
        }

        using var destination =
            new MemoryStream();

        PdfEditingSaveService.Write(
            destination,
            session,
            changeSet);

        Assert.True(
            destination.CanWrite);
        Assert.True(
            destination.Length > 0);
    }

    private static PdfDocument CreateDocument()
    {
        PdfDocument document =
            PdfDocument.CreateEmpty();

        var catalogId =
            new PdfObjectIdentifier(1);
        var rootId =
            new PdfObjectIdentifier(2);
        var branchId =
            new PdfObjectIdentifier(3);
        var pageId =
            new PdfObjectIdentifier(4);

        document.BindCoreStructure(
            catalogId,
            rootId);
        document.Objects.Add(
            catalogId,
            new PdfDictionary());
        document.Objects.Add(
            rootId,
            CreatePagesDictionary(
                null,
                [branchId],
                1));
        document.Objects.Add(
            branchId,
            CreatePagesDictionary(
                rootId,
                [pageId],
                1));

        var page =
            new PdfPage(
                612,
                792);
        page.BindObjectIdentifier(
            pageId);
        document.Pages.Add(
            page);
        document.Objects.Add(
            pageId,
            new PdfDictionary
            {
                {
                    TypeName,
                    PageName
                },
                {
                    ParentName,
                    new PdfIndirectReference(
                        branchId)
                },
                {
                    PdfName.FromLatin1("MediaBox"),
                    new PdfArray(
                    [
                        new PdfInteger(0),
                        new PdfInteger(0),
                        new PdfInteger(612),
                        new PdfInteger(792)
                    ])
                }
            });

        document.Trailer.SetRootObject(
            catalogId);
        return document;
    }

    private static PdfDictionary CreatePagesDictionary(
        PdfObjectIdentifier? parent,
        IReadOnlyList<PdfObjectIdentifier> kids,
        int count)
    {
        var dictionary =
            new PdfDictionary
            {
                {
                    TypeName,
                    PagesName
                },
                {
                    KidsName,
                    new PdfArray(
                        kids.Select(
                            identifier =>
                                (PdfObject)new PdfIndirectReference(
                                    identifier)))
                },
                {
                    CountName,
                    new PdfInteger(count)
                }
            };

        if (parent.HasValue)
        {
            dictionary.Set(
                ParentName,
                new PdfIndirectReference(
                    parent.Value));
        }

        return dictionary;
    }
}
