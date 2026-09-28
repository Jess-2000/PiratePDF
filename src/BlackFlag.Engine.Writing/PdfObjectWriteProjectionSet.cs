using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>Owns a deterministic validated set of indirect-object write projections.</summary>
public sealed class PdfObjectWriteProjectionSet
{
    private readonly IReadOnlyList<PdfObjectIdentifier> identifiers;

    public PdfObjectWriteProjectionSet(IEnumerable<PdfObjectWriteProjection> projections)
    {
        ArgumentNullException.ThrowIfNull(projections);
        // Snapshot before validation and sorting so enumeration finishes once and caller storage is never reordered.
        PdfObjectWriteProjection[] snapshot = [.. projections];

        if (snapshot.Length == 0)
        {
            throw new ArgumentException("A write projection set cannot be empty.", nameof(projections));
        }

        for (int index = 0; index < snapshot.Length; index++)
        {
            _ = snapshot[index]
                ?? throw new ArgumentException("Write projections cannot contain null entries.", nameof(projections));
        }

        Array.Sort(
            snapshot,
            static (left, right) =>
            {
                int objectNumberComparison =
                    left.Identifier.ObjectNumber.CompareTo(right.Identifier.ObjectNumber);
                return objectNumberComparison != 0
                    ? objectNumberComparison
                    : left.Identifier.GenerationNumber.CompareTo(right.Identifier.GenerationNumber);
            });

        var objectNumbers = new HashSet<int>();
        for (int index = 0; index < snapshot.Length; index++)
        {
            PdfObjectWriteProjection projection = snapshot[index];

            if (!objectNumbers.Add(projection.Identifier.ObjectNumber))
            {
                throw new ArgumentException(
                    $"A write projection set cannot contain multiple active generations for object number {projection.Identifier.ObjectNumber}.",
                    nameof(projections));
            }
        }

        Projections = Array.AsReadOnly(snapshot);
        identifiers = Array.AsReadOnly(snapshot.Select(item => item.Identifier).ToArray());
    }

    public IReadOnlyList<PdfObjectWriteProjection> Projections { get; }
    public IReadOnlyList<PdfObjectIdentifier> Identifiers => identifiers;

    public void ApplyTo(PdfObjectStore objects)
    {
        ArgumentNullException.ThrowIfNull(objects);

        for (int index = 0; index < Projections.Count; index++)
        {
            PdfObjectWriteProjection projection = Projections[index];
            if (objects.Contains(projection.Identifier))
            {
                objects.Set(projection.Identifier, projection.Value);
            }
            else
            {
                objects.Add(projection.Identifier, projection.Value);
            }
        }
    }
}
