using System.Collections.ObjectModel;
using BlackFlag.Engine.Editing.Pages;
using BlackFlag.Engine.Editing.Text;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.SDK.Editing;
using BlackFlag.Engine.Services.Text;

namespace BlackFlag.Engine.SDK.Adapters.Editing;

public sealed partial class PdfEditingSdkSession
{
    private readonly List<ExistingTextSourceRegistration> existingTextSources = [];
    private readonly Dictionary<Guid, ExistingTextTargetRegistration> existingTextTargets = [];
    private readonly Dictionary<PdfExistingTextIdentity, Guid> existingTextIds = [];

    /// <summary>
    /// Registers one host-created interpreted source snapshot for SDK discovery.
    /// The source is intentionally registered on the adapter rather than exposed
    /// through the isolated SDK interface.
    /// </summary>
    public Guid RegisterExistingTextSource(
        Guid pageId,
        PdfExistingTextDiscoverySource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        PdfEditablePage page = FindPage(pageId);

        for (int index = existingTextSources.Count - 1; index >= 0; index--)
        {
            ExistingTextSourceRegistration current = existingTextSources[index];
            if (current.Page.Id == pageId &&
                current.Source.SourceKind == source.SourceKind &&
                current.Source.ContentOwnerIdentifier == source.ContentOwnerIdentifier)
            {
                InvalidateExistingTextSource(current);
                existingTextSources.RemoveAt(index);
            }
        }

        var registration = new ExistingTextSourceRegistration(Guid.NewGuid(), page, source);
        existingTextSources.Add(registration);
        return registration.Id;
    }

    public IReadOnlyList<PdfExistingTextSdkTarget> DiscoverExistingText(Guid pageId)
    {
        _ = FindPage(pageId);
        var result = new List<PdfExistingTextSdkTarget>();

        foreach (ExistingTextSourceRegistration registration in existingTextSources)
        {
            if (!registration.IsCurrent || registration.Page.Id != pageId)
            {
                continue;
            }

            IReadOnlyList<PdfExistingTextDescriptor> descriptors =
                PdfExistingTextEditingService.Discover(session, registration.Page, registration.Source);

            foreach (PdfExistingTextDescriptor descriptor in descriptors)
            {
                result.Add(RegisterTarget(registration, descriptor));
            }
        }

        return Array.AsReadOnly(result.ToArray());
    }

    public IReadOnlyList<PdfExistingTextSdkSearchMatch> FindExistingText(
        string searchText,
        bool matchCase = true)
    {
        IReadOnlyList<PdfExistingTextSearchScope> scopes = CreateCurrentScopes();
        IReadOnlyList<PdfExistingTextSearchMatch> matches =
            PdfExistingTextEditingService.Find(session, scopes, searchText, matchCase);

        var result = new PdfExistingTextSdkSearchMatch[matches.Count];
        for (int index = 0; index < matches.Count; index++)
        {
            PdfExistingTextSearchMatch match = matches[index];
            ExistingTextSourceRegistration registration = GetScopeRegistration(scopes, match.ScopeIndex);
            PdfExistingTextSdkTarget target = RegisterTarget(registration, match.Descriptor);
            result[index] = new PdfExistingTextSdkSearchMatch(
                target.Id,
                match.TextElementIndex,
                match.TextOffset,
                match.TextLength,
                match.MatchedText);
        }

        return Array.AsReadOnly(result);
    }

    public PdfExistingTextSdkFindReplaceResult ReplaceAllExistingText(
        string searchText,
        string replacementText,
        bool matchCase = true,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PdfExistingTextSearchScope> scopes = CreateCurrentScopes();
        PdfExistingTextFindReplaceResult result =
            PdfExistingTextEditingService.ReplaceAll(
                session, scopes, searchText, replacementText, matchCase, cancellationToken);

        var mapped = new PdfExistingTextSdkSearchMatch[result.Matches.Count];
        for (int index = 0; index < result.Matches.Count; index++)
        {
            PdfExistingTextSearchMatch match = result.Matches[index];
            ExistingTextSourceRegistration registration = GetScopeRegistration(scopes, match.ScopeIndex);
            PdfExistingTextSdkTarget target = RegisterTarget(registration, match.Descriptor);
            mapped[index] = new PdfExistingTextSdkSearchMatch(
                target.Id, match.TextElementIndex, match.TextOffset, match.TextLength, match.MatchedText);
        }

        PdfEditingSdkChangeSet changeSet = MapChangeSet(result.ChangeSet);
        InvalidateAllExistingTextSources();
        return new PdfExistingTextSdkFindReplaceResult(mapped, changeSet);
    }

