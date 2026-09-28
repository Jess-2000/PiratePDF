namespace PiratePDF.Fuzz.Runner;

internal sealed class BoundedCapture
{
    private readonly byte[] retained = new byte[WorkerSupervisor.MaximumRetainedBytes];

    internal int Count { get; private set; }
    internal bool Truncated { get; private set; }
    internal ReadOnlyMemory<byte> Bytes => retained.AsMemory(0, Count);

    internal async Task DrainAsync(Stream stream, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4096];
        while (true)
        {
            int count = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (count == 0) return;
            int keep = Math.Min(count, retained.Length - Count);
            buffer.AsSpan(0, keep).CopyTo(retained.AsSpan(Count));
            Count += keep;
            Truncated |= keep != count;
            // Continue to EOF/deadline even when retention is full, without counting unbounded totals.
        }
    }
}
