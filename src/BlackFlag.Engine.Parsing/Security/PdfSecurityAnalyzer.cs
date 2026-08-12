using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.Loading;
using BlackFlag.Engine.Security.Inspection;
using BlackFlag.Engine.Security.Model;

namespace BlackFlag.Engine.Parsing.Security;

/// <summary>
/// Inventories security-relevant PDF structures without executing document
/// actions, JavaScript, embedded content, or interactive behavior.
/// </summary>
public sealed class PdfSecurityAnalyzer
{
    private static readonly PdfName TypeKey = new("Type");
    private static readonly PdfName SubtypeKey = new("Subtype");
    private static readonly PdfName ActionTypeKey = new("S");
    private static readonly PdfName JavaScriptKey = new("JS");
    private static readonly PdfName OpenActionKey = new("OpenAction");
    private static readonly PdfName AdditionalActionsKey = new("AA");
    private static readonly PdfName ActionKey = new("A");
    private static readonly PdfName NextKey = new("Next");
    private static readonly PdfName NamesKey = new("Names");
    private static readonly PdfName JavaScriptNamesKey = new("JavaScript");
    private static readonly PdfName EmbeddedFilesKey = new("EmbeddedFiles");
    private static readonly PdfName EmbeddedFileKey = new("EF");
    private static readonly PdfName AcroFormKey = new("AcroForm");
    private static readonly PdfName XfaKey = new("XFA");
    private static readonly PdfName UriKey = new("URI");
    private static readonly PdfName FileKey = new("F");
    private static readonly PdfName UnicodeFileKey = new("UF");
    private static readonly PdfName FileSpecName = new("Filespec");
    private static readonly PdfName AnnotationName = new("Annot");
    private static readonly PdfName FileAttachmentName = new("FileAttachment");
    private static readonly PdfName RichMediaName = new("RichMedia");
    private static readonly PdfName ScreenName = new("Screen");
    private static readonly PdfName MovieName = new("Movie");
    private static readonly PdfName SoundName = new("Sound");
    private static readonly PdfName WidgetName = new("Widget");

    private readonly IPdfIndirectObjectResolver resolver;

    /// <summary>Initializes a PDF security analyzer.</summary>
    public PdfSecurityAnalyzer(IPdfIndirectObjectResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        this.resolver = resolver;
    }

    /// <summary>
    /// Analyzes a resolved PDF catalog and all reachable security-relevant
    /// objects using the supplied shared BlackFlag inspection budget.
    /// </summary>
    public PdfSecurityAnalysisResult Analyze(
        PdfDictionary catalog,
        DocumentSecurityInspectionBudget? budget = null,
        PdfSecurityAnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        budget ??= new DocumentSecurityInspectionBudget();
        options ??= PdfSecurityAnalysisOptions.Default;
        options.Validate();

        var state = new AnalysisState(
            resolver,
            budget,
            options,
            cancellationToken);

        state.Visit(
            catalog,
            logicalPath: "/Catalog",
            sourceObjectId: null,
            depth: 0,
            context: VisitContext.General);

        return new PdfSecurityAnalysisResult(state.Observations);
    }

    private enum VisitContext
    {
        General,
        OpenAction,
        AdditionalAction,
        Action,
        JavaScriptNameTree,
        EmbeddedFilesNameTree,
        AcroForm
    }

    private sealed class AnalysisState
    {
        private readonly IPdfIndirectObjectResolver resolver;
        private readonly DocumentSecurityInspectionBudget budget;
        private readonly PdfSecurityAnalysisOptions options;
        private readonly CancellationToken cancellationToken;
        private readonly HashSet<PdfObjectId> visitedReferences = [];
        private readonly HashSet<string> observationKeys =
            new(StringComparer.Ordinal);
        private readonly List<DocumentSecurityObservation> observations = [];
        private int resolvedObjectCount;

        public AnalysisState(
            IPdfIndirectObjectResolver resolver,
            DocumentSecurityInspectionBudget budget,
            PdfSecurityAnalysisOptions options,
            CancellationToken cancellationToken)
        {
            this.resolver = resolver;
            this.budget = budget;
            this.options = options;
            this.cancellationToken = cancellationToken;
        }

        public IReadOnlyList<DocumentSecurityObservation> Observations =>
            observations;

