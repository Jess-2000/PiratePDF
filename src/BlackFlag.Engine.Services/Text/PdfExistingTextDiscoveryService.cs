using System.Collections.ObjectModel;
using BlackFlag.Engine.Document.Objects;
using BlackFlag.Engine.Editing.Documents;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Maps immutable rendering text runs back to deterministic existing-text
/// editing descriptors for one authoritative content source.
/// </summary>
public static class PdfExistingTextDiscoveryService
{
    /// <summary>
    /// Discovers provenance-backed existing-text descriptors for one page or
    /// Form content source.
    /// </summary>
    public static IReadOnlyList<PdfExistingTextDescriptor> Discover(
        PdfEditingSession session,
        PdfEditablePage displayedPage,
        PdfExistingTextDiscoverySource source)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(displayedPage);
        ArgumentNullException.ThrowIfNull(source);

        session.EnsureActive();
        session.Document.EnsureOwnership(
            displayedPage);

        OperationLookup lookup =
            BuildOperationLookup(
                source.Operations);

        string?[] fontResourceNames =
            BuildFontResourceNames(
                source.Operations);

        var runOrdinals =
            new Dictionary<
                PdfContentSourceSpan,
                int>();

        var descriptors =
            new List<PdfExistingTextDescriptor>();

        foreach (PdfDisplayItem item
                in source.DisplayList)
        {
            if (item is not PdfDisplayTextRun run)
            {
                continue;
            }

            int operationIndex =
                lookup.GetRequiredIndex(
                    run.SourceSpan);

            int runOrdinal =
                GetAndIncrementRunOrdinal(
                    runOrdinals,
                    run.SourceSpan);

            PdfExistingTextIdentity identity =
                new(
                    session.Id,
                    displayedPage.Id,
                    source.SourceKind,
                    source.ContentOwnerIdentifier,
                    source.GetContentStreamIdentifier(
                        run.SourceSpan.StreamIndex),
                    run.SourceSpan.StreamIndex,
                    run.SourceSpan.StartOffset,
                    run.SourceSpan.EndOffset,
                    operationIndex,
                    runOrdinal);

            PdfObjectIdentifier? fontIdentifier =
                ConvertIndirectIdentity(
                    run.Font.Identity);

            (PdfExistingTextEditability Editability, string? Reason)
                classification =
                    Classify(
                        source,
                        source.Operations[operationIndex],
                        run);

            descriptors.Add(
                new PdfExistingTextDescriptor(
                    identity,
                    run.EncodedBytes,
                    run.UnicodeText,
                    fontResourceNames[operationIndex],
                    fontIdentifier,
                    run.FontSize,
                    classification.Editability,
                    classification.Reason));
        }

