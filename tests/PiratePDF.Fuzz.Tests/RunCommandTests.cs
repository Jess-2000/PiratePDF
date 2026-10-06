using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BlackFlag.Engine.Filters;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

public sealed class RunCommandTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "PiratePDF-Fuzz-" + Guid.NewGuid().ToString("N"));

    public RunCommandTests() => Directory.CreateDirectory(directory);

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task BuiltInTargetExecutesWithExactKnownOutput()
    {
        string path = Input("48656c6c6f>"u8.ToArray());
        RunResult result = await new RunCommand().ExecuteAsync(Arguments(path, "filter-asciihex"));

        AssertResult(result, RunExitCode.Success, "success", "TargetResult");
        using JsonDocument json = JsonDocument.Parse(result.Json);
        Assert.Equal("filter-asciihex", json.RootElement.GetProperty("target").GetString());
        Assert.Equal(1, json.RootElement.GetProperty("configurationVersion").GetInt32());
        Assert.Equal(FuzzResult.Success("Hello"u8).Observation, json.RootElement.GetProperty("observation").GetString());
        Assert.Equal("48656c6c6f>"u8.ToArray(), File.ReadAllBytes(path));
    }

    [Theory]
    [InlineData("filter-asciihex")]
    [InlineData("filter-flate")]
    [InlineData("font-sanitize")]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task BuiltInMalformedInputIsExpectedTargetRejection(string id)
    {
        RunResult result = await new RunCommand().ExecuteAsync(Arguments(Input([255]), id));

        AssertResult(result, RunExitCode.TargetRejected, "rejected", "TargetResult");
        using JsonDocument json = JsonDocument.Parse(result.Json);
        Assert.Equal(id == "font-sanitize" ? "MalformedHeader" : "InvalidData",
            json.RootElement.GetProperty("observation").GetString());
    }

    [Theory]
    [InlineData("unknown", "v1", "UnknownTarget")]
    [InlineData("TARGET", "v1", "UnknownTarget")]
    [InlineData("target", "v2", "ConfigurationMismatch")]
    public async Task RegistryAndVersionValidationPrecedeFileAccess(string id, string version, string code)
    {
        int calls = 0;
        var command = Command((input, _) => { calls++; return FuzzResult.Success(input); });
        RunResult result = await command.ExecuteAsync(Arguments("\0private-path", id, version));
        AssertResult(result, RunExitCode.ValidationFailure, "invalid", code);
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("v0")]
    [InlineData("v01")]
    [InlineData("v-1")]
    [InlineData("v+1")]
    [InlineData("V1")]
    [InlineData("v2147483648")]
    [InlineData("v1 ")]
    public async Task ConfigurationSyntaxFailsDeterministically(string version)
    {
        var command = Command(static (_, _) => throw new InvalidOperationException());
        RunResult result = await command.ExecuteAsync(Arguments("missing", version: version));
        AssertResult(result, RunExitCode.ValidationFailure, "invalid", "Usage");
    }

    [Fact]
    public async Task UsageRejectsMissingDuplicateExtraAndUnknownOptions()
    {
        string[][] invalid =
        [
            [], ["run"], ["replay", "--target", "target", "--input", "missing", "--config", "v1"],
            ["run", "--target", "target", "--target", "target", "--config", "v1"],
            ["run", "--target", "target", "--input", "missing", "--other", "v1"],
            ["run", "--target", "target", "--input", "missing", "--config", "v1", "extra"],
            Arguments(""), Arguments("missing", new string('a', 65)), Arguments(new string('a', 32768))
        ];
        foreach (string[] args in invalid)
        {
            AssertResult(await new RunCommand().ExecuteAsync(args), RunExitCode.ValidationFailure, "invalid", "Usage");
        }
        AssertResult(await new RunCommand().ExecuteAsync(null), RunExitCode.ValidationFailure, "invalid", "Usage");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(5)]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task InputAdmissionAcceptsEmptyAndExactLimitAndRejectsLimitPlusOne(int size)
    {
        int calls = 0;
        var command = Command((input, _) =>
        {
            calls++;
            Assert.Equal(size, input.Length);
            return FuzzResult.Success(input);
        });
        RunResult result = await command.ExecuteAsync(Arguments(Input(new byte[size])));
        AssertResult(result, size > 4 ? RunExitCode.ValidationFailure : RunExitCode.Success,
            size > 4 ? "invalid" : "success", size > 4 ? "InputTooLarge" : "TargetResult");
        Assert.Equal(size > 4 ? 0 : 1, calls);
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task NonDefaultRegisteredVersionAndIsolationRemainMetadata()
    {
        var target = new FuzzTarget("worker", 7, 1, FuzzIsolation.DedicatedProcess,
            static (input, _) => FuzzResult.Success(input));
        var command = new RunCommand(new TargetRegistry([target]));
        RunResult result = await command.ExecuteAsync(Arguments(Input([1]), "worker", "v7"));

        AssertResult(result, RunExitCode.Success, "success", "TargetResult");
        using JsonDocument json = JsonDocument.Parse(result.Json);
        Assert.Equal(7, json.RootElement.GetProperty("configurationVersion").GetInt32());
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task MissingDirectoryAndInvalidPathsNeverInvokeTarget()
    {
        int calls = 0;
        var command = Command((input, _) => { calls++; return FuzzResult.Success(input); });
        string[] paths = [Path.Combine(directory, "missing-private-document"), directory, "\0invalid",
            @"\\.\pipe\never-open", @"\\?\C:\private", Path.Combine(directory, "NUL.secret.bin"),
            Path.Combine(directory, "ordinary.bin:private-stream")];
        foreach (string path in paths)
        {
            RunResult result = await command.ExecuteAsync(Arguments(path));

            AssertResult(result, RunExitCode.ValidationFailure, "invalid", "InputUnavailable");
        }
        Assert.Equal(0, calls);
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task ExclusiveFileLockRejectsBeforeTargetExecution()
    {
        string path = Input([1]);
        using var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var command = Command(static (_, _) => throw new InvalidOperationException());
        RunResult result = await command.ExecuteAsync(Arguments(path));

        AssertResult(result, RunExitCode.ValidationFailure, "invalid", "InputUnavailable");
    }

    [Fact]
    public async Task PreCancellationDoesNotAccessFileOrInvokeTarget()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var command = Command(static (_, _) => throw new InvalidOperationException());
        RunResult result = await command.ExecuteAsync(Arguments("\0invalid"), cancellation.Token);
        AssertResult(result, RunExitCode.Cancelled, "cancelled", "Cancelled");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task CancellationDuringTargetCannotBecomeSuccessOrRejection(bool rejection)
    {
        using var cancellation = new CancellationTokenSource();
        var command = Command((input, token) =>
        {
            Assert.Equal(cancellation.Token, token);
            cancellation.Cancel();
            return rejection ? FuzzResult.Rejected("InvalidData") : FuzzResult.Success(input);
        });
        RunResult result = await command.ExecuteAsync(Arguments(Input([])), cancellation.Token);

        AssertResult(result, RunExitCode.Cancelled, "cancelled", "Cancelled");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task FilterCancellationRequiresARequestedToken(bool requested)
    {
        using var cancellation = new CancellationTokenSource();
        var command = Command((_, _) =>
        {
            if (requested) cancellation.Cancel();
            throw new PdfFilterDecodeException(PdfFilterDecodeFailure.Cancelled, "private document content");
        });
        RunResult result = await command.ExecuteAsync(Arguments(Input([])), cancellation.Token);

        AssertResult(result, requested ? RunExitCode.Cancelled : RunExitCode.UnexpectedFailure,
            requested ? "cancelled" : "failure", requested ? "Cancelled" : "UnexpectedTargetFailure");
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task UnexpectedDefectsIncludingSpontaneousCancellationAreFailuresWithoutDetails()
    {
        string secret = "PRIVATE_DOCUMENT_TOKEN_123";
        Exception[] failures =
        [
            new InvalidOperationException(secret), new IOException(secret), new ArgumentException(secret),
            new IndexOutOfRangeException(secret), new OverflowException(secret), new NullReferenceException(secret),
            new OutOfMemoryException(secret), new AccessViolationException(secret), new OperationCanceledException(secret),
            new PdfFilterDecodeException(PdfFilterDecodeFailure.InvalidData, secret)
        ];
        string path = Input([1]);
        foreach (Exception failure in failures)
        {
            // Synthetic failures do not allocate excessively, enter native code or crash the test process.
            RunResult result = await Command((_, _) => throw failure).ExecuteAsync(Arguments(path));

            AssertResult(result, RunExitCode.UnexpectedFailure, "failure", "UnexpectedTargetFailure");
            Assert.DoesNotContain(secret, result.Json);
            Assert.DoesNotContain(failure.GetType().Name, result.Json);
            Assert.DoesNotContain(path, result.Json);
        }
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task RepeatOutputIgnoresCulturePathAndOptionOrder()
    {
        string firstPath = Input([1, 2, 3]);
        string secondPath = Path.Combine(directory, "second.bin");
        File.Copy(firstPath, secondPath);
        var command = Command(static (input, _) => FuzzResult.Success(input));
        RunResult first = await command.ExecuteAsync(Arguments(firstPath));
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            RunResult second = await command.ExecuteAsync(["run", "--config", "v1", "--input", secondPath, "--target", "target"]);
            Assert.Equal(first.Json, second.Json);
            Assert.Equal(first.ExitCode, second.ExitCode);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task MaximumMetadataProducesBoundedOutputWithoutInputOrPath()
    {
        string id = new('a', 64);
        string observation = new('Z', 64);
        string privateInput = "PRIVATE_DOCUMENT_TOKEN_123";
        var target = new FuzzTarget(id, int.MaxValue, 64, FuzzIsolation.SupervisedManagedWorker,
            (_, _) => FuzzResult.Rejected(observation));
        var command = new RunCommand(new TargetRegistry([target]));
        string path = Input(Encoding.UTF8.GetBytes(privateInput));
        RunResult result = await command.ExecuteAsync(Arguments(path, id, "v2147483647"));

        AssertResult(result, RunExitCode.TargetRejected, "rejected", "TargetResult");
        Assert.DoesNotContain(privateInput, result.Json);
        Assert.DoesNotContain(path, result.Json);
        using JsonDocument json = JsonDocument.Parse(result.Json);
        Assert.Equal(observation, json.RootElement.GetProperty("observation").GetString());
    }

    [Fact]
    public async Task UnknownTargetDoesNotEchoUntrustedMetadata()
    {
        RunResult result = await new RunCommand().ExecuteAsync(
            Arguments("PRIVATE_PATH", "PRIVATE_TOKEN\n\"value\":\"secret\""));
        AssertResult(result, RunExitCode.ValidationFailure, "invalid", "UnknownTarget");
        Assert.DoesNotContain("PRIVATE", result.Json);
        Assert.DoesNotContain("secret", result.Json);
        using JsonDocument json = JsonDocument.Parse(result.Json);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("target").ValueKind);
    }

    [Fact]
    [Trait(TestPlatform.Key, TestPlatform.Windows)]
    public async Task ForeignCancellationTokenIsNotARequestedCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        using var foreign = new CancellationTokenSource();
        foreign.Cancel();
        var command = Command((_, _) =>
        {
            cancellation.Cancel();
            throw new OperationCanceledException(foreign.Token);
        });
        RunResult result = await command.ExecuteAsync(Arguments(Input([])), cancellation.Token);

        AssertResult(result, RunExitCode.UnexpectedFailure, "failure", "UnexpectedTargetFailure");
    }

    [Fact]
    public void ExitCodesAreStableAndDistinct()
    {
        Assert.Equal(0, (int)RunExitCode.Success);
        Assert.Equal(1, (int)RunExitCode.TargetRejected);
        Assert.Equal(2, (int)RunExitCode.ValidationFailure);
        Assert.Equal(3, (int)RunExitCode.Cancelled);
        Assert.Equal(4, (int)RunExitCode.UnexpectedFailure);
        Assert.Equal(5, Enum.GetValues<RunExitCode>().Distinct().Count());
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);

    private string Input(byte[] bytes)
    {
        string path = Path.Combine(directory, "private-document.bin");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private static RunCommand Command(ByteTarget execute) => new(new TargetRegistry(
        [new FuzzTarget("target", 1, 4, FuzzIsolation.SupervisedManagedWorker, execute)]));

    private static string[] Arguments(string path, string id = "target", string version = "v1") =>
        ["run", "--target", id, "--input", path, "--config", version];

    private static void AssertResult(RunResult result, RunExitCode exitCode, string outcome, string code)
    {
        Assert.Equal(exitCode, result.ExitCode);
        Assert.InRange(Encoding.UTF8.GetByteCount(result.Json), 1, RunResult.MaximumJsonBytes);
        Assert.DoesNotContain('\n', result.Json);
        using JsonDocument json = JsonDocument.Parse(result.Json);
        Assert.Equal(new[] { "schemaVersion", "target", "configurationVersion", "outcome", "code", "observation", "exitCode" },
            json.RootElement.EnumerateObject().Select(property => property.Name));
        Assert.Equal(1, json.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(outcome, json.RootElement.GetProperty("outcome").GetString());
        Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
        Assert.Equal((int)exitCode, json.RootElement.GetProperty("exitCode").GetInt32());
        if (exitCode is not RunExitCode.Success and not RunExitCode.TargetRejected)
        {
            Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("observation").ValueKind);
        }
    }
}
