
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Parsing.Security;
using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;
using BlackFlag.Engine.Security.Policy;

namespace BlackFlag.Engine.Parsing.Tests.Security;

public sealed class PdfSecurityAnalyzerTests
{
    [Fact]
    public void AnalysisResultObservationsCannotBeMutated()
    {
        var original = new DocumentSecurityObservation(
            DocumentSecurityObservationCategory.ActiveContent,
            "Original",
            "Original description.",
            "pdf");
        var replacement = new DocumentSecurityObservation(
            DocumentSecurityObservationCategory.ExternalReference,
            "Replacement",
            "Replacement description.",
            "pdf");
        var result = new PdfSecurityAnalysisResult([original]);

        Assert.False(result.Observations is DocumentSecurityObservation[]);
        IList<DocumentSecurityObservation> mutableView =
            Assert.IsAssignableFrom<IList<DocumentSecurityObservation>>(
                result.Observations);
        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutableView[0] = replacement);
        Assert.Same(original, result.Observations[0]);
        Assert.Same(result.Observations, result.Observations);
    }

    [Fact]
    public void DetectsAutomaticJavaScriptWithoutExecutingIt()
    {
        PdfDictionary action = Dictionary(
            ("S", new PdfName("JavaScript")),
            ("JS", new PdfLiteralString("app.alert('fixture');")));

        PdfDictionary catalog = Dictionary(
            ("Type", new PdfName("Catalog")),
            ("OpenAction", action));

        PdfSecurityAnalysisResult result =
            new PdfSecurityAnalyzer(
                new DictionaryResolver())
            .Analyze(catalog);

        Assert.Contains(
            result.Observations,
            item =>
                item.Category ==
                    DocumentSecurityObservationCategory.ScriptContent &&
                item.Kind == "JavaScript");

        Assert.Contains(
            result.Observations,
            item =>
                item.Category ==
                    DocumentSecurityObservationCategory.AutomaticAction);

        DocumentSecurityAssessment assessment =
            new DocumentSecurityAssessmentEngine()
                .Evaluate(
                    result.Observations,
                    DocumentSecurityOperation.Open);

        Assert.True(assessment.ShouldGenerateReport);
        Assert.Empty(assessment.Interactions);
    }

    [Fact]
    public void DetectsEmbeddedFileNameTreeAndFileSpecification()
    {
        PdfDictionary fileSpec = Dictionary(
            ("Type", new PdfName("Filespec")),
            ("F", new PdfLiteralString("test.txt")),
            ("EF", Dictionary(
                ("F", new PdfIndirectReference(9, 0)))));

        PdfDictionary embeddedNames = Dictionary(
            ("Names", new PdfArray(
            [
                new PdfLiteralString("test.txt"),
                fileSpec
            ])));

        PdfDictionary names = Dictionary(
            ("EmbeddedFiles", embeddedNames));

        PdfDictionary catalog = Dictionary(
            ("Type", new PdfName("Catalog")),
            ("Names", names));

        var resolver =
            new DictionaryResolver(
                new KeyValuePair<PdfObjectId, PdfObject>(
                    new PdfObjectId(9, 0),
                    new PdfStream(
                        PdfDictionary.Empty,
                        "hello"u8.ToArray())));

        PdfSecurityAnalysisResult result =
            new PdfSecurityAnalyzer(resolver)
                .Analyze(catalog);

        Assert.Contains(
            result.Observations,
            item =>
                item.Kind == "EmbeddedFilesNameTree");

        Assert.Contains(
            result.Observations,
            item =>
                item.Kind == "FileSpecification" &&
                item.Properties["FileName"] == "test.txt");
    }

    [Fact]
    public void DetectsExternalAndLaunchActions()
    {
        PdfDictionary uri = Dictionary(
            ("S", new PdfName("URI")),
            ("URI", new PdfLiteralString("https://example.invalid")));

        PdfDictionary launch = Dictionary(
            ("S", new PdfName("Launch")),
            ("F", new PdfLiteralString("example.exe")));

        PdfDictionary additional = Dictionary(
            ("WC", uri),
            ("WS", launch));

        PdfDictionary catalog = Dictionary(
            ("Type", new PdfName("Catalog")),
            ("AA", additional));

        PdfSecurityAnalysisResult result =
            new PdfSecurityAnalyzer(
                new DictionaryResolver())
            .Analyze(catalog);

        Assert.Contains(
            result.Observations,
            item =>
                item.Kind == "UriAction" &&
                item.Category ==
                    DocumentSecurityObservationCategory.ExternalReference);

        Assert.Contains(
            result.Observations,
            item =>
                item.Kind == "LaunchAction" &&
                item.Category ==
                    DocumentSecurityObservationCategory.ExecutableContent);
    }

    [Fact]
    public void DetectsAcroFormXfaAndInteractiveAnnotations()
    {
        PdfDictionary acroForm = Dictionary(
            ("XFA", new PdfLiteralString("fixture")));

        PdfDictionary annotation = Dictionary(
            ("Type", new PdfName("Annot")),
            ("Subtype", new PdfName("RichMedia")));

        PdfDictionary page = Dictionary(
            ("Annots", new PdfArray([annotation])));

        PdfDictionary catalog = Dictionary(
            ("Type", new PdfName("Catalog")),
            ("AcroForm", acroForm),
            ("FixturePage", page));

        PdfSecurityAnalysisResult result =
            new PdfSecurityAnalyzer(
                new DictionaryResolver())
            .Analyze(catalog);

        Assert.Contains(result.Observations, item => item.Kind == "AcroForm");
        Assert.Contains(result.Observations, item => item.Kind == "XFA");
        Assert.Contains(
            result.Observations,
            item => item.Kind == "RichMediaAnnotation");
    }

    [Fact]
    public void CyclicIndirectReferencesAreVisitedOnce()
    {
        PdfDictionary first = Dictionary(
            ("Next", new PdfIndirectReference(2, 0)));

        PdfDictionary second = Dictionary(
            ("Back", new PdfIndirectReference(1, 0)));

        PdfDictionary catalog = Dictionary(
            ("Type", new PdfName("Catalog")),
            ("Fixture", new PdfIndirectReference(1, 0)));

        var resolver = new DictionaryResolver(
            new(
                new PdfObjectId(1, 0),
                first),
            new(
                new PdfObjectId(2, 0),
                second));

        PdfSecurityAnalysisResult result =
            new PdfSecurityAnalyzer(resolver)
                .Analyze(catalog);

        Assert.NotNull(result);
    }

    [Fact]
    public void MissingReferencedObjectBecomesStructuralObservation()
    {
        PdfDictionary catalog = Dictionary(
            ("Type", new PdfName("Catalog")),
            ("OpenAction", new PdfIndirectReference(50, 0)));

        PdfSecurityAnalysisResult result =
            new PdfSecurityAnalyzer(
                new DictionaryResolver())
            .Analyze(catalog);

        Assert.Contains(
            result.Observations,
            item =>
                item.Kind == "UnresolvedSecurityReference" &&
                item.Category ==
                    DocumentSecurityObservationCategory.StructuralAnomaly);
    }

    [Fact]
    public void CleanCatalogProducesNoSecurityReport()
    {
        PdfDictionary catalog = Dictionary(
            ("Type", new PdfName("Catalog")));

        PdfSecurityAnalysisResult result =
            new PdfSecurityAnalyzer(
                new DictionaryResolver())
            .Analyze(catalog);

        DocumentSecurityAssessment assessment =
            new DocumentSecurityAssessmentEngine()
                .Evaluate(
                    result.Observations,
                    DocumentSecurityOperation.Open);

        Assert.Empty(result.Observations);
        Assert.False(assessment.ShouldGenerateReport);
    }

    private static PdfDictionary Dictionary(
        params (string Key, PdfObject Value)[] entries) =>
        new(
            entries.Select(
                entry =>
                    new KeyValuePair<PdfName, PdfObject>(
                        new PdfName(entry.Key),
                        entry.Value)));

    private sealed class DictionaryResolver :
        IPdfIndirectObjectResolver
    {
        private readonly Dictionary<PdfObjectId, PdfObject> objects;

        public DictionaryResolver(
            params KeyValuePair<PdfObjectId, PdfObject>[] objects)
        {
            this.objects = objects.ToDictionary();
        }

        public PdfObject Resolve(PdfObjectId objectId) =>
            objects.TryGetValue(
                objectId,
                out PdfObject value)
                ? value
                : throw new KeyNotFoundException(
                    $"Object {objectId} was not registered.");

        public bool TryResolve(
            PdfObjectId objectId,
            out PdfObject value) =>
            objects.TryGetValue(
                objectId,
                out value);
    }
}