    public PdfExistingTextSdkReplaceResult ReplaceExistingText(
        Guid targetId,
        string replacementText,
        bool preserveAdvance = true,
        double tolerance = 0.001,
        CancellationToken cancellationToken = default)
    {
        ExistingTextTargetRegistration target = GetCurrentTarget(targetId);
        var (Operation, Measurement) = PdfExistingTextEditingService.Replace(
            session,
            target.Source.Page,
            target.Source.Source,
            target.Descriptor,
            replacementText,
            preserveAdvance,
            tolerance,
            cancellationToken);

        PdfEditingSdkOperation operation = MapOperation(Operation);
        PdfExistingTextSdkAdvanceMeasurement measurement = MapMeasurement(Measurement);
        InvalidateExistingTextSource(target.Source);
        return new PdfExistingTextSdkReplaceResult(operation, measurement);
    }

    public PdfEditingSdkOperation ReplacePositionedExistingText(
        Guid targetId,
        IReadOnlyList<string> replacementSegments,
        CancellationToken cancellationToken = default)
    {
        ExistingTextTargetRegistration target = GetCurrentTarget(targetId);
        var operation = PdfExistingTextEditingService.ReplacePositioned(
            session,
            target.Source.Page,
            target.Source.Source,
            target.Descriptor,
            replacementSegments,
            cancellationToken);

        PdfEditingSdkOperation mapped = MapOperation(operation);
        InvalidateExistingTextSource(target.Source);
        return mapped;
    }

    public PdfEditingSdkOperation ApplyExistingTextTypography(
        Guid targetId,
        PdfExistingTextSdkTypographyUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        ExistingTextTargetRegistration target = GetCurrentTarget(targetId);
        var operation = PdfExistingTextEditingService.ApplyTypography(
            session,
            target.Source.Page,
            target.Source.Source,
            target.Descriptor,
            new PdfExistingTextTypographyUpdate
            {
                FontSize = update.FontSize,
                CharacterSpacing = update.CharacterSpacing,
                WordSpacing = update.WordSpacing,
                HorizontalScaling = update.HorizontalScaling,
                TextRenderingMode = update.TextRenderingMode,
                TextRise = update.TextRise
            });

        PdfEditingSdkOperation mapped = MapOperation(operation);
        InvalidateExistingTextSource(target.Source);
        return mapped;
    }

    public PdfEditingSdkOperation ApplyExistingTextStyle(
        Guid targetId,
        PdfExistingTextSdkStyleUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        ExistingTextTargetRegistration target = GetCurrentTarget(targetId);
        var operation = PdfExistingTextEditingService.ApplyStyle(
            session,
            target.Source.Page,
            target.Source.Source,
            target.Descriptor,
            new PdfExistingTextStyleUpdate
            {
                ScriptStyle = update.ScriptStyle.HasValue
                    ? MapScriptStyle(update.ScriptStyle.Value)
                    : null,
                SuperscriptScale = update.SuperscriptScale,
                SuperscriptRise = update.SuperscriptRise,
                SubscriptScale = update.SubscriptScale,
                SubscriptDrop = update.SubscriptDrop,
                FillColor = MapColor(update.FillColor),
                StrokeColor = MapColor(update.StrokeColor),
                Decoration = MapDecoration(update.Decoration)
            });

        PdfEditingSdkOperation mapped = MapOperation(operation);
        InvalidateExistingTextSource(target.Source);
        return mapped;
    }

