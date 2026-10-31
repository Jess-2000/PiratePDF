using System.Threading;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class CampaignAccountingTests
{
    [Fact]
    public void FailedAdmissionDoesNotCopyOrExposePoolSource()
    {
        var pool = new CampaignPool(128, 8388608);
        Assert.Equal(CampaignStop.None, pool.Add("owned", new byte[] { 1, 2 }));
        var budget = new CampaignBudget(new(1, TimeSpan.FromSeconds(10), processedBytes: 1));
        Assert.Equal(("owned", 2), pool.Describe(0));
        Assert.Equal(CampaignStop.ProcessedByteLimit, pool.TrySource(0, budget, TimeSpan.Zero, out var denied));
        Assert.Null(denied);
        Assert.Equal(0, budget.ProcessedBytes);
        var exact = new CampaignBudget(new(1, TimeSpan.FromSeconds(10), processedBytes: 2));
        Assert.Equal(CampaignStop.None, pool.TrySource(0, exact, TimeSpan.Zero, out var copied));
        Assert.Equal(new byte[] { 1, 2 }, copied.Bytes.ToArray());
        Assert.Equal(2, exact.ProcessedBytes);
        Assert.Equal(CampaignStop.ProcessedByteLimit, pool.TrySource(0, exact, TimeSpan.Zero, out denied));
        Assert.Null(denied);
    }

    [Fact]
    public void EveryManifestRereadAndAttemptConsumesWorkWithoutRefunds()
    {
        long units = CampaignWork.ManifestUnits + CampaignWork.AttemptUnits(10, 20, 16384) + 10;
        var budget = new CampaignBudget(new(10, TimeSpan.FromSeconds(10), processedBytes: checked((int)(units * 2))));
        Assert.Equal(CampaignStop.None, budget.AdmitWork(units, TimeSpan.Zero));
        Assert.Equal(CampaignStop.None, budget.AdmitWork(units, TimeSpan.Zero));
        Assert.Equal(CampaignStop.ProcessedByteLimit, budget.AdmitWork(1, TimeSpan.Zero));
        Assert.Equal(units * 2, budget.ProcessedBytes);
        Assert.Equal(0, budget.Requests);
        Assert.Equal(101, CampaignWork.SeedUnits(20));
    }

    [Fact]
    public void WideWorkCostsCannotOverflowRemainingAllowance()
    {
        var budget = new CampaignBudget(new(1, TimeSpan.FromSeconds(10)));
        Assert.Equal(CampaignStop.ProcessedByteLimit, budget.AdmitWork(long.MaxValue, TimeSpan.Zero));
        Assert.Equal(0, budget.ProcessedBytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => budget.AdmitWork(-1, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignWork.AttemptUnits(int.MaxValue, 0, 65536));
        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignWork.AttemptUnits(0, int.MaxValue, 65536));
        Assert.Equal(65536, CampaignWork.CandidateCeiling(65536, 65536));
        Assert.Equal(8, CampaignWork.CandidateCeiling(0, 16384));
    }

    [Fact]
    public void DuplicateStreakStopsExactlyAtEightAndUniqueInputResetsStreak()
    {
        var attempts = new CampaignAttempts();
        for (int index = 0; index < 7; index++)
        {
            Assert.Equal(index, attempts.Next());
            Assert.Equal(CampaignStop.Duplicate, attempts.Observe(true));
        }
        _ = attempts.Next();
        Assert.Equal(CampaignStop.None, attempts.Observe(false));
        for (int index = 0; index < 8; index++)
        {
            _ = attempts.Next();
            Assert.Equal(index == 7 ? CampaignStop.DuplicateLimit : CampaignStop.Duplicate, attempts.Observe(true));
        }
        Assert.Equal(16, attempts.Attempts);
        Assert.Equal(15, attempts.Duplicates);
    }

    [Fact]
    public void TotalAttemptLimitIsIndependentOfConsecutiveDuplicateStreak()
    {
        var attempts = new CampaignAttempts();
        for (int index = 0; index < 80000; index++)
        {
            Assert.Equal(CampaignStop.None, attempts.BeforeAttempt());
            Assert.Equal(index, attempts.Next());
            Assert.Equal(CampaignStop.None, attempts.Observe(false));
        }
        Assert.Equal(CampaignStop.DuplicateLimit, attempts.BeforeAttempt());
        Assert.Throws<InvalidOperationException>(() => attempts.Next());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(76)]
    [InlineData(int.MaxValue)]
    public void OuterClockCannotBeUnbounded(int seconds) => Assert.Throws<ArgumentOutOfRangeException>(() =>
        new CampaignDeadline(TimeSpan.FromSeconds(seconds)));

    [Fact]
    public void OuterExpirationAndCallerCancellationAreDistinctWithoutSleeping()
    {
        var time = new ManualCampaignTime();
        using var caller = new CancellationTokenSource();
        using var outer = new CampaignDeadline(TimeSpan.FromSeconds(75), time, caller.Token);
        time.Expire();
        Assert.True(outer.Token.IsCancellationRequested);
        Assert.True(outer.Expired);
        Assert.Equal(CampaignStop.OuterDeadline, outer.CancellationStop(caller.Token));
        caller.Cancel();
        Assert.Equal(CampaignStop.Cancelled, outer.CancellationStop(caller.Token));
        using var cancelled = new CampaignDeadline(TimeSpan.FromSeconds(75), caller: caller.Token);
        Assert.True(cancelled.Token.IsCancellationRequested);
        Assert.False(cancelled.ExpirationToken.IsCancellationRequested);
    }

    [Fact]
    public async Task SmokeLauncherStillRejectsMoreThanTwentySecondsBeforeStarting()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ChildProcess.RunAsync("unused", [], "unused",
            TimeSpan.FromSeconds(21), "unused"));
    }
}

/// <summary>Owned controlled clock: timer callbacks fire on request, without wall-clock timing assertions.</summary>
internal sealed class ManualCampaignTime : TimeProvider
{
    private TimerCallback callback;
    private object state;
    public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
    {
        this.callback = callback;
        this.state = state;
        return new ManualTimer();
    }
    internal void Expire() => callback(state);
    private sealed class ManualTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
