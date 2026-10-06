using System.Security.Cryptography;
using System.Text.Json;

namespace PiratePDF.Fuzz.Runner;

internal static class FindingFiles
{
    internal static bool Id(string? value) => value is { Length: > 0 and <= 64 } &&
        value[0] is >= 'a' and <= 'z' && value.All(static c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

    internal static bool Hash(string? value) => value is { Length: 64 } &&
        value.All(static c => c is >= 'A' and <= 'F' or >= '0' and <= '9');

    internal static string Digest(ReadOnlyMemory<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes.Span));

    internal static string Root(string repository)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(repository) || repository.Length > 1024 ||
            !Path.IsPathFullyQualified(repository) || repository.StartsWith(@"\\", StringComparison.Ordinal) ||
            repository.AsSpan(2).Contains(':')) throw new FindingException(FindingExit.InvalidInvocation);
        string root = Path.GetFullPath(repository);
        CheckDirectory(root, false);
        return root;
    }

    internal static void CheckDirectory(string path, bool missing)
    {
        for (string? current = path; current is not null; current = Path.GetDirectoryName(current))
        {
            if (missing && !Path.Exists(current)) continue;
            FileAttributes attributes = File.GetAttributes(current);
            if ((attributes & FileAttributes.Directory) == 0 ||
                (attributes & (FileAttributes.ReparsePoint | FileAttributes.Device)) != 0)
                throw new FindingException(FindingExit.IntegrityFailure);
        }
    }

    internal static void CreateDirectory(string path)
    {
        CheckDirectory(path, true);
        Directory.CreateDirectory(path);
        CheckDirectory(path, false);
    }

    internal static async Task WriteNewAsync(string path, ReadOnlyMemory<byte> bytes, CancellationToken token)
    {
        CheckDirectory(Path.GetDirectoryName(path)!, false);
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            1, FileOptions.Asynchronous);
        await stream.WriteAsync(bytes, token).ConfigureAwait(false);
    }

    internal static Task WriteJsonAsync<T>(string path, T value, CancellationToken token)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, FindingJson.Options);
        if (bytes.Length > FindingStore.MaximumMetadataBytes) throw new FindingException(FindingExit.LimitExceeded);
        return WriteNewAsync(path, bytes, token);
    }

    internal static bool Cleanup(string root, IEnumerable<string> files, IEnumerable<string> directories)
    {
        try
        {
            CheckDirectory(root, false);
            foreach (string file in files.Reverse())
            {
                if (!Path.Exists(file)) continue;
                CheckDirectory(Path.GetDirectoryName(file)!, false);
                if ((File.GetAttributes(file) & (FileAttributes.Directory | FileAttributes.ReparsePoint | FileAttributes.Device)) != 0) return false;
                File.Delete(file);
            }
            foreach (string directory in directories.Reverse())
            {
                CheckDirectory(directory, false);
                Directory.Delete(directory, recursive: false);
            }
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or FindingException) { return false; }
    }

    internal static bool IsAdmissionError(Exception error) => error is IOException or UnauthorizedAccessException or
        NotSupportedException or ArgumentException or JsonException or FindingException;

    internal static FindingResult Failure(Exception error) => error switch
    {
        FindingException finding => new(finding.Exit,
            finding.Exit == FindingExit.Duplicate ? FindingClass.Duplicate : FindingClass.IntegrityFailure),
        _ => new(FindingExit.IntegrityFailure, FindingClass.IntegrityFailure)
    };
}
