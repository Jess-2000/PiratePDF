using BlackFlag.Engine.Security.Containers;

namespace BlackFlag.Engine.Security.Tests.Containers;

public sealed class DocumentEmbeddedObjectClassifierTests
{
    private readonly DocumentEmbeddedObjectClassifier classifier = new();

    [Fact]
    public void PdfSignatureWinsOverMisleadingFileName()
    {
        var embedded = new DocumentEmbeddedObject(
            "%PDF-1.7\n"u8.ToArray(),
            "/embedded/item",
            declaredFileName: "not-a-pdf.exe");

        DocumentEmbeddedObjectClassification result = classifier.Classify(embedded);

        Assert.Equal(DocumentEmbeddedObjectKind.Pdf, result.Kind);
        Assert.True(result.IsContainer);
        Assert.False(result.IsExecutable);
    }

    [Fact]
    public void OoxmlZipUsesDeclaredFormatHint()
    {
        var embedded = new DocumentEmbeddedObject(
            new byte[] { 0x50, 0x4B, 0x03, 0x04 },
            "/embedded/document.docm",
            declaredFileName: "document.docm");

        DocumentEmbeddedObjectClassification result = classifier.Classify(embedded);

        Assert.Equal(DocumentEmbeddedObjectKind.OoxmlPackage, result.Kind);
        Assert.True(result.IsContainer);
    }

    [Fact]
    public void OleCompoundDocumentIsRecognizedBySignature()
    {
        byte[] bytes = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

        DocumentEmbeddedObjectClassification result =
            classifier.Classify(new DocumentEmbeddedObject(bytes, "/embedded/ole"));

        Assert.Equal(DocumentEmbeddedObjectKind.OleCompoundDocument, result.Kind);
    }

    [Fact]
    public void PortableExecutableIsClassifiedAsExecutable()
    {
        byte[] bytes = new byte[128];
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        bytes[0x3C] = 0x40;
        bytes[0x40] = (byte)'P';
        bytes[0x41] = (byte)'E';

        DocumentEmbeddedObjectClassification result =
            classifier.Classify(new DocumentEmbeddedObject(bytes, "/embedded/program.exe"));

        Assert.Equal(DocumentEmbeddedObjectKind.Executable, result.Kind);
        Assert.True(result.IsExecutable);
    }

    [Fact]
    public void ScriptNameIsRecognizedWithoutExecution()
    {
        DocumentEmbeddedObjectClassification result =
            classifier.Classify(
                new DocumentEmbeddedObject(
                    "Write-Output 'fixture'"u8.ToArray(),
                    "/embedded/script.ps1",
                    declaredFileName: "script.ps1"));

        Assert.Equal(DocumentEmbeddedObjectKind.Script, result.Kind);
        Assert.True(result.IsExecutable);
    }

    [Fact]
    public void RtfIsClassifiedAsInspectableContainer()
    {
        DocumentEmbeddedObjectClassification result =
            new DocumentEmbeddedObjectClassifier()
                .Classify(
                    new DocumentEmbeddedObject(
                        "{\\rtf1 clean}"u8.ToArray(),
                        "/document.rtf",
                        declaredFileName: "document.rtf"));

        Assert.Equal(DocumentEmbeddedObjectKind.Rtf, result.Kind);
        Assert.True(result.IsContainer);
    }

    [Fact]
    public void MarkdownFileNameIsClassifiedAsInspectableContainer()
    {
        DocumentEmbeddedObjectClassification result =
            new DocumentEmbeddedObjectClassifier()
                .Classify(
                    new DocumentEmbeddedObject(
                        "# heading"u8.ToArray(),
                        "/document.md",
                        declaredFileName: "document.md"));

        Assert.Equal(DocumentEmbeddedObjectKind.Markdown, result.Kind);
        Assert.True(result.IsContainer);
    }

}
