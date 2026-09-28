using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PiratePDF.Fuzz.Tests;

/// <summary>Entry point only for controlled child tests; never called by the ordinary test host.</summary>
public static class SupervisorTestChild
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "filter-asciihex")
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(args));
            return 0; // Controlled Windows argument-parser fixture, no target invocation.
        }

        if (args.Length == 2 && args[0] == "generate-owned-corpus")
        {
            CorpusSeedRecipes.WriteNewCorpus(args[1]);
            return 0;
        }

        if (args.Length == 2 && args[0] == "grandchild")
        {
            WriteIdentity(args[1]);
            await Task.Delay(TimeSpan.FromSeconds(60));
            return 99;
        }

        if (args.Length != 7 || args[0] != "run") return 91;
        if (Path.GetFileName(args[4]) == "input.bin" && args[4].Contains(Path.Combine("artifacts", "fuzz", "findings"), StringComparison.Ordinal))
            return await FindingTestChild.RunAsync(args);
        if (Path.GetFileName(Path.GetDirectoryName(args[4])) == "mutation-work") return await MutationTestChild.RunAsync(args);
        if (args[4].EndsWith(".bin", StringComparison.Ordinal)) return await ReplayTestChild.RunAsync(args);
        string input = args[4];
        string mode = Path.GetFileName(input);
        var record = new JsonObject
        {
            ["schemaVersion"] = 1,
            ["target"] = args[2],
            ["configurationVersion"] = 1,
            ["outcome"] = "success",
            ["code"] = "TargetResult",
            ["observation"] = FuzzResult.Success([]).Observation,
            ["exitCode"] = 0
        };
        switch (mode)
        {
            case "abnormal": return 91;
            case "missing": return 0;
            case "malformed": Console.Write("PRIVATE_DOCUMENT_NOT_JSON"); return 0;
            case "duplicate": Console.WriteLine(record.ToJsonString()); break;
            case "duplicate-property":
                Console.Write(record.ToJsonString().Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1", StringComparison.Ordinal));
                return 0;
            case "oversized-record": Console.Write(new string(' ', 513)); return 0;
            case "record-exact":
            case "record-over":
                string json = record.ToJsonString();
                Console.WriteLine(json.Insert(1, new string(' ', (mode == "record-exact" ? 512 : 513) - json.Length)));
                return 0;
            case "extra": record["private"] = "PRIVATE_DOCUMENT"; break;
            case "wrong-target": record["target"] = "font-sanitize"; break;
            case "wrong-config": record["configurationVersion"] = 2; break;
            case "wrong-schema": record["schemaVersion"] = 2; break;
            case "wrong-type": record["exitCode"] = "0"; break;
            case "wrong-outcome": record["outcome"] = "rejected"; break;
            case "wrong-code": record["code"] = "PRIVATE_DOCUMENT"; break;
            case "wrong-digest": record["observation"] = "PRIVATE_DOCUMENT"; break;
            case "invalid-utf8": await Console.OpenStandardOutput().WriteAsync(new byte[] { 123, 255, 125 }); return 0;
            case "stderr-only": Console.Error.WriteLine(record.ToJsonString()); return 0;
            case "stderr-message": Console.Error.Write("PRIVATE_DOCUMENT"); break;
            case "mismatch": Console.WriteLine(record.ToJsonString()); return 1;
            case "worker-failure":
                record["outcome"] = "failure";
                record["code"] = "UnexpectedTargetFailure";
                record["observation"] = null;
                record["exitCode"] = 4;
                Console.WriteLine(record.ToJsonString());
                return 4;
            case "spontaneous-cancel":
                record["outcome"] = "cancelled";
                record["code"] = "Cancelled";
                record["observation"] = null;
                record["exitCode"] = 3;
                Console.WriteLine(record.ToJsonString());
                return 3;
            case "stdout-exact": await WriteBlocksAsync(Console.OpenStandardOutput(), 16); return 0;
            case "stdout-over": await WriteBlocksAsync(Console.OpenStandardOutput(), 32); return 0;
            case "unicode-over":
                byte[] unicode = Encoding.UTF8.GetBytes(new string('\u20ac', 1024));
                for (int index = 0; index < 32; index++) await Console.OpenStandardOutput().WriteAsync(unicode);
                return 0;
            case "stderr-exact": await WriteBlocksAsync(Console.OpenStandardError(), 16); break;
            case "stderr-over": await WriteBlocksAsync(Console.OpenStandardError(), 32); break;
            case "both-over":
                await Task.WhenAll(WriteBlocksAsync(Console.OpenStandardOutput(), 64), WriteBlocksAsync(Console.OpenStandardError(), 64));
                return 0;
            case "flood":
                WriteIdentity(input + ".root");
                Stopwatch clock = Stopwatch.StartNew();
                while (clock.Elapsed < TimeSpan.FromSeconds(60))
                {
                    await Task.WhenAll(WriteBlocksAsync(Console.OpenStandardOutput(), 16), WriteBlocksAsync(Console.OpenStandardError(), 16));
                }
                return 99;
            case "tree":
            case "orphanpipe":
                WriteIdentity(input + ".root");
                var start = new ProcessStartInfo(Environment.ProcessPath) { UseShellExecute = false, CreateNoWindow = true };
                start.ArgumentList.Add(typeof(SupervisorTestChild).Assembly.Location);
                start.ArgumentList.Add("grandchild");
                start.ArgumentList.Add(input + ".child");
                using (Process child = Process.Start(start))
                {
                    if (mode == "orphanpipe")
                    {
                        Console.WriteLine(record.ToJsonString());
                        return 0; // Deliberately leave inherited pipes open to test finite failed cleanup.
                    }
                    await Task.Delay(TimeSpan.FromSeconds(60));
                    child.Kill(entireProcessTree: true);
                    await child.WaitForExitAsync();
                }
                return 99;
            case "args with spaces & ; $() ' literal":
                if (args[1] != "--target" || args[2] != "filter-asciihex" || args[3] != "--input" ||
                    args[5] != "--config" || args[6] != "v1") return 91;
                break;
            default: return 91;
        }
        Console.WriteLine(record.ToJsonString());
        return 0;
    }

    private static async Task WriteBlocksAsync(Stream stream, int blocks)
    {
        byte[] buffer = Enumerable.Repeat((byte)'X', 4096).ToArray();
        for (int index = 0; index < blocks; index++) await stream.WriteAsync(buffer);
        await stream.FlushAsync();
    }

    private static void WriteIdentity(string path)
    {
        using Process self = Process.GetCurrentProcess();
        File.WriteAllText(path, self.Id.ToString(CultureInfo.InvariantCulture) + ":" +
            self.StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture));
    }
}
