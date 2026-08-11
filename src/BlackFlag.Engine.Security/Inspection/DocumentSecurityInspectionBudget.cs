
namespace BlackFlag.Engine.Security.Inspection;

/// <summary>
/// Tracks one shared resource budget across a document and all recursively
/// inspected embedded content.
/// </summary>
public sealed class DocumentSecurityInspectionBudget
{
    private readonly DocumentSecurityInspectionLimits limits;
    private long expandedBytes;
    private long workUnits;
    private int embeddedObjects;
    private int observations;

    /// <summary>Initializes a shared inspection budget.</summary>
    public DocumentSecurityInspectionBudget(
        DocumentSecurityInspectionLimits? limits = null)
    {
        this.limits =
            limits ??
            DocumentSecurityInspectionLimits.Default;

        this.limits.Validate();
    }

    /// <summary>Gets the limits controlling this budget.</summary>
    public DocumentSecurityInspectionLimits Limits => limits;

    /// <summary>Gets expanded bytes consumed so far.</summary>
    public long ExpandedBytes => Interlocked.Read(ref expandedBytes);

    /// <summary>Gets work units consumed so far.</summary>
    public long WorkUnits => Interlocked.Read(ref workUnits);

    /// <summary>Gets embedded-object count consumed so far.</summary>
    public int EmbeddedObjects => Volatile.Read(ref embeddedObjects);

    /// <summary>Gets observation count consumed so far.</summary>
    public int Observations => Volatile.Read(ref observations);

    /// <summary>Validates a recursive container depth.</summary>
    public void ValidateContainerDepth(int depth)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(depth);

        if (depth > limits.MaximumContainerDepth)
        {
            throw new InvalidDataException(
                $"Security inspection container depth {depth} exceeds the configured maximum of {limits.MaximumContainerDepth}.");
        }
    }

    /// <summary>Accounts for an embedded object before recursively inspecting it.</summary>
    public void ConsumeEmbeddedObject(long expandedLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(expandedLength);

        if (expandedLength > limits.MaximumSingleEmbeddedObjectBytes)
        {
            throw new InvalidDataException(
                $"Embedded object size {expandedLength} exceeds the configured maximum of {limits.MaximumSingleEmbeddedObjectBytes} bytes.");
        }

        int count = Interlocked.Increment(ref embeddedObjects);
        if (count > limits.MaximumEmbeddedObjects)
        {
            Interlocked.Decrement(ref embeddedObjects);
            throw new InvalidDataException(
                "Security inspection exceeded the configured embedded-object limit.");
        }

        ConsumeExpandedBytes(expandedLength);
    }

    /// <summary>Accounts for expanded container bytes.</summary>
    public void ConsumeExpandedBytes(long byteCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);

        long value = Interlocked.Add(ref expandedBytes, byteCount);
        if (value > limits.MaximumExpandedBytes)
        {
            Interlocked.Add(ref expandedBytes, -byteCount);
            throw new InvalidDataException(
                "Security inspection exceeded the configured expanded-byte limit.");
        }
    }

    /// <summary>Validates a compressed-to-expanded size relationship.</summary>
    public void ValidateCompressionRatio(
        long compressedLength,
        long expandedLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(compressedLength);
        ArgumentOutOfRangeException.ThrowIfNegative(expandedLength);

        if (expandedLength == 0)
        {
            return;
        }

        if (compressedLength == 0)
        {
            throw new InvalidDataException(
                "A non-empty expanded payload cannot have a zero-byte compressed source.");
        }

        double ratio =
            expandedLength /
            (double)compressedLength;

        if (ratio > limits.MaximumCompressionRatio)
        {
            throw new InvalidDataException(
                $"Security inspection decompression ratio {ratio:F2}:1 exceeds the configured maximum of {limits.MaximumCompressionRatio:F2}:1.");
        }
    }

    /// <summary>Accounts for one normalized observation.</summary>
    public void ConsumeObservation()
    {
        int count = Interlocked.Increment(ref observations);
        if (count > limits.MaximumObservations)
        {
            Interlocked.Decrement(ref observations);
            throw new InvalidDataException(
                "Security inspection exceeded the configured observation limit.");
        }
    }

    /// <summary>Accounts for shared inspection work.</summary>
    public void ConsumeWork(long units = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(units);

        long value = Interlocked.Add(ref workUnits, units);
        if (value > limits.MaximumInspectionWorkUnits)
        {
            Interlocked.Add(ref workUnits, -units);
            throw new InvalidDataException(
                "Security inspection exceeded the configured work-unit limit.");
        }
    }
}
