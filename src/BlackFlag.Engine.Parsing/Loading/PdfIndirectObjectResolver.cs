using System.Collections.Concurrent;
using BlackFlag.Engine.Abstractions.IO;
using BlackFlag.Engine.Filters;
using BlackFlag.Engine.IO.RandomAccess;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Parsing.CrossReferences;
using BlackFlag.Engine.Parsing.ObjectStreams;
using BlackFlag.Engine.Syntax.Lexing;
using BlackFlag.Engine.Syntax.Parsing;

namespace BlackFlag.Engine.Parsing.Loading;

/// <summary>
/// Resolves direct and compressed indirect objects with lazy thread-safe
/// caching.
/// </summary>
public sealed class PdfIndirectObjectResolver : IPdfIndirectObjectResolver
{
    private readonly IRandomAccessSource source;
    private readonly PdfObjectLocationTable locationTable;
    private readonly PdfObjectStreamParser objectStreamParser;
    private readonly ConcurrentDictionary<PdfObjectId, Lazy<PdfObject>> cache =
        new();
    private readonly AsyncLocal<HashSet<PdfObjectId>?> activeResolutions = new();

    /// <summary>Initializes an indirect-object resolver.</summary>
    public PdfIndirectObjectResolver(
        IRandomAccessSource source,
        PdfObjectLocationTable locationTable,
        IPdfFilterPipeline filterPipeline,
        PdfFilterDecodeLimits decodeLimits)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(locationTable);
        ArgumentNullException.ThrowIfNull(filterPipeline);
        ArgumentNullException.ThrowIfNull(decodeLimits);

        this.source = source;
        this.locationTable = locationTable;
        objectStreamParser = new PdfObjectStreamParser(
            filterPipeline,
            decodeLimits);
    }

    /// <inheritdoc />
    public PdfObject Resolve(PdfObjectId objectId)
    {
        HashSet<PdfObjectId> active =
            activeResolutions.Value ??= [];

        if (!active.Add(objectId))
        {
            throw CreateException(
                objectId,
                PdfObjectResolutionFailure.CircularDependency,
                $"Circular indirect-object resolution detected for {objectId}.");
        }

        try
        {
            Lazy<PdfObject> lazy = cache.GetOrAdd(
                objectId,
                id => new Lazy<PdfObject>(
                    () => Load(id),
                    LazyThreadSafetyMode.ExecutionAndPublication));

            return lazy.Value;
        }
        catch
        {
            cache.TryRemove(objectId, out _);
            throw;
        }
        finally
        {
            active.Remove(objectId);
            if (active.Count == 0)
            {
                activeResolutions.Value = null;
            }
        }
    }

    /// <inheritdoc />
    public bool TryResolve(
        PdfObjectId objectId,
        out PdfObject? value)
    {
        try
        {
            value = Resolve(objectId);
            return true;
        }
        catch (PdfObjectResolutionException exception)
            when (exception.Failure is
                PdfObjectResolutionFailure.Missing or
                PdfObjectResolutionFailure.Free)
        {
            value = null;
            return false;
        }
    }

    private PdfObject Load(PdfObjectId objectId)
    {
        if (!locationTable.TryGetLocation(objectId, out PdfObjectLocation? location))
        {
            throw CreateException(
                objectId,
                PdfObjectResolutionFailure.Missing,
                $"No effective cross-reference entry exists for object {objectId}.");
        }

        return location!.Kind switch
        {
            PdfObjectLocationKind.Free => throw CreateException(
                objectId,
                PdfObjectResolutionFailure.Free,
                $"Object {objectId} is marked free."),
            PdfObjectLocationKind.Direct => LoadDirect(location),
            PdfObjectLocationKind.Compressed => LoadCompressed(location),
            _ => throw new ArgumentOutOfRangeException(
                nameof(objectId),
                location.Kind,
                "The object location kind is not supported.")
        };
    }

    private PdfObject LoadDirect(PdfObjectLocation location)
    {
        try
        {
            var cursor = new RandomAccessCursor(source, location.Offset);
            var lexer = new PdfLexer(cursor);
            var reader = new PdfTokenReader(lexer);
            var parser = new PdfObjectParser(reader, source);

            PdfIndirectObject indirectObject = parser.ParseIndirectObject();

            if (
                indirectObject.ObjectNumber != location.ObjectId.ObjectNumber ||
                indirectObject.GenerationNumber != location.ObjectId.GenerationNumber)
            {
                throw CreateException(
                    location.ObjectId,
                    PdfObjectResolutionFailure.IdentifierMismatch,
                    $"The object at offset {location.Offset} identifies itself as "
                    + $"{indirectObject.ObjectNumber} "
                    + $"{indirectObject.GenerationNumber}, not {location.ObjectId}.");
            }

            return indirectObject.Value;
        }
        catch (PdfObjectResolutionException)
        {
            throw;
        }
        catch (Exception exception)
            when (exception is FormatException or
                EndOfStreamException or
                PdfUnexpectedTokenException or
                ArgumentException or
                OverflowException)
        {
            throw CreateException(
                location.ObjectId,
                PdfObjectResolutionFailure.MalformedObject,
                $"Object {location.ObjectId} could not be parsed at offset "
                + $"{location.Offset}.",
                exception);
        }
    }

    private PdfObject LoadCompressed(PdfObjectLocation location)
    {
        var objectStreamId = new PdfObjectId(
            location.ObjectStreamNumber,
            generationNumber: 0);

        PdfObject objectStreamValue = Resolve(objectStreamId);

        if (objectStreamValue is not PdfStream stream)
        {
            throw CreateException(
                location.ObjectId,
                PdfObjectResolutionFailure.ExpectedObjectStream,
                $"Object {objectStreamId} must be a stream to contain compressed "
                + $"object {location.ObjectId}.");
        }

        PdfCompressedObjects objects;

        try
        {
            objects = objectStreamParser.Parse(stream);
        }
        catch (Exception exception)
            when (exception is FormatException or
                InvalidDataException or
                ArgumentException or
                OverflowException)
        {
            throw CreateException(
                location.ObjectId,
                PdfObjectResolutionFailure.MalformedObject,
                $"Object stream {objectStreamId} could not be decoded.",
                exception);
        }

        if (location.ObjectStreamIndex >= objects.Entries.Count)
        {
            throw CreateException(
                location.ObjectId,
                PdfObjectResolutionFailure.CompressedObjectMismatch,
                $"Object-stream index {location.ObjectStreamIndex} is outside "
                + $"object stream {objectStreamId}.");
        }

        PdfCompressedObjectEntry entry =
            objects.Entries[location.ObjectStreamIndex];

        if (entry.ObjectNumber != location.ObjectId.ObjectNumber)
        {
            throw CreateException(
                location.ObjectId,
                PdfObjectResolutionFailure.CompressedObjectMismatch,
                $"Object-stream index {location.ObjectStreamIndex} identifies "
                + $"object {entry.ObjectNumber}, not "
                + $"{location.ObjectId.ObjectNumber}.");
        }

        return objects.GetByIndex(location.ObjectStreamIndex);
    }

    private static PdfObjectResolutionException CreateException(
        PdfObjectId objectId,
        PdfObjectResolutionFailure failure,
        string message,
        Exception? innerException = null) =>
        innerException is null
            ? new PdfObjectResolutionException(
                objectId,
                failure,
                message)
            : new PdfObjectResolutionException(
                objectId,
                failure,
                message,
                innerException);
}
