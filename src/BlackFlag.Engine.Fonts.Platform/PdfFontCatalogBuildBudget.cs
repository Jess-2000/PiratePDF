namespace BlackFlag.Engine.Fonts.Platform;

internal sealed class PdfFontCatalogBuildBudget(PdfExternalFontCatalogLimits limits)
{
    private int directoryEntries;
    private int candidates;
    private long sourceBytes;
    private PdfExternalFontCatalogLimitException? failure;

    internal long RemainingSourceBytes => limits.MaximumSourceBytes - sourceBytes;

    internal void AdmitDirectoryEntry()
    {
        if (directoryEntries == limits.MaximumDirectoryEntries)
            Fail(nameof(limits.MaximumDirectoryEntries), limits.MaximumDirectoryEntries);
        directoryEntries++;
    }

    internal void AdmitCandidate()
    {
        if (candidates == limits.MaximumCandidates)
            Fail(nameof(limits.MaximumCandidates), limits.MaximumCandidates);
        candidates++;
    }

    internal void CheckSourceLength(long length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ThrowIfExceeded();
        if (length > RemainingSourceBytes)
            Fail(nameof(limits.MaximumSourceBytes), limits.MaximumSourceBytes);
    }

    internal void RecordRead(long count)
    {
        // Subtraction precedes addition, including for a long.MaxValue host limit.
        CheckSourceLength(count);
        sourceBytes += count;
    }

    internal void ThrowIfExceeded()
    {
        if (failure is not null) throw failure;
    }

    private void Fail(string name, long limit)
    {
        failure = new PdfExternalFontCatalogLimitException(name, limit);
        throw failure;
    }
}
