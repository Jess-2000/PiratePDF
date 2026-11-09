using System.Text.Json.Nodes;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

internal sealed class CampaignFixture : IDisposable
{
    internal string Repository { get; } = Path.Combine(Path.GetTempPath(), "PiratePDF Campaign & space " + Guid.NewGuid().ToString("N"));
    internal const string Revision = "f51824dbc031b58497a38a6292ee11568b9ffeda";
    internal string Manifest => Path.Combine(Repository, "tests", "FuzzCorpus", "manifest.json");
    internal string Incoming(string run) => Path.Combine(Repository, "artifacts", "fuzz", "incoming", run, "case.bin");
    internal string Evidence(string run) => Path.Combine(Repository, "artifacts", "fuzz", "campaigns", run);
    internal string Finding(string run) => Directory.GetDirectories(Path.Combine(Repository, "artifacts", "fuzz", "findings", run)).Single();

    internal CampaignFixture(string mode = "success")
    {
        string source = ReplayFixture.FindRepository();
        string manifest = File.ReadAllText(Path.Combine(source, "tests", "FuzzCorpus", "manifest.json"));
        JsonArray seeds = JsonNode.Parse(manifest)["seeds"].AsArray();
        foreach (JsonNode seed in seeds)
        {
            string relative = seed["path"].GetValue<string>();
            string destination = Path.Combine(Repository, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.Copy(Path.Combine(source, relative), destination);
        }
        File.WriteAllText(Manifest, manifest);
        File.WriteAllText(Path.Combine(Repository, "campaign-mode.txt"), mode);
    }

    internal CampaignRunner Runner(bool real = false, TimeSpan? timeout = null) => new(ReplayFixture.Supervisor(real, timeout));
    internal System.Threading.Tasks.Task<CampaignResult> Run(string run = "owned-run", CampaignConfiguration config = null,
        bool real = false, string target = "filter-asciihex", System.Threading.CancellationToken token = default) =>
        Runner(real).RunAsync(Repository, run, target, Revision, config ?? new(2, TimeSpan.FromSeconds(10)), token);

    public void Dispose() => Directory.Delete(Repository, recursive: true);
}
