using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Conservative byte-pass reservations, including bounded read buffers and one-byte EOF probes.</summary>
public static class CampaignWork
{
    public const long ManifestUnits = 3L * (CorpusValidator.MaximumManifestBytes + 1);

    public static long ValidationUnits(IReadOnlyList<CorpusSeed> seeds)
    {
        ArgumentNullException.ThrowIfNull(seeds);
        if (seeds.Count != 15) throw new ArgumentException("Expected the complete owned corpus.", nameof(seeds));
        long units = ManifestUnits;
        foreach (CorpusSeed seed in seeds)
            units += 3L * TargetRegistry.Get(seed.Target).MaximumInputBytes + 1;
        return units;
    }

    public static long SeedUnits(int length)
    {
        Validate(length);
        // Exact-length read buffer/read/hash, then pool digest and owned copy; EOF probe is separate.
        return 5L * length + 1;
    }

    public static int CandidateCeiling(int length, int maximum)
    {
        Validate(length);
        if (maximum is < 1 or > FindingStore.MaximumArtifactBytes || length > maximum)
            throw new ArgumentOutOfRangeException(nameof(maximum));
        return Math.Min(maximum, length + 8);
    }

    public static long AttemptUnits(int parentLength, int rootLength, int maximum)
    {
        int output = CandidateCeiling(parentLength, maximum);
        Validate(rootLength);
        // Source copy is charged by TrySource separately. Reserve root reread/hash, mutation parent hash/copy,
        // output allocation/hash, staging write/read/compare, worker read/consume, and pool digest/copy.
        // Unused reservations (duplicates, shrink operations, failures) are deliberately not refunded.
        return 3L * rootLength + 2L * parentLength + 9L * output + 2L * maximum + 3;
    }

    private static void Validate(int length)
    {
        if (length is < 0 or > FindingStore.MaximumArtifactBytes) throw new ArgumentOutOfRangeException(nameof(length));
    }
}
