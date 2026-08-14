using System.Reflection;

namespace BlackFlag.Engine.Writing.Tests;

public sealed class WritingAssemblyTests
{
    [Fact]
    public void WritingAssemblyCanBeLoaded()
    {
        var assembly =
            Assembly.Load(
                new AssemblyName(
                    "BlackFlag.Engine.Writing"));

        Assert.Equal(
            "BlackFlag.Engine.Writing",
            assembly.GetName().Name);
    }
}
