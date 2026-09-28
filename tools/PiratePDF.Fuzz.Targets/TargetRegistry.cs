namespace PiratePDF.Fuzz.Targets;

/// <summary>Owns a bounded, immutable, ordinal registry; static access uses the built-in registrations.</summary>
public sealed class TargetRegistry
{
    private readonly Dictionary<string, FuzzTarget> byId;

    private static readonly TargetRegistry Default = new(
    [
        new("filter-asciihex", 1, FilterTargets.MaximumInputBytes, FuzzIsolation.SupervisedManagedWorker, FilterTargets.AsciiHex),
        new("filter-flate", 1, FilterTargets.MaximumInputBytes, FuzzIsolation.SupervisedManagedWorker, FilterTargets.Flate),
        new("font-sanitize", 1, FontTargets.MaximumInputBytes, FuzzIsolation.SupervisedManagedWorker, FontTargets.Sanitize)
    ]);

    public TargetRegistry(IReadOnlyList<FuzzTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        int count = targets.Count;
        if (count > 256)
        {
            throw new ArgumentException("A registry supports at most 256 targets.", nameof(targets));
        }

        byId = new Dictionary<string, FuzzTarget>(count, StringComparer.Ordinal);
        var owned = new FuzzTarget[count];
        for (int index = 0; index < count; index++)
        {
            FuzzTarget target = targets[index]
                ?? throw new ArgumentException("A registry cannot contain a null target.", nameof(targets));
            if (!byId.TryAdd(target.Id, target))
            {
                throw new ArgumentException("Duplicate fuzz target ID.", nameof(targets));
            }

            owned[index] = target;
        }

        Array.Sort(owned, static (left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
        Targets = Array.AsReadOnly(owned);
    }

    public IReadOnlyList<FuzzTarget> Targets { get; }
    public static IReadOnlyList<FuzzTarget> All => Default.Targets;
    public static FuzzTarget Get(string id) => Default.Find(id);

    public FuzzTarget Find(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return byId.TryGetValue(id, out FuzzTarget? target)
            ? target
            : throw new ArgumentException("Unknown fuzz target.", nameof(id));
    }
}
