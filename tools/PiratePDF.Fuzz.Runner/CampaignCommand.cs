using System.Globalization;
using System.Runtime.InteropServices;

namespace PiratePDF.Fuzz.Runner;

public static class CampaignCommand
{
    public static async Task<CampaignResult> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (!TryParse(args, out Dictionary<string, string> options, out CampaignConfiguration? configuration))
            return new(CampaignStop.InvalidInvocation);
        string host = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..",
            OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"));
        return await new CampaignRunner(new WorkerSupervisor(host, typeof(RunCommand).Assembly.Location)).RunAsync(
            options["--repository-root"], options["--run"], options["--target"], options["--revision"], configuration!, cancellationToken).ConfigureAwait(false);
    }

    private static bool TryParse(string[] args, out Dictionary<string, string> options, out CampaignConfiguration? configuration)
    {
        options = new(StringComparer.Ordinal);
        configuration = null;
        if (args is null || args.Length != 19 || args[0] != "campaign-mutate") return false;
        for (int index = 1; index < args.Length; index += 2)
        {
            if (args[index] is not ("--repository-root" or "--run" or "--target" or "--config" or "--algorithm" or
                "--seed" or "--cases" or "--seconds" or "--revision") || !options.TryAdd(args[index], args[index + 1])) return false;
        }
        if (options.Count != 9 || options["--config"] != "v1" || options["--algorithm"] != CampaignMutation.Algorithm ||
            options["--seed"] != "1049" || !Number(options["--cases"], 10000, out int cases) ||
            !Number(options["--seconds"], 60, out int seconds) ||
            !ValidIdentity(options["--repository-root"], options["--run"], options["--target"], options["--revision"])) return false;
        configuration = new(cases, TimeSpan.FromSeconds(seconds));
        return true;
    }

    private static bool Number(string text, int maximum, out int value)
    {
        value = 0;
        return text is { Length: > 0 and <= 5 } && text[0] != '0' && text.All(static c => c is >= '0' and <= '9') &&
            int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value is >= 1 && value <= maximum;
    }

    internal static bool ValidIdentity(string repository, string run, string target, string revision) =>
        repository is { Length: > 0 and <= 1024 } && Path.IsPathFullyQualified(repository) &&
        !repository.Contains('\0') && !repository.StartsWith(@"\\", StringComparison.Ordinal) &&
        !repository.Split(['/', '\\']).Any(static part => part is "." or "..") &&
        FindingFiles.Id(run) && run is not ("pending" or "proposal" or "proposal-work") &&
        target is "all" or "filter-asciihex" or "filter-flate" or "font-sanitize" &&
        revision is { Length: 40 } && revision.All(static c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
