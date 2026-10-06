using System.Reflection;
using BlackFlag.Engine.Abstractions.IO;

namespace BlackFlag.Engine.Abstractions.Tests;

public sealed class IRandomAccessSourceContractTests
{
    [Fact]
    public void ExposesPositionIndependentReadContract()
    {
        Type source = typeof(IRandomAccessSource);

        Assert.True(source.IsInterface);

        PropertyInfo? length = source.GetProperty(nameof(IRandomAccessSource.Length));
        Assert.NotNull(length);
        Assert.Equal(typeof(long), length.PropertyType);
        Assert.True(length.CanRead);
        Assert.False(length.CanWrite);

        MethodInfo? read = source.GetMethod(
            nameof(IRandomAccessSource.Read),
            [typeof(long), typeof(Span<byte>)]);
        Assert.NotNull(read);
        Assert.Equal(typeof(int), read.ReturnType);

        MethodInfo? readAsync = source.GetMethod(
            nameof(IRandomAccessSource.ReadAsync),
            [typeof(long), typeof(Memory<byte>), typeof(CancellationToken)]);
        Assert.NotNull(readAsync);
        Assert.Equal(typeof(ValueTask<int>), readAsync.ReturnType);

        ParameterInfo cancellationToken = readAsync.GetParameters()[2];
        Assert.True(cancellationToken.IsOptional);
        Assert.True(cancellationToken.HasDefaultValue);
    }

    [Fact]
    public void RequiresSynchronousAndAsynchronousDisposal()
    {
        Type source = typeof(IRandomAccessSource);

        Assert.True(typeof(IDisposable).IsAssignableFrom(source));
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(source));
    }

    [Fact]
    public void AssemblyReferencesOnlyApprovedBlackFlagLayer()
    {
        Assembly abstractions = typeof(IRandomAccessSource).Assembly;
        string[] blackFlagReferences = abstractions.GetReferencedAssemblies()
            .Select(static reference => reference.Name ?? string.Empty)
            .Where(static name =>
                name.StartsWith("BlackFlag.Engine.", StringComparison.Ordinal))
            .ToArray();

        Assert.All(
            blackFlagReferences,
            static name => Assert.Equal("BlackFlag.Engine.Core", name));
    }
}