        public void Visit(
            PdfObject value,
            string logicalPath,
            PdfObjectId? sourceObjectId,
            int depth,
            VisitContext context)
        {
            cancellationToken.ThrowIfCancellationRequested();
            budget.ConsumeWork();
            budget.ValidateContainerDepth(
                Math.Min(
                    depth,
                    budget.Limits.MaximumContainerDepth));

            if (depth > options.MaximumObjectGraphDepth)
            {
                throw new InvalidDataException(
                    $"PDF security analysis object-graph depth {depth} exceeds the configured maximum of {options.MaximumObjectGraphDepth}.");
            }

            switch (value)
            {
                case PdfIndirectReference reference:
                    VisitReference(
                        reference,
                        logicalPath,
                        depth,
                        context);
                    break;

                case PdfDictionary dictionary:
                    VisitDictionary(
                        dictionary,
                        logicalPath,
                        sourceObjectId,
                        depth,
                        context);
                    break;

                case PdfStream stream:
                    VisitDictionary(
                        stream.Dictionary,
                        logicalPath,
                        sourceObjectId,
                        depth,
                        context);
                    break;

                case PdfArray array:
                    for (int index = 0; index < array.Count; index++)
                    {
                        Visit(
                            array[index],
                            $"{logicalPath}[{index}]",
                            sourceObjectId,
                            checked(depth + 1),
                            context);
                    }
                    break;
            }
        }

        private void VisitReference(
            PdfIndirectReference reference,
            string logicalPath,
            int depth,
            VisitContext context)
        {
            var objectId = new PdfObjectId(
                reference.ObjectNumber,
                reference.GenerationNumber);

            if (!visitedReferences.Add(objectId))
            {
                return;
            }

            resolvedObjectCount++;
            if (resolvedObjectCount > options.MaximumResolvedObjects)
            {
                throw new InvalidDataException(
                    "PDF security analysis exceeded the configured resolved-object limit.");
            }

            if (!resolver.TryResolve(objectId, out PdfObject? resolved) ||
                resolved is null)
            {
                AddObservation(
                    DocumentSecurityObservationCategory.StructuralAnomaly,
                    "UnresolvedSecurityReference",
                    $"A PDF object reference required during security inspection could not be resolved ({objectId}).",
                    logicalPath,
                    objectId,
                    properties: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["ObjectId"] = objectId.ToString()
                    });

                return;
            }

            Visit(
                resolved,
                logicalPath,
                objectId,
                checked(depth + 1),
                context);
        }

        private void VisitDictionary(
            PdfDictionary dictionary,
            string logicalPath,
            PdfObjectId? sourceObjectId,
            int depth,
            VisitContext context)
        {
            ClassifyDictionary(
                dictionary,
                logicalPath,
                sourceObjectId,
                context);

            foreach (KeyValuePair<PdfName, PdfObject> entry in dictionary)
            {
                string childPath =
                    $"{logicalPath}/{entry.Key.Value}";

                VisitContext childContext =
                    GetChildContext(
                        entry.Key,
                        context);

                if (ShouldRecordPresence(entry.Key, dictionary, childContext))
                {
                    RecordPresenceObservation(
                        entry.Key,
                        entry.Value,
                        dictionary,
                        childPath,
                        sourceObjectId,
                        childContext);
                }

                Visit(
                    entry.Value,
                    childPath,
                    sourceObjectId,
                    checked(depth + 1),
                    childContext);
            }
        }

        private void ClassifyDictionary(
            PdfDictionary dictionary,
            string logicalPath,
            PdfObjectId? sourceObjectId,
            VisitContext context)
        {
            if (IsActionDictionary(dictionary))
            {
                ClassifyAction(
                    dictionary,
                    logicalPath,
                    sourceObjectId,
                    context);
            }

            if (IsFileSpecification(dictionary))
            {
                AddObservation(
                    DocumentSecurityObservationCategory.EmbeddedContent,
                    "FileSpecification",
                    DescribeFileSpecification(dictionary),
                    logicalPath,
                    sourceObjectId,
                    properties: FileProperties(dictionary));
            }

            if (TryGetName(dictionary, TypeKey, out PdfName? type) &&
                type == AnnotationName &&
                TryGetName(dictionary, SubtypeKey, out PdfName? subtype))
            {
                ClassifyAnnotation(
                    subtype!,
                    logicalPath,
                    sourceObjectId);
            }

            if (dictionary.ContainsKey(XfaKey))
            {
                AddObservation(
                    DocumentSecurityObservationCategory.InteractiveContent,
                    "XFA",
                    "The PDF contains XFA form content.",
                    $"{logicalPath}/XFA",
                    sourceObjectId);
            }
        }

