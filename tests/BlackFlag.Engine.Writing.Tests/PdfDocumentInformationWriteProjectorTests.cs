using BlackFlag.Engine.Document.Metadata;
using BlackFlag.Engine.Document.Model;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Document.Objects.Primitives;
using BlackFlag.Engine.Editing.Documents;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class PdfDocumentInformationWriteProjectorTests
{
    private static readonly PdfName TitleName = PdfName.FromLatin1("Title");
    private static readonly PdfName AuthorName = PdfName.FromLatin1("Author");
    private static readonly PdfName ModificationDateName = PdfName.FromLatin1("ModDate");

    [Fact]
    public void CreateProjectsTargetedFieldAndPreservesUntargetedEntries()
    {
        PdfDocument document = CreateDocumentWithInformationDictionary();
        PdfDictionary original = document.Objects.Get<PdfDictionary>(
            document.Trailer.InformationObject!.Value);
        PdfName customName = PdfName.FromLatin1("FusionProPrivate");
        var customValue = PdfString.FromLatin1("preserve-me");
        original.Add(customName, customValue);
        original.Add(AuthorName, PdfString.FromLatin1("Original Author"));

        using var session = new PdfEditingSession(document);
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Title,
            "Revised Title");

        PdfObjectWriteProjectionSet projections =
            PdfDocumentInformationWriteProjector.Create(
                document,
                [PdfDocumentInformationField.Title]);

        PdfDictionary projected = Assert.IsType<PdfDictionary>(
            Assert.Single(projections.Projections).Value);

        Assert.Equal(
            "Revised Title",
            projected.Get<PdfString>(TitleName).GetLatin1Text());
        Assert.Equal(
            "Original Author",
            projected.Get<PdfString>(AuthorName).GetLatin1Text());
        Assert.Same(customValue, projected.Get(customName));
        Assert.False(original.ContainsKey(TitleName));
    }

    [Fact]
    public void CreateRemovesOnlyExplicitlyTargetedAbsentField()
    {
        PdfDocument document = CreateDocumentWithInformationDictionary();
        PdfDictionary original = document.Objects.Get<PdfDictionary>(
            document.Trailer.InformationObject!.Value);
        original.Add(AuthorName, PdfString.FromLatin1("Original Author"));
        original.Add(TitleName, PdfString.FromLatin1("Original Title"));

        PdfObjectWriteProjectionSet projections =
            PdfDocumentInformationWriteProjector.Create(
                document,
                [PdfDocumentInformationField.Author]);

        PdfDictionary projected = Assert.IsType<PdfDictionary>(
            Assert.Single(projections.Projections).Value);

        Assert.False(projected.ContainsKey(AuthorName));
        Assert.Equal(
            "Original Title",
            projected.Get<PdfString>(TitleName).GetLatin1Text());
    }

    [Fact]
    public void CreateUsesPdfModDateKeyForModificationDate()
    {
        PdfDocument document = CreateDocumentWithInformationDictionary();

        using var session = new PdfEditingSession(document);
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.ModificationDate,
            "D:20260816120000-04'00'");

        PdfDictionary projected = Assert.IsType<PdfDictionary>(
            Assert.Single(
                PdfDocumentInformationWriteProjector.Create(
                    document,
                    [PdfDocumentInformationField.ModificationDate])
                .Projections)
            .Value);

        Assert.Equal(
            "D:20260816120000-04'00'",
            projected.Get<PdfString>(ModificationDateName).GetLatin1Text());
    }

    [Fact]
    public void CreateEncodesNonAsciiTextAsUtf16BigEndianHexString()
    {
        PdfDocument document = CreateDocumentWithInformationDictionary();

        using var session = new PdfEditingSession(document);
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Title,
            "Résumé — 海");

        PdfDictionary projected = Assert.IsType<PdfDictionary>(
            Assert.Single(
                PdfDocumentInformationWriteProjector.Create(
                    document,
                    [PdfDocumentInformationField.Title])
                .Projections)
            .Value);

        PdfString value = projected.Get<PdfString>(TitleName);
        byte[] bytes = value.ToByteArray();

        Assert.Equal(PdfStringFormat.Hexadecimal, value.Format);
        Assert.Equal(0xFE, bytes[0]);
        Assert.Equal(0xFF, bytes[1]);
    }

    [Fact]
    public void CreatePreservesExplicitEmptyString()
    {
        PdfDocument document = CreateDocumentWithInformationDictionary();

        using var session = new PdfEditingSession(document);
        _ = session.Document.SetDocumentInformation(
            PdfDocumentInformationField.Title,
            string.Empty);

        PdfDictionary projected = Assert.IsType<PdfDictionary>(
            Assert.Single(
                PdfDocumentInformationWriteProjector.Create(
                    document,
                    [PdfDocumentInformationField.Title])
                .Projections)
            .Value);

        PdfString value = projected.Get<PdfString>(TitleName);
        Assert.True(value.IsEmpty);
        Assert.Equal(PdfStringFormat.Literal, value.Format);
    }

    [Fact]
    public void CreateRejectsMissingInformationObject()
    {
        PdfDocument document = PdfDocument.CreateEmpty();

        Assert.Throws<NotSupportedException>(
            () => PdfDocumentInformationWriteProjector.Create(
                document,
                [PdfDocumentInformationField.Title]));
    }

    [Fact]
    public void CreateRejectsNonDictionaryInformationObject()
    {
        PdfDocument document = PdfDocument.CreateEmpty();
        var identifier = new PdfObjectIdentifier(7);
        document.Objects.Add(identifier, PdfString.FromLatin1("not-a-dictionary"));
        document.Trailer.SetInformationObject(identifier);

        Assert.Throws<InvalidOperationException>(
            () => PdfDocumentInformationWriteProjector.Create(
                document,
                [PdfDocumentInformationField.Title]));
    }

    private static PdfDocument CreateDocumentWithInformationDictionary()
    {
        PdfDocument document = PdfDocument.CreateEmpty();
        var rootIdentifier = new PdfObjectIdentifier(1);
        var informationIdentifier = new PdfObjectIdentifier(7);

        document.Objects.Add(rootIdentifier, new PdfDictionary());
        document.Objects.Add(informationIdentifier, new PdfDictionary());
        document.Trailer.SetRootObject(rootIdentifier);
        document.Trailer.SetInformationObject(informationIdentifier);
        return document;
    }
}
