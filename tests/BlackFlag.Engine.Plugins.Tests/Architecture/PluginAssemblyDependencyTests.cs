using System;
using System.Linq;
using System.Reflection;
using BlackFlag.Engine.Plugins.Runtime;

namespace BlackFlag.Engine.Plugins.Tests.Architecture;

public sealed class PluginAssemblyDependencyTests
{
    [Fact]
    public void PluginHostReferencesOnlyApprovedBlackFlagLayers()
    {
        Assembly plugins = typeof(PluginRuntimeHost).Assembly;
        string[] references = plugins.GetReferencedAssemblies()
            .Select(static reference => reference.Name ?? string.Empty)
            .Where(static name => name.StartsWith("BlackFlag.Engine.", StringComparison.Ordinal))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["BlackFlag.Engine.SDK", "BlackFlag.Engine.Security"], references);
    }
}