        private void ClassifyAction(
            PdfDictionary dictionary,
            string logicalPath,
            PdfObjectId? sourceObjectId,
            VisitContext context)
        {
            if (!TryGetName(
                    dictionary,
                    ActionTypeKey,
                    out PdfName? actionName))
            {
                return;
            }

            string action = actionName!.Value;
            bool automatic =
                context is VisitContext.OpenAction or
                VisitContext.AdditionalAction;

            switch (action)
            {
                case "JavaScript":
                    AddObservation(
                        DocumentSecurityObservationCategory.ScriptContent,
                        "JavaScript",
                        automatic
                            ? "The PDF contains JavaScript configured to run from an automatic document action."
                            : "The PDF contains JavaScript.",
                        logicalPath,
                        sourceObjectId,
                        Properties(
                            ("Language", "JavaScript"),
                            ("Action", action),
                            ("Automatic", automatic.ToString())));

                    if (automatic)
                    {
                        AddAutomaticAction(
                            action,
                            logicalPath,
                            sourceObjectId);
                    }
                    break;

                case "Launch":
                    AddObservation(
                        DocumentSecurityObservationCategory.ExecutableContent,
                        "LaunchAction",
                        "The PDF contains a Launch action that can request an external application or file.",
                        logicalPath,
                        sourceObjectId,
                        Properties(
                            ("Action", action),
                            ("Automatic", automatic.ToString())));

                    if (automatic)
                    {
                        AddAutomaticAction(
                            action,
                            logicalPath,
                            sourceObjectId);
                    }
                    break;

                case "URI":
                    AddObservation(
                        DocumentSecurityObservationCategory.ExternalReference,
                        "UriAction",
                        "The PDF contains an action referencing an external URI.",
                        logicalPath,
                        sourceObjectId,
                        Properties(
                            ("Action", action),
                            ("Target", ReadText(dictionary, UriKey) ?? string.Empty),
                            ("Automatic", automatic.ToString())));

                    if (automatic)
                    {
                        AddAutomaticAction(
                            action,
                            logicalPath,
                            sourceObjectId);
                    }
                    break;

                case "GoToR":
                    AddObservation(
                        DocumentSecurityObservationCategory.ExternalReference,
                        "RemoteGoToAction",
                        "The PDF contains an action that navigates to another file.",
                        logicalPath,
                        sourceObjectId,
                        Properties(
                            ("Action", action),
                            ("Target", ReadFileName(dictionary) ?? string.Empty),
                            ("Automatic", automatic.ToString())));

                    if (automatic)
                    {
                        AddAutomaticAction(
                            action,
                            logicalPath,
                            sourceObjectId);
                    }
                    break;

                case "SubmitForm":
                    AddObservation(
                        DocumentSecurityObservationCategory.ExternalReference,
                        "SubmitFormAction",
                        "The PDF contains a form action that can submit document data to an external target.",
                        logicalPath,
                        sourceObjectId,
                        Properties(
                            ("Action", action),
                            ("Target", ReadFileName(dictionary) ?? string.Empty),
                            ("Automatic", automatic.ToString())));

                    if (automatic)
                    {
                        AddAutomaticAction(
                            action,
                            logicalPath,
                            sourceObjectId);
                    }
                    break;

                case "ImportData":
                    AddObservation(
                        DocumentSecurityObservationCategory.ExternalReference,
                        "ImportDataAction",
                        "The PDF contains an action that can import data from another file.",
                        logicalPath,
                        sourceObjectId,
                        Properties(
                            ("Action", action),
                            ("Target", ReadFileName(dictionary) ?? string.Empty),
                            ("Automatic", automatic.ToString())));

                    if (automatic)
                    {
                        AddAutomaticAction(
                            action,
                            logicalPath,
                            sourceObjectId);
                    }
                    break;

                case "Rendition":
                case "Movie":
                case "Sound":
                    AddObservation(
                        DocumentSecurityObservationCategory.InteractiveContent,
                        $"{action}Action",
                        $"The PDF contains an interactive {action} action.",
                        logicalPath,
                        sourceObjectId,
                        Properties(
                            ("Action", action),
                            ("Automatic", automatic.ToString())));

                    if (automatic)
                    {
                        AddAutomaticAction(
                            action,
                            logicalPath,
                            sourceObjectId);
                    }
                    break;

                default:
                    AddObservation(
                        DocumentSecurityObservationCategory.ActiveContent,
                        "PdfAction",
                        $"The PDF contains an action of type /{action}.",
                        logicalPath,
                        sourceObjectId,
                        Properties(
                            ("Action", action),
                            ("Automatic", automatic.ToString())));

                    if (automatic)
                    {
                        AddAutomaticAction(
                            action,
                            logicalPath,
                            sourceObjectId);
                    }
                    break;
            }
        }

