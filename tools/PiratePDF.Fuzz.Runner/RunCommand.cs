using System.Globalization;
using BlackFlag.Engine.Filters;
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Executes one registered target. This worker provides no process or deadline supervision.</summary>
public sealed class RunCommand
{
    private readonly TargetRegistry registry;

    public RunCommand() : this(new TargetRegistry(TargetRegistry.All)) { }

    public RunCommand(TargetRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        this.registry = registry;
    }

    public async Task<RunResult> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (!TryParse(args, out string id, out string path, out int version))
        {
            return new(RunExitCode.ValidationFailure, "Usage");
        }

        FuzzTarget target;
        try
        {
            target = registry.Find(id);
        }
        catch (ArgumentException)
        {
            return new(RunExitCode.ValidationFailure, "UnknownTarget");
        }

        if (version != target.ConfigurationVersion)
        {
            return new(RunExitCode.ValidationFailure, "ConfigurationMismatch", target);
        }

        ReadOnlyMemory<byte> input;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            input = await InputFile.ReadAsync(path, target.MaximumInputBytes, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new(RunExitCode.Cancelled, "Cancelled", target);
        }
        catch (InputTooLargeException)
        {
            return new(RunExitCode.ValidationFailure, "InputTooLarge", target);
        }
        catch (PlatformNotSupportedException)
        {
            return new(RunExitCode.ValidationFailure, "UnsupportedInputPlatform", target);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return new(RunExitCode.ValidationFailure, "InputUnavailable", target);
        }
        catch (Exception)
        {
            return new(RunExitCode.UnexpectedFailure, "UnexpectedRunnerFailure", target);
        }

        try
        {
            FuzzResult result = target.Run(input.Span, cancellationToken);
            return new(result.Outcome == "success" ? RunExitCode.Success : RunExitCode.TargetRejected,
                "TargetResult", target, result);
        }
        catch (OperationCanceledException error) when (cancellationToken.IsCancellationRequested &&
            error.CancellationToken == cancellationToken)
        {
            return new(RunExitCode.Cancelled, "Cancelled", target);
        }
        catch (PdfFilterDecodeException error) when (cancellationToken.IsCancellationRequested &&
            error.Failure == PdfFilterDecodeFailure.Cancelled)
        {
            return new(RunExitCode.Cancelled, "Cancelled", target);
        }
        catch (Exception)
        {
            // A worker protocol failure, never malformed input or recovery. The CLI exits immediately.
            // Runtime-fatal failures may prevent even this result; later supervision must detect those exits.
            return new(RunExitCode.UnexpectedFailure, "UnexpectedTargetFailure", target);
        }
    }

    private static bool TryParse(string[] args, out string id, out string path, out int version)
    {
        id = path = string.Empty;
        version = 0;
        if (args is null || args.Length != 7 || args[0] != "run") return false;

        string? configuration = null;
        for (int index = 1; index < args.Length; index += 2)
        {
            string value = args[index + 1];
            if (string.IsNullOrEmpty(value)) return false;
            switch (args[index])
            {
                case "--target" when id.Length == 0 && value.Length <= 64:
                    id = value;
                    break;
                case "--input" when path.Length == 0 && value.Length <= 32767:
                    path = value;
                    break;
                case "--config" when configuration is null && value.Length <= 11:
                    configuration = value;
                    break;
                default:
                    return false;
            }
        }

        // PP-AUD-029 spells configuration versions v1, v2, ...; decimal parsing is culture independent.
        return id.Length > 0 && path.Length > 0 && configuration is { Length: > 1 } &&
            configuration[0] == 'v' && configuration[1] != '0' &&
            int.TryParse(configuration.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out version) && version > 0;
    }
}
