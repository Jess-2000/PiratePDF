using System.Runtime.InteropServices;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Replays only tests/FuzzCorpus beneath one explicitly supplied absolute repository root.</summary>
public static class ReplayCommand
{
    public static Task<ReplayResult> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args is null || args.Length != 3 || args[0] != "replay" || args[1] != "--repository-root")
            return Task.FromResult(new ReplayResult(ReplayExitCode.InvalidInvocation));

        // Derive the installed host from this runtime, never PATH, the corpus or a command-line tool override.
        string host = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..",
            OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"));
        var supervisor = new WorkerSupervisor(host, typeof(RunCommand).Assembly.Location);
        return new CorpusReplay(supervisor).RunAsync(args[2], cancellationToken);
    }
}
