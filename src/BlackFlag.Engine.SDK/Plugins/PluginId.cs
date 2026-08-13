namespace BlackFlag.Engine.SDK.Plugins;

/// <summary>Identifies a plugin independently of its display name or version.</summary>
public sealed record PluginId
{
    public const int MaximumLength = 128;

    public PluginId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > MaximumLength)
            throw new ArgumentOutOfRangeException(nameof(value), $"Plugin IDs cannot exceed {MaximumLength} characters.");
        if (!IsValid(value))
            throw new ArgumentException("Plugin IDs must contain lowercase ASCII letters, digits, dots, or hyphens and must begin and end with a letter or digit.", nameof(value));
        Value = value;
    }

    public string Value { get; }
    public override string ToString() => Value;

    private static bool IsValid(string value)
    {
        if (!IsAlphaNumeric(value[0]) || !IsAlphaNumeric(value[^1])) return false;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (!(IsAlphaNumeric(c) || c is '.' or '-')) return false;
        }
        return true;
    }

    private static bool IsAlphaNumeric(char c) => c is >= 'a' and <= 'z' or >= '0' and <= '9';
}
