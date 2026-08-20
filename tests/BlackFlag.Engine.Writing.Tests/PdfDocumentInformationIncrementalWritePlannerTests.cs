using System.Text;

using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Editing.Documents;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfDocumentInformationIncrementalWritePlannerTests
{
    private static readonly PdfName TitleName = PdfName.FromLatin1("Title");

    [Fact]
    public void CreateAllocatesMissingInformationDictionaryAndUpdatesTrailer()
    {
        PdfDocument document = CreateDocument(PdfVersion.Pdf17);
        using var session = new PdfEditingSession(document);
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Title,
            "Created Info");

        PdfWritePlan writePlan = CreateWritePlan(document);
        int expectedObjectNumber = writePlan.NextObjectNumber;

        PdfIncrementalWritePlan plan =
            PdfDocumentInformationIncrementalWritePlanner.Create(
                writePlan,
                CreateSource(),
                [PdfDocumentInformationField.Title]);

        PdfObjectIdentifier identifier =
            Assert.IsType<PdfObjectIdentifier>(
                document.Trailer.InformationObject);
        Assert.Equal(expectedObjectNumber, identifier.ObjectNumber);
        Assert.Equal(0, identifier.GenerationNumber);
        Assert.Contains(identifier, plan.ChangedObjectIdentifiers);

        PdfDictionary dictionary =
            document.Objects.Get<PdfDictionary>(identifier);
        Assert.Equal(
            "Created Info",
            dictionary.Get<PdfString>(TitleName).GetLatin1Text());
    }

    [Fact]
    public void CreateUsesExistingInformationDictionaryWithoutAllocatingReplacement()
    {
        PdfDocument document = CreateDocument(PdfVersion.Pdf17);
        var informationIdentifier = new PdfObjectIdentifier(7);
        document.Objects.Add(informationIdentifier, new PdfDictionary());
        document.Trailer.SetInformationObject(informationIdentifier);

        using var session = new PdfEditingSession(document);
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Title,
            "Updated");

        PdfWritePlan writePlan = CreateWritePlan(document);
        PdfIncrementalWritePlan plan =
            PdfDocumentInformationIncrementalWritePlanner.Create(
                writePlan,
                CreateSource(),
                [PdfDocumentInformationField.Title]);

        Assert.Equal(
            informationIdentifier,
            document.Trailer.InformationObject);
        Assert.Equal(
            [informationIdentifier],
            plan.ChangedObjectIdentifiers);
    }

    [Fact]
    public void CreateRollsBackNewInformationReferenceWhenPlanningFails()
    {
        PdfDocument document = CreateDocument(new PdfVersion(1, 4));
        using var session = new PdfEditingSession(document);
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Title,
            "Rollback");

        PdfWritePlan writePlan = PdfWritePlanner.Create(
            document,
            new PdfWriteOptions
            {
                Mode = PdfWriteMode.IncrementalUpdate,
                CrossReferenceMode = PdfCrossReferenceWriteMode.CrossReferenceStream
            });
        var allocatedIdentifier =
            new PdfObjectIdentifier(writePlan.NextObjectNumber);

        Assert.Throws<NotSupportedException>(
            () => PdfDocumentInformationIncrementalWritePlanner.Create(
                writePlan,
                CreateSource(),
                [PdfDocumentInformationField.Title]));

        Assert.Null(document.Trailer.InformationObject);
        Assert.False(document.Objects.Contains(allocatedIdentifier));
    }

    [Fact]
    public void CreateDoesNotCreateEmptyInformationDictionaryForAbsentTargets()
    {
        PdfDocument document = CreateDocument(PdfVersion.Pdf17);
        PdfWritePlan writePlan = CreateWritePlan(document);

        Assert.Throws<InvalidOperationException>(
            () => PdfDocumentInformationIncrementalWritePlanner.Create(
                writePlan,
                CreateSource(),
                [PdfDocumentInformationField.Title]));

        Assert.Null(document.Trailer.InformationObject);
        Assert.Equal(1, document.Objects.Count);
    }

    private static PdfDocument CreateDocument(PdfVersion version)
    {
        PdfDocument document = PdfDocument.CreateEmpty(version);
        var rootIdentifier = new PdfObjectIdentifier(1);
        document.Objects.Add(rootIdentifier, new PdfDictionary());
        document.Trailer.SetRootObject(rootIdentifier);
        return document;
    }

    private static PdfWritePlan CreateWritePlan(PdfDocument document) =>
        PdfWritePlanner.Create(
            document,
            new PdfWriteOptions
            {
                Mode = PdfWriteMode.IncrementalUpdate,
                CrossReferenceMode = PdfCrossReferenceWriteMode.TraditionalTable
            });

    private static PdfMemorySourceRevision CreateSource() =>
        new(
            Encoding.ASCII.GetBytes("%PDF-1.7\nxref\n%%EOF\n"),
            9,
            PdfCrossReferenceWriteMode.TraditionalTable);
}
