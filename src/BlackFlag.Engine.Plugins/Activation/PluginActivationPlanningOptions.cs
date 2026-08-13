namespace BlackFlag.Engine.Plugins.Activation;
public sealed record PluginActivationPlanningOptions
{
    public const int DefaultMaximumPlugins = 4096;
    public const int DefaultMaximumDependencyDepth = 128;
    public const int HardMaximumPlugins = 16384;
    public const int HardMaximumDependencyDepth = 1024;
    public int MaximumPlugins { get; init; } = DefaultMaximumPlugins;
    public int MaximumDependencyDepth { get; init; } = DefaultMaximumDependencyDepth;
    internal void Validate()
    {
        if (MaximumPlugins is <= 0 or > HardMaximumPlugins) throw new ArgumentOutOfRangeException(nameof(MaximumPlugins));
        if (MaximumDependencyDepth is <= 0 or > HardMaximumDependencyDepth) throw new ArgumentOutOfRangeException(nameof(MaximumDependencyDepth));
    }
}