        private void ClassifyAnnotation(
            PdfName subtype,
            string logicalPath,
            PdfObjectId? sourceObjectId)
        {
            DocumentSecurityObservation? observation =
                subtype.Value switch
                {
                    "FileAttachment" => new(
                        DocumentSecurityObservationCategory.EmbeddedContent,
                        "FileAttachmentAnnotation",
                        "The PDF contains a file-attachment annotation.",
                        "pdf",
                        Location(logicalPath, sourceObjectId)),

                    "RichMedia" => new(
                        DocumentSecurityObservationCategory.InteractiveContent,
                        "RichMediaAnnotation",
                        "The PDF contains a rich-media annotation.",
                        "pdf",
                        Location(logicalPath, sourceObjectId)),

                    "Screen" or "Movie" or "Sound" => new(
                        DocumentSecurityObservationCategory.InteractiveContent,
                        $"{subtype.Value}Annotation",
                        $"The PDF contains an interactive {subtype.Value} annotation.",
                        "pdf",
                        Location(logicalPath, sourceObjectId)),

                    "Widget" => new(
                        DocumentSecurityObservationCategory.InteractiveContent,
                        "WidgetAnnotation",
                        "The PDF contains an interactive form widget.",
                        "pdf",
                        Location(logicalPath, sourceObjectId)),

                    _ => null
                };

            if (observation is not null)
            {
                AddObservation(observation);
            }
        }

        private static bool ShouldRecordPresence(
            PdfName key,
            PdfDictionary parent,
            VisitContext childContext) =>
            key == OpenActionKey ||
            key == AdditionalActionsKey ||
            key == EmbeddedFilesKey ||
            key == AcroFormKey ||
            key == XfaKey ||
            (childContext == VisitContext.JavaScriptNameTree &&
            key == JavaScriptKey);

        private void RecordPresenceObservation(
            PdfName key,
            PdfObject value,
            PdfDictionary parent,
            string logicalPath,
            PdfObjectId? sourceObjectId,
            VisitContext childContext)
        {
            if (key == OpenActionKey)
            {
                AddObservation(
                    DocumentSecurityObservationCategory.AutomaticAction,
                    "OpenAction",
                    "The PDF contains an action configured for document open.",
                    logicalPath,
                    sourceObjectId);
                return;
            }

            if (key == AdditionalActionsKey)
            {
                AddObservation(
                    DocumentSecurityObservationCategory.AutomaticAction,
                    "AdditionalActions",
                    "The PDF contains additional automatic actions.",
                    logicalPath,
                    sourceObjectId);
                return;
            }

            if (key == EmbeddedFilesKey)
            {
                AddObservation(
                    DocumentSecurityObservationCategory.EmbeddedContent,
                    "EmbeddedFilesNameTree",
                    "The PDF contains an embedded-files name tree.",
                    logicalPath,
                    sourceObjectId);
                return;
            }

            if (key == AcroFormKey)
            {
                AddObservation(
                    DocumentSecurityObservationCategory.InteractiveContent,
                    "AcroForm",
                    "The PDF contains an interactive AcroForm.",
                    logicalPath,
                    sourceObjectId);
                return;
            }

            if (key == XfaKey)
            {
                AddObservation(
                    DocumentSecurityObservationCategory.InteractiveContent,
                    "XFA",
                    "The PDF contains XFA form content.",
                    logicalPath,
                    sourceObjectId);
                return;
            }

            if (childContext == VisitContext.JavaScriptNameTree &&
                key == JavaScriptKey)
            {
                AddObservation(
                    DocumentSecurityObservationCategory.ScriptContent,
                    "JavaScriptNameTree",
                    "The PDF contains document-level JavaScript in its name tree.",
                    logicalPath,
                    sourceObjectId,
                    Properties(("Language", "JavaScript")));
            }
        }

