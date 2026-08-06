using System.Collections.ObjectModel;
using BlackFlag.Engine.IO.RandomAccess;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Syntax.Lexing;
using BlackFlag.Engine.Syntax.Parsing;

namespace BlackFlag.Engine.Parsing.ObjectStreams;

/// <summary>
/// Provides lazy access to objects decoded from one PDF object stream.
/// </summary>
public sealed class PdfCompressedObjects
{
    private readonly byte[] decodedBytes;
    private readonly ReadOnlyCollection<PdfCompressedObjectEntry> entries;
    private readonly Dictionary<int, PdfObject> cache = [];
    private readonly object cacheLock = new();

    internal PdfCompressedObjects(
        PdfDictionary dictionary,
        ReadOnlySpan<byte> decodedBytes,
        int firstObjectOffset,
        IEnumerable<PdfCompressedObjectEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentNullException.ThrowIfNull(entries);

        ArgumentOutOfRangeException.ThrowIfNegative(
            firstObjectOffset,
            nameof(firstObjectOffset));

        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            firstObjectOffset,
            decodedBytes.Length,
            nameof(firstObjectOffset));

        PdfCompressedObjectEntry[] copiedEntries =
            entries.ToArray();

        if (copiedEntries.Length == 0)
        {
            throw new ArgumentException(
                "An object stream must contain at least one indexed object.",
                nameof(entries));
        }

        var objectNumbers =
            new HashSet<int>();

        for (int index = 0; index < copiedEntries.Length; index++)
        {
            PdfCompressedObjectEntry entry =
                copiedEntries[index] ??
                throw new ArgumentException(
                    "An object stream index cannot contain a null entry.",
                    nameof(entries));

            if (entry.Index != index)
            {
                throw new ArgumentException(
                    $"Entry {index} reports index {entry.Index}.",
                    nameof(entries));
            }

            if (!objectNumbers.Add(entry.ObjectNumber))
            {
                throw new ArgumentException(
                    $"Object stream index contains duplicate object {entry.ObjectNumber}.",
                    nameof(entries));
            }

            if (
                index > 0 &&
                entry.RelativeOffset <= copiedEntries[index - 1].RelativeOffset)
            {
                throw new ArgumentException(
                    "Object stream offsets must be strictly increasing.",
                    nameof(entries));
            }

            if (
                firstObjectOffset + entry.RelativeOffset >=
                decodedBytes.Length)
            {
                throw new ArgumentException(
                    $"Object {entry.ObjectNumber} begins outside the decoded object stream.",
                    nameof(entries));
            }
        }

        Dictionary = dictionary;
        FirstObjectOffset = firstObjectOffset;
        this.decodedBytes = decodedBytes.ToArray();
        this.entries =
            Array.AsReadOnly(copiedEntries);
    }

    /// <summary>
    /// Gets the object stream dictionary.
    /// </summary>
    public PdfDictionary Dictionary { get; }

    /// <summary>
    /// Gets the byte offset at which compressed object data begins.
    /// </summary>
    public int FirstObjectOffset { get; }

    /// <summary>
    /// Gets the immutable compressed-object index.
    /// </summary>
    public IReadOnlyList<PdfCompressedObjectEntry> Entries =>
        entries;

    /// <summary>
    /// Gets the object at the specified zero-based object-stream index.
    /// </summary>
    public PdfObject GetByIndex(
        int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(
            index,
            nameof(index));

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(
            index,
            entries.Count,
            nameof(index));

        PdfCompressedObjectEntry entry =
            entries[index];

        lock (cacheLock)
        {
            if (cache.TryGetValue(
                    entry.ObjectNumber,
                    out PdfObject? cached))
            {
                return cached;
            }
        }

        PdfObject parsed =
            ParseEntry(entry);

        lock (cacheLock)
        {
            if (cache.TryGetValue(
                    entry.ObjectNumber,
                    out PdfObject? cached))
            {
                return cached;
            }

            cache.Add(
                entry.ObjectNumber,
                parsed);

            return parsed;
        }
    }

    /// <summary>
    /// Gets the compressed object with the specified object number.
    /// </summary>
    public PdfObject GetByObjectNumber(
        int objectNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            objectNumber,
            nameof(objectNumber));

        for (int index = 0; index < entries.Count; index++)
        {
            if (entries[index].ObjectNumber == objectNumber)
            {
                return GetByIndex(index);
            }
        }

        throw new KeyNotFoundException(
            $"Object stream does not contain object {objectNumber}.");
    }

    private PdfObject ParseEntry(
        PdfCompressedObjectEntry entry)
    {
        int start =
            checked(
                FirstObjectOffset +
                entry.RelativeOffset);

        int end =
            entry.Index + 1 < entries.Count
                ? checked(
                    FirstObjectOffset +
                    entries[entry.Index + 1].RelativeOffset)
                : decodedBytes.Length;

        if (end <= start)
        {
            throw new FormatException(
                $"Object stream entry {entry.ObjectNumber} has an invalid byte range.");
        }

        ReadOnlySpan<byte> objectBytes =
            decodedBytes.AsSpan(
                start,
                end - start);

        using var source =
            new ReadOnlyMemoryRandomAccessSource(
                objectBytes);

        var cursor =
            new RandomAccessCursor(source);

        var lexer =
            new PdfLexer(cursor);

        var reader =
            new PdfTokenReader(lexer);

        var parser =
            new PdfObjectParser(
                reader,
                source);

        PdfObject value =
            parser.Parse();

        if (!reader.EndOfFile)
        {
            throw new FormatException(
                $"Compressed object {entry.ObjectNumber} contains trailing PDF syntax.");
        }

        if (value is PdfStream)
        {
            throw new FormatException(
                $"Compressed object {entry.ObjectNumber} cannot be a stream object.");
        }

        return value;
    }
}

