using BlackFlag.Engine.Document.Objects;

namespace BlackFlag.Engine.Writing;

/// <summary>Owns a deterministic validated set of indirect-object write projections.</summary>
public sealed class PdfObjectWriteProjectionSet
{
    private readonly IReadOnlyList<PdfObjectIdentifier> identifiers;

    public PdfObjectWriteProjectionSet(IEnumerable<PdfObjectWriteProjection> projections)
    {
        ArgumentNullException.ThrowIfNull(projections);
        PdfObjectWriteProjection[] snapshot = [.. projections
            .OrderBy(item => item.Identifier.ObjectNumber)
            .ThenBy(item => item.Identifier.GenerationNumber)];

        if (snapshot.Length == 0)
        {
            throw new ArgumentException("A write projection set cannot be empty.", nameof(projections));
        }

        var objectNumbers = new HashSet<int>();
        for (int index = 0; index < snapshot.Length; index++)
        {
            PdfObjectWriteProjection projection = snapshot[index]
                ?? throw new ArgumentException("Write projections cannot contain null entries.", nameof(projections));

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
