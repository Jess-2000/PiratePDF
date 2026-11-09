using System.Security;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Internal controlled qualification only. H3 must separately admit an operational driver/target profile.</summary>
internal sealed class NativeExecutionRequest
{
    internal string PlanPath { get; }
    internal string PlanHash { get; }
    internal SecureString Password { get; } // Caller-owned; never serialized, logged or included in a command/environment.
    internal NativeExecutionPolicy Policy { get; }
    internal string Mode { get; }

    internal NativeExecutionRequest(string planPath, string planHash, SecureString password, NativeExecutionPolicy policy, string mode)
    {
        NativeLaunchPolicy.ValidatePath(planPath);
        if (planHash.Length != 64 || !planHash.All(Uri.IsHexDigit)) throw new ArgumentException("An approved plan identity is required.");
        if (mode is not ("exit0" or "exit17" or "hang" or "both" or "child" or "allocate" or "network" or "access" or "full-volume"))
            throw new ArgumentException("Only closed H2b controlled-fixture modes are admitted.");
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(policy);
        PlanPath = planPath;
        PlanHash = planHash;
        Password = password;
        Policy = policy;
        Mode = mode;
    }
}

// Administrative configuration, not child-provided evidence. Live OS checks are still mandatory.
internal sealed record NativeProvisioningPlan
{
    public int Version { get; init; }
    public string RunId { get; init; } = "";
    public string Account { get; init; } = "";
    public string AccountSid { get; init; } = "";
    public string RestrictingSid { get; init; } = "";
    public string ControllerRoot { get; init; } = "";
    public string RepositoryRoot { get; init; } = "";
    public string MountRoot { get; init; } = "";
    public string VolumeName { get; init; } = "";
    public string BackingPath { get; init; } = "";
    public string BackingIdentity { get; init; } = "";
    public Guid DiskId { get; init; }
    public Guid NetworkOwner { get; init; }
    public long Capacity { get; init; }
    public string Executable { get; init; } = "";
    public string FixtureAssembly { get; init; } = "";
    public NativeImageIdentity[] Images { get; init; } = [];
    public string[] RuntimeResources { get; init; } = [];
    public string[] ExcludedResources { get; init; } = [];
    public bool QualificationApproved { get; init; }
    public bool FilesystemQualified { get; init; }
    public bool NetworkQualified { get; init; }
    public bool StorageQualified { get; init; }
    public bool DesktopQualified { get; init; }
}

internal sealed record NativeImageIdentity(string Path, string Sha256, long MaximumBytes, string? SourcePath = null);
