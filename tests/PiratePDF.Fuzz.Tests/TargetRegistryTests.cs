using System.Globalization;

namespace PiratePDF.Fuzz.Tests;

public sealed class TargetRegistryTests
{
    [Fact]
    public void BuiltInMetadataIsStableAndOrdered()
    {
        Assert.Equal(new[] { "filter-asciihex", "filter-flate", "font-sanitize" },
            TargetRegistry.All.Select(target => target.Id));
        Assert.All(TargetRegistry.All, target =>
        {
            Assert.Equal(1, target.ConfigurationVersion);
            Assert.Equal(FuzzIsolation.SupervisedManagedWorker, target.Isolation);
            Assert.Same(target, TargetRegistry.Get(target.Id));
            Assert.Equal(target.Id == "font-sanitize" ? 65536 : 16384, target.MaximumInputBytes);
        });
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    public void RegistryOwnsItsSnapshotAndUsesOrdinalOrder(string cultureName)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            FuzzTarget first = Target("i-1");
            FuzzTarget second = Target("i1");
            FuzzTarget[] source = [second, first];
            var registry = new TargetRegistry(source);
            source[0] = Target("replacement");
            Assert.Equal(new[] { "i-1", "i1" }, registry.Targets.Select(target => target.Id));
            Assert.Same(first, registry.Find("i-1"));
            Assert.Same(second, registry.Find("i1"));
            var exposed = Assert.IsAssignableFrom<IList<FuzzTarget>>(registry.Targets);
            Assert.Throws<NotSupportedException>(() => exposed[0] = Target("changed"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void DuplicateIdsRejectBeforeAnyTargetExecution()
    {
        int calls = 0;
        var target = new FuzzTarget("duplicate", 1, 1, FuzzIsolation.SupervisedManagedWorker,
            (_, _) => { calls++; return FuzzResult.Success([]); });
        var otherVersion = new FuzzTarget("duplicate", 2, 2, FuzzIsolation.DedicatedProcess,
            (_, _) => { calls++; return FuzzResult.Success([]); });
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new TargetRegistry([target, otherVersion]));
        Assert.Equal("targets", error.ParamName);
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("FILTER-FLATE")]
    [InlineData("not-registered")]
    public void UnknownIdsFailDeterministically(string id)
    {
        var registry = new TargetRegistry([Target("known")]);
        ArgumentException first = Assert.Throws<ArgumentException>(() => registry.Find(id));
        ArgumentException second = Assert.Throws<ArgumentException>(() => registry.Find(id));
        Assert.Equal("id", first.ParamName);
        Assert.Equal(first.Message, second.Message);
        Assert.Throws<ArgumentException>(() => TargetRegistry.Get(id));
    }

    [Fact]
    public void NullInputsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new TargetRegistry(null));
        Assert.Throws<ArgumentException>(() => new TargetRegistry([null]));
        Assert.Throws<ArgumentNullException>(() => TargetRegistry.Get(null));
    }

    [Fact]
    public void EmptyRegistryAndExactRegistrationCeilingAreSupported()
    {
        Assert.Empty(new TargetRegistry([]).Targets);
        FuzzTarget[] targets = Enumerable.Range(0, 256)
            .Select(index => Target("target-" + index.ToString(CultureInfo.InvariantCulture))).ToArray();
        var registry = new TargetRegistry(targets);
        Assert.Equal(256, registry.Targets.Count);
        Assert.Same(targets[255], registry.Find("target-255"));
        Assert.Throws<ArgumentException>(() => new TargetRegistry([.. targets, Target("extra")]));
    }

    private static FuzzTarget Target(string id) =>
        new(id, 1, 4, FuzzIsolation.SupervisedManagedWorker, static (input, _) => FuzzResult.Success(input));
}
