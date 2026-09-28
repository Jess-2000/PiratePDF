using System.Runtime.InteropServices;
using System.Text.Json;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Bounded direct controls in a separately staged, operator-owned diagnostic directory.</summary>
public static class SmokeControlCommand
{
    public static async Task<int> ExecuteAsync(string repositoryRoot, string area, string mode, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows() || !Path.IsPathFullyQualified(repositoryRoot) || area.Length is 0 or > 64
            || area.Any(static value => !char.IsAsciiLetterOrDigit(value) && value != '-')
            || mode is not ("registry-direct" or "inspect-il" or "registry-runtime")) return 2;
        string directory = Path.Combine(repositoryRoot, "artifacts", "fuzz", "initialization-20260925", area);
        string glue = Path.Combine(directory, "publish", "PiratePDF.Fuzz.SharpFuzz.dll");
        if (!File.Exists(glue) || File.Exists(Path.Combine(directory, mode + ".json"))) return 2;
        string host = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..", "dotnet.exe"));
        if (mode == "registry-runtime")
        {
            // SharpFuzz's no-driver control reads its first argument as a file. Own this exact empty fixture.
            using var input = new FileStream(Path.Combine(directory, mode), FileMode.CreateNew);
        }
        ChildResult result = await ChildProcess.RunAsync(host, [glue, mode], directory,
            TimeSpan.FromSeconds(10), Path.Combine(directory, mode + ".lease"), token).ConfigureAwait(false);
        using var output = new StreamWriter(new FileStream(Path.Combine(directory, mode + ".json"), FileMode.CreateNew));
        output.Write(JsonSerializer.Serialize(result));
        Console.WriteLine(JsonSerializer.Serialize(new { area, mode, result.ExitCode, result.TimedOut,
            result.Cancelled, result.CleanupSucceeded, result.OutputTruncated, result.ErrorTruncated }));
        return !result.CleanupSucceeded ? 125 : result.TimedOut ? 124 : result.ExitCode;
    }
}
