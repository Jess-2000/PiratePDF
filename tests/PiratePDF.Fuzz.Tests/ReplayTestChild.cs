using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PiratePDF.Fuzz.Tests;

/// <summary>Controlled replay child only. Mutations affect isolated temporary test fixtures, never the owned corpus.</summary>
internal static class ReplayTestChild
{
    internal static async Task<int> RunAsync(string[] args)
    {
        string input = args[4];
        string id = Path.GetFileNameWithoutExtension(input);
        string repository = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(input), "..", "..", ".."));
        if (!File.Exists(input) || args[1] != "--target" || args[2] != "filter-asciihex" ||
            args[3] != "--input" || args[5] != "--config" || args[6] != "v1") return 91;
        File.AppendAllText(Path.Combine(repository, "executions.log"), id + "\n");
        string next = Path.Combine(Path.GetDirectoryName(input), "z-last.bin");
        switch (id)
        {
            case "a-change-hash": File.WriteAllBytes(next, [63]); break;
            case "a-change-length": File.WriteAllBytes(next, [62, 62]); break;
            case "a-change-over": File.WriteAllBytes(next, new byte[16385]); break;
            case "a-change-missing": File.Delete(next); break;
            case "a-success":
            case "a-rejected":
            case "z-last": break;
            case "a-input-invalid":
                Console.WriteLine(new JsonObject
                {
                    ["schemaVersion"] = 1, ["target"] = args[2], ["configurationVersion"] = 1,
                    ["outcome"] = "invalid", ["code"] = "InputUnavailable", ["observation"] = null, ["exitCode"] = 2
                }.ToJsonString());
                return 2;
            default:
                // Reuse existing finite crash/protocol/tree fixtures through their established entry point.
                string[] forwarded = [.. args];
                forwarded[4] = Path.Combine(Path.GetDirectoryName(input), id[2..]);
                return await SupervisorTestChild.Main(forwarded);
        }
        bool rejected = id == "a-rejected";
        Console.WriteLine(new JsonObject
        {
            ["schemaVersion"] = 1, ["target"] = args[2], ["configurationVersion"] = 1,
            ["outcome"] = rejected ? "rejected" : "success", ["code"] = "TargetResult",
            ["observation"] = rejected ? "InvalidData" : FuzzResult.Success([]).Observation,
            ["exitCode"] = rejected ? 1 : 0
        }.ToJsonString());
        return rejected ? 1 : 0;
    }
}
