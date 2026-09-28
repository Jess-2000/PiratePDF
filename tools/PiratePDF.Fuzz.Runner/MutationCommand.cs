using System.Globalization;
using System.Runtime.InteropServices;

namespace PiratePDF.Fuzz.Runner;

public static class MutationCommand
{
    public static Task<MutationResult> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args is null || args.Length is not (7 or 13) || args[0] is not ("mutate" or "mutate-one")) return Usage();
        bool single = args[0] == "mutate-one";
        if (args.Length != (single ? 13 : 7)) return Usage();
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        string[] allowed = single ? ["--repository-root", "--algorithm", "--target", "--seed", "--config", "--mutation"] :
            ["--repository-root", "--algorithm", "--per-seed"];
        for (int index = 1; index < args.Length; index += 2)
        {
            if (!allowed.Contains(args[index], StringComparer.Ordinal) || string.IsNullOrEmpty(args[index + 1]) ||
                args[index + 1].Length > (args[index] == "--repository-root" ? 32767 : 64) ||
                !options.TryAdd(args[index], args[index + 1])) return Usage();
        }
        if (!Number(options[single ? "--mutation" : "--per-seed"], out int count)) return Usage();
        int version = 0;
        if (single && (!options["--config"].StartsWith('v') || !Number(options["--config"][1..], out version) || version == 0)) return Usage();
        string host = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..",
            OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"));
        var mutation = new CorpusMutation(new WorkerSupervisor(host, typeof(RunCommand).Assembly.Location));
        return single ? mutation.RunOneAsync(options["--repository-root"], options["--algorithm"], options["--target"],
            options["--seed"], version, count, cancellationToken) :
            mutation.RunBatchAsync(options["--repository-root"], options["--algorithm"], count, cancellationToken);
    }

    private static bool Number(string value, out int number) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number) &&
        value == number.ToString(CultureInfo.InvariantCulture);

    private static Task<MutationResult> Usage() => Task.FromResult(new MutationResult(MutationExitCode.InvalidRequest, MutationFailure.Usage));
}
