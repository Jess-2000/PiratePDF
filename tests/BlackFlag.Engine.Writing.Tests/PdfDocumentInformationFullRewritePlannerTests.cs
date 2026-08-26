using System.Text;
using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Editing.Documents;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfDocumentInformationFullRewritePlannerTests
{
    private static readonly PdfName TitleName = PdfName.FromLatin1("Title");

    [Fact]
    public void CreateAllocatesMissingInformationDictionaryAndRefreshesPlan()
    {
        PdfDocument document = CreateDocument();
        using var session = new PdfEditingSession(document);
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Title,
            "Created Info");

        PdfWritePlan basePlan = PdfWritePlanner.Create(document);
        int expectedObjectNumber = basePlan.NextObjectNumber;

        PdfWritePlan plan =
            PdfDocumentInformationFullRewritePlanner.Create(
                basePlan,
                [PdfDocumentInformationField.Title]);

        PdfObjectIdentifier identifier =
            Assert.IsType<PdfObjectIdentifier>(
                document.Trailer.InformationObject);

        Assert.Equal(expectedObjectNumber, identifier.ObjectNumber);
        Assert.Contains(identifier, plan.ExistingObjectIdentifiers);
        Assert.Equal(identifier.ObjectNumber + 1, plan.NextObjectNumber);

        PdfDictionary dictionary =
            document.Objects.Get<PdfDictionary>(identifier);
        Assert.Equal(
            "Created Info",
            dictionary.Get<PdfString>(TitleName).GetLatin1Text());
    }

    [Fact]
    public void CreateUsesExistingInformationDictionaryWithoutAllocatingReplacement()
    {
        PdfDocument document = CreateDocument();
        var informationIdentifier = new PdfObjectIdentifier(7);
        document.Objects.Add(informationIdentifier, new PdfDictionary());
        document.Trailer.SetInformationObject(informationIdentifier);

        using var session = new PdfEditingSession(document);
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Title,
            "Updated");

        PdfWritePlan plan =
            PdfDocumentInformationFullRewritePlanner.Create(
                PdfWritePlanner.Create(document),
                [PdfDocumentInformationField.Title]);

        Assert.Equal(
            informationIdentifier,
            document.Trailer.InformationObject);
        Assert.Contains(informationIdentifier, plan.ExistingObjectIdentifiers);
        Assert.Equal(
            "Updated",
            document.Objects
                .Get<PdfDictionary>(informationIdentifier)
                .Get<PdfString>(TitleName)
                .GetLatin1Text());
    }

    [Fact]
    public void CreateDoesNotCreateEmptyInformationDictionaryForAbsentTargets()
    {
        PdfDocument document = CreateDocument();
        PdfWritePlan basePlan = PdfWritePlanner.Create(document);

        Assert.Throws<InvalidOperationException>(
            () => PdfDocumentInformationFullRewritePlanner.Create(
                basePlan,
                [PdfDocumentInformationField.Title]));

        Assert.Null(document.Trailer.InformationObject);
        Assert.Equal(1, document.Objects.Count);
    }

    [Fact]
    public void CreateRejectsIncrementalBasePlanWithoutMutatingTrailer()
    {
        PdfDocument document = CreateDocument();
        using var session = new PdfEditingSession(document);
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Title,
            "Not Written");

        PdfWritePlan basePlan = PdfWritePlanner.Create(
            document,
            new PdfWriteOptions
            {
                Mode = PdfWriteMode.IncrementalUpdate
            });

        Assert.Throws<InvalidOperationException>(
            () => PdfDocumentInformationFullRewritePlanner.Create(
                basePlan,
                [PdfDocumentInformationField.Title]));

        Assert.Null(document.Trailer.InformationObject);
        Assert.Equal(1, document.Objects.Count);
    }

    [Fact]
    public void FullRewriteSerializesNewInformationDictionaryAndTrailerReference()
    {
        PdfDocument document = CreateDocument();
        using var session = new PdfEditingSession(document);
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Title,
            "Serialized Info");

        PdfWritePlan plan =
            PdfDocumentInformationFullRewritePlanner.Create(
                PdfWritePlanner.Create(document),
                [PdfDocumentInformationField.Title]);

        PdfObjectIdentifier identifier =
            Assert.IsType<PdfObjectIdentifier>(
                document.Trailer.InformationObject);
        string text =
            Encoding.Latin1.GetString(
                PdfFullRewriteWriter.Write(plan));

        Assert.Contains(
            $"{identifier.ObjectNumber} 0 obj\n<< /Title (Serialized Info) >>\nendobj\n",
            text,
            StringComparison.Ordinal);
        Assert.Contains(
            $"/Info {identifier.ObjectNumber} 0 R",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CrossReferenceStreamFullRewriteCarriesNewInformationReference()
    {
        PdfDocument document = CreateDocument();
        using var session = new PdfEditingSession(document);
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Title,
            "Stream Info");

        PdfWritePlan plan =
            PdfDocumentInformationFullRewritePlanner.Create(
                PdfWritePlanner.Create(
                    document,
                    new PdfWriteOptions
                    {
                        CrossReferenceMode =
                            PdfCrossReferenceWriteMode.CrossReferenceStream
                    }),
                [PdfDocumentInformationField.Title]);

        PdfObjectIdentifier identifier =
            Assert.IsType<PdfObjectIdentifier>(
                document.Trailer.InformationObject);
        string text =
            Encoding.Latin1.GetString(
                PdfFullRewriteWriter.Write(plan));

        Assert.Contains(
            $"/Info {identifier.ObjectNumber} 0 R",
            text,
            StringComparison.Ordinal);
    }

    private static PdfDocument CreateDocument()
    {
        PdfDocument document = PdfDocument.CreateEmpty(PdfVersion.Pdf17);
        var rootIdentifier = new PdfObjectIdentifier(1);
        document.Objects.Add(rootIdentifier, new PdfDictionary());
        document.Trailer.SetRootObject(rootIdentifier);
        return document;
    }
}
