using System.Collections.ObjectModel;

using BlackFlag.Engine.Editing.Transactions;

namespace BlackFlag.Engine.Services.Text;

/// <summary>
/// Describes one atomic replace-all result and its original logical matches.
/// </summary>
public sealed class PdfExistingTextFindReplaceResult
{
    private readonly ReadOnlyCollection<PdfExistingTextSearchMatch> matches;

    internal PdfExistingTextFindReplaceResult(
        IEnumerable<PdfExistingTextSearchMatch> matches,
        PdfEditChangeSet changeSet)
    {
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(changeSet);

        this.matches =
            Array.AsReadOnly(
                matches.ToArray());

        ChangeSet = changeSet;
    }

    /// <summary>Gets the logical matches that were replaced.</summary>
    public IReadOnlyList<PdfExistingTextSearchMatch> Matches =>
        matches;

    /// <summary>Gets the single committed Editing transaction.</summary>
    public PdfEditChangeSet ChangeSet { get; }

    /// <summary>Gets the number of logical matches replaced.</summary>
    public int MatchCount =>
        matches.Count;
}
