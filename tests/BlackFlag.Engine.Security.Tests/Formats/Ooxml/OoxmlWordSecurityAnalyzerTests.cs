using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using BlackFlag.Engine.Security.Formats.Ooxml;
using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Tests.Formats.Ooxml;

public sealed class OoxmlWordSecurityAnalyzerTests
{
    [Fact]
    public void CleanDocxProducesNoSecurityObservations()
    {
        byte[] document =
            CreatePackage(
                ("[Content_Types].xml", ContentTypesXml()),
                ("word/document.xml", DocumentXml("Normal text")));

        OoxmlWordSecurityAnalysisResult result =
            new OoxmlWordSecurityAnalyzer()
                .Analyze(
                    document,
                    "clean.docx");

        Assert.Empty(result.Observations);
    }

    [Fact]
    public void DocmVbaProjectIsDetectedAsScriptContent()
    {
        byte[] document =
            CreatePackage(
                ("[Content_Types].xml", MacroContentTypesXml()),
                ("word/document.xml", DocumentXml("Text")),
                ("word/vbaProject.bin", OleFixture()));

        OoxmlWordSecurityAnalysisResult result =
            new OoxmlWordSecurityAnalyzer()
                .Analyze(
                    document,
                    "macro.docm");

        Assert.Contains(
            result.Observations,
            observation =>
                observation.Category ==
                    DocumentSecurityObservationCategory.ScriptContent &&
                observation.Kind == "VbaProject");
    }

    [Fact]
    public void ExternalRelationshipIsDetectedWithoutFollowingIt()
    {
        byte[] document =
            CreatePackage(
                ("[Content_Types].xml", ContentTypesXml()),
                ("word/document.xml", DocumentXml("Text")),
                ("word/_rels/document.xml.rels", RelationshipsXml(
                    """
                    <Relationship
                        Id="rId9"
                        Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink"
                        Target="https://example.invalid/resource"
                        TargetMode="External" />
                    """)));

        OoxmlWordSecurityAnalysisResult result =
            new OoxmlWordSecurityAnalyzer()
                .Analyze(
                    document,
                    "external.docx");

        DocumentSecurityObservation observation =
            Assert.Single(
                result.Observations,
                item =>
                    item.Kind == "ExternalRelationship");

        Assert.Equal(
            DocumentSecurityObservationCategory.ExternalReference,
            observation.Category);

        Assert.Equal(
            "https://example.invalid/resource",
            observation.Properties["Target"]);
    }

    [Fact]
    public void DdeFieldIsDetectedAsActiveContent()
    {
        byte[] document =
            CreatePackage(
                ("[Content_Types].xml", ContentTypesXml()),
                ("word/document.xml",
                    Encoding.UTF8.GetBytes(
                        """
                        <?xml version="1.0" encoding="UTF-8"?>
                        <w:document
                            xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                        <w:body>
                            <w:p>
                            <w:r>
                                <w:instrText>DDEAUTO "cmd.exe" "/c harmless-fixture"</w:instrText>
                            </w:r>
                            </w:p>
                        </w:body>
                        </w:document>
                        """)));

        OoxmlWordSecurityAnalysisResult result =
            new OoxmlWordSecurityAnalyzer()
                .Analyze(
                    document,
                    "dde.docx");

        DocumentSecurityObservation observation =
            Assert.Single(
                result.Observations,
                item =>
                    item.Kind == "DdeField");

        Assert.Equal(
            DocumentSecurityObservationCategory.ActiveContent,
            observation.Category);
    }

    [Fact]
    public void ActiveXPartIsDetected()
    {
        byte[] document =
            CreatePackage(
                ("[Content_Types].xml", ContentTypesXml()),
                ("word/document.xml", DocumentXml("Text")),
                ("word/activeX/activeX1.bin", OleFixture()));

        OoxmlWordSecurityAnalysisResult result =
            new OoxmlWordSecurityAnalyzer()
                .Analyze(
                    document,
                    "activex.docx");

        Assert.Contains(
            result.Observations,
            observation =>
                observation.Kind == "ActiveXControl" &&
                observation.Category ==
                    DocumentSecurityObservationCategory.ActiveContent);
    }

    [Fact]
    public void EmbeddedExecutableUsesSharedRecursiveClassifier()
    {
        byte[] executable = PortableExecutableFixture();

        byte[] document =
            CreatePackage(
                ("[Content_Types].xml", ContentTypesXml()),
                ("word/document.xml", DocumentXml("Text")),
                ("word/embeddings/payload.exe", executable));

        OoxmlWordSecurityAnalysisResult result =
            new OoxmlWordSecurityAnalyzer()
                .Analyze(
                    document,
                    "embedded.docx");

        Assert.Contains(
            result.Observations,
            observation =>
                observation.Category ==
                    DocumentSecurityObservationCategory.ExecutableContent &&
                observation.Kind == "EmbeddedExecutableContent");
    }

