namespace PiratePDF.Fuzz.Runner;

/// <summary>Cooperative campaign-only outer clock; it cannot interrupt blocked synchronous OS calls.</summary>
public sealed class CampaignDeadline : IDisposable
{
    public static readonly TimeSpan MaximumDuration = TimeSpan.FromSeconds(75);
    private readonly CancellationTokenSource timer;
    private readonly CancellationTokenSource linked;

    public CampaignDeadline(TimeSpan duration, TimeProvider? timeProvider = null, CancellationToken caller = default)
    {
        if (duration <= TimeSpan.Zero || duration > MaximumDuration) throw new ArgumentOutOfRangeException(nameof(duration));
        timer = new(duration, timeProvider ?? TimeProvider.System);
        linked = CancellationTokenSource.CreateLinkedTokenSource(caller, timer.Token);
    }

    public CancellationToken Token => linked.Token;
    public CancellationToken ExpirationToken => timer.Token;
    public bool Expired => timer.IsCancellationRequested;
    public CampaignStop CancellationStop(CancellationToken caller) =>
        caller.IsCancellationRequested ? CampaignStop.Cancelled : Expired ? CampaignStop.OuterDeadline : CampaignStop.Deadline;

    public void Dispose()
    {
        linked.Dispose();
        timer.Dispose();
    }
}
