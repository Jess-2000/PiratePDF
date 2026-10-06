using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Fixed compatibility experiment, not a configurable campaign interface.</summary>
public static class SmokeCommand
{
    public static async Task<int> ExecuteAsync(string repositoryRoot, string mode, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows() || !Path.IsPathFullyQualified(repositoryRoot) || mode is not
            ("filter-asciihex" or "probe-abnormal" or "probe-harness" or "probe-timeout" or "probe-outer-timeout" or "probe-cancel"))
            return 2;
        string root = Path.GetFullPath(repositoryRoot);
        string stage = Path.Combine(root, "artifacts", "fuzz", "initialization-20260925", "smoke-" + mode);
        string glue = Path.Combine(stage, "publish", "PiratePDF.Fuzz.SharpFuzz.dll");
        string driver = Path.Combine(root, "tools", "PiratePDF.Fuzz.SharpFuzz", "tools", "libfuzzer-dotnet-windows.exe");
        CheckDirectories(Path.GetDirectoryName(glue)!);
        using (FileStream stream = File.OpenRead(driver))
        {
            if (stream.Length != 1512448 || Convert.ToHexString(SHA256.HashData(stream)) !=
                "17AF5B3F6FF4D2C57B44B9A35C13051B570EB66F0557D00015DF3832709050BF")
                throw new InvalidDataException("Pinned smoke driver mismatch.");
        }
        if (!File.Exists(glue)) return 2;
        CorpusValidationResult corpus = await CorpusValidator.ValidateAsync(root, token).ConfigureAwait(false);
        if (corpus.Failure != CorpusFailure.None) return 2;
        string directory = Path.Combine(stage, mode);
        // Each invocation owns a new fixed directory; never overwrite a prior experiment.
        if (Directory.Exists(directory) || File.Exists(directory)) return 2;
        Directory.CreateDirectory(directory);
        CheckDirectories(directory);
        string seeds = Path.Combine(directory, "seeds");
        Directory.CreateDirectory(seeds);
        foreach (CorpusSeed seed in corpus.Seeds.Where(static seed => seed.Target == "filter-asciihex"))
        {
            ReadOnlyMemory<byte> bytes = await InputFile.ReadAsync(Path.Combine(root, seed.Path),
                TargetRegistry.Get(seed.Target).MaximumInputBytes, token).ConfigureAwait(false);
            if (bytes.Length != seed.Length || Convert.ToHexString(SHA256.HashData(bytes.Span)) != seed.Sha256)
                throw new InvalidDataException("Smoke seed identity changed.");
            await using var file = new FileStream(Path.Combine(seeds, seed.Id + ".bin"), FileMode.CreateNew);
            await file.WriteAsync(bytes, token).ConfigureAwait(false);
        }
        string host = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..", "dotnet.exe"));
        bool outerProbe = mode is "probe-outer-timeout" or "probe-cancel";
        string target = outerProbe ? "probe-timeout" : mode;
        string lease = Path.Combine(directory, "child.lease");
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (mode == "probe-cancel") cancellation.CancelAfter(TimeSpan.FromSeconds(2));
        string[] arguments = [.. WindowsLibFuzzerArguments.Create(host, glue, target),
            "-seed=1049", "-runs=128", "-max_total_time=3", "-timeout=1",
            "-max_len=16384", "-rss_limit_mb=512", "-print_final_stats=1",
            "-artifact_prefix=" + directory + Path.DirectorySeparatorChar, seeds];
        File.WriteAllText(Path.Combine(directory, "arguments.json"), JsonSerializer.Serialize(arguments));
        ChildResult result = await ChildProcess.RunAsync(driver, arguments, directory,
            TimeSpan.FromSeconds(mode == "probe-outer-timeout" ? 2 : 10), lease, cancellation.Token).ConfigureAwait(false);
        // Raw driver evidence stays byte-capped and ignored; the console contains only status metadata.
        File.WriteAllText(Path.Combine(directory, "result.json"), JsonSerializer.Serialize(result));
        Console.WriteLine(JsonSerializer.Serialize(new { mode, result.ExitCode, result.TimedOut, result.Cancelled,
            result.CleanupSucceeded, result.OutputTruncated, result.ErrorTruncated }));
        return !result.CleanupSucceeded ? 125 : result.Cancelled ? 130 : result.TimedOut ? 124 : result.ExitCode;
    }

    private static void CheckDirectories(string directory)
    {
        for (string? current = directory; current is not null; current = Path.GetDirectoryName(current))
        {
            FileAttributes attributes = File.GetAttributes(current);
            if ((attributes & FileAttributes.Directory) == 0 ||
                (attributes & (FileAttributes.ReparsePoint | FileAttributes.Device)) != 0)
                throw new IOException("Unsafe smoke directory.");
        }
    }
}