        return new ReadOnlyCollection<PdfExistingTextDescriptor>(
            descriptors);
    }

    private static OperationLookup BuildOperationLookup(
        IReadOnlyList<PdfGraphicsOperation> operations)
    {
        var indexes =
            new Dictionary<
                PdfContentSourceSpan,
                int>();

        for (int index = 0;
            index < operations.Count;
            index++)
        {
            PdfContentSourceSpan span =
                operations[index].SourceSpan;

            if (!indexes.TryAdd(
                    span,
                    index))
            {
                throw new InvalidOperationException(
                    $"Canonical graphics operations contain duplicate source span {FormatSpan(span)}.");
            }
        }

        return new OperationLookup(
            indexes);
    }

    private static string?[] BuildFontResourceNames(
        IReadOnlyList<PdfGraphicsOperation> operations)
    {
        var result =
            new string?[operations.Count];
        var stack =
            new Stack<string?>();

        string? currentFontName = null;

        for (int index = 0;
            index < operations.Count;
            index++)
        {
            PdfGraphicsOperation operation =
                operations[index];

            switch (operation.Operator.Code)
            {
                case "q":
                    stack.Push(
                        currentFontName);
                    break;

                case "Q":
                    if (stack.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "Existing-text discovery encountered an unbalanced graphics-state restore.");
                    }

                    currentFontName =
                        stack.Pop();
                    break;

                case "Tf":
                    currentFontName =
                        ReadFontResourceName(
                            operation);
                    break;
            }

            result[index] =
                currentFontName;
        }

        if (stack.Count != 0)
        {
            throw new InvalidOperationException(
                "Existing-text discovery encountered an unbalanced graphics-state save.");
        }

        return result;
    }

    private static string ReadFontResourceName(
        PdfGraphicsOperation operation)
    {
        if (operation.Operands.Count < 2 ||
            operation.Operands[0] is not PdfName fontName)
        {
            throw new InvalidOperationException(
                "A canonical Tf operation is missing its font resource name.");
        }

        return fontName.Value;
    }

    private static (
        PdfExistingTextEditability Editability,
        string? Reason)
        Classify(
        PdfExistingTextDiscoverySource source,
        PdfGraphicsOperation operation,
        PdfDisplayTextRun run)
    {
        if (source.SourceKind ==
                PdfExistingTextSourceKind.FormXObject &&
            source.HasSharedSourceImpact)
        {
            return (
                PdfExistingTextEditability.EditableWithSharedSourceImpact,
                "The text is defined by a Form XObject known to affect multiple placements.");
        }

        if (run.Font.Technology ==
            PdfFontTechnology.Type3)
        {
            return (
                PdfExistingTextEditability.ReadOnlyUnsupportedFont,
                "Type 3 text is not eligible for direct existing-text mutation.");
        }

        if (run.UnicodeText is null)
        {
            return (
                PdfExistingTextEditability.ReadOnlyUnsupportedEncoding,
                "The text run does not have a complete directly derivable Unicode mapping.");
        }

        switch (operation.Operator.Code)
        {
            case "Tj":
            case "'":
            case "\"":
                return (
                    PdfExistingTextEditability.DirectlyEditable,
                    null);

            case "TJ":
                if (run.Elements.Count == 1 &&
                    run.Elements[0].Kind ==
                        PdfTextRunElementKind.Text)
                {
                    return (
                        PdfExistingTextEditability.DirectlyEditable,
                        null);
                }

                return (
                    PdfExistingTextEditability.ReadOnlyAmbiguous,
                    "Composite TJ positioning requires the dedicated BF-1054D editing strategy.");

            default:
                return (
                    PdfExistingTextEditability.ReadOnlyAmbiguous,
                    $"Display text provenance points to unsupported source operator '{operation.Operator.Code}'.");
        }
    }

    private static PdfObjectIdentifier? ConvertIndirectIdentity(
        PdfResourceIdentity identity)
    {
        if (!identity.IsIndirect)
        {
            return null;
        }

        return new PdfObjectIdentifier(
            identity.ObjectNumber!.Value,
            identity.GenerationNumber!.Value);
    }

    private static int GetAndIncrementRunOrdinal(
        Dictionary<PdfContentSourceSpan, int> ordinals,
        PdfContentSourceSpan span)
    {
        if (!ordinals.TryGetValue(
                span,
                out int ordinal))
        {
            ordinals.Add(
                span,
                1);
            return 0;
        }

        ordinals[span] =
            checked(
                ordinal + 1);

        return ordinal;
    }

    private static string FormatSpan(
        PdfContentSourceSpan span) =>
        $"[{span.StreamIndex}:{span.StartOffset}-{span.EndOffset})";

    private sealed class OperationLookup
    {
        private readonly IReadOnlyDictionary<
            PdfContentSourceSpan,
            int> indexes;

        public OperationLookup(
            IReadOnlyDictionary<
                PdfContentSourceSpan,
                int> indexes)
        {
            this.indexes =
                indexes;
        }

        public int GetRequiredIndex(
            PdfContentSourceSpan span)
        {
            if (indexes.TryGetValue(
                    span,
                    out int index))
            {
                return index;
            }

            throw new InvalidOperationException(
                $"Display text source span {FormatSpan(span)} does not map to the supplied canonical operation source.");
        }
    }
}
