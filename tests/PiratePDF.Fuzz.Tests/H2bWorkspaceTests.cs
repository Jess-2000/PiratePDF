namespace PiratePDF.Fuzz.Tests;

public sealed class H2bWorkspaceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "PiratePDF H2b Scan " + Guid.NewGuid().ToString("N"));
    public H2bWorkspaceTests() => Directory.CreateDirectory(root);

    [Theory]
    [InlineData(4L, 2, 8L, "WithinPolicy")]
    [InlineData(3L, 2, 8L, "PolicyViolation")]
    [InlineData(4L, 1, 8L, "PolicyViolation")]
    [InlineData(4L, 2, 7L, "PolicyViolation")]
    public void LogicalSizeCountAndAggregateBudgetsAreDetectionOnly(long fileBytes, int entries, long total, string expected)
    {
        File.WriteAllBytes(Path.Combine(root, "a.bin"), [1, 2, 3, 4]);
        File.WriteAllBytes(Path.Combine(root, "b.bin"), [1, 2, 3, 4]);
        Assert.Equal(expected, H2bTestChild.Call("NativeWorkspace", "Inspect", root, fileBytes, entries, total, 1000)!.ToString());
        Assert.Equal(2, Directory.EnumerateFiles(root).Count());
    }

    [Fact]
    public void MissingWorkspaceIsIncompleteRatherThanSafe() => Assert.Equal("Incomplete",
        H2bTestChild.Call("NativeWorkspace", "Inspect", Path.Combine(root, "missing"), 4L, 2, 8L, 1000)!.ToString());

    public void Dispose() => Directory.Delete(root, recursive: true);
}
