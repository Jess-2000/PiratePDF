namespace BlackFlag.Engine.SDK.Plugins;

/// <summary>Identifies a versioned host capability requested by a plugin.</summary>
public sealed record PluginCapability
{
    public const int MaximumLength = 96;

    public PluginCapability(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > MaximumLength) throw new ArgumentOutOfRangeException(nameof(value));
        Value = value;
    }

    public string Value { get; }
    public override string ToString() => Value;
}
