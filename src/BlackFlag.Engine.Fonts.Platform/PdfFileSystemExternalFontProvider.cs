using System.Collections.ObjectModel;
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
    private readonly Lazy<ProviderSnapshot> snapshot;

    protected PdfFileSystemExternalFontProvider(
        string id,
        IEnumerable<string> searchDirectories,
        IPdfExternalFontSanitizer? sanitizer = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(searchDirectories);

        Id = id;

        this.searchDirectories =
        [
            .. searchDirectories
                .Where(
                    static path =>
                        !string.IsNullOrWhiteSpace(path))
                .Select(
                    static path =>
                        Path.GetFullPath(path))
                .Distinct(
                    OperatingSystem.IsWindows()
                        ? StringComparer.OrdinalIgnoreCase
                        : StringComparer.Ordinal)
        ];

        this.sanitizer =
            sanitizer ??
            new PdfOpenTypeFontSanitizer();

        snapshot =
            new Lazy<ProviderSnapshot>(
                BuildSnapshot,
                LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string Id { get; }

    public IReadOnlyList<PdfExternalFontFace> GetFaces(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ProviderSnapshot value =
            snapshot.Value;

        return new ReadOnlyCollection<PdfExternalFontFace>(
        [
            .. value.Entries.Values
                .Select(
                    static entry =>
                        entry.Face)
                .OrderBy(
                    static face =>
                        face.FamilyName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    static face =>
                        face.FaceName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    static face =>
                        face.FaceId,
                    StringComparer.Ordinal)
        ]);
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

    private ProviderSnapshot BuildSnapshot()
    {
        var entries =
            new Dictionary<string, ProviderEntry>(
                StringComparer.Ordinal);

        foreach (string path in EnumerateCandidates())
        {
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

                PdfSanitizedExternalFontProgram program =
                    sanitizer.Sanitize(
                        stream);

                PdfSanitizedExternalFontMetadata metadata =
                    PdfSanitizedExternalFontMetadataInspector.Inspect(
                        program);

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
        }

        return new ProviderSnapshot(
            entries);
    }

    private IEnumerable<string> EnumerateCandidates()
    {
        var seen =
            new HashSet<string>(
                OperatingSystem.IsWindows()
                    ? StringComparer.OrdinalIgnoreCase
                    : StringComparer.Ordinal);

        foreach (string directory in searchDirectories)
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            IEnumerable<string> files;

            try
            {
                files =
                    Directory.EnumerateFiles(
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

            foreach (string file in files)
            {
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

                if (IsSupportedFontFile(fullPath) &&
                    seen.Add(fullPath))
                {
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

        if (!snapshot.Value.Entries.TryGetValue(
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
        IReadOnlyDictionary<string, ProviderEntry> Entries);
}
