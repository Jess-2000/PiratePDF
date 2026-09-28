namespace PiratePDF.Fuzz.Runner;

/// <summary>One exclusive bounded staging slot beneath the existing ignored artifacts/fuzz area.</summary>
internal sealed class MutationWorkspace : IDisposable
{
    private readonly string root;
    private FileStream? lease;
    private FileStream? reader;
    private bool ownsCase;
    private bool createdRoot;
    internal bool CleanupSucceeded { get; private set; }
    internal string InputPath => Path.Combine(root, "case.bin");

    internal MutationWorkspace(string repositoryRoot) => root = Path.Combine(repositoryRoot, "artifacts", "fuzz", "mutation-work");

    internal void Open()
    {
        // Never truncate an existing lease/case or recursively delete a caller-owned directory.
        string parent = Path.GetDirectoryName(root)!;
        CheckDirectories(parent, allowMissing: true);
        Directory.CreateDirectory(parent);
        CheckDirectories(parent, allowMissing: false);
        createdRoot = !Directory.Exists(root);
        Directory.CreateDirectory(root);
        CheckDirectories(root, allowMissing: false);
        lease = new FileStream(Path.Combine(root, "active.lock"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        if (HasForeignContents()) throw new IOException("Mutation workspace is not empty.");
    }

    internal async Task PrepareAsync(ReadOnlyMemory<byte> bytes, int maximum, CancellationToken token)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximum, bytes.Length);
        CheckDirectories(root, allowMissing: false);
        await using (var writer = new FileStream(InputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            bufferSize: 1, FileOptions.Asynchronous))
        {
            ownsCase = true;
            await writer.WriteAsync(bytes, token).ConfigureAwait(false);
        }
        // Hold a read-only sharing lease through worker consumption, reducing ordinary Windows write/delete races.
        // Ancestor substitution/hard-link confinement is still not an atomic security boundary.
        reader = new FileStream(InputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        ReadOnlyMemory<byte> stored = await InputFile.ReadAsync(InputPath, maximum, token).ConfigureAwait(false);
        if (!stored.Span.SequenceEqual(bytes.Span)) throw new IOException("Temporary input identity mismatch.");
    }

    internal bool ClearCase()
    {
        try
        {
            reader?.Dispose();
            reader = null;
            if (!ownsCase) return true;
            CheckDirectories(root, allowMissing: false);
            FileAttributes attributes = File.GetAttributes(InputPath);
            if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.Directory | FileAttributes.Device)) != 0) return false;
            File.Delete(InputPath);
            ownsCase = false;
            return !HasForeignContents();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException) { return false; }
    }

    public void Dispose() => CleanupSucceeded = Close();

    private bool Close()
    {
        bool clean = ClearCase();
        bool ownsLease = lease is not null;
        try
        {
            lease?.Dispose();
            lease = null;
            if (ownsLease)
            {
                CheckDirectories(root, allowMissing: false);
                File.Delete(Path.Combine(root, "active.lock"));
                // Refuse foreign contents; leave evidence and report failure instead of recursive removal.
                if (Directory.EnumerateFileSystemEntries(root).Any()) return false;
            }
            if (ownsLease && createdRoot && clean) Directory.Delete(root, recursive: false);
            return clean;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException) { return false; }
    }

    // At most two directory entries are visited: the one allowed lease, then the first foreign entry.
    private bool HasForeignContents() => Directory.EnumerateFileSystemEntries(root)
        .Any(path => !Path.GetFileName(path).Equals("active.lock", StringComparison.Ordinal));

    private static void CheckDirectories(string path, bool allowMissing)
    {
        string? current = path;
        while (current is not null)
        {
            if (!allowMissing || Directory.Exists(current) || File.Exists(current))
            {
                FileAttributes attributes = File.GetAttributes(current);
                if ((attributes & FileAttributes.Directory) == 0 ||
                    (attributes & (FileAttributes.ReparsePoint | FileAttributes.Device)) != 0) throw new NotSupportedException();
            }
            current = Path.GetDirectoryName(current);
        }
    }
}
