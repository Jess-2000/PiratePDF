using System.Collections.ObjectModel;

namespace BlackFlag.Engine.SDK.Editing;

public enum PdfExistingTextSdkAdvanceFit
{
    Exact = 0,
    LocallyAdjustable = 1,
    RequiresReflow = 2
}

/// <summary>SDK-owned width comparison for one direct text replacement.</summary>
public sealed record PdfExistingTextSdkAdvanceMeasurement(
    double OriginalAdvance,
    double ReplacementAdvance,
    double Delta,
    double AbsoluteDelta,
    PdfExistingTextSdkAdvanceFit Fit);

/// <summary>Result of one direct width-aware replacement.</summary>
public sealed record PdfExistingTextSdkReplaceResult(
    PdfEditingSdkOperation Operation,
    PdfExistingTextSdkAdvanceMeasurement Measurement);

/// <summary>One SDK find result backed by an opaque target id.</summary>
public sealed record PdfExistingTextSdkSearchMatch(
    Guid TargetId,
    int TextElementIndex,
    int TextOffset,
    int TextLength,
    string MatchedText);

/// <summary>Result of one atomic SDK replace-all transaction.</summary>
public sealed class PdfExistingTextSdkFindReplaceResult
{
    private readonly ReadOnlyCollection<PdfExistingTextSdkSearchMatch> matches;

    public PdfExistingTextSdkFindReplaceResult(
        IEnumerable<PdfExistingTextSdkSearchMatch> matches,
        PdfEditingSdkChangeSet changeSet)
    {
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(changeSet);
        this.matches = Array.AsReadOnly(matches.ToArray());
        ChangeSet = changeSet;
    }

    public IReadOnlyList<PdfExistingTextSdkSearchMatch> Matches => matches;
    public PdfEditingSdkChangeSet ChangeSet { get; }
    public int MatchCount => matches.Count;
}

/// <summary>SDK-owned description of an already resolved font resource.</summary>
public sealed record PdfExistingTextSdkFontCandidate(
    string ResourceName,
    string FamilyName,
    string FaceName,
    int Weight,
    int Stretch,
    string Slant,
    string Technology,
    string EmbeddingPermission,
    bool IsSubset,
    bool HasToUnicodeMap);
