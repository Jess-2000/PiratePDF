using System;
using System.IO;
using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Policy;
using BlackFlag.Engine.Security.Reporting;

namespace BlackFlag.Engine.Security.Tests.Reporting;

public sealed class DocumentSecurityTextReportWriterTests
{
    [Fact]
    public void WriterProducesDeterministicHumanReadableFinding()
    {
        var assessment =
            new DocumentSecurityAssessmentEngine()
                .Evaluate(
                [
                    new(
                        DocumentSecurityObservationCategory.ScriptContent,
                        "JavaScript",
                        "The document contains JavaScript.",
                        "pdf",
                        new DocumentSecuritySourceLocation
                        {
                            LogicalPath = "/Catalog/Names"
                        })
                ],
                DocumentSecurityOperation.ManualInspection);

        DocumentSecurityReport report =
            Assert.IsType<DocumentSecurityReport>(
                DocumentSecurityReportFactory.Create(
                    assessment,
                    DateTimeOffset.UnixEpoch));

        using var writer = new StringWriter(
            System.Globalization.CultureInfo.InvariantCulture);

        DocumentSecurityTextReportWriter.Write(
            report,
            writer);

        string text = writer.ToString();

        Assert.Contains(
            "Generated (UTC): 1970-01-01T00:00:00.0000000+00:00",
            text,
            StringComparison.Ordinal);
        Assert.Contains(
            "Inspection: Manual",
            text,
            StringComparison.Ordinal);
        Assert.Contains(
            "Kind: JavaScript",
            text,
            StringComparison.Ordinal);
        Assert.Contains(
            "Location: /Catalog/Names",
            text,
            StringComparison.Ordinal);
    }
}
