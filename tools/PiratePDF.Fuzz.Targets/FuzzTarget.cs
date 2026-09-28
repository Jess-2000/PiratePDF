namespace PiratePDF.Fuzz.Targets;

public delegate FuzzResult ByteTarget(ReadOnlySpan<byte> input, CancellationToken cancellationToken);

/// <summary>Immutable target metadata and the single guarded invocation boundary.</summary>
public sealed class FuzzTarget
{
    private readonly ByteTarget execute;

    public FuzzTarget(
        string id,
        int configurationVersion,
        int maximumInputBytes,
        FuzzIsolation isolation,
        ByteTarget execute)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        if (id.Length > 64 || id.Any(static value =>
                value is not (>= 'a' and <= 'z') and not (>= '0' and <= '9') and not '-'))
        {
            throw new ArgumentException("Target IDs require lowercase ASCII letters, digits or hyphens.", nameof(id));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(configurationVersion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumInputBytes);
        if (!Enum.IsDefined(isolation))
        {
            throw new ArgumentOutOfRangeException(nameof(isolation));
        }

        ArgumentNullException.ThrowIfNull(execute);
        Id = id;
        ConfigurationVersion = configurationVersion;
        MaximumInputBytes = maximumInputBytes;
        Isolation = isolation;
        this.execute = execute;
    }

    public string Id { get; }
    public int ConfigurationVersion { get; }
    public int MaximumInputBytes { get; }
    public FuzzIsolation Isolation { get; }

    /// <summary>Checks input admission before any adapter allocation. Cancellation and unexpected failures escape.</summary>
    public FuzzResult Run(ReadOnlySpan<byte> input, CancellationToken cancellationToken = default)
    {
        if (input.Length > MaximumInputBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Input exceeds target limit.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        FuzzResult result = execute(input, cancellationToken)
            ?? throw new InvalidOperationException("A target returned no result.");
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}
