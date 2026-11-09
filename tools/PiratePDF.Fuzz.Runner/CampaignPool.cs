namespace PiratePDF.Fuzz.Runner;

public sealed record CampaignParent(string RootSeed, string Sha256, ReadOnlyMemory<byte> Bytes);

/// <summary>Owned, finite, deterministic exploration pool; it conveys no coverage or oracle judgment.</summary>
public sealed class CampaignPool
{
    private readonly int maximumCount;
    private readonly int maximumBytes;
    private readonly List<CampaignParent> inputs = [];
    private readonly HashSet<string> hashes = new(StringComparer.Ordinal);

    public CampaignPool(int maximumCount, int maximumBytes)
    {
        if (maximumCount is < 1 or > CampaignConfiguration.MaximumPoolInputs ||
            maximumBytes is < 1 or > CampaignConfiguration.MaximumPoolBytes) throw new ArgumentOutOfRangeException(nameof(maximumCount));
        this.maximumCount = maximumCount;
        this.maximumBytes = maximumBytes;
    }

    public int Count => inputs.Count;
    public int Bytes { get; private set; }
    public bool Contains(string hash) => hashes.Contains(hash);
    public CampaignStop Admission(int length) => length < 0 || length > FindingStore.MaximumArtifactBytes ||
        Count == maximumCount || length > maximumBytes - Bytes ? CampaignStop.PoolLimit : CampaignStop.None;

    public CampaignStop Add(string rootSeed, ReadOnlyMemory<byte> bytes)
    {
        if (!FindingFiles.Id(rootSeed)) throw new ArgumentException("Invalid owned seed identity.");
        if (bytes.Length > FindingStore.MaximumArtifactBytes) return CampaignStop.PoolLimit;
        string hash = FindingFiles.Digest(bytes);
        if (Contains(hash)) return CampaignStop.Duplicate;
        CampaignStop stop = Admission(bytes.Length);
        if (stop != CampaignStop.None) return stop;
        inputs.Add(new(rootSeed, hash, bytes.ToArray()));
        hashes.Add(hash);
        Bytes += bytes.Length;
        return CampaignStop.None;
    }

    // Return an independent caller-owned copy; never expose the pool's trusted mutable backing arrays.
    public CampaignParent Source(int index)
    {
        CampaignParent item = inputs[index];
        return item with { Bytes = item.Bytes.ToArray() };
    }

    /// <summary>Inspect trusted lengths/identity without copying the source payload.</summary>
    public (string RootSeed, int Length) Describe(int index) => (inputs[index].RootSeed, inputs[index].Bytes.Length);

    public CampaignStop TrySource(int index, CampaignBudget budget, TimeSpan elapsed, out CampaignParent? parent)
    {
        ArgumentNullException.ThrowIfNull(budget);
        parent = null;
        CampaignStop stop = budget.AdmitWork(inputs[index].Bytes.Length, elapsed);
        if (stop != CampaignStop.None) return stop;
        parent = Source(index);
        return CampaignStop.None;
    }
}
