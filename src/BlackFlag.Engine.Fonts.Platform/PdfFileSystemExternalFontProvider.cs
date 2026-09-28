using System.Collections.ObjectModel;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using BlackFlag.Engine.Fonts.Sanitization;

namespace BlackFlag.Engine.Fonts.Platform;

/// <summary>
/// Provides the common sanitizer-backed implementation for OS font
/// directories. The cache contains trusted metadata and paths only.
/// </summary>
public abstract class PdfFileSystemExternalFontProvider :
    IPdfExternalFontProvider
{
    private readonly IReadOnlyList<string> searchDirectories;
    private readonly IPdfExternalFontSanitizer sanitizer;
    private readonly PdfExternalFontCatalogLimits limits;
    private readonly object snapshotLock = new();
    private ProviderSnapshot? snapshot;
    private ExceptionDispatchInfo? snapshotFailure;
    private bool building;

    protected PdfFileSystemExternalFontProvider(
        string id,
        IEnumerable<string> searchDirectories,
        IPdfExternalFontSanitizer? sanitizer = null)
        : this(id, searchDirectories, sanitizer, new PdfExternalFontCatalogLimits())
    {
    }

    protected PdfFileSystemExternalFontProvider(
        string id,
        IEnumerable<string> searchDirectories,
        IPdfExternalFontSanitizer? sanitizer,
        PdfExternalFontCatalogLimits limits)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(searchDirectories);
        ArgumentNullException.ThrowIfNull(limits);
        limits.Validate();

        Id = id;

        this.limits = limits;
        var directories = new HashSet<string>(
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        var orderedDirectories = new List<string>();
        int suppliedDirectories = 0;
        foreach (string path in searchDirectories)
        {
            if (suppliedDirectories == limits.MaximumSearchDirectories)
                throw new PdfExternalFontCatalogLimitException(
                    nameof(limits.MaximumSearchDirectories), limits.MaximumSearchDirectories);
            suppliedDirectories++;
            if (string.IsNullOrWhiteSpace(path)) continue;
            string fullPath = Path.GetFullPath(path);
            if (directories.Add(fullPath)) orderedDirectories.Add(fullPath);
        }
        this.searchDirectories = orderedDirectories;

        this.sanitizer =
            sanitizer ??
            new PdfOpenTypeFontSanitizer();
    }

    public string Id { get; }

    /// <summary>
    /// Returns a complete snapshot. Cold cancellation or aggregate exhaustion
    /// publishes nothing and permits a later explicit retry. Pre-canceled warm
    /// requests still throw without invalidating the completed snapshot.
    /// </summary>
    public IReadOnlyList<PdfExternalFontFace> GetFaces(
        CancellationToken cancellationToken = default)
        => new ReadOnlyCollection<PdfExternalFontFace>(GetSnapshot(cancellationToken).Faces);

    private ProviderSnapshot GetSnapshot(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ProviderSnapshot? completed = Volatile.Read(ref snapshot);
        if (completed is not null) return completed;

        // Keep one synchronous builder, while waiting callers observe their own
        // cancellation independently. This is a lock-wait slice, not a work budget.
        while (!Monitor.TryEnter(snapshotLock, millisecondsTimeout: 50))
            cancellationToken.ThrowIfCancellationRequested();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (snapshot is not null) return snapshot;
            snapshotFailure?.Throw();
            if (building)
                throw new InvalidOperationException("Recursive font catalog construction is not supported.");

            building = true;
            try
            {
                completed = BuildSnapshot(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                Volatile.Write(ref snapshot, completed);
                return completed;
            }
            catch (Exception exception) when (exception is not
                (OperationCanceledException or PdfExternalFontCatalogLimitException))
            {
                // Preserve Lazy's failure caching for unexpected defects. Caller
                // cancellation and aggregate exhaustion alone leave a retryable build.
                snapshotFailure = ExceptionDispatchInfo.Capture(exception);
                throw;
            }
            finally
            {
                building = false;
            }
        }
        finally
        {
            Monitor.Exit(snapshotLock);
        }
    }

    public bool SupportsRune(
        PdfExternalFontFace face,
        Rune rune)
    {
        ProviderEntry entry =
            GetRequiredEntry(
                face);

        return entry.Metadata.SupportsRune(
            rune);
    }

    public Stream OpenFontProgram(
        PdfExternalFontFace face)
    {
        ProviderEntry entry =
            GetRequiredEntry(
                face);

        return new FileStream(
            entry.Path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.SequentialScan);
    }

    protected virtual bool IsSupportedFontFile(
        string path) =>
        Path.GetExtension(path) switch
        {
            string extension
                when extension.Equals(
                    ".ttf",
                    StringComparison.OrdinalIgnoreCase) =>
                true,

            string extension
                when extension.Equals(
                    ".otf",
                    StringComparison.OrdinalIgnoreCase) =>
                true,

            _ =>
                false
        };

    private ProviderSnapshot BuildSnapshot(CancellationToken cancellationToken)
    {
        var budget = new PdfFontCatalogBuildBudget(limits);
        var entries =
            new Dictionary<string, ProviderEntry>(
                StringComparer.Ordinal);

        foreach (string path in EnumerateCandidates(budget, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var stream =
                    new FileStream(
                        path,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        bufferSize: 64 * 1024,
                        FileOptions.SequentialScan);

                cancellationToken.ThrowIfCancellationRequested();
                budget.CheckSourceLength(stream.Length);
                using var boundedStream = new PdfFontCatalogReadStream(stream, budget, cancellationToken);
                PdfSanitizedExternalFontProgram program =
                    sanitizer.Sanitize(
                        boundedStream,
                        cancellationToken: cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                budget.ThrowIfExceeded();

                PdfSanitizedExternalFontMetadata metadata =
                    PdfSanitizedExternalFontMetadataInspector.Inspect(
                        program);

                cancellationToken.ThrowIfCancellationRequested();

                string faceId =
                    CreateFaceId(
                        path);

                var face =
                    new PdfExternalFontFace(
                        Id,
                        faceId,
                        metadata.FamilyName,
                        metadata.FaceName,
                        metadata.Weight,
                        metadata.Stretch,
                        metadata.Slant,
                        program.Technology,
                        metadata.EmbeddingRights)
                    {
                        NoSubsetting = metadata.NoSubsetting,
                        BitmapEmbeddingOnly = metadata.BitmapEmbeddingOnly
                    };

                entries.TryAdd(
                    faceId,
                    new ProviderEntry(
                        face,
                        path,
                        metadata));
            }
            catch (Exception exception)
                when (exception is
                    IOException or
                    UnauthorizedAccessException or
                    PdfExternalFontSanitizationException or
                    InvalidDataException or
                    NotSupportedException)
            {
                // Installed-font discovery is best-effort. Invalid, unreadable,
                // collection-based, or unsupported programs are omitted rather
                // than weakening the sanitizer boundary.
            }
            cancellationToken.ThrowIfCancellationRequested();
            budget.ThrowIfExceeded();
        }

        cancellationToken.ThrowIfCancellationRequested();
        // Sorting is bounded by admitted standalone candidates and completes before
        // publication. Keep the existing family/face/id comparison policy exactly.
        PdfExternalFontFace[] faces = entries.Values.Select(static entry => entry.Face)
            .OrderBy(static face => face.FamilyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static face => face.FaceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static face => face.FaceId, StringComparer.Ordinal)
            .ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        return new ProviderSnapshot(entries, faces);
    }

    private IEnumerable<string> EnumerateCandidates(
        PdfFontCatalogBuildBudget budget,
        CancellationToken cancellationToken)
    {
        var seen =
            new HashSet<string>(
                OperatingSystem.IsWindows()
                    ? StringComparer.OrdinalIgnoreCase
                    : StringComparer.Ordinal);

        foreach (string directory in searchDirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(directory))
            {
                continue;
            }

            IEnumerable<string> files;

            try
            {
                files =
                    Directory.EnumerateFileSystemEntries(
                        directory,
                        "*",
                        SearchOption.TopDirectoryOnly);
            }
            catch (Exception exception)
                when (exception is
                    IOException or
                    UnauthorizedAccessException)
            {
                continue;
            }

            using IEnumerator<string> enumerator = files.GetEnumerator();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!enumerator.MoveNext()) break;
                cancellationToken.ThrowIfCancellationRequested();
                budget.AdmitDirectoryEntry();
                string file = enumerator.Current;
                // Count subdirectories too, but retain nonrecursive font discovery.
                if (Directory.Exists(file)) continue;
                string fullPath;

                try
                {
                    fullPath =
                        Path.GetFullPath(file);
                }
                catch (Exception exception)
                    when (exception is
                        ArgumentException or
                        NotSupportedException or
                        PathTooLongException)
                {
                    continue;
                }

                bool supported = IsSupportedFontFile(fullPath);
                cancellationToken.ThrowIfCancellationRequested();
                if (supported && !seen.Contains(fullPath))
                {
                    budget.AdmitCandidate();
                    seen.Add(fullPath);
                    yield return fullPath;
                }
            }
        }
    }

    private ProviderEntry GetRequiredEntry(
        PdfExternalFontFace face)
    {
        ArgumentNullException.ThrowIfNull(face);

        if (!string.Equals(
                face.ProviderId,
                Id,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Font face '{face.FaceId}' belongs to provider '{face.ProviderId}', not '{Id}'.",
                nameof(face));
        }

        if (!GetSnapshot(CancellationToken.None).Entries.TryGetValue(
                face.FaceId,
                out ProviderEntry? entry))
        {
            throw new InvalidOperationException(
                $"Font face '{face.FaceId}' is not present in provider '{Id}'.");
        }

        return entry;
    }

    private static string CreateFaceId(
        string path) =>
        Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    path)));

    private sealed record ProviderEntry(
        PdfExternalFontFace Face,
        string Path,
        PdfSanitizedExternalFontMetadata Metadata);

    private sealed record ProviderSnapshot(
        IReadOnlyDictionary<string, ProviderEntry> Entries,
        PdfExternalFontFace[] Faces);
}
