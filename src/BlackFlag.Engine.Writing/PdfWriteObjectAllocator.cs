using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>
/// Reserves generation-zero indirect-object identifiers from a write plan's
/// current object namespace without reusing historical gaps.
/// </summary>
public sealed class PdfWriteObjectAllocator
{
    private readonly HashSet<int> reservedObjectNumbers;
    private int nextCandidate;

    internal PdfWriteObjectAllocator(PdfWritePlan writePlan)
    {
        ArgumentNullException.ThrowIfNull(writePlan);

        reservedObjectNumbers = writePlan.Document.Objects.Identifiers
            .Select(identifier => identifier.ObjectNumber)
            .ToHashSet();

        nextCandidate = writePlan.NextObjectNumber;
    }

    /// <summary>
    /// Reserves and returns the next available generation-zero identifier.
    /// </summary>
    public PdfObjectIdentifier ReserveNext()
    {
        while (reservedObjectNumbers.Contains(nextCandidate))
        {
            if (nextCandidate == int.MaxValue)
            {
                throw new InvalidOperationException(
                    "The indirect-object namespace cannot allocate another object number.");
            }

            nextCandidate++;
        }

        int objectNumber = nextCandidate;
        reservedObjectNumbers.Add(objectNumber);

        if (nextCandidate < int.MaxValue)
        {
            nextCandidate++;
        }

        return new PdfObjectIdentifier(objectNumber);
    }
}
