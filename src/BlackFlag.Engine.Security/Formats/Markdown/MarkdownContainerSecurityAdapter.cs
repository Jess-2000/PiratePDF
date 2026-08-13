using System.Text;
using BlackFlag.Engine.Security.Containers;
using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Security.Formats.Markdown;

/// <summary>Passively inspects Markdown links and raw HTML active content.</summary>
public sealed class MarkdownContainerSecurityAdapter :
    IDocumentContainerSecurityAdapter
{
    private const string SourceFormat = "markdown";
    private readonly MarkdownSecurityAnalysisOptions options;

    public MarkdownContainerSecurityAdapter(MarkdownSecurityAnalysisOptions? options = null)
    {
        this.options = options ?? MarkdownSecurityAnalysisOptions.Default;
        this.options.Validate();
    }

    public bool CanInspect(DocumentEmbeddedObjectClassification classification)
    {
        ArgumentNullException.ThrowIfNull(classification);
        return classification.Kind == DocumentEmbeddedObjectKind.Markdown;
    }

    public DocumentContainerSecurityInspection Inspect(
        DocumentEmbeddedObject embeddedObject,
        DocumentSecurityInspectionBudget budget,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embeddedObject);
        ArgumentNullException.ThrowIfNull(budget);

        if (embeddedObject.Content.Length > options.MaximumDocumentBytes)
        {
            throw new InvalidDataException(
                $"Markdown document exceeds the configured maximum of {options.MaximumDocumentBytes} bytes.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        budget.ConsumeWork();

        string text;
        try
        {
            text = new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true)
                .GetString(embeddedObject.Content.Span);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException(
                "Markdown content is not valid UTF-8.",
                exception);
        }

        var observations = new List<DocumentSecurityObservation>();
        var keys = new HashSet<string>(StringComparer.Ordinal);

        InspectRawHtml(text, embeddedObject.LogicalPath, observations, keys);
        InspectDestinations(text, embeddedObject.LogicalPath, observations, keys);

        return new DocumentContainerSecurityInspection(observations);
    }

    private static void InspectRawHtml(
        string text,
        string logicalPath,
        List<DocumentSecurityObservation> observations,
        HashSet<string> keys)
    {
        string[] activeTags =
        [
            "<script",
            "<iframe",
            "<object",
            "<embed"
        ];

        if (activeTags.Any(tag =>
            text.Contains(tag, StringComparison.OrdinalIgnoreCase)) ||
            text.Contains(" onload=", StringComparison.OrdinalIgnoreCase) ||
            text.Contains(" onclick=", StringComparison.OrdinalIgnoreCase) ||
            text.Contains(" onerror=", StringComparison.OrdinalIgnoreCase))
        {
            AddObservation(
                observations,
                keys,
                DocumentSecurityObservationCategory.ActiveContent,
                "RawHtmlActiveContent",
                "The Markdown document contains raw HTML capable of active behavior.",
                logicalPath);
        }

        if (text.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("vbscript:", StringComparison.OrdinalIgnoreCase))
        {
            AddObservation(
                observations,
                keys,
                DocumentSecurityObservationCategory.ScriptContent,
                "ScriptUri",
                "The Markdown document contains a script URI.",
                logicalPath);
        }
    }

    private static void InspectDestinations(
        string text,
        string logicalPath,
        List<DocumentSecurityObservation> observations,
        HashSet<string> keys)
    {
        int searchFrom = 0;

        while (searchFrom < text.Length)
        {
            int open = text.IndexOf("](", searchFrom, StringComparison.Ordinal);
            if (open < 0)
            {
                break;
            }

            int close = text.IndexOf(')', open + 2);
            if (close < 0)
            {
                break;
            }

            string destination = text[(open + 2)..close].Trim();

            if (IsExternal(destination))
            {
                AddObservation(
                    observations,
                    keys,
                    DocumentSecurityObservationCategory.ExternalReference,
                    "ExternalReference",
                    "The Markdown document contains an external link or resource reference.",
                    logicalPath);
            }

            searchFrom = close + 1;
        }

        searchFrom = 0;

        while (searchFrom < text.Length)
        {
            int open = text.IndexOf('<', searchFrom);
            if (open < 0)
            {
                break;
            }

            int close = text.IndexOf('>', open + 1);
            if (close < 0)
            {
                break;
            }

            string candidate = text[(open + 1)..close].Trim();
            if (IsExternal(candidate))
            {
                AddObservation(
                    observations,
                    keys,
                    DocumentSecurityObservationCategory.ExternalReference,
                    "ExternalReference",
                    "The Markdown document contains an external link or resource reference.",
                    logicalPath);
            }

            searchFrom = close + 1;
        }
    }

    private static bool IsExternal(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.StartsWith('#') ||
            value.StartsWith("./", StringComparison.Ordinal) ||
            value.StartsWith("../", StringComparison.Ordinal) ||
            value.StartsWith('/'))
        {
            return false;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
            !string.Equals(uri.Scheme, "data", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddObservation(
        List<DocumentSecurityObservation> observations,
        HashSet<string> keys,
        DocumentSecurityObservationCategory category,
        string kind,
        string description,
        string logicalPath)
    {
        string key = $"{category}|{kind}|{logicalPath}";
        if (!keys.Add(key))
        {
            return;
        }

        observations.Add(
            new DocumentSecurityObservation(
                category,
                kind,
                description,
                SourceFormat,
                new DocumentSecuritySourceLocation
                {
                    LogicalPath = logicalPath
                }));
    }
}
