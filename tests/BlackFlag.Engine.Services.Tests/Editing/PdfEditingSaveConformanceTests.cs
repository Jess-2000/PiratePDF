using System.Text;
using BlackFlag.Engine.Document.Annotations;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Document.Pages;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Services.Editing;
using BlackFlag.Engine.Writing;

namespace BlackFlag.Engine.Services.Tests.Editing;

public sealed class PdfEditingSaveConformanceTests
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
    private static readonly PdfName MediaBoxName =
        PdfName.FromLatin1("MediaBox");
    private static readonly PdfName AnnotsName =
        PdfName.FromLatin1("Annots");

    [Fact]
    public void FullRewriteTraditionalSerializesNestedMixedPageTransactionAndUntargetedObject()
    {
        PdfDocument document =
            CreateNestedDocument();

        var untargetedId =
            new PdfObjectIdentifier(20);
        document.Objects.Add(
            untargetedId,
            new PdfDictionary
            {
                {
                    PdfName.FromLatin1("PreservationSentinel"),
                    PdfString.FromLatin1("keep-me")
                }
            });

        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            var moved =
                session.Document.Pages[0];

            _ = session.Document.MovePage(
                moved,
                1);
            _ = session.Document.ResizePage(
                moved,
                500,
                700);
            _ = session.Document.SetPageRotation(
                moved,
                PdfPageRotation.Clockwise90);

            changeSet = transaction.Commit();
        }

        byte[] output =
            PdfEditingSaveService.Write(
                session,
                changeSet,
                new PdfWriteOptions
                {
                    Mode =
                        PdfWriteMode.FullRewrite,
                    CrossReferenceMode =
                        PdfCrossReferenceWriteMode.TraditionalTable
                });

        string serialized =
            Encoding.Latin1.GetString(
                output);

        Assert.StartsWith(
            "%PDF-1.7",
            serialized);
        Assert.Contains(
            "/Rotate 90",
            serialized);
        Assert.Contains(
            "(keep-me)",
            serialized);
        Assert.Contains(
            "xref\n",
            serialized);
        Assert.Contains(
            "trailer\n",
            serialized);
        Assert.EndsWith(
            "%%EOF\n",
            serialized);
    }

    [Fact]
    public void FullRewriteCrossReferenceStreamSerializesThroughPublicSaveBoundary()
    {
        PdfDocument document =
            CreateNestedDocument();

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

        byte[] output =
            PdfEditingSaveService.Write(
                session,
                changeSet,
                new PdfWriteOptions
                {
                    Mode =
                        PdfWriteMode.FullRewrite,
                    CrossReferenceMode =
                        PdfCrossReferenceWriteMode.CrossReferenceStream
                });

        string serialized =
            Encoding.Latin1.GetString(
                output);

        Assert.Contains(
            "/Type /XRef",
            serialized);
        Assert.Contains(
            "/Rotate 180",
            serialized);
        Assert.DoesNotContain(
            "\ntrailer\n",
            serialized);
        Assert.EndsWith(
            "%%EOF\n",
            serialized);
    }

    [Fact]
    public void IncrementalTraditionalPreservesOriginalRevisionBytesAndUntargetedObjects()
    {
        PdfDocument document =
            CreateNestedDocument();

        var untouchedId =
            new PdfObjectIdentifier(20);
        PdfDictionary untouched =
            new()
            {
                {
                    PdfName.FromLatin1("Untouched"),
                    PdfString.FromLatin1("original")
                }
            };
        document.Objects.Add(
            untouchedId,
            untouched);

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

        byte[] sourceBytes =
            Encoding.ASCII.GetBytes(
                "%PDF-1.7\n% untouched-source-revision\nxref\n%%EOF\n");

        var source =
            new PdfMemorySourceRevision(
                sourceBytes,
                34,
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
                        PdfCrossReferenceWriteMode.TraditionalTable
                },
                source);

        Assert.True(
            output.AsSpan(
                    0,
                    sourceBytes.Length)
                .SequenceEqual(
                    sourceBytes));
        Assert.Same(
            untouched,
            document.Objects.Get<PdfDictionary>(
                untouchedId));

        string appended =
            Encoding.Latin1.GetString(
                output.AsSpan(
                    sourceBytes.Length));

        Assert.Contains(
            "/Rotate 270",
            appended);
        Assert.DoesNotContain(
            "(original)",
            appended);
        Assert.Contains(
            "/Prev 34",
            appended);
    }

    [Fact]
    public void IncrementalCrossReferenceStreamPreserveSourceUsesSourceRepresentation()
    {
        PdfDocument document =
            CreateNestedDocument();

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

        byte[] sourceBytes =
            Encoding.ASCII.GetBytes(
                "%PDF-1.7\n% source-xref-stream\n%%EOF\n");

        var source =
            new PdfMemorySourceRevision(
                sourceBytes,
                18,
                PdfCrossReferenceWriteMode.CrossReferenceStream);

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

        string appended =
            Encoding.Latin1.GetString(
                output.AsSpan(
                    sourceBytes.Length));

        Assert.Contains(
            "/Type /XRef",
            appended);
        Assert.Contains(
            "/Prev 18",
            appended);
        Assert.Contains(
            "/Rotate 90",
            appended);
    }

    [Fact]
    public void AnnotationTransactionSerializesEndToEndThroughPublicSaveBoundary()
    {
        PdfDocument document =
            CreateAnnotationDocument();

        using PdfEditingSession session =
            PdfEditingService.OpenSession(document);

        PdfEditChangeSet changeSet;
        using (var transaction = session.BeginTransaction())
        {
            _ = session.Document.SetAnnotationContents(
                session.Document.Pages[0],
                session.Document.Pages[0].Annotations[0],
                "AK end-to-end annotation");

            changeSet = transaction.Commit();
        }

        byte[] output =
            PdfEditingSaveService.Write(
                session,
                changeSet);

        string serialized =
            Encoding.Latin1.GetString(
                output);

        Assert.Contains(
            "(AK end-to-end annotation)",
            serialized);
        Assert.Contains(
            "/Subtype /Text",
            serialized);
        Assert.EndsWith(
            "%%EOF\n",
            serialized);
    }

    private static PdfDocument CreateNestedDocument()
    {
        PdfDocument document =
            PdfDocument.CreateEmpty();

        var catalogId =
            new PdfObjectIdentifier(1);
        var rootId =
            new PdfObjectIdentifier(2);
        var firstBranchId =
            new PdfObjectIdentifier(3);
        var secondBranchId =
            new PdfObjectIdentifier(4);
        var firstPageId =
            new PdfObjectIdentifier(5);
        var secondPageId =
            new PdfObjectIdentifier(6);

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
                [
                    firstBranchId,
                    secondBranchId
                ],
                2));
        document.Objects.Add(
            firstBranchId,
            CreatePagesDictionary(
                rootId,
                [firstPageId],
                1));
        document.Objects.Add(
            secondBranchId,
            CreatePagesDictionary(
                rootId,
                [secondPageId],
                1));

        AddBoundPage(
            document,
            firstPageId,
            firstBranchId);
        AddBoundPage(
            document,
            secondPageId,
            secondBranchId);

        document.Trailer.SetRootObject(
            catalogId);

        return document;
    }

    private static PdfDocument CreateAnnotationDocument()
    {
        PdfDocument document =
            PdfDocument.CreateEmpty();

        var catalogId =
            new PdfObjectIdentifier(1);
        var treeId =
            new PdfObjectIdentifier(2);
        var pageId =
            new PdfObjectIdentifier(3);
        var annotationId =
            new PdfObjectIdentifier(4);

        document.BindCoreStructure(
            catalogId,
            treeId);
        document.Objects.Add(
            catalogId,
            new PdfDictionary());
        document.Objects.Add(
            treeId,
            CreatePagesDictionary(
                null,
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

        var annotation =
            new PdfAnnotation(
                PdfAnnotationSubtype.Text,
                new PdfAnnotationRectangle(
                    0,
                    0,
                    100,
                    100));
        annotation.BindObjectIdentifier(
            annotationId);
        page.InsertAnnotation(
            0,
            annotation);

        PdfDictionary pageDictionary =
            CreatePageDictionary(
                treeId);
        pageDictionary.Set(
            AnnotsName,
            new PdfArray(
            [
                new PdfIndirectReference(
                    annotationId)
            ]));

        document.Objects.Add(
            pageId,
            pageDictionary);
        document.Objects.Add(
            annotationId,
            new PdfDictionary
            {
                {
                    TypeName,
                    PdfName.FromLatin1("Annot")
                },
                {
                    PdfName.FromLatin1("Subtype"),
                    PdfName.FromLatin1("Text")
                },
                {
                    PdfName.FromLatin1("Rect"),
                    new PdfArray(
                    [
                        new PdfInteger(0),
                        new PdfInteger(0),
                        new PdfInteger(100),
                        new PdfInteger(100)
                    ])
                }
            });

        document.Trailer.SetRootObject(
            catalogId);

        return document;
    }

    private static void AddBoundPage(
        PdfDocument document,
        PdfObjectIdentifier pageId,
        PdfObjectIdentifier parentId)
    {
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
            CreatePageDictionary(
                parentId));
    }

    private static PdfDictionary CreatePageDictionary(
        PdfObjectIdentifier parent)
    {
        return new PdfDictionary
        {
            {
                TypeName,
                PageName
            },
            {
                ParentName,
                new PdfIndirectReference(
                    parent)
            },
            {
                MediaBoxName,
                new PdfArray(
                [
                    new PdfInteger(0),
                    new PdfInteger(0),
                    new PdfInteger(612),
                    new PdfInteger(792)
                ])
            }
        };
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
                    new PdfInteger(
                        count)
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
