#nullable enable
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.SharpFuzz;

/// <summary>Resolves instrumented registry code only from an initialized fuzz-runtime callback.</summary>
internal sealed class SharpFuzzTargetSession(string targetId)
{
    private FuzzTarget? target;

    internal FuzzResult Invoke(ReadOnlySpan<byte> input, CancellationToken cancellationToken = default)
    {
        // SharpFuzz injects counter-pointer reads into Get and the registry initializer.
        // Merely constructing the session must not execute either before LibFuzzer.Run initializes coverage.
        target ??= TargetRegistry.Get(targetId);
        return target.Run(input, cancellationToken);
    }
}
