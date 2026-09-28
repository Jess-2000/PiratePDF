using System.Runtime.InteropServices;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Strict fixed-position commands. Artifact metadata can never select a host, worker or destination.</summary>
public static class FindingCommand
{
    public static async Task<FindingResult> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args is null || args.Length < 7 || args[1] != "--repository-root" || args[3] != "--run" || args[5] != "--finding")
            return new(FindingExit.InvalidInvocation);
        try
        {
            var store = new FindingStore(args[2], args[4]);
            _ = store.CandidatePath(args[6]);
            string host = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..",
                OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"));
            var supervisor = new WorkerSupervisor(host, typeof(RunCommand).Assembly.Location);
            switch (args[0])
            {
                case "retain-finding" when args.Length == 11 && args[7] == "--input" && args[9] == "--source":
                    FindingSource source = FindingJson.Parse<FindingSource>(await InputFile.ReadAsync(store.IncomingPath(args[10]),
                        FindingStore.MaximumMetadataBytes, cancellationToken).ConfigureAwait(false));
                    return await store.RetainAsync(args[6], args[8], source, cancellationToken).ConfigureAwait(false);
                case "reproduce-finding" when args.Length == 7:
                    return await new FindingReproduction(supervisor).RunAsync(store, args[6], cancellationToken).ConfigureAwait(false);
                case "compare-minimization" when args.Length == 9 && args[7] == "--smaller":
                    return await FindingMinimization.CompareAsync(store, args[6], args[8], supervisor, cancellationToken).ConfigureAwait(false);
                case "propose-promotion" when args.Length == 11 && args[7] == "--seed" && args[9] == "--decision":
                    if (!Enum.TryParse(args[10], ignoreCase: false, out PromotionDecision decision) ||
                        decision.ToString() != args[10] || !Enum.IsDefined(decision)) return new(FindingExit.InvalidInvocation);
                    return await FindingPromotion.ProposeAsync(store, args[6], args[8], decision, supervisor, cancellationToken).ConfigureAwait(false);
                default: return new(FindingExit.InvalidInvocation);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return new(FindingExit.Cancelled, FindingClass.Cancellation); }
        catch (Exception error) when (FindingFiles.IsAdmissionError(error)) { return FindingFiles.Failure(error); }
    }
}
