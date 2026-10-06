using System.Reflection;

namespace PiratePDF.Fuzz.Tests;

/// <summary>Unmarked tests are portable. Windows traits select real file/path integration, never silent skips.</summary>
public static class TestPlatform
{
    public const string Key = "Platform";
    public const string Windows = "Windows";
    public const string Portable = "Portable";

    public static IReadOnlyList<PlatformTest> Catalog()
    {
        var tests = new List<PlatformTest>();
        foreach (Type type in typeof(TestPlatform).Assembly.GetTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
        foreach (MethodInfo method in type.GetMethods().OrderBy(method => method.Name, StringComparer.Ordinal))
        {
            FactAttribute fact = method.GetCustomAttribute<FactAttribute>();
            if (fact is null) continue;
            if (fact.Skip is not null) throw new InvalidOperationException("Platform coverage cannot silently skip a test.");
            string[] platforms = type.GetCustomAttributesData().Concat(method.GetCustomAttributesData())
                .Where(trait => trait.AttributeType == typeof(TraitAttribute) && (string)trait.ConstructorArguments[0].Value == Key)
                .Select(trait => (string)trait.ConstructorArguments[1].Value).ToArray();
            if (platforms.Length > 1 || platforms.Any(value => value != Windows))
                throw new InvalidOperationException("Unknown or ambiguous platform category.");
            var data = method.GetCustomAttributes<Xunit.Sdk.DataAttribute>().ToArray();
            if (data.Any(item => item is not InlineDataAttribute) || fact is TheoryAttribute && data.Length == 0)
                throw new InvalidOperationException("Update the coverage catalog before introducing dynamic test data.");
            tests.Add(new(type.FullName + "." + method.Name, platforms.SingleOrDefault() ?? Portable, Math.Max(1, data.Length)));
        }
        return tests.AsReadOnly();
    }
}

public sealed record PlatformTest(string Method, string Platform, int Cases);

public sealed class PlatformContractTests
{
    [Fact]
    public void EveryTestHasOneSupportedRouteAndBothRoutesArePopulated()
    {
        IReadOnlyList<PlatformTest> catalog = TestPlatform.Catalog();
        Assert.Equal(catalog.Count, catalog.Select(test => test.Method).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(catalog, test => test.Platform == TestPlatform.Portable);
        Assert.Contains(catalog, test => test.Platform == TestPlatform.Windows);
        Assert.All(catalog, test => Assert.True(test.Cases > 0));
    }

    [Theory]
    [InlineData("CorpusManifestTests.SeedLinkEscapeIsRejected")]
    [InlineData("FindingStorageTests.ArtifactLinkEscapeIsRejected")]
    [InlineData("WindowsLibFuzzerArgumentsTests.RealWindowsChildReceivesExactlyOneTargetWithoutShell")]
    public void SecurityAndPathFixturesStayOnWindowsRoute(string method) =>
        Assert.Equal(TestPlatform.Windows, TestPlatform.Catalog().Single(test => test.Method.EndsWith("." + method, StringComparison.Ordinal)).Platform);
}