        private void AddAutomaticAction(
            string action,
            string logicalPath,
            PdfObjectId? sourceObjectId)
        {
            AddObservation(
                DocumentSecurityObservationCategory.AutomaticAction,
                "AutomaticPdfAction",
                $"The PDF can invoke a /{action} action automatically.",
                logicalPath,
                sourceObjectId,
                Properties(("Action", action)));
        }

        private void AddObservation(
            DocumentSecurityObservationCategory category,
            string kind,
            string description,
            string logicalPath,
            PdfObjectId? sourceObjectId,
            IReadOnlyDictionary<string, string>? properties = null)
        {
            AddObservation(
                new DocumentSecurityObservation(
                    category,
                    kind,
                    description,
                    "pdf",
                    Location(logicalPath, sourceObjectId),
                    properties));
        }

        private void AddObservation(
            DocumentSecurityObservation observation)
        {
            string key =
                string.Concat(
                    observation.Category,
                    "|",
                    observation.Kind,
                    "|",
                    observation.Location?.LogicalPath,
                    "|",
                    observation.Location?.ObjectIdentifier);

            if (!observationKeys.Add(key))
            {
                return;
            }

            budget.ConsumeObservation();
            observations.Add(observation);
        }

        private static VisitContext GetChildContext(
            PdfName key,
            VisitContext current)
        {
            if (key == OpenActionKey)
            {
                return VisitContext.OpenAction;
            }

            if (key == AdditionalActionsKey)
            {
                return VisitContext.AdditionalAction;
            }

            if (key == ActionKey ||
                key == NextKey)
            {
                return current is VisitContext.OpenAction or
                    VisitContext.AdditionalAction
                    ? current
                    : VisitContext.Action;
            }

            if (key == EmbeddedFilesKey)
            {
                return VisitContext.EmbeddedFilesNameTree;
            }

            if (key == JavaScriptNamesKey)
            {
                return VisitContext.JavaScriptNameTree;
            }

            if (key == AcroFormKey)
            {
                return VisitContext.AcroForm;
            }

            return current;
        }

        private static bool IsActionDictionary(
            PdfDictionary dictionary) =>
            dictionary.TryGetValue(
                ActionTypeKey,
                out PdfObject actionType) &&
            actionType is PdfName;

        private static bool IsFileSpecification(
            PdfDictionary dictionary)
        {
            if (dictionary.ContainsKey(EmbeddedFileKey))
            {
                return true;
            }

            return TryGetName(
                    dictionary,
                    TypeKey,
                    out PdfName? type) &&
                type == FileSpecName;
        }

        private static string DescribeFileSpecification(
            PdfDictionary dictionary)
        {
            string? fileName =
                ReadFileName(dictionary);

            return string.IsNullOrWhiteSpace(fileName)
                ? "The PDF contains a file specification or embedded-file reference."
                : $"The PDF contains a file specification for '{fileName}'.";
        }

        private static Dictionary<string, string> FileProperties(
            PdfDictionary dictionary) =>
            Properties(
                ("FileName", ReadFileName(dictionary) ?? string.Empty),
                ("Embedded", dictionary.ContainsKey(EmbeddedFileKey).ToString()));

        private static string? ReadFileName(
            PdfDictionary dictionary) =>
            ReadText(dictionary, UnicodeFileKey) ??
            ReadText(dictionary, FileKey);

        private static string? ReadText(
            PdfDictionary dictionary,
            PdfName key)
        {
            if (!dictionary.TryGetValue(
                    key,
                    out PdfObject value))
            {
                return null;
            }

            return value switch
            {
                PdfLiteralString literal => literal.Value,
                PdfHexString hex => Convert.ToHexString(hex.Value.Span),
                _ => null
            };
        }

        private static bool TryGetName(
            PdfDictionary dictionary,
            PdfName key,
            out PdfName? value)
        {
            if (dictionary.TryGetValue(
                    key,
                    out PdfObject item) &&
                item is PdfName name)
            {
                value = name;
                return true;
            }

            value = null;
            return false;
        }

        private static DocumentSecuritySourceLocation Location(
            string logicalPath,
            PdfObjectId? sourceObjectId) =>
            new()
            {
                LogicalPath = logicalPath,
                ObjectIdentifier =
                    sourceObjectId?.ToString()
            };

        private static Dictionary<string, string> Properties(
            params (string Key, string Value)[] entries)
        {
            var result =
                new Dictionary<string, string>(
                    StringComparer.Ordinal);

            foreach ((string key, string value) in entries)
            {
                result[key] = value;
            }

            return result;
        }
    }
}
