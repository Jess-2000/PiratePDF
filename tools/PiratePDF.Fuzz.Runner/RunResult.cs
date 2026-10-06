using System.Text.Json;
using PiratePDF.Fuzz.Targets;

namespace PiratePDF.Fuzz.Runner;

public enum RunExitCode
{
    Success = 0,
    TargetRejected = 1,
    ValidationFailure = 2,
    Cancelled = 3,
    UnexpectedFailure = 4
}

/// <summary>A single bounded protocol record; paths and exception text are never included.</summary>
public sealed class RunResult
{
    public const int MaximumJsonBytes = 512;

    internal RunResult(RunExitCode exitCode, string code, FuzzTarget? target = null, FuzzResult? result = null)
    {
        ExitCode = exitCode;
        // Metadata comes only from validated registrations and bounded target result factories.
        Json = JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            target = target?.Id,
            configurationVersion = target?.ConfigurationVersion,
            outcome = exitCode switch
            {
                RunExitCode.Success => "success",
                RunExitCode.TargetRejected => "rejected",
                RunExitCode.ValidationFailure => "invalid",
                RunExitCode.Cancelled => "cancelled",
                _ => "failure"
            },
            code,
            observation = result?.Observation,
            exitCode = (int)exitCode
        });
    }

    public RunExitCode ExitCode { get; }
    public string Json { get; }
}
