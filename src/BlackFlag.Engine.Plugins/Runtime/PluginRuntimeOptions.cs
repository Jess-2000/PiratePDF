namespace BlackFlag.Engine.Plugins.Runtime;

public sealed class PluginRuntimeOptions
{
    public int MaximumActivePlugins { get; init; } = 256;
    internal void Validate()
    {
        if (MaximumActivePlugins is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(MaximumActivePlugins));
    }
}
