using System.Globalization;
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Dedicated nonrecursive worker entry; no input is opened before authenticated ACK.</summary>
public static class CampaignGatedWorkerCommand
{
    public static async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args is null || args.Length != 6 || args[0] != "campaign-worker-internal" ||
            !int.TryParse(args[5], NumberStyles.None, CultureInfo.InvariantCulture, out int index) ||
            index is < 0 or >= CampaignConfiguration.MaximumCases ||
            !TargetRegistry.All.Any(target => target.Id == args[1]))
            return 2;
        if (!await CampaignWorkerGate.AwaitAcknowledgementAsync(args[3], args[4], index, cancellationToken).ConfigureAwait(false))
            return 2;
        RunResult result = await new RunCommand().ExecuteAsync(["run", "--target", args[1], "--input", args[2], "--config", "v1"],
            cancellationToken).ConfigureAwait(false);
        Console.WriteLine(result.Json);
        return (int)result.ExitCode;
    }
}
