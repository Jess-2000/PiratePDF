namespace PiratePDF.Fuzz.Runner;

/// <summary>Finite H2b settings. Construction does not authorize an operational campaign.</summary>
internal sealed class NativeExecutionPolicy
{
    internal NativeExecutionPolicy(int executionSeconds, int cleanupSeconds, int scanMilliseconds,
        int scanWorkMilliseconds, long memoryBytes, int processes, long storageBytes, long fileBytes, int entries)
    {
        if (executionSeconds is < 1 or > 300 || cleanupSeconds is < 1 or > 10 ||
            scanMilliseconds is < 10 or > 1000 || scanWorkMilliseconds is < 1 or > 1000 ||
            memoryBytes is < 16777216 or > 4294967296 || processes is < 1 or > 8 ||
            storageBytes is < 16777216 or > 4294967296 || fileBytes < 1 || fileBytes > storageBytes ||
            entries is < 1 or > 10000)
            throw new ArgumentOutOfRangeException(nameof(executionSeconds), "H2b settings exceed the implementation admission envelope.");
        Execution = TimeSpan.FromSeconds(executionSeconds);
        Cleanup = TimeSpan.FromSeconds(cleanupSeconds);
        ScanInterval = TimeSpan.FromMilliseconds(scanMilliseconds);
        ScanWork = TimeSpan.FromMilliseconds(scanWorkMilliseconds);
        MemoryBytes = memoryBytes;
        Processes = processes;
        StorageBytes = storageBytes;
        FileBytes = fileBytes;
        Entries = entries;
    }

    internal TimeSpan Execution { get; }
    internal TimeSpan Cleanup { get; }
    internal TimeSpan ScanInterval { get; }
    internal TimeSpan ScanWork { get; }
    internal long MemoryBytes { get; }
    internal int Processes { get; }
    internal long StorageBytes { get; }
    internal long FileBytes { get; }
    internal int Entries { get; }
    internal TimeSpan Startup { get; } = TimeSpan.FromSeconds(5);
    internal TimeSpan Finalization { get; } = TimeSpan.FromSeconds(5);
    internal long CorpusBytes => StorageBytes / 2;
    internal long ArtifactBytes => StorageBytes / 4;
    internal long TemporaryBytes => StorageBytes / 4;
    internal int Artifacts => Math.Min(Entries, 128);
}
