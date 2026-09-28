using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

internal sealed class FindingFixture : IDisposable
{
    private readonly ReplayFixture corpus = new();
    internal string Repository => corpus.Repository;
    internal FindingStore Store { get; }
    internal string RunPath => Path.Combine(Repository, "artifacts", "fuzz", "findings", "owned-run");
    internal FindingFixture()
    {
        corpus.Add("base-seed");
        Directory.CreateDirectory(Path.Combine(Repository, "artifacts", "fuzz", "incoming"));
        Store = new(Repository, "owned-run");
    }

    internal static FindingSource Source(FindingClass observed = FindingClass.UnexpectedManagedFailure) => new(
        "filter-asciihex", 1, FindingKind.UnexpectedResult, observed, FindingOrigin.OwnedGenerated,
        "controlled-owned-recipe", new string('A', 64), "PP-AUD-037", 1049, "neutral-net10-v1",
        "93865f639acf96f0b9598c97b04d49cea38e8260", new string('B', 64), new string('C', 64));

    internal string FilePath(string id, string name = "input.bin") => Path.Combine(RunPath, id, name);
    internal async Task<FindingResult> Retain(string id = "case-one", byte[] bytes = null, FindingSource source = null)
    {
        bytes ??= "F0>"u8.ToArray();
        string input = Path.Combine(Repository, "artifacts", "fuzz", "incoming", id + ".bin");
        File.WriteAllBytes(input, bytes);
        return await Store.RetainAsync(id, "incoming/" + id + ".bin", source ?? Source());
    }

    internal void Edit(string id, Action<JsonObject> edit)
    {
        string path = FilePath(id, "metadata.json");
        var document = JsonNode.Parse(File.ReadAllText(path)).AsObject();
        edit(document);
        File.WriteAllText(path, document.ToJsonString());
    }

    internal static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    internal static WorkerSupervisor Supervisor(TimeSpan? timeout = null) => ReplayFixture.Supervisor(timeout: timeout);
    internal async Task<FindingResult> Reproduce(string id = "case-one", WorkerSupervisor supervisor = null) =>
        await new FindingReproduction(supervisor ?? Supervisor()).RunAsync(Store, id);
    internal void Fixed() => File.WriteAllText(Path.Combine(Repository, "fixed.flag"), "fixed");
    public void Dispose() => corpus.Dispose();
}
