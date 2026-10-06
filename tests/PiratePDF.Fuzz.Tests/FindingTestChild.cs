using System.Diagnostics;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

internal static class FindingTestChild
{
    internal static async Task<int> RunAsync(string[] args)
    {
        string input = args[4];
        string repository = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(input), "..", "..", "..", "..", ".."));
        byte[] bytes = await File.ReadAllBytesAsync(input); // Only bounded owned test fixtures; production uses InputFile.
        string log = Path.Combine(repository, "finding-executions.log");
        File.AppendAllText(log, "attempt\n");
        byte mode = bytes.Length == 0 ? (byte)'S' : bytes[0];
        if (mode == 'A') return 91;
        if (mode == 'P') { Console.WriteLine("PRIVATE_FIXTURE_INVALID_PROTOCOL"); return 0; }
        if (mode == 'T')
        {
            using Process process = Process.GetCurrentProcess();
            File.WriteAllText(Path.Combine(repository, "finding-child.pid"), process.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await Task.Delay(TimeSpan.FromSeconds(60));
            return 91;
        }
        if (mode == 'I' && File.ReadLines(log).Count() % 2 == 0) mode = (byte)'S';
        if (mode == 'M')
        {
            string metadata = Path.Combine(Path.GetDirectoryName(input), "metadata.json");
            var document = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(metadata));
            document["source"]["seed"] = 12;
            File.WriteAllText(metadata, document.ToJsonString());
        }
        bool fail = mode is (byte)'F' or (byte)'I' or (byte)'M';
        if (File.Exists(Path.Combine(repository, "fixed.flag"))) fail = false;
        if (File.Exists(Path.Combine(repository, "reject.flag"))) mode = (byte)'R';
        if (File.Exists(Path.Combine(repository, "change-corpus.flag")))
            File.WriteAllBytes(Path.Combine(repository, "tests", "FuzzCorpus", "filter-asciihex", "z-late.bin"), [2]);
        var target = new FuzzTarget("filter-asciihex", 1, 16384, FuzzIsolation.SupervisedManagedWorker,
            (_, _) => fail ? throw new InvalidOperationException("PRIVATE_FIXTURE_EXCEPTION") :
                mode == 'R' ? FuzzResult.Rejected("InvalidData") : FuzzResult.Success([]));
        RunResult result = await new RunCommand(new TargetRegistry([target])).ExecuteAsync(args);
        Console.WriteLine(result.Json);
        return (int)result.ExitCode;
    }
}
