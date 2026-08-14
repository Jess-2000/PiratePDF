namespace BlackFlag.Engine.SDK.Plugins;

/// <summary>Compares plugin semantic versions while treating omitted build and revision components as zero.</summary>
internal static class PluginVersionComparison
{
    public static int Compare(Version left, Version right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        int comparison = left.Major.CompareTo(right.Major);
        if (comparison != 0) return comparison;

        comparison = left.Minor.CompareTo(right.Minor);
        if (comparison != 0) return comparison;

        comparison = Component(left.Build).CompareTo(Component(right.Build));
        if (comparison != 0) return comparison;

        return Component(left.Revision).CompareTo(Component(right.Revision));
    }

    private static int Component(int value) => value < 0 ? 0 : value;
}
