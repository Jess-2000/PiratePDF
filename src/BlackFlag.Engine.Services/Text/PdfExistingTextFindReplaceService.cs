using System.Collections.ObjectModel;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Editing.Transactions;
using BlackFlag.Engine.Rendering.DisplayList;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Finds provenance-backed existing text and performs atomic replace-all
/// operations through the already established BF-1054C and BF-1054D mutation
/// strategies.
/// </summary>
public static class PdfExistingTextFindReplaceService
{
    /// <summary>
    /// Finds Unicode text inside supported existing-text elements.
    /// </summary>
    public static IReadOnlyList<PdfExistingTextSearchMatch> Find(
        PdfEditingSession session,
        IReadOnlyList<PdfExistingTextSearchScope> scopes,
        string searchText,
        bool matchCase = true)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentException.ThrowIfNullOrEmpty(searchText);

        session.EnsureActive();

        StringComparison comparison =
            matchCase
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

        var matches =
            new List<PdfExistingTextSearchMatch>();

        for (int scopeIndex = 0;
            scopeIndex < scopes.Count;
            scopeIndex++)
        {
            PdfExistingTextSearchScope scope =
                scopes[scopeIndex] ??
                throw new ArgumentException(
                    $"Search scope {scopeIndex} is null.",
                    nameof(scopes));

            session.Document.EnsureOwnership(
                scope.DisplayedPage);

            IReadOnlyList<PdfExistingTextDescriptor> descriptors =
                PdfExistingTextDiscoveryService.Discover(
                    session,
                    scope.DisplayedPage,
                    scope.Source);

            for (int descriptorIndex = 0;
                descriptorIndex < descriptors.Count;
                descriptorIndex++)
            {
                PdfExistingTextDescriptor descriptor =
                    descriptors[descriptorIndex];

                switch (descriptor.Editability)
                {
                    case PdfExistingTextEditability.DirectlyEditable:
                        AddDirectMatches(
                            matches,
                            scopeIndex,
                            descriptor,
                            searchText,
                            comparison);
                        break;

                    case PdfExistingTextEditability.EditableWithPositionPreservation:
                        AddPositionedMatches(
                            matches,
                            scopeIndex,
                            scope.Source,
                            descriptor,
                            searchText,
                            comparison);
                        break;
                }
            }
        }

