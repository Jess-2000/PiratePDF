using System.Security.Cryptography;

namespace PiratePDF.Fuzz.Tests;

public sealed class H2bNativeLaunchTests
{
    [Theory]
    [InlineData("relative.exe")]
    [InlineData(@"\\server\share\driver.exe")]
    [InlineData(@"\\?\C:\driver.exe")]
    [InlineData("C:\\driver.exe:payload")]
    [InlineData("C:\\area\\..\\driver.exe")]
    [InlineData("C:\\area \\driver.exe")]
    [InlineData("C:\\bad\nname.exe")]
    [InlineData("C:\\NUL")]
    [InlineData("C:\\COM1.txt")]
    [InlineData("C:\\LPT\u00b2.bin")]
    public void UnsafeLaunchPathsAreRejected(string path) =>
        Assert.Throws<ArgumentException>(() => H2bTestChild.Call("NativeLaunchPolicy", "ValidatePath", path));

    [Theory]
    [InlineData("", "\"\"")]
    [InlineData("a b", "\"a b\"")]
    [InlineData("a\"b", "\"a\\\"b\"")]
    [InlineData("a\\", "\"a\\\\\"")]
    public void WindowsArgumentEscapingPreservesQuotesAndTrailingSlashes(string argument, string expected) =>
        Assert.Equal(expected, H2bTestChild.Call("NativeLaunchPolicy", "Quote", argument));

    [Fact]
    public void EnvironmentIsClosedAndDoesNotCopyInheritedConfiguration()
    {
        string path = Path.GetFullPath(Path.GetTempPath());
        string[] values = (string[])H2bTestChild.Call("NativeLaunchPolicy", "EnvironmentBlock", path, path)!;
        Assert.Equal(7, values.Length);
        Assert.Contains("DOTNET_EnableDiagnostics=0", values);
        Assert.DoesNotContain(values, value => value.StartsWith("PATH=", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("PROFILER", StringComparison.OrdinalIgnoreCase) || value.Contains("STARTUP_HOOK", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public void AdmittedImageCannotBeWrittenOrReplacedWhileLeasesAreHeld()
    {
        // All ancestors must support read-only leases. The operator profile is outside the owned execution root.
        string root = Path.Combine(ReplayFixture.FindRepository(), "artifacts", "fuzz", "h2b-image-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string image = Path.Combine(root, "controlled-image.bin");
        File.WriteAllBytes(image, [1, 2, 3]);
        try
        {
            string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(image)));
            using var admitted = (IDisposable)H2bTestChild.New("TrustedNativeImage", image, hash, 1024L);
            Assert.Throws<IOException>(() => File.WriteAllBytes(image, [4]));
            Assert.Throws<IOException>(() => File.Move(image, image + ".moved"));
            Assert.Throws<IOException>(() => Directory.Move(root, root + ".moved"));
            Assert.Throws<IOException>(() => H2bTestChild.New("TrustedNativeImage", image, new string('0', 64), 1024L));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
