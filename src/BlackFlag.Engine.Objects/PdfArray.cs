namespace BlackFlag.Engine.Objects;

using System.Collections;

/// <summary>
/// Represents an immutable PDF array object.
/// </summary>
public sealed class PdfArray :
    PdfObject,
    IReadOnlyList<PdfObject>,
    IEquatable<PdfArray>
{
    private readonly PdfObject[] items;

    private PdfArray()
    {
        items = [];
    }

    /// <summary>
    /// Initializes a new PDF array from the supplied objects.
    /// </summary>
    /// <param name="items">The objects to include in the array.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="items"/> or one of its elements is
    /// <see langword="null"/>.
    /// </exception>
    public PdfArray(IEnumerable<PdfObject> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items is ICollection<PdfObject> collection)
        {
            this.items = new PdfObject[collection.Count];
            collection.CopyTo(this.items, 0);
        }
        else
        {
            this.items = [.. items];
        }

        for (int index = 0; index < this.items.Length; index++)
        {
            if (this.items[index] is null)
            {
                throw new ArgumentNullException(
                    nameof(items),
                    "A PDF array cannot contain a CLR null reference. "
                    + "Use PdfNull.Instance for a PDF null value.");
            }
        }
    }

    /// <summary>
    /// Gets the canonical empty PDF array.
    /// </summary>
    public static PdfArray Empty { get; } = new();

    /// <inheritdoc />
    public override PdfObjectType Type => PdfObjectType.Array;

    /// <inheritdoc />
    public int Count => items.Length;

    /// <inheritdoc />
    public PdfObject this[int index] => items[index];

    /// <inheritdoc />
    public override TResult Accept<TResult>(PdfObjectVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.Visit(this);
    }

    /// <inheritdoc />
    public bool Equals(PdfArray? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null || items.Length != other.items.Length)
        {
            return false;
        }

        for (int index = 0; index < items.Length; index++)
        {
            if (!items[index].Equals(other.items[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PdfArray other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hash = new();

        for (int index = 0; index < items.Length; index++)
        {
            hash.Add(items[index]);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Returns an allocation-free enumerator for the objects in this array.
    /// </summary>
    /// <returns>An enumerator over the array's objects.</returns>
    public Enumerator GetEnumerator() => new(items);

    IEnumerator<PdfObject> IEnumerable<PdfObject>.GetEnumerator() =>
        ((IEnumerable<PdfObject>)items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable)items).GetEnumerator();

    /// <summary>
    /// Enumerates the objects in a <see cref="PdfArray"/>.
    /// </summary>
    public struct Enumerator
    {
        private readonly PdfObject[] items;
        private int index;

        internal Enumerator(PdfObject[] items)
        {
            this.items = items;
            index = -1;
        }

        /// <summary>
        /// Gets the object at the current enumerator position.
        /// </summary>
        public readonly PdfObject Current => items[index];

        /// <summary>
        /// Advances the enumerator to the next object.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the enumerator advanced to an object;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool MoveNext()
        {
            int nextIndex = index + 1;

            if ((uint)nextIndex < (uint)items.Length)
            {
                index = nextIndex;
                return true;
            }

            index = items.Length;
            return false;
        }
    }
}