        return new ReadOnlyCollection<PdfExistingTextSearchMatch>(
            matches);
    }

    /// <summary>
    /// Finds and replaces every supported match as one atomic Editing
    /// transaction. No-op searches are rejected and do not create history.
    /// </summary>
    public static PdfExistingTextFindReplaceResult ReplaceAll(
        PdfEditingSession session,
        IReadOnlyList<PdfExistingTextSearchScope> scopes,
        string searchText,
        string replacementText,
        bool matchCase = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentException.ThrowIfNullOrEmpty(searchText);
        ArgumentNullException.ThrowIfNull(replacementText);

        session.EnsureActive();

        if (session.HasActiveTransaction)
        {
            throw new InvalidOperationException(
                "Atomic existing-text replace-all requires the editing session to have no active transaction.");
        }

        IReadOnlyList<PdfExistingTextSearchMatch> matches =
            Find(
                session,
                scopes,
                searchText,
                matchCase);

        if (matches.Count == 0)
        {
            throw new InvalidOperationException(
                "Existing-text replace-all found no supported matches.");
        }

        ReplacementTarget[] targets =
            BuildTargets(
                scopes,
                matches,
                searchText,
                replacementText,
                matchCase);

        ValidateDistinctSourceOperations(
            targets);

        using PdfEditTransaction transaction =
            session.BeginTransaction();

        try
        {
            for (int index = 0;
                index < targets.Length;
                index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ReplacementTarget target =
                    targets[index];

                switch (target.Descriptor.Editability)
                {
                    case PdfExistingTextEditability.DirectlyEditable:
                        _ = PdfExistingTextReplacementService.Replace(
                            session,
                            target.Scope.DisplayedPage,
                            target.Scope.Source,
                            target.Descriptor,
                            target.DirectReplacement!,
                            cancellationToken);
                        break;

                    case PdfExistingTextEditability.EditableWithPositionPreservation:
                        _ = PdfPositionedTextReplacementService.Replace(
                            session,
                            target.Scope.DisplayedPage,
                            target.Scope.Source,
                            target.Descriptor,
                            target.PositionedReplacement!,
                            cancellationToken);
                        break;

                    default:
                        throw new NotSupportedException(
                            "The replacement target no longer has a supported existing-text mutation strategy.");
                }
            }

            PdfEditChangeSet changeSet =
                transaction.Commit();

            return new PdfExistingTextFindReplaceResult(
                matches,
                changeSet);
        }
        catch
        {
            if (transaction.State ==
                PdfEditTransactionState.Active)
            {
                transaction.Rollback();
            }

            throw;
        }
    }

    private static void AddDirectMatches(
        List<PdfExistingTextSearchMatch> matches,
        int scopeIndex,
        PdfExistingTextDescriptor descriptor,
        string searchText,
        StringComparison comparison)
    {
        string? text =
            descriptor.UnicodeText;

        if (text is null)
        {
            return;
        }

        AddMatches(
            matches,
            scopeIndex,
            descriptor,
            textElementIndex: 0,
            text,
            searchText,
            comparison);
    }

    private static void AddPositionedMatches(
        List<PdfExistingTextSearchMatch> matches,
        int scopeIndex,
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor,
        string searchText,
        StringComparison comparison)
    {
        PdfDisplayTextRun run =
            GetRequiredRun(
                source,
                descriptor.Identity);

        int textElementIndex = 0;

        for (int index = 0;
            index < run.Elements.Count;
            index++)
        {
            PdfTextRunElement element =
                run.Elements[index];

            if (element.Kind !=
                PdfTextRunElementKind.Text)
            {
                continue;
            }

            if (element.UnicodeText is not null)
            {
                AddMatches(
                    matches,
                    scopeIndex,
                    descriptor,
                    textElementIndex,
                    element.UnicodeText,
                    searchText,
                    comparison);
            }

            textElementIndex++;
        }
    }

    private static void AddMatches(
        List<PdfExistingTextSearchMatch> matches,
        int scopeIndex,
        PdfExistingTextDescriptor descriptor,
        int textElementIndex,
        string text,
        string searchText,
        StringComparison comparison)
    {
        int searchOffset = 0;

        while (searchOffset <=
            text.Length - searchText.Length)
        {
            int matchOffset =
                text.IndexOf(
                    searchText,
                    searchOffset,
                    comparison);

            if (matchOffset < 0)
            {
                return;
            }

            matches.Add(
                new PdfExistingTextSearchMatch(
                    scopeIndex,
                    descriptor,
                    textElementIndex,
                    matchOffset,
                    searchText.Length,
                    text.Substring(
                        matchOffset,
                        searchText.Length)));

            searchOffset =
                checked(
                    matchOffset +
                    searchText.Length);
        }
    }

    private static ReplacementTarget[] BuildTargets(
        IReadOnlyList<PdfExistingTextSearchScope> scopes,
        IReadOnlyList<PdfExistingTextSearchMatch> matches,
        string searchText,
        string replacementText,
        bool matchCase)
    {
        StringComparison comparison =
            matchCase
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

        return
        [
            .. matches
                .GroupBy(
                    static match =>
                        new TargetKey(
                            match.ScopeIndex,
                            match.Descriptor.Identity))
                .Select(
                    group =>
                        BuildTarget(
                            scopes,
                            group.Key.ScopeIndex,
                            group.First().Descriptor,
                            searchText,
                            replacementText,
                            comparison))
                .OrderBy(
                    static target =>
                        target.Descriptor.Identity.ContentStreamIdentifier?.ObjectNumber
                        ?? int.MaxValue)
                .ThenBy(
                    static target =>
                        target.Descriptor.Identity.ContentStreamIdentifier?.GenerationNumber
                        ?? int.MaxValue)
                .ThenByDescending(
                    static target =>
                        target.Descriptor.Identity.StartOffset)
                .ThenByDescending(
                    static target =>
                        target.Descriptor.Identity.OperationIndex)
                .ThenByDescending(
                    static target =>
                        target.Descriptor.Identity.RunOrdinal)
        ];
    }

    private static ReplacementTarget BuildTarget(
        IReadOnlyList<PdfExistingTextSearchScope> scopes,
        int scopeIndex,
        PdfExistingTextDescriptor descriptor,
        string searchText,
        string replacementText,
        StringComparison comparison)
    {
        PdfExistingTextSearchScope scope =
            scopes[scopeIndex];

        return descriptor.Editability switch
        {
            PdfExistingTextEditability.DirectlyEditable =>
                new ReplacementTarget(
                    scope,
                    descriptor,
                    ReplaceOccurrences(
                        descriptor.UnicodeText ??
                        throw new InvalidOperationException(
                            "A directly editable search target lost its Unicode text."),
                        searchText,
                        replacementText,
                        comparison),
                    null),

            PdfExistingTextEditability.EditableWithPositionPreservation =>
                new ReplacementTarget(
                    scope,
                    descriptor,
                    null,
                    BuildPositionedReplacement(
                        scope.Source,
                        descriptor,
                        searchText,
                        replacementText,
                        comparison)),

            _ =>
                throw new NotSupportedException(
                    "The search target does not have a supported replacement strategy.")
        };
    }

    private static string[] BuildPositionedReplacement(
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor,
        string searchText,
        string replacementText,
        StringComparison comparison)
    {
        PdfDisplayTextRun run =
            GetRequiredRun(
                source,
                descriptor.Identity);

        return
        [
            .. run.Elements
                .Where(
                    static element =>
                        element.Kind ==
                        PdfTextRunElementKind.Text)
                .Select(
                    element =>
                        ReplaceOccurrences(
                            element.UnicodeText ??
                            throw new InvalidOperationException(
                                "A positioned search target lost its Unicode text."),
                            searchText,
                            replacementText,
                            comparison))
        ];
    }

    private static string ReplaceOccurrences(
        string source,
        string searchText,
        string replacementText,
        StringComparison comparison)
    {
        var result =
            new System.Text.StringBuilder(
                source.Length);

        int sourceOffset = 0;

        while (sourceOffset < source.Length)
        {
            int matchOffset =
                source.IndexOf(
                    searchText,
                    sourceOffset,
                    comparison);

            if (matchOffset < 0)
            {
                _ = result.Append(
                    source,
                    sourceOffset,
                    source.Length - sourceOffset);
                break;
            }

            _ = result.Append(
                source,
                sourceOffset,
                matchOffset - sourceOffset);

            _ = result.Append(
                replacementText);

            sourceOffset =
                checked(
                    matchOffset +
                    searchText.Length);
        }

        return result.ToString();
    }

    private static void ValidateDistinctSourceOperations(
        IReadOnlyList<ReplacementTarget> targets)
    {
        var seen =
            new HashSet<SourceOperationKey>();

        for (int index = 0;
            index < targets.Count;
            index++)
        {
            PdfExistingTextIdentity identity =
                targets[index].Descriptor.Identity;

            var key =
                new SourceOperationKey(
                    identity.ContentStreamIdentifier,
                    identity.StreamIndex,
                    identity.StartOffset,
                    identity.EndOffset,
                    identity.OperationIndex);

            if (!seen.Add(
                    key))
            {
                throw new NotSupportedException(
                    "Replace-all encountered multiple editable display runs for one source operation; deterministic multi-run mutation requires a dedicated strategy.");
            }
        }
    }

    private static PdfDisplayTextRun GetRequiredRun(
        PdfExistingTextDiscoverySource source,
        PdfExistingTextIdentity identity)
    {
        int ordinal = 0;

        foreach (PdfDisplayItem item
                in source.DisplayList)
        {
            if (item is not PdfDisplayTextRun run ||
                run.SourceSpan.StreamIndex !=
                    identity.StreamIndex ||
                run.SourceSpan.StartOffset !=
                    identity.StartOffset ||
                run.SourceSpan.EndOffset !=
                    identity.EndOffset)
            {
                continue;
            }

            if (ordinal ==
                identity.RunOrdinal)
            {
                return run;
            }

            ordinal++;
        }

        throw new InvalidOperationException(
            "The existing-text search target has stale display-run provenance.");
    }

    private readonly record struct TargetKey(
        int ScopeIndex,
        PdfExistingTextIdentity Identity);

    private readonly record struct SourceOperationKey(
        Document.Objects.PdfObjectIdentifier?
            ContentStreamIdentifier,
        int StreamIndex,
        long StartOffset,
        long EndOffset,
        int OperationIndex);

    private sealed record ReplacementTarget(
        PdfExistingTextSearchScope Scope,
        PdfExistingTextDescriptor Descriptor,
        string? DirectReplacement,
        IReadOnlyList<string>? PositionedReplacement);
}