    [Fact]
    public void NestedDocmUsesSameRecursiveEngineAndFindsMacro()
    {
        byte[] nested =
            CreatePackage(
                ("[Content_Types].xml", MacroContentTypesXml()),
                ("word/document.xml", DocumentXml("Nested")),
                ("word/vbaProject.bin", OleFixture()));

        byte[] outer =
            CreatePackage(
                ("[Content_Types].xml", ContentTypesXml()),
                ("word/document.xml", DocumentXml("Outer")),
                ("word/embeddings/nested.docm", nested));

        var budget = new DocumentSecurityInspectionBudget();

        OoxmlWordSecurityAnalysisResult result =
            new OoxmlWordSecurityAnalyzer()
                .Analyze(
                    outer,
                    "outer.docx",
                    budget);

        Assert.Contains(
            result.Observations,
            observation =>
                observation.Kind == "VbaProject" &&
                observation.Location?.LogicalPath is not null);

        Assert.True(budget.EmbeddedObjects >= 2);
    }

    [Fact]
    public void SharedCompressionRatioLimitIsEnforcedBeforeExpansion()
    {
        byte[] largeXml =
            DocumentXml(
                new string(
                    'A',
                    64 * 1024));

        byte[] document =
            CreatePackage(
                ("[Content_Types].xml", ContentTypesXml()),
                ("word/document.xml", largeXml));

        var budget =
            new DocumentSecurityInspectionBudget(
                new DocumentSecurityInspectionLimits
                {
                    MaximumCompressionRatio = 2
                });

        Assert.Throws<InvalidDataException>(
            () => new OoxmlWordSecurityAnalyzer()
                .Analyze(
                    document,
                    "compressed.docx",
                    budget));
    }

    [Fact]
    public void NonWordOoxmlFileNameIsRejected()
    {
        byte[] document =
            CreatePackage(
                ("[Content_Types].xml", ContentTypesXml()));

        Assert.Throws<ArgumentException>(
            () => new OoxmlWordSecurityAnalyzer()
                .Analyze(
                    document,
                    "workbook.xlsx"));
    }

    [Fact]
    public void CancellationStopsInspection()
    {
        byte[] document =
            CreatePackage(
                ("[Content_Types].xml", ContentTypesXml()),
                ("word/document.xml", DocumentXml("Text")));

        using var source =
            new CancellationTokenSource();

        source.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => new OoxmlWordSecurityAnalyzer()
                .Analyze(
                    document,
                    "cancelled.docx",
                    cancellationToken: source.Token));
    }

    private static byte[] CreatePackage(
        params (string Name, byte[] Content)[] entries)
    {
        using var output = new MemoryStream();

        using (var archive =
            new ZipArchive(
                output,
                ZipArchiveMode.Create,
                leaveOpen: true))
        {
            foreach ((string name, byte[] content) in entries)
            {
                ZipArchiveEntry entry =
                    archive.CreateEntry(
                        name,
                        CompressionLevel.Optimal);

                using Stream stream =
                    entry.Open();

                stream.Write(content);
            }
        }

        return output.ToArray();
    }

    private static byte[] ContentTypesXml() =>
        Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
            <Default Extension="xml" ContentType="application/xml" />
            <Override
                PartName="/word/document.xml"
                ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml" />
            </Types>
            """);

    private static byte[] MacroContentTypesXml() =>
        Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
            <Default Extension="xml" ContentType="application/xml" />
            <Override
                PartName="/word/document.xml"
                ContentType="application/vnd.ms-word.document.macroEnabled.main+xml" />
            <Override
                PartName="/word/vbaProject.bin"
                ContentType="application/vnd.ms-office.vbaProject" />
            </Types>
            """);

    private static byte[] DocumentXml(string text) =>
        Encoding.UTF8.GetBytes(
            $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <w:document
                xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
            <w:body>
                <w:p>
                <w:r>
                    <w:t>{text}</w:t>
                </w:r>
                </w:p>
            </w:body>
            </w:document>
            """);

    private static byte[] RelationshipsXml(string relationship) =>
        Encoding.UTF8.GetBytes(
            $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <Relationships
                xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
            {relationship}
            </Relationships>
            """);

    private static byte[] OleFixture() =>
        [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

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
