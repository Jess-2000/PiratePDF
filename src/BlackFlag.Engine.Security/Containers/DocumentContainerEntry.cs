namespace BlackFlag.Engine.Security.Containers;

/// <summary>Represents one expanded child produced by a safe container adapter.</summary>
public sealed record DocumentContainerEntry
{
    public DocumentContainerEntry(
        DocumentEmbeddedObject embeddedObject,
        long compressedLength,
        bool budgetAccounted = false)
    {
        ArgumentNullException.ThrowIfNull(embeddedObject);
        ArgumentOutOfRangeException.ThrowIfNegative(compressedLength);

        EmbeddedObject = embeddedObject;
        CompressedLength = compressedLength;
        BudgetAccounted = budgetAccounted;
    }

    public DocumentEmbeddedObject EmbeddedObject { get; }

    public long CompressedLength { get; }

    /// <summary>
    /// Gets whether the producing adapter already charged the shared inspection
    /// budget for this embedded object before expanding its bytes.
    /// </summary>
    public bool BudgetAccounted { get; }
}
