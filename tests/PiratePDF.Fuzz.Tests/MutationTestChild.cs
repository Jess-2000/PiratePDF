using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PiratePDF.Fuzz.Tests;

internal static class MutationTestChild
{
    internal static async Task<int> RunAsync(string[] args)
    {
        string work = Path.GetDirectoryName(args[4]);
        string repository = Path.GetFullPath(Path.Combine(work, "..", "..", ".."));
        string mode = File.ReadAllText(Path.Combine(repository, "mutation-mode.txt"));
        byte[] bytes = File.ReadAllBytes(args[4]);
        File.AppendAllText(Path.Combine(repository, "mutation-executions.log"), Convert.ToHexString(SHA256.HashData(bytes)) + "\n");
        switch (mode)
        {
            case "success":
            case "rejected": break;
            case "block-cleanup": File.WriteAllText(Path.Combine(work, "foreign.txt"), "Owned test cleanup obstacle"); break;
            case "change-hash": File.WriteAllBytes(Path.Combine(repository, "tests", "FuzzCorpus", "filter-asciihex", "a-success.bin"), [63]); break;
            case "change-length": File.WriteAllBytes(Path.Combine(repository, "tests", "FuzzCorpus", "filter-asciihex", "a-success.bin"), [62, 62]); break;
            default:
                string probes = Path.Combine(repository, "mutation-probes");
                Directory.CreateDirectory(probes);
                string[] forwarded = [.. args];
                forwarded[4] = Path.Combine(probes, mode);
                return await SupervisorTestChild.Main(forwarded);
        }
        bool reject = mode == "rejected";
        Console.WriteLine(new JsonObject
        {
            ["schemaVersion"] = 1, ["target"] = args[2], ["configurationVersion"] = 1,
            ["outcome"] = reject ? "rejected" : "success", ["code"] = "TargetResult",
            ["observation"] = reject ? "InvalidData" : FuzzResult.Success(bytes).Observation, ["exitCode"] = reject ? 1 : 0
        }.ToJsonString());
        return reject ? 1 : 0;
    }
}
