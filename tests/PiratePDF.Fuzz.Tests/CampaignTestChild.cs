using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

/// <summary>Owned finite fixture behavior only; production never accepts a worker mode or binary from input metadata.</summary>
internal static class CampaignTestChild
{
    internal static async Task<int> RunAsync(string[] args)
    {
        string input = args[4];
        string repository = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(input), "..", "..", "..", ".."));
        string mode = File.ReadAllText(Path.Combine(repository, "campaign-mode.txt"));
        // The parent admits at most 64 KiB. This test-only copy verifies immutable retention after an error.
        File.WriteAllBytes(Path.Combine(repository, "last-candidate.bin"), File.ReadAllBytes(input));
        File.AppendAllText(Path.Combine(repository, "campaign-executions.log"), args[2] + "\n");
        if (mode is "tree" or "orphanpipe" or "both-over" or "malformed" or "abnormal")
        {
            string[] forwarded = [.. args];
            forwarded[4] = Path.Combine(repository, mode);
            return await SupervisorTestChild.Main(forwarded);
        }
        if (mode == "retention-failure")
        {
            string run = Path.GetFileName(Path.GetDirectoryName(input));
            string findings = Path.Combine(repository, "artifacts", "fuzz", "findings", run);
            Directory.CreateDirectory(findings);
            File.WriteAllText(Path.Combine(findings, "foreign.txt"), "owned fixture");
        }
        if (mode == "cleanup-failure") File.WriteAllText(Path.Combine(Path.GetDirectoryName(input), "foreign.txt"), "owned fixture");
        if (mode == "evidence-corruption")
        {
            string run = Path.GetFileName(Path.GetDirectoryName(input));
            File.WriteAllText(Path.Combine(repository, "artifacts", "fuzz", "campaigns", "." + run + "-pending", "foreign.txt"), "owned fixture");
        }
        if (mode is "publication-rename" or "failure-publication-rename")
        {
            string run = Path.GetFileName(Path.GetDirectoryName(input));
            string destination = Path.Combine(repository, "artifacts", "fuzz", "campaigns", run);
            Directory.CreateDirectory(destination);
            File.WriteAllText(Path.Combine(destination, "foreign.txt"), "owned fixture");
        }
        if (mode == "publication-summary")
        {
            string run = Path.GetFileName(Path.GetDirectoryName(input));
            File.WriteAllText(Path.Combine(repository, "artifacts", "fuzz", "campaigns", "." + run + "-pending", "summary.json"), "owned fixture");
        }
        if (mode == "manifest-change") File.AppendAllText(Path.Combine(repository, "tests", "FuzzCorpus", "manifest.json"), " ");
        if (mode == "seed-change") File.WriteAllBytes(Path.Combine(repository, "tests", "FuzzCorpus", "filter-asciihex", "asciihex-invalid-character.bin"), [1]);
        FuzzTarget existing = TargetRegistry.Get(args[2]);
        var target = new FuzzTarget(existing.Id, 1, existing.MaximumInputBytes, existing.Isolation,
            (_, _) => mode is "failure" or "retention-failure" or "failure-publication-rename" ? throw new InvalidOperationException("PRIVATE_FIXTURE_FAILURE") :
                mode == "rejected" ? FuzzResult.Rejected("InvalidData") : FuzzResult.Success([]));
        RunResult result = await new RunCommand(new TargetRegistry([target])).ExecuteAsync(args);
        Console.WriteLine(result.Json);
        return (int)result.ExitCode;
    }
}
