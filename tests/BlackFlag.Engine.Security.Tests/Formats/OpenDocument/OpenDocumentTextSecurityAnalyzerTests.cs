using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlackFlag.Engine.Security.Formats.OpenDocument;
using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Tests.Formats.OpenDocument;

public sealed class OpenDocumentTextSecurityAnalyzerTests
{
    [Fact]
    public void CleanOdtProducesNoSecurityObservations()
    {
        byte[] document = CreatePackage(
            ("mimetype", Encoding.UTF8.GetBytes("application/vnd.oasis.opendocument.text")),
            ("content.xml", Xml("<office:body><office:text /></office:body>")));

        OpenDocumentTextSecurityAnalysisResult result =
            new OpenDocumentTextSecurityAnalyzer().Analyze(document, "clean.odt");

        Assert.Empty(result.Observations);
    }

    [Fact]
    public void EmbeddedBasicMacroIsDetected()
    {
        byte[] document = CreatePackage(
            ("mimetype", Encoding.UTF8.GetBytes("application/vnd.oasis.opendocument.text")),
            ("content.xml", Xml("<office:body><office:text /></office:body>")),
            ("Basic/Standard/Module1.xba", Encoding.UTF8.GetBytes("<script>harmless fixture</script>")));

        OpenDocumentTextSecurityAnalysisResult result =
            new OpenDocumentTextSecurityAnalyzer().Analyze(document, "macro.odt");

        Assert.Contains(
            result.Observations,
            item => item.Category == DocumentSecurityObservationCategory.ScriptContent &&
                    item.Kind == "EmbeddedScript");
    }

    [Fact]
    public void ExternalReferenceIsDetectedWithoutDereferencing()
    {
        byte[] document = CreatePackage(
            ("mimetype", Encoding.UTF8.GetBytes("application/vnd.oasis.opendocument.text")),
            ("content.xml", Xml(
                """<office:body><office:text><text:a xlink:href="https://example.invalid/resource">link</text:a></office:text></office:body>""")));

        OpenDocumentTextSecurityAnalysisResult result =
            new OpenDocumentTextSecurityAnalyzer().Analyze(document, "external.odt");

        DocumentSecurityObservation observation =
            Assert.Single(result.Observations, item => item.Kind == "ExternalReference");

        Assert.Equal(
            "https://example.invalid/resource",
            observation.Properties["Target"]);
    }

    [Fact]
    public void ScriptEventListenerIsDetected()
    {
        byte[] document = CreatePackage(
            ("mimetype", Encoding.UTF8.GetBytes("application/vnd.oasis.opendocument.text")),
            ("content.xml", Xml(
                """<office:body><office:text><script:event-listener script:event-name="dom:load" /></office:text></office:body>""")));

        OpenDocumentTextSecurityAnalysisResult result =
            new OpenDocumentTextSecurityAnalyzer().Analyze(document, "event.odt");

        Assert.Contains(
            result.Observations,
            item => item.Category == DocumentSecurityObservationCategory.ActiveContent &&
                    item.Kind == "ScriptEvent");
    }

    [Fact]
    public void EmbeddedExecutableUsesSharedRecursiveClassifier()
    {
        byte[] document = CreatePackage(
            ("mimetype", Encoding.UTF8.GetBytes("application/vnd.oasis.opendocument.text")),
            ("content.xml", Xml("<office:body><office:text /></office:body>")),
            ("Object 1/payload.exe", PortableExecutableFixture()));

        OpenDocumentTextSecurityAnalysisResult result =
            new OpenDocumentTextSecurityAnalyzer().Analyze(document, "embedded.odt");

        Assert.Contains(
            result.Observations,
            item => item.Category == DocumentSecurityObservationCategory.ExecutableContent &&
                    item.Kind == "EmbeddedExecutableContent");
    }

    [Fact]
    public void CompressionRatioLimitIsEnforcedBeforeXmlExpansion()
    {
        byte[] content = Xml(
            $"<office:body><office:text><text:p>{new string('A', 64 * 1024)}</text:p></office:text></office:body>");

        byte[] document = CreatePackage(
            ("mimetype", Encoding.UTF8.GetBytes("application/vnd.oasis.opendocument.text")),
            ("content.xml", content));

        var budget = new DocumentSecurityInspectionBudget(
            new DocumentSecurityInspectionLimits
            {
                MaximumCompressionRatio = 2
            });

        Assert.Throws<InvalidDataException>(
            () => new OpenDocumentTextSecurityAnalyzer()
                .Analyze(document, "compressed.odt", budget));
    }

    [Fact]
    public void NonOdtFileNameIsRejected()
    {
        byte[] document = CreatePackage(
            ("mimetype", Encoding.UTF8.GetBytes("application/vnd.oasis.opendocument.text")));

        Assert.Throws<ArgumentException>(
            () => new OpenDocumentTextSecurityAnalyzer()
                .Analyze(document, "spreadsheet.ods"));
    }

    [Fact]
    public async Task CancellationStopsInspection()
    {
        byte[] document = CreatePackage(
            ("mimetype", Encoding.UTF8.GetBytes("application/vnd.oasis.opendocument.text")),
            ("content.xml", Xml("<office:body><office:text /></office:body>")));

        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => Task.FromResult(new OpenDocumentTextSecurityAnalyzer()
                .Analyze(document, "cancelled.odt", cancellationToken: source.Token)));
    }

    private static byte[] CreatePackage(params (string Name, byte[] Content)[] entries)
    {
        using var output = new MemoryStream();

        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach ((string name, byte[] content) in entries)
            {
                ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Optimal);
                using Stream stream = entry.Open();
                stream.Write(content);
            }
        }

        return output.ToArray();
    }

    private static byte[] Xml(string body) =>
        Encoding.UTF8.GetBytes(
            $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <office:document-content
                xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
                xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0"
                xmlns:script="urn:oasis:names:tc:opendocument:xmlns:script:1.0"
                xmlns:xlink="http://www.w3.org/1999/xlink">
            {body}
            </office:document-content>
            """);

    private static byte[] PortableExecutableFixture()
    {
        byte[] bytes = new byte[128];
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        bytes[0x3C] = 0x40;
        bytes[0x40] = (byte)'P';
        bytes[0x41] = (byte)'E';
        return bytes;
    }
}
