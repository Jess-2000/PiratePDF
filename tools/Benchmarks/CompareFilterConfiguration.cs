using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

string repository = args[2];
using JsonDocument manifest = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repository, "tests/FuzzCorpus/manifest.json")));
var samples = new List<object>();
foreach (string id in new[] { "filter-asciihex", "filter-flate" })
{
    byte[][] inputs = manifest.RootElement.GetProperty("seeds").EnumerateArray()
        .Where(seed => seed.GetProperty("target").GetString() == id)
        .Select(seed => File.ReadAllBytes(Path.Combine(repository, seed.GetProperty("path").GetString()!))).ToArray();
    TargetCall before = Load(args[0], id);
    TargetCall after = Load(args[1], id);
    foreach (byte[] input in inputs)
    {
        object left = before(input, default), right = after(input, default);
        foreach (string property in new[] { "Outcome", "Observation" })
            if (!Equals(left.GetType().GetProperty(property)!.GetValue(left), right.GetType().GetProperty(property)!.GetValue(right)))
                throw new InvalidOperationException("Owned target observations differ.");
    }
    for (int index = 0; index < 5000; index++)
    {
        before(inputs[index % inputs.Length], default);
        after(inputs[index % inputs.Length], default);
    }
    for (int repeat = 0; repeat < 16; repeat++)
    {
        foreach (bool baseline in repeat % 2 == 0 ? new[] { true, false } : new[] { false, true })
        {
            TargetCall invoke = baseline ? before : after;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long allocated = GC.GetAllocatedBytesForCurrentThread();
            var watch = Stopwatch.StartNew();
            object? last = null;
            for (int index = 0; index < 10000; index++) last = invoke(inputs[index % inputs.Length], default);
            watch.Stop();
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            if (last is null) throw new InvalidOperationException("Target did not run.");
            samples.Add(new { Target = id, Baseline = baseline, Repeat = repeat, Cases = 10000,
                Milliseconds = watch.Elapsed.TotalMilliseconds, AllocatedBytes = allocated });
        }
    }
}
File.WriteAllText(args[3], JsonSerializer.Serialize(samples, new JsonSerializerOptions { WriteIndented = true }));

static TargetCall Load(string directory, string id)
{
    var context = new TargetContext(Path.GetFullPath(directory));
    Assembly assembly = context.LoadFromAssemblyPath(Path.Combine(Path.GetFullPath(directory), "PiratePDF.Fuzz.Targets.dll"));
    object target = assembly.GetType("PiratePDF.Fuzz.Targets.TargetRegistry")!.GetMethod("Get")!.Invoke(null, [id])!;
    return target.GetType().GetMethod("Run")!.CreateDelegate<TargetCall>(target);
}

internal delegate object TargetCall(ReadOnlySpan<byte> input, CancellationToken cancellationToken);

internal sealed class TargetContext(string directory) : AssemblyLoadContext
{
    protected override Assembly? Load(AssemblyName name)
    {
        string path = Path.Combine(directory, name.Name + ".dll");
        return File.Exists(path) ? LoadFromAssemblyPath(path) : null;
    }
}
