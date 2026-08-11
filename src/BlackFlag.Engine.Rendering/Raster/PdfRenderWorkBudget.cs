
namespace BlackFlag.Engine.Rendering.Raster;

/// <summary>
/// Tracks bounded shared work across nested rendering operations.
/// </summary>
public sealed class PdfRenderWorkBudget
{
    private readonly long maximumWorkUnits;
    private long consumed;

    /// <summary>Initializes a render-session work budget.</summary>
    public PdfRenderWorkBudget(long maximumWorkUnits)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumWorkUnits);
        this.maximumWorkUnits = maximumWorkUnits;
    }

    /// <summary>Gets the maximum work units.</summary>
    public long MaximumWorkUnits => maximumWorkUnits;

    /// <summary>Gets work consumed so far.</summary>
    public long ConsumedWorkUnits => Interlocked.Read(ref consumed);

    /// <summary>Consumes work or fails when the shared limit would be exceeded.</summary>
    public void Consume(long workUnits)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workUnits);

        long value = Interlocked.Add(ref consumed, workUnits);

        if (value > maximumWorkUnits)
        {
            Interlocked.Add(ref consumed, -workUnits);
            throw new InvalidOperationException(
                $"Rendering work exceeded the configured limit of {maximumWorkUnits} units.");
        }
    }
}
