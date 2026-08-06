namespace BlackFlag.Engine.Objects;

using System.Collections;

/// <summary>
/// Represents an immutable PDF dictionary object.
/// </summary>
public sealed class PdfDictionary :
    PdfObject,
    IReadOnlyDictionary<PdfName, PdfObject>,
    IEquatable<PdfDictionary>
{
    private readonly KeyValuePair<PdfName, PdfObject>[] entries;
    private readonly Dictionary<PdfName, int> indexes;
    private readonly PdfName[] keys;
    private readonly PdfObject[] values;

    private PdfDictionary()
    {
        entries = [];
        indexes = [];
        keys = [];
        values = [];
    }

    /// <summary>
    /// Initializes a new PDF dictionary from the supplied entries.
    /// </summary>
    /// <param name="entries">The entries to include in the dictionary.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="entries"/>, one of its keys, or one of its values is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="entries"/> contains a duplicate key.
    /// </exception>
    public PdfDictionary(
        IEnumerable<KeyValuePair<PdfName, PdfObject>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        this.entries = entries is ICollection<KeyValuePair<PdfName, PdfObject>>
            collection
            ? CopyEntries(collection)
            : [.. entries];

        indexes = new Dictionary<PdfName, int>(this.entries.Length);
        keys = new PdfName[this.entries.Length];
        values = new PdfObject[this.entries.Length];

        for (int index = 0; index < this.entries.Length; index++)
        {
            KeyValuePair<PdfName, PdfObject> entry = this.entries[index];

            if (entry.Key is null)
            {
                throw new ArgumentNullException(
                    nameof(entries),
                    "A PDF dictionary cannot contain a CLR null key.");
            }

            if (entry.Value is null)
            {
                throw new ArgumentNullException(
                    nameof(entries),
                    "A PDF dictionary cannot contain a CLR null value. "
                    + "Use PdfNull.Instance for a PDF null value.");
            }

            if (!indexes.TryAdd(entry.Key, index))
            {
                throw new ArgumentException(
                    $"The PDF dictionary contains the duplicate key "
                    + $"'/{entry.Key.Value}'.",
                    nameof(entries));
            }

            keys[index] = entry.Key;
            values[index] = entry.Value;
        }
    }

    /// <summary>
    /// Gets the canonical empty PDF dictionary.
    /// </summary>
    public static PdfDictionary Empty { get; } = new();

    /// <inheritdoc />
    public override PdfObjectType Type => PdfObjectType.Dictionary;

    /// <inheritdoc />
    public int Count => entries.Length;

    /// <inheritdoc />
    public IEnumerable<PdfName> Keys => keys;

    /// <inheritdoc />
    public IEnumerable<PdfObject> Values => values;

    /// <inheritdoc />
    public PdfObject this[PdfName key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            if (!indexes.TryGetValue(key, out int index))
            {
                throw new KeyNotFoundException(
                    $"The PDF dictionary does not contain the key "
                    + $"'/{key.Value}'.");
            }

            return entries[index].Value;
        }
    }

    /// <inheritdoc />
    public bool ContainsKey(PdfName key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return indexes.ContainsKey(key);
    }

    /// <inheritdoc />
    public bool TryGetValue(PdfName key, out PdfObject value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (indexes.TryGetValue(key, out int index))
        {
            value = entries[index].Value;
            return true;
        }

        value = null!;
        return false;
    }

    /// <inheritdoc />
    public override TResult Accept<TResult>(PdfObjectVisitor<TResult> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.Visit(this);
    }

    /// <inheritdoc />
    public bool Equals(PdfDictionary? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null || entries.Length != other.entries.Length)
        {
            return false;
        }

        for (int index = 0; index < entries.Length; index++)
        {
            KeyValuePair<PdfName, PdfObject> entry = entries[index];

            if (!other.TryGetValue(entry.Key, out PdfObject otherValue)
                || !entry.Value.Equals(otherValue))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PdfDictionary other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        int hash = 0;

        for (int index = 0; index < entries.Length; index++)
        {
            KeyValuePair<PdfName, PdfObject> entry = entries[index];
            hash = unchecked(
                hash + HashCode.Combine(entry.Key, entry.Value));
        }

        return hash;
    }

    /// <summary>
    /// Returns an allocation-free enumerator for this dictionary's entries.
    /// </summary>
    /// <returns>An enumerator over the dictionary's entries.</returns>
    public Enumerator GetEnumerator() => new(entries);

    IEnumerator<KeyValuePair<PdfName, PdfObject>>
        IEnumerable<KeyValuePair<PdfName, PdfObject>>.GetEnumerator() =>
            ((IEnumerable<KeyValuePair<PdfName, PdfObject>>)entries)
            .GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable)entries).GetEnumerator();

    private static KeyValuePair<PdfName, PdfObject>[] CopyEntries(
        ICollection<KeyValuePair<PdfName, PdfObject>> entries)
    {
        KeyValuePair<PdfName, PdfObject>[] copy =
            new KeyValuePair<PdfName, PdfObject>[entries.Count];

        entries.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Enumerates the entries in a <see cref="PdfDictionary"/>.
    /// </summary>
    public struct Enumerator
    {
        private readonly KeyValuePair<PdfName, PdfObject>[] entries;
        private int index;

        internal Enumerator(
            KeyValuePair<PdfName, PdfObject>[] entries)
        {
            this.entries = entries;
            index = -1;
        }

        /// <summary>
        /// Gets the entry at the current enumerator position.
        /// </summary>
        public readonly KeyValuePair<PdfName, PdfObject> Current =>
            entries[index];

        /// <summary>
        /// Advances the enumerator to the next entry.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the enumerator advanced to an entry;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool MoveNext()
        {
            int nextIndex = index + 1;

            if ((uint)nextIndex < (uint)entries.Length)
            {
                index = nextIndex;
                return true;
            }

            index = entries.Length;
            return false;
        }
    }
}