    public IReadOnlyList<PdfExistingTextSdkFontCandidate> GetExistingTextFontCandidates(Guid targetId)
    {
        ExistingTextTargetRegistration target = GetCurrentTarget(targetId);
        var candidates = PdfExistingTextEditingService.GetFontCandidates(target.Source.Source);
        return Array.AsReadOnly(candidates.Select(static candidate =>
            new PdfExistingTextSdkFontCandidate(
                candidate.ResourceName,
                candidate.FamilyName,
                candidate.FaceName,
                candidate.Weight,
                candidate.Stretch,
                candidate.Slant.ToString(),
                candidate.Technology.ToString(),
                candidate.EmbeddingPermission.ToString(),
                candidate.IsSubset,
                candidate.HasToUnicodeMap)).ToArray());
    }

    public PdfEditingSdkOperation ChangeExistingTextFont(
        Guid targetId,
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        ExistingTextTargetRegistration target = GetCurrentTarget(targetId);
        var operation = PdfExistingTextEditingService.ChangeFont(
            session, target.Source.Page, target.Source.Source, target.Descriptor, resourceName, cancellationToken);
        PdfEditingSdkOperation mapped = MapOperation(operation);
        InvalidateExistingTextSource(target.Source);
        return mapped;
    }

    private ReadOnlyCollection<PdfExistingTextSearchScope> CreateCurrentScopes()
    {
        PdfExistingTextSearchScope[] scopes =
            existingTextSources
                .Where(static source => source.IsCurrent)
                .Select(static source => new PdfExistingTextSearchScope(source.Page, source.Source))
                .ToArray();

        if (scopes.Length == 0)
        {
            throw new InvalidOperationException("No current existing-text sources are registered for this SDK editing session.");
        }

        return Array.AsReadOnly(scopes);
    }

    private ExistingTextSourceRegistration GetScopeRegistration(
        IReadOnlyList<PdfExistingTextSearchScope> scopes,
        int scopeIndex)
    {
        PdfExistingTextSearchScope scope = scopes[scopeIndex];
        return existingTextSources.Single(source =>
            source.IsCurrent &&
            ReferenceEquals(source.Page, scope.DisplayedPage) &&
            ReferenceEquals(source.Source, scope.Source));
    }

    private PdfExistingTextSdkTarget RegisterTarget(
        ExistingTextSourceRegistration source,
        PdfExistingTextDescriptor descriptor)
    {
        if (!existingTextIds.TryGetValue(descriptor.Identity, out Guid targetId))
        {
            targetId = Guid.NewGuid();
            existingTextIds.Add(descriptor.Identity, targetId);
        }

        existingTextTargets[targetId] = new ExistingTextTargetRegistration(source, descriptor);

        return new PdfExistingTextSdkTarget(
            targetId,
            source.Page.Id,
            source.Id,
            (PdfExistingTextSdkSourceKind)descriptor.Identity.SourceKind,
            GetTextSegments(source.Source, descriptor),
            descriptor.FontResourceName,
            descriptor.FontSize,
            new PdfExistingTextSdkTypography(
                descriptor.Typography.FontSize,
                descriptor.Typography.CharacterSpacing,
                descriptor.Typography.WordSpacing,
                descriptor.Typography.HorizontalScaling,
                descriptor.Typography.TextRenderingMode,
                descriptor.Typography.TextRise),
            (PdfExistingTextSdkEditability)descriptor.Editability,
            descriptor.EditabilityReason);
    }

    private ExistingTextTargetRegistration GetCurrentTarget(Guid targetId)
    {
        if (targetId == Guid.Empty)
        {
            throw new ArgumentException("An existing-text target id is required.", nameof(targetId));
        }

        if (!existingTextTargets.TryGetValue(targetId, out ExistingTextTargetRegistration? target) ||
            !target.Source.IsCurrent)
        {
            throw new InvalidOperationException(
                "The existing-text SDK target is stale or is not registered. Re-render/register the source and rediscover text.");
        }

        return target;
    }

    private static ReadOnlyCollection<string> GetTextSegments(
        PdfExistingTextDiscoverySource source,
        PdfExistingTextDescriptor descriptor)
    {
        PdfExistingTextIdentity identity = descriptor.Identity;
        int ordinal = 0;

        foreach (PdfDisplayItem item in source.DisplayList)
        {
            if (item is not PdfDisplayTextRun run ||
                run.SourceSpan.StreamIndex != identity.StreamIndex ||
                run.SourceSpan.StartOffset != identity.StartOffset ||
                run.SourceSpan.EndOffset != identity.EndOffset)
            {
                continue;
            }

            if (ordinal++ != identity.RunOrdinal)
            {
                continue;
            }

            string[] segments = [.. run.Elements
                .Where(static element => element.Kind == PdfTextRunElementKind.Text)
                .Select(static element => element.UnicodeText ?? string.Empty)];
            return Array.AsReadOnly(segments);
        }

        return Array.AsReadOnly([descriptor.UnicodeText ?? string.Empty]);
    }

