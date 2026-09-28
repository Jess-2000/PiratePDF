using System.Security.Cryptography;
using System.Text.Json;
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Read-only bounded manifest/file validation. Never invokes a target or launches a worker.</summary>
public static class CorpusValidator
{
    public const int MaximumManifestBytes = 128 * 1024;
    public const int MaximumEntries = 128;
    public const int MaximumFileSystemEntries = 256;

    public static async Task<CorpusValidationResult> ValidateAsync(string repositoryRoot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Reuse the established regular-file admission policy; do not open possible Unix FIFOs.
        if (!OperatingSystem.IsWindows()) return new(CorpusFailure.UnsupportedPlatform);
        try
        {
            string root = Path.Combine(Path.GetFullPath(repositoryRoot), "tests", "FuzzCorpus");
            ReadOnlyMemory<byte> manifest;
            try { manifest = await InputFile.ReadAsync(Path.Combine(root, "manifest.json"), MaximumManifestBytes, cancellationToken).ConfigureAwait(false); }
            catch (InputTooLargeException) { return new(CorpusFailure.ManifestTooLarge); }
            catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException) { return new(CorpusFailure.MissingManifest); }
            IReadOnlyList<CorpusSeed> seeds = CorpusManifestParser.Parse(manifest);
            CheckInventory(root, seeds, cancellationToken);
            foreach (CorpusSeed seed in seeds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Both components derive from validated registry/ID metadata, not normalized untrusted paths.
                string path = Path.Combine(root, seed.Target, seed.Id + ".bin");
                ReadOnlyMemory<byte> bytes;
                try { bytes = await InputFile.ReadAsync(path, TargetRegistry.Get(seed.Target).MaximumInputBytes, cancellationToken).ConfigureAwait(false); }
                catch (InputTooLargeException) { return new(CorpusFailure.SeedTooLarge); }
                catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException) { return new(CorpusFailure.MissingSeed); }
                if (bytes.Length != seed.Length) return new(CorpusFailure.LengthMismatch);
                if (Convert.ToHexString(SHA256.HashData(bytes.Span)) != seed.Sha256) return new(CorpusFailure.HashMismatch);
            }
            return new(CorpusFailure.None, seeds);
        }
        catch (CorpusValidationException error) { return new(error.Failure); }
        catch (JsonException) { return new(CorpusFailure.InvalidManifest); }
        catch (NotSupportedException) { return new(CorpusFailure.UnsafePath); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return new(CorpusFailure.IoFailure); }
        catch (ArgumentException) { return new(CorpusFailure.UnsafePath); }
        catch (InvalidOperationException) { return new(CorpusFailure.InvalidManifest); }
    }

    private static void CheckInventory(string root, IReadOnlyList<CorpusSeed> seeds, CancellationToken token)
    {
        var listed = new HashSet<string>(seeds.Select(static seed => seed.Path), StringComparer.Ordinal);
        int visited = 0;
        foreach (string path in Entries(root, ref visited, token))
        {
            FileAttributes attributes = Attributes(path);
            string name = Path.GetFileName(path);
            if (name is "manifest.json" or ".gitattributes" && (attributes & FileAttributes.Directory) == 0) continue;
            if ((attributes & FileAttributes.Directory) == 0 || !TargetRegistry.All.Any(target => target.Id == name))
                throw new CorpusValidationException(CorpusFailure.UnlistedFile);
            // Exactly one level, never recursive traversal; reject reparse points before entering a directory.
            foreach (string seedPath in Entries(path, ref visited, token))
            {
                attributes = Attributes(seedPath);
                string relative = "tests/FuzzCorpus/" + name + "/" + Path.GetFileName(seedPath);
                if ((attributes & FileAttributes.Directory) != 0 || !listed.Contains(relative))
                    throw new CorpusValidationException(CorpusFailure.UnlistedFile);
            }
        }
    }

    private static FileAttributes Attributes(string path)
    {
        FileAttributes attributes = File.GetAttributes(path);
        if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.Device)) != 0)
            throw new CorpusValidationException(CorpusFailure.UnsafePath);
        return attributes;
    }

    private static List<string> Entries(string directory, ref int visited, CancellationToken token)
    {
        var entries = new List<string>();
        foreach (string path in Directory.EnumerateFileSystemEntries(directory))
        {
            token.ThrowIfCancellationRequested();
            if (++visited > MaximumFileSystemEntries) throw new CorpusValidationException(CorpusFailure.TooManyFiles);
            entries.Add(path);
        }
        entries.Sort(StringComparer.Ordinal);
        return entries;
    }
}
