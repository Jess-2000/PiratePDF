using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using PiratePDF.Fuzz.Runner;
using PiratePDF.Fuzz.Targets;

// Owned repository fixtures only. Direct measurements do not admit external PDF inputs.
string repository = Path.GetFullPath(args[0]);
string output = Path.GetFullPath(args[1]);
CorpusValidationResult corpus = await CorpusValidator.ValidateAsync(repository);
if (!corpus.IsValid) throw new InvalidOperationException(corpus.Failure.ToString());
var seeds = corpus.Seeds.Select(seed => (Seed: seed, Bytes: File.ReadAllBytes(Path.Combine(repository, seed.Path)))).ToArray();
var measurements = new List<object>();
string host = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "../../..", "dotnet.exe");
var supervisor = new WorkerSupervisor(Path.GetFullPath(host), typeof(RunCommand).Assembly.Location);
long checksum = 0;

foreach (string id in new[] { "filter-asciihex", "filter-flate", "font-sanitize" })
{
    var fixtures = seeds.Where(item => item.Seed.Target == id).ToArray();
    FuzzTarget target = TargetRegistry.Get(id);
    await Measure("target-" + id, 5000, index =>
    {
        FuzzResult result = target.Run(fixtures[index % fixtures.Length].Bytes);
        checksum += result.Observation.Length;
        return Task.CompletedTask;
    });
}
await Measure("result-sha256", 100000, _ =>
{
    checksum += FuzzResult.Success("owned fixture"u8).Observation.Length;
    return Task.CompletedTask;
});
foreach (int length in new[] { 64, 16384 })
{
    byte[] bytes = Enumerable.Range(0, length).Select(index => (byte)index).ToArray();
    await Measure("mutation-" + length, 14000, index =>
    {
        byte[] mutated = DeterministicMutation.Generate("filter-asciihex", "owned", 1, index % 7, bytes);
        checksum += mutated.Length;
        return Task.CompletedTask;
    });
}
await Measure("corpus-validation", 10, async _ =>
{
    CorpusValidationResult result = await CorpusValidator.ValidateAsync(repository);
    if (!result.IsValid || result.Seeds.Count != seeds.Length) throw new InvalidOperationException("Corpus differs.");
    checksum += result.Seeds.Count;
});
var first = seeds.First(item => item.Seed.Target == "filter-asciihex" && item.Seed.ExpectedOutcome == "success");
await Measure("supervised-worker", 5, async _ =>
{
    SupervisionResult result = await supervisor.RunAsync(first.Seed.Target, Path.Combine(repository, first.Seed.Path), 1);
    if (result.Status != SupervisionStatus.Completed || result.ExitCode != 0) throw new InvalidOperationException("Worker failed.");
    checksum++;
});
await Measure("supervised-replay", 1, async _ =>
{
    ReplayResult result = await new CorpusReplay(supervisor).RunAsync(repository);
    if (result.ExitCode != ReplayExitCode.Success || result.Attempted != seeds.Length) throw new InvalidOperationException("Replay failed.");
    checksum += result.Attempted;
});
string retentionRoot = Path.Combine(Path.GetDirectoryName(output)!, Path.GetFileNameWithoutExtension(output) + "-retention");
if (Directory.Exists(retentionRoot)) throw new InvalidOperationException("Use a fresh output file for retained benchmark evidence.");
Directory.CreateDirectory(Path.Combine(retentionRoot, "tests/FuzzCorpus"));
File.Copy(Path.Combine(repository, "tests/FuzzCorpus/manifest.json"), Path.Combine(retentionRoot, "tests/FuzzCorpus/manifest.json"));
foreach (var item in seeds)
{
    string destination = Path.Combine(retentionRoot, item.Seed.Path);
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    File.WriteAllBytes(destination, item.Bytes);
}
Directory.CreateDirectory(Path.Combine(retentionRoot, "artifacts/fuzz/incoming"));
int retained = 0;
var source = new FindingSource("filter-asciihex", 1, FindingKind.ControlledFixture,
    FindingClass.UnexpectedManagedFailure, FindingOrigin.ControlledProbe, "b16-owned-probe",
    new string('A', 64), "PP-AUD-047", 1049, "neutral-net10-v1", new string('a', 40),
    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(FuzzTarget).Assembly.Location))),
    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(RunCommand).Assembly.Location))));
await Measure("finding-retention", 5, async _ =>
{
    string id = "owned-" + retained++;
    File.WriteAllBytes(Path.Combine(retentionRoot, "artifacts/fuzz/incoming", id + ".bin"), "owned-b16-probe"u8.ToArray());
    FindingResult result = await new FindingStore(retentionRoot, id).RetainAsync("case-one", "incoming/" + id + ".bin", source);
    if (result.ExitCode != FindingExit.Success) throw new InvalidOperationException("Controlled finding retention failed.");
    checksum++;
});
using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
foreach (var item in seeds)
{
    for (int index = 0; index < 7; index++)
        digest.AppendData(DeterministicMutation.Generate(item.Seed.Target, item.Seed.Id, item.Seed.ConfigurationVersion, index, item.Bytes));
}
var report = new
{
    Runtime = RuntimeInformation.FrameworkDescription, OS = RuntimeInformation.OSDescription,
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(), Configuration = "Release",
    Processors = Environment.ProcessorCount, SeedCount = seeds.Length, Repetitions = 7,
    Warmup = "one complete workload per scenario before seven measured repetitions",
    Allocation = "process-wide GC.GetTotalAllocatedBytes(true), including async continuations",
    PeakMemory = "cumulative benchmark-process peak working set, excludes supervised children",
    CorpusSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(repository, "tests/FuzzCorpus/manifest.json")))),
    MutationDigest = Convert.ToHexString(digest.GetHashAndReset()), Checksum = checksum, Measurements = measurements
};
File.WriteAllText(output, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

async Task Measure(string name, int cases, Func<int, Task> operation)
{
    for (int index = 0; index < cases; index++) await operation(index);
    var samples = new List<object>();
    for (int repeat = 0; repeat < 7; repeat++)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long allocated = GC.GetTotalAllocatedBytes(true);
        var watch = Stopwatch.StartNew();
        for (int index = 0; index < cases; index++) await operation(index);
        watch.Stop();
        allocated = GC.GetTotalAllocatedBytes(true) - allocated;
        using var process = Process.GetCurrentProcess();
        samples.Add(new { Milliseconds = watch.Elapsed.TotalMilliseconds, AllocatedBytes = allocated,
            CasesPerSecond = cases / watch.Elapsed.TotalSeconds, PeakWorkingSetBytes = process.PeakWorkingSet64 });
    }
    measurements.Add(new { Name = name, Cases = cases, Samples = samples });
    Console.WriteLine(name);
}
