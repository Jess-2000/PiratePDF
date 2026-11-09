namespace PiratePDF.Fuzz.Runner;

/// <summary>Opt-in mutation-only policy v1. All values may shorten, never enlarge, finite ceilings.</summary>
public sealed class CampaignConfiguration
{
    public const int MaximumCases = 10000;
    public const int MaximumProcessedBytes = 16 * 1024 * 1024;
    public const int MaximumPoolInputs = 128;
    public const int MaximumPoolBytes = 8 * 1024 * 1024;
    public const int MaximumTelemetryBytes = 16 * 1024 * 1024;
    public const int DuplicateRetries = 8;

    public CampaignConfiguration(int cases, TimeSpan duration, int processedBytes = MaximumProcessedBytes,
        int poolInputs = MaximumPoolInputs, int poolBytes = MaximumPoolBytes, int telemetryBytes = MaximumTelemetryBytes)
    {
        if (cases is < 1 or > MaximumCases || duration <= TimeSpan.Zero || duration > TimeSpan.FromSeconds(60) ||
            processedBytes is < 1 or > MaximumProcessedBytes || poolInputs is < 1 or > MaximumPoolInputs ||
            poolBytes is < 1 or > MaximumPoolBytes || telemetryBytes is < 16384 or > MaximumTelemetryBytes)
            throw new ArgumentOutOfRangeException(nameof(cases), "Invalid bounded campaign configuration.");
        Cases = cases;
        Duration = duration;
        ProcessedBytes = processedBytes;
        PoolInputs = poolInputs;
        PoolBytes = poolBytes;
        TelemetryBytes = telemetryBytes;
    }

    public int Cases { get; }
    public TimeSpan Duration { get; }
    public int ProcessedBytes { get; }
    public int PoolInputs { get; }
    public int PoolBytes { get; }
    public int TelemetryBytes { get; }
}

public enum CampaignStop
{
    None, CaseLimit, Deadline, ProcessedByteLimit, PoolLimit, TelemetryLimit, Duplicate, DuplicateLimit,
    InvalidInvocation, IntegrityFailure, WorkspaceFailure, WorkerFailure, Cancelled, CleanupFailure, RetentionFailure,
    OuterDeadline, PublicationFailure
}

/// <summary>Checked work admission independent of clocks/processes, also used by portable policy tests.</summary>
public sealed class CampaignBudget(CampaignConfiguration configuration)
{
    public int Requests { get; private set; }
    public int ProcessedBytes { get; private set; }

    public CampaignStop Admit(int length, TimeSpan elapsed)
    {
        if (length is < 0 or > FindingStore.MaximumArtifactBytes || elapsed < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(length));
        return AdmitWork(length, elapsed);
    }

    /// <summary>Reserve conservative payload byte-pass units before an operation; no refunds after partial work.</summary>
    public CampaignStop AdmitWork(long units, TimeSpan elapsed)
    {
        if (units < 0 || elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(units));
        if (elapsed >= configuration.Duration) return CampaignStop.Deadline;
        if (Requests >= configuration.Cases) return CampaignStop.CaseLimit;
        if (units > configuration.ProcessedBytes - ProcessedBytes) return CampaignStop.ProcessedByteLimit;
        ProcessedBytes += (int)units;
        return CampaignStop.None;
    }

    public void Dispatched()
    {
        if (Requests == configuration.Cases) throw new InvalidOperationException("Campaign request ceiling reached.");
        Requests++;
    }
}

/// <summary>Finite retry policy; duplicates consume attempts/work but never worker or exploration counts.</summary>
public sealed class CampaignAttempts
{
    public int Attempts { get; private set; }
    public int Duplicates { get; private set; }
    private int streak;
    public CampaignStop BeforeAttempt() => Attempts >= CampaignConfiguration.MaximumCases * CampaignConfiguration.DuplicateRetries ?
        CampaignStop.DuplicateLimit : CampaignStop.None;
    public int Next()
    {
        if (BeforeAttempt() != CampaignStop.None) throw new InvalidOperationException("Mutation attempt ceiling reached.");
        return Attempts++;
    }
    public CampaignStop Observe(bool duplicate)
    {
        if (!duplicate) { streak = 0; return CampaignStop.None; }
        Duplicates++;
        return ++streak >= CampaignConfiguration.DuplicateRetries ? CampaignStop.DuplicateLimit : CampaignStop.Duplicate;
    }
}

public sealed class CampaignTelemetryBudget
{
    public const int MaximumRecordBytes = 4096;
    public const int TerminalBytes = 8192;
    public const int MaximumRecords = 30004;
    private readonly int maximum;
    private int records;
    private int bytes;
    private bool terminal;

    public CampaignTelemetryBudget(int maximum)
    {
        if (maximum is < TerminalBytes or > CampaignConfiguration.MaximumTelemetryBytes)
            throw new ArgumentOutOfRangeException(nameof(maximum));
        this.maximum = maximum;
    }

    public bool Reserve(int length)
    {
        if (length is < 1 or > MaximumRecordBytes || records == MaximumRecords || length > maximum - TerminalBytes - bytes)
            return false;
        bytes += length;
        records++;
        return true;
    }

    public bool ReserveTerminal(int length)
    {
        if (terminal || length is < 1 or > TerminalBytes) return false;
        terminal = true;
        bytes += length;
        return true;
    }
}
