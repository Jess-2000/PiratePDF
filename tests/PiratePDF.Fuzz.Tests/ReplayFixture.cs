using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

internal sealed class ReplayFixture : IDisposable
{
    internal string Repository { get; } = Path.Combine(Path.GetTempPath(), "PiratePDF Replay & ' space " + Guid.NewGuid().ToString("N"));
    internal string Root => Path.Combine(Repository, "tests", "FuzzCorpus");
    internal string ManifestPath => Path.Combine(Root, "manifest.json");
    internal string LogPath => Path.Combine(Repository, "executions.log");
    internal JsonArray Seeds { get; } = [];
    internal JsonObject Manifest { get; }
    internal static string Host => Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..",
        OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"));

    internal ReplayFixture()
    {
        Directory.CreateDirectory(Path.Combine(Root, "filter-asciihex"));
        Manifest = new JsonObject { ["schema"] = "piratepdf-fuzz-corpus", ["version"] = 1, ["seeds"] = Seeds };
    }

    internal string SeedPath(string id) => Path.Combine(Root, "filter-asciihex", id + ".bin");

    internal void Add(string id, string expected = "success", byte[] bytes = null)
    {
        bytes ??= [62];
        File.WriteAllBytes(SeedPath(id), bytes);
        Seeds.Add(new JsonObject
        {
            ["id"] = id, ["target"] = "filter-asciihex", ["configurationVersion"] = 1,
            ["path"] = "tests/FuzzCorpus/filter-asciihex/" + id + ".bin", ["length"] = bytes.Length,
            ["sha256"] = Convert.ToHexString(SHA256.HashData(bytes)),
            ["sourceRecipe"] = "tests/PiratePDF.Fuzz.Tests/ReplayFixture.cs", ["origin"] = "repository-owned",
            ["rights"] = "Repository-owned fixture.", ["category"] = "valid-minimal", ["purpose"] = "Replay contract test.",
            ["expectedOutcome"] = expected, ["related"] = new JsonArray("BF-1040")
        });
        Save();
    }

    internal void Save() => File.WriteAllText(ManifestPath, Manifest.ToJsonString());
    internal static WorkerSupervisor Supervisor(bool real = false, TimeSpan? timeout = null) =>
        new(Host, real ? typeof(RunCommand).Assembly.Location : typeof(SupervisorTestChild).Assembly.Location, timeout);

    internal static string FindRepository()
    {
        DirectoryInfo directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    public void Dispose() => Directory.Delete(Repository, recursive: true);
}
