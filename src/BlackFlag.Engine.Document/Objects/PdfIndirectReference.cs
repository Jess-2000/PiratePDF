namespace BlackFlag.Engine.Document.Objects;

/// <summary>
/// Represents a reference to an indirect PDF object.
/// </summary>
public sealed class PdfIndirectReference :
    PdfObject,
    IEquatable<PdfIndirectReference>
{
    public PdfIndirectReference(
        PdfObjectIdentifier identifier)
        : base(PdfObjectKind.IndirectReference)
    {
        Identifier = identifier;
    }

    public PdfIndirectReference(
        int objectNumber,
        int generationNumber = 0)
        : this(
            new PdfObjectIdentifier(
                objectNumber,
                generationNumber))
    {
    }

    public PdfObjectIdentifier Identifier { get; }

    public int ObjectNumber =>
        Identifier.ObjectNumber;

    public int GenerationNumber =>
        Identifier.GenerationNumber;

    public PdfObject Resolve(
        PdfObjectStore objectStore)
    {
        ArgumentNullException.ThrowIfNull(objectStore);

        return objectStore.Get(Identifier);
    }

    public T Resolve<T>(
        PdfObjectStore objectStore)
        where T : PdfObject
    {
        ArgumentNullException.ThrowIfNull(objectStore);

        return objectStore.Get<T>(Identifier);
    }

    public bool TryResolve(
        PdfObjectStore objectStore,
        out PdfObject? value)
    {
        ArgumentNullException.ThrowIfNull(objectStore);

        return objectStore.TryGet(
            Identifier,
            out value);
    }

    public bool Equals(
        PdfIndirectReference? other)
    {
        return other is not null &&
               Identifier == other.Identifier;
    }

    public override bool Equals(object? obj)
    {
        return obj is PdfIndirectReference other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return Identifier.GetHashCode();
    }

    public override string ToString()
    {
        return $"{Identifier} R";
    }

    public static bool operator ==(
        PdfIndirectReference? left,
        PdfIndirectReference? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(
        PdfIndirectReference? left,
        PdfIndirectReference? right)
    {
        return !Equals(left, right);
    }
}