using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class WindowsLibFuzzerArgumentsTests
{
    private static string Host => Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..", "dotnet.exe"));

    [Theory]
    [InlineData(@"C:\fuzz\target.dll")]
    [InlineData(@"C:\fuzz area\target.dll")]
    [InlineData(@"C:\fuzz (owned) & literal\target.dll")]
    [InlineData(@"C:\percent%bang!^'[]\target.dll")]
    public void OneCompleteArgumentStringPreservesLiteralPaths(string assembly)
    {
        string[] arguments = WindowsLibFuzzerArguments.Create(Host, assembly, "filter-asciihex");
        Assert.Equal(2, arguments.Length);
        Assert.Equal("--target_path=\"" + Host + "\"", arguments[0]);
        Assert.Equal("--target_arg=\"" + assembly + "\" filter-asciihex", arguments[1]);
        Assert.Single(arguments, static value => value.StartsWith("--target_arg=", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("filter-asciihex --target_arg=other")]
    [InlineData("filter-asciihex filter-flate")]
    [InlineData("\"filter-asciihex\"")]
    [InlineData("unknown")]
    public void AmbiguousOrUnknownTargetIsRejected(string target) =>
        Assert.Throws<ArgumentException>(() => WindowsLibFuzzerArguments.Create(Host, @"C:\fuzz\target.dll", target));

    [Theory]
    [InlineData("target.dll")]
    [InlineData("C:\\fuzz\\bad\"name.dll")]
    [InlineData("C:\\fuzz\\bad\nname.dll")]
    [InlineData(@"C:\fuzz\target.dll\")]
    [InlineData(@"C:\fuzz\target.dll:stream")]
    [InlineData(@"C:\semi;colon\target.dll")]
    public void NonFileOrUnquotableAssemblyIsRejected(string assembly) =>
        Assert.Throws<ArgumentException>(() => WindowsLibFuzzerArguments.Create(Host, assembly, "filter-asciihex"));

    [Fact]
    public void ShellHostIsRejected() => Assert.Throws<ArgumentException>(() =>
        WindowsLibFuzzerArguments.Create(@"C:\Windows\System32\cmd.exe", @"C:\fuzz\target.dll", "filter-asciihex"));

    [Theory]
    [InlineData("ordinary")]
    [InlineData("with spaces")]
    [InlineData("(owned) & literal")]
    [InlineData("percent%bang!^'[]")]
    public async Task RealWindowsChildReceivesExactlyOneTargetWithoutShell(string name)
    {
        string parent = Path.Combine(ReplayFixture.FindRepository(), "artifacts", "fuzz", "transport-tests");
        string directory = Path.Combine(parent, Guid.NewGuid().ToString("N"), name);
        Directory.CreateDirectory(directory);
        var owned = new List<string>();
        try
        {
            // Copy only top-level managed test runtime files; no native engine, user input or recursive discovery.
            foreach (string source in Directory.EnumerateFiles(AppContext.BaseDirectory))
            {
                if (Path.GetExtension(source) is not (".dll" or ".json")) continue;
                string copy = Path.Combine(directory, Path.GetFileName(source));
                File.Copy(source, copy, overwrite: false);
                owned.Add(copy);
            }
            string assembly = Path.Combine(directory, Path.GetFileName(typeof(SupervisorTestChild).Assembly.Location));
            string[] driverArguments = WindowsLibFuzzerArguments.Create(Host, assembly, "filter-asciihex");
            // The pinned driver's first target_arg is appended verbatim to its child command line.
            // Exercise Windows/.NET parsing of that value, not a second copy of a quoting algorithm.
            var start = new ProcessStartInfo(Host)
            {
                Arguments = driverArguments[1]["--target_arg=".Length..],
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            Assert.False(start.UseShellExecute);
            Assert.Equal(Host, start.FileName);
            using var process = new Process { StartInfo = start };
            process.Start();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                char[] output = new char[256];
                int count = await process.StandardOutput.ReadBlockAsync(output.AsMemory(), deadline.Token);
                await process.WaitForExitAsync(deadline.Token);
                char[] error = new char[2048];
                int errorCount = await process.StandardError.ReadBlockAsync(error.AsMemory(), deadline.Token);
                Assert.True(process.ExitCode == 0, "Controlled child failed: " + new string(error, 0, errorCount));
                Assert.Equal(new[] { "filter-asciihex" }, JsonSerializer.Deserialize<string[]>(new string(output, 0, count).Trim()));
                Assert.Equal(0, errorCount);
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await process.WaitForExitAsync(cleanup.Token);
                }
            }
        }
        finally
        {
            // Only this fixture's exact files and empty directories; never recursive deletion.
            foreach (string path in owned) File.Delete(path);
            Directory.Delete(directory);
            Directory.Delete(Path.GetDirectoryName(directory)!);
        }
    }
}