    private void InvalidateExistingTextSource(ExistingTextSourceRegistration source)
    {
        source.IsCurrent = false;
        Guid[] targetIds = [.. existingTextTargets
            .Where(pair => ReferenceEquals(pair.Value.Source, source))
            .Select(static pair => pair.Key)];
        foreach (Guid id in targetIds)
        {
            existingTextTargets.Remove(id);
        }
    }

    private void InvalidateAllExistingTextSources()
    {
        foreach (ExistingTextSourceRegistration source in existingTextSources)
        {
            source.IsCurrent = false;
        }
        existingTextTargets.Clear();
    }

    private static PdfExistingTextSdkAdvanceMeasurement MapMeasurement(PdfTextAdvanceMeasurement measurement) =>
        new(
            measurement.OriginalAdvance,
            measurement.ReplacementAdvance,
            measurement.Delta,
            measurement.AbsoluteDelta,
            (PdfExistingTextSdkAdvanceFit)measurement.Fit);

    private static PdfExistingTextScriptStyle MapScriptStyle(PdfExistingTextSdkScriptStyle style) =>
        style switch
        {
            PdfExistingTextSdkScriptStyle.Normal => PdfExistingTextScriptStyle.Normal,
            PdfExistingTextSdkScriptStyle.Superscript => PdfExistingTextScriptStyle.Superscript,
            PdfExistingTextSdkScriptStyle.Subscript => PdfExistingTextScriptStyle.Subscript,
            _ => throw new ArgumentOutOfRangeException(nameof(style))
        };

    private static PdfExistingTextColor? MapColor(PdfExistingTextSdkColor? color)
    {
        if (color is null) return null;
        return color.ColorSpace switch
        {
            PdfExistingTextSdkColorSpace.DeviceGray when color.Components.Count == 1 =>
                PdfExistingTextColor.Gray(color.Components[0]),
            PdfExistingTextSdkColorSpace.DeviceRgb when color.Components.Count == 3 =>
                PdfExistingTextColor.Rgb(color.Components[0], color.Components[1], color.Components[2]),
            PdfExistingTextSdkColorSpace.DeviceCmyk when color.Components.Count == 4 =>
                PdfExistingTextColor.Cmyk(color.Components[0], color.Components[1], color.Components[2], color.Components[3]),
            _ => throw new ArgumentException("The SDK color component count does not match its device color space.", nameof(color))
        };
    }

    private static PdfExistingTextDecoration? MapDecoration(PdfExistingTextSdkDecoration? decoration)
    {
        if (decoration is null) return null;
        PdfExistingTextDecorationKind kind = decoration.Kind switch
        {
            PdfExistingTextSdkDecorationKind.Underline => PdfExistingTextDecorationKind.Underline,
            PdfExistingTextSdkDecorationKind.Strikeout => PdfExistingTextDecorationKind.Strikeout,
            _ => throw new ArgumentOutOfRangeException(nameof(decoration))
        };

        return decoration.IsAutomatic
            ? PdfExistingTextDecoration.Automatic(kind, decoration.Offset, decoration.Thickness)
            : new PdfExistingTextDecoration(kind, decoration.AdvanceWidth!.Value, decoration.Offset, decoration.Thickness);
    }

    private sealed class ExistingTextSourceRegistration
    {
        public ExistingTextSourceRegistration(Guid id, PdfEditablePage page, PdfExistingTextDiscoverySource source)
        {
            Id = id;
            Page = page;
            Source = source;
        }

        public Guid Id { get; }
        public PdfEditablePage Page { get; }
        public PdfExistingTextDiscoverySource Source { get; }
        public bool IsCurrent { get; set; } = true;
    }

    private sealed record ExistingTextTargetRegistration(
        ExistingTextSourceRegistration Source,
        PdfExistingTextDescriptor Descriptor);
}
