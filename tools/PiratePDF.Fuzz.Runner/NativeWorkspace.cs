using System.Diagnostics;

namespace PiratePDF.Fuzz.Runner;

internal enum NativeInspection { WithinPolicy, PolicyViolation, Incomplete }

/// <summary>Detection only. Neither scanning nor canonical strings establish a writable-storage boundary.</summary>
internal static class NativeWorkspace
{
    internal static NativeInspection InspectOwned(string root, NativeExecutionPolicy policy)
    {
        long started = Stopwatch.GetTimestamp();
        foreach ((string path, long bytes, int entries) in new[]
        {
            (root, policy.StorageBytes, policy.Entries),
            (Path.Combine(root, "corpus"), policy.CorpusBytes, policy.Entries),
            (Path.Combine(root, "artifacts"), policy.ArtifactBytes, policy.Artifacts),
            (Path.Combine(root, "temp"), policy.TemporaryBytes, policy.Entries)
        })
        {
            int remaining = (int)Math.Floor(policy.ScanWork.TotalMilliseconds - Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            if (remaining < 1) return NativeInspection.Incomplete;
            NativeInspection observed = Inspect(path, policy.FileBytes, entries, bytes, remaining);
            if (observed != NativeInspection.WithinPolicy) return observed;
        }
        return NativeInspection.WithinPolicy;
    }

    internal static async Task StageCopyAsync(string source, string destination, string expectedHash, int maximum, CancellationToken token)
    {
        NativeLaunchPolicy.ValidatePath(destination);
        if (maximum is < 1 or > 1048576 || expectedHash.Length != 64) throw new ArgumentException("Invalid staging budget/identity.");
        ReadOnlyMemory<byte> bytes = await InputFile.ReadAsync(source, maximum, token).ConfigureAwait(false);
        if (!Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes.Span)).Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Staged source identity mismatch.");
        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        await output.WriteAsync(bytes, token).ConfigureAwait(false);
        await output.FlushAsync(token).ConfigureAwait(false);
    }

    internal static NativeInspection Inspect(string root, long fileBytes, int maximumEntries, long maximumBytes, int workMilliseconds)
    {
        NativeLaunchPolicy.ValidatePath(root);
        if (fileBytes < 1 || maximumEntries < 1 || maximumBytes < 1 || workMilliseconds is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(fileBytes));
        long started = Stopwatch.GetTimestamp();
        long bytes = 0;
        int entries = 0;
        var pending = new Stack<string>();
        pending.Push(root);
        try
        {
            while (pending.Count != 0)
            {
                string directory = pending.Pop();
                if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) return NativeInspection.PolicyViolation;
                foreach (string path in Directory.EnumerateFileSystemEntries(directory))
                {
                    if (Stopwatch.GetElapsedTime(started).TotalMilliseconds > workMilliseconds) return NativeInspection.Incomplete;
                    if (++entries > maximumEntries) return NativeInspection.PolicyViolation;
                    FileAttributes attributes = File.GetAttributes(path);
                    if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.Device)) != 0) return NativeInspection.PolicyViolation;
                    if ((attributes & FileAttributes.Directory) != 0) pending.Push(path);
                    else
                    {
                        long length = new FileInfo(path).Length;
                        if (length > fileBytes || length > maximumBytes - bytes) return NativeInspection.PolicyViolation;
                        bytes += length;
                    }
                }
            }
            return NativeInspection.WithinPolicy;
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException) { return NativeInspection.Incomplete; }
    }
}
