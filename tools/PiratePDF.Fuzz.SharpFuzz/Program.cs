using PiratePDF.Fuzz.SharpFuzz;
using SharpFuzz;
using System.Diagnostics;
using System.Globalization;

string? leasePath = Environment.GetEnvironmentVariable("B9_CHILD_LEASE");
if (leasePath is not null)
{
    using Process self = Process.GetCurrentProcess();
    using var lease = new StreamWriter(new FileStream(leasePath, FileMode.CreateNew));
    lease.Write(string.Create(CultureInfo.InvariantCulture,
        $"{self.Id}:{self.StartTime.ToUniversalTime().Ticks}"));
}

if (args.Length != 1)
{
    Console.Error.WriteLine("Specify one target ID or an explicit compatibility probe.");
    return 2;
}

// Probes are opt-in modes, never registered as document targets or normal corpus cases.
if (args[0] is "registry-direct" or "registry-runtime" or "inspect-il") return RegistryDiagnostics.Run(args[0]);

if (args[0] == "probe-abnormal")
{
    Fuzzer.LibFuzzer.Run(_ => { Mark("entered"); Environment.Exit(23); });
    return 0;
}

if (args[0] == "probe-harness")
{
    Fuzzer.LibFuzzer.Run(_ => { Mark("entered"); throw new InvalidOperationException("B9 controlled harness failure"); });
    return 0;
}

if (args[0] == "probe-timeout")
{
    Fuzzer.LibFuzzer.Run(_ => { Mark("entered"); Thread.Sleep(Timeout.Infinite); });
    return 0;
}

var session = new SharpFuzzTargetSession(args[0]);
Mark("runtime-call");
Fuzzer.LibFuzzer.Run(input =>
{
    // Driver max_len is not an API trust boundary.
    Mark("entered");
    var result = session.Invoke(input);
    Mark(result.Outcome);
});
return 0;

// At most four fixed one-byte markers, solely in the supervisor-owned ignored evidence directory.
// No bytes, exception messages, or per-input artifacts are emitted by this observer.
void Mark(string outcome)
{
    if (leasePath is null) return;
    string path = leasePath + "." + outcome;
    if (!File.Exists(path))
    {
        using var marker = new FileStream(path, FileMode.CreateNew);
        marker.WriteByte(1);
    }
}
