using PiratePDF.Fuzz.Runner;

if (args.Length == 0 || args[0] != "smoke")
{
    using var cancellation = new CancellationTokenSource();
    ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellation.Cancel();
    };
    Console.CancelKeyPress += cancelHandler;
    try
    {
        if (args.Length == 3 && args[0] == "station-artifact-inspect")
        {
            Console.WriteLine(StationArtifactInspection.Inspect(args[1], args[2]));
            return 0;
        }

        if (args.Length > 0 && args[0] == "station-lease-receive")
        {
            if (!OperatingSystem.IsWindows() || args.Length != 2) return 64;
            return await NativeStationTransaction.RunReceiverAsync(args[1], cancellation.Token);
        }

        if (args.Length > 0 && args[0] == "campaign-worker-internal")
            return await CampaignGatedWorkerCommand.ExecuteAsync(args, cancellation.Token);

        if (args.Length > 0 && args[0] == "campaign-mutate")
        {
            CampaignResult campaignResult = await CampaignCommand.ExecuteAsync(args, cancellation.Token);
            Console.WriteLine(campaignResult.Json);
            return campaignResult.ExitCode;
        }

        if (args.Length > 0 && args[0] is "retain-finding" or "reproduce-finding" or "compare-minimization" or "propose-promotion")
        {
            FindingResult findingResult = await FindingCommand.ExecuteAsync(args, cancellation.Token);
            Console.WriteLine(findingResult.Json);
            return (int)findingResult.ExitCode;
        }

        if (args.Length == 4 && args[0] == "smoke-control")
            return await SmokeControlCommand.ExecuteAsync(args[1], args[2], args[3], cancellation.Token);

        if (args.Length > 0 && args[0] is "mutate" or "mutate-one")
        {
            MutationResult mutationResult = await MutationCommand.ExecuteAsync(args, cancellation.Token);
            Console.WriteLine(mutationResult.Json);
            return (int)mutationResult.ExitCode;
        }

        if (args.Length > 0 && args[0] == "replay")
        {
            ReplayResult replayResult = await ReplayCommand.ExecuteAsync(args, cancellation.Token);
            Console.WriteLine(replayResult.Json);
            return (int)replayResult.ExitCode;
        }

        RunResult runResult = await new RunCommand().ExecuteAsync(args, cancellation.Token);
        Console.WriteLine(runResult.Json);
        return (int)runResult.ExitCode;
    }
    finally
    {
        Console.CancelKeyPress -= cancelHandler;
    }
}

if (args.Length != 3) return 2;
using var smokeCancellation = new CancellationTokenSource();
ConsoleCancelEventHandler smokeCancelHandler = (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    smokeCancellation.Cancel();
};
Console.CancelKeyPress += smokeCancelHandler;
try
{
    return await SmokeCommand.ExecuteAsync(args[1], args[2], smokeCancellation.Token);
}
finally
{
    Console.CancelKeyPress -= smokeCancelHandler;
}
