namespace PiratePDF.DocGen.Changes;

internal sealed class DocumentChangeSet
{
    private readonly DocumentChange[] changes;

    public DocumentChangeSet(
        IEnumerable<DocumentChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        this.changes = changes
            .OrderBy(
                change => change.FilePath,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(change => change.StartIndex)
            .ThenBy(change => change.ChangeType)
            .ToArray();

        ValidateNoOverlappingChanges(this.changes);
    }

    // =========================================================================
    // Change Collection
    // =========================================================================

    public IReadOnlyList<DocumentChange> Changes =>
        changes;

    public int Count =>
        changes.Length;

    public bool IsEmpty =>
        changes.Length == 0;

    public int FileCount =>
        changes
            .Select(change => change.FilePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

    // =========================================================================
    // Queries
    // =========================================================================

    public DocumentChange[] GetChangesForFile(
        string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fullPath = Path.GetFullPath(filePath);

        return changes
            .Where(change => string.Equals(
                change.FilePath,
                fullPath,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(change => change.StartIndex)
            .ToArray();
    }

    public IReadOnlyDictionary<string, int> GetChangeTypeCounts()
    {
        return changes
            .GroupBy(change => change.ChangeType)
            .OrderBy(group => group.Key)
            .ToDictionary(
                group => group.Key.ToString(),
                group => group.Count(),
                StringComparer.Ordinal);
    }

    // =========================================================================
    // Validation
    // =========================================================================

    private static void ValidateNoOverlappingChanges(
        DocumentChange[] changes)
    {
        foreach (var group in changes.GroupBy(
                     change => change.FilePath,
                     StringComparer.OrdinalIgnoreCase))
        {
            var orderedChanges = group
                .OrderBy(change => change.StartIndex)
                .ThenBy(change => change.EndIndex)
                .ToArray();

            for (var index = 1; index < orderedChanges.Length; index++)
            {
                var previous = orderedChanges[index - 1];
                var current = orderedChanges[index];

                if (ChangesOverlap(previous, current))
                {
                    throw new ArgumentException(
                        $"Changes overlap in '{group.Key}' at character " +
                        $"ranges [{previous.StartIndex}, {previous.EndIndex}) " +
                        $"and [{current.StartIndex}, {current.EndIndex}).",
                        nameof(changes));
                }
            }
        }
    }

    private static bool ChangesOverlap(
        DocumentChange first,
        DocumentChange second)
    {
        if (
            first.ChangeType == DocumentChangeType.Insert &&
            second.ChangeType == DocumentChangeType.Insert)
        {
            return first.StartIndex == second.StartIndex;
        }

        if (first.ChangeType == DocumentChangeType.Insert)
        {
            return first.StartIndex > second.StartIndex &&
                   first.StartIndex < second.EndIndex;
        }

        if (second.ChangeType == DocumentChangeType.Insert)
        {
            return second.StartIndex > first.StartIndex &&
                   second.StartIndex < first.EndIndex;
        }

        return
            first.StartIndex < second.EndIndex &&
            second.StartIndex < first.EndIndex;
    }
}