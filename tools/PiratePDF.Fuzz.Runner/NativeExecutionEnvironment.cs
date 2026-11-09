using System.Runtime.Versioning;
using System.Security;
using System.Security.Cryptography;
using System.Text.Json;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Current admission checkpoint. Operator-authored manifests cannot turn unqualified providers into proof.</summary>
internal static class NativeExecutionEnvironment
{
    internal static NativeExecutionResult Inspect() => new()
    {
        Admission = NativeBoundaryState.Refused,
        Refusal = "No separately approved, live-verified execution environment was supplied. No process was launched.",
        Filesystem = NativeBoundaryState.NotChecked,
        Network = NativeBoundaryState.NotChecked,
        Storage = NativeBoundaryState.NotChecked,
        EvidenceComplete = false
    };

    internal static NativeProvisioningPlan ParsePlan(string json)
    {
        NativeProvisioningPlan plan = ParseConfiguration(json);
        if (!plan.QualificationApproved || !plan.FilesystemQualified || !plan.NetworkQualified || !plan.StorageQualified || !plan.DesktopQualified)
            throw new SecurityException("Separately approved live security qualification is absent. No native execution is permitted.");
        // The continuation report records the unresolved brokered-DNS/local-IPC and
        // lifecycle review. Manifest booleans are not a substitute for that security proof.
        throw new SecurityException("H2b security qualification admission is not implemented; administrative flags cannot enable execution.");
    }

    internal static NativeProvisioningPlan ParseConfiguration(string json)
    {
        if (json.Length > 65536) throw new SecurityException("Provisioning plan exceeds its work budget.");
        NativeProvisioningPlan plan = JsonSerializer.Deserialize<NativeProvisioningPlan>(json) ?? throw new SecurityException("Missing provisioning plan.");
        if (plan.Version != 1 || plan.RunId is null || plan.RunId.Length != 8 || !plan.RunId.All(Uri.IsHexDigit) || plan.RunId.Any(char.IsUpper) ||
            plan.Account != "pph2b_" + plan.RunId || plan.NetworkOwner == Guid.Empty || plan.DiskId == Guid.Empty ||
            plan.Images is null || plan.Images.Length is < 2 or > 64 || plan.RuntimeResources is null || plan.RuntimeResources.Length is < 1 or > 128 ||
            plan.ExcludedResources is null || plan.ExcludedResources.Length is < 1 or > 128)
            throw new SecurityException("Invalid or incomplete provisioning identity.");
        foreach (string path in new[] { plan.ControllerRoot, plan.RepositoryRoot, plan.MountRoot, plan.BackingPath, plan.Executable, plan.FixtureAssembly }
            .Concat(plan.RuntimeResources).Concat(plan.ExcludedResources)) NativeLaunchPolicy.ValidatePath(path);
        long remainingWork = 536870912;
        foreach (NativeImageIdentity image in plan.Images)
        {
            if (image is null || image.Sha256 is null) throw new SecurityException("Missing dependency identity.");
            NativeStorageBoundary.RequireWithin(Path.Combine(plan.ControllerRoot, "launch"), image.Path);
            if (image.Sha256.Length != 64 || !image.Sha256.All(Uri.IsHexDigit) || image.MaximumBytes is < 1 or > 268435456)
                throw new SecurityException("Invalid approved dependency identity.");
            if (image.MaximumBytes > remainingWork) throw new SecurityException("Dependency admission exceeds aggregate work budget.");
            remainingWork -= image.MaximumBytes;
        }
        if (plan.Images.Select(value => value.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != plan.Images.Length ||
            !plan.Images.Any(value => value.Path.Equals(plan.Executable, StringComparison.OrdinalIgnoreCase)) ||
            !plan.Images.Any(value => value.Path.Equals(plan.FixtureAssembly, StringComparison.OrdinalIgnoreCase)))
            throw new SecurityException("Executable/fixture must belong to the unique approved dependency closure.");
        return plan;
    }

    [SupportedOSPlatform("windows")]
    internal static async Task<Admission> AdmitAsync(NativeExecutionRequest request, CancellationToken token)
    {
        NativeFilesystemAdmission.VerifyProtectedPath(request.PlanPath);
        byte[] bytes = (await InputFile.ReadAsync(request.PlanPath, 65536, token).ConfigureAwait(false)).ToArray();
        if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(request.PlanHash, StringComparison.OrdinalIgnoreCase))
            throw new SecurityException("Approved provisioning plan hash mismatch.");
        NativeProvisioningPlan plan = ParsePlan(System.Text.Encoding.UTF8.GetString(bytes));
        var admission = new Admission(plan);
        try
        {
            admission.Identity = NativeExecutionIdentity.Open(plan.Account, request.Password, plan.AccountSid, plan.RestrictingSid);
            NativeExecutionIdentity.VerifyLogonRights(plan.AccountSid);
            admission.Storage = new(plan, request.Policy);
            admission.Network = new(plan.NetworkOwner, plan.AccountSid, plan.RestrictingSid);
            admission.Network.Verify();
            foreach (NativeImageIdentity image in plan.Images)
            {
                token.ThrowIfCancellationRequested();
                NativeFilesystemAdmission.Verify(admission.Identity.Token, image.Path, false);
                var held = new TrustedNativeImage(image.Path, image.Sha256, image.MaximumBytes);
                admission.Images.Add(held);
                if (image.Path.Equals(plan.Executable, StringComparison.OrdinalIgnoreCase)) admission.Executable = held;
            }
            foreach (string resource in plan.RuntimeResources)
                NativeFilesystemAdmission.VerifyRuntime(admission.Identity.Token, resource);
            foreach (string path in new[] { admission.Storage.Root, admission.Storage.Corpus, admission.Storage.Artifacts, admission.Storage.Temporary })
                NativeFilesystemAdmission.Verify(admission.Identity.Token, path, true);
            foreach (string excluded in plan.ExcludedResources.Concat(new[] { plan.RepositoryRoot, plan.BackingPath, request.PlanPath,
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) }))
                NativeFilesystemAdmission.VerifyDenied(admission.Identity.Token, excluded);
            return admission;
        }
        catch { admission.Dispose(); throw; }
    }

    [SupportedOSPlatform("windows")]
    internal sealed class Admission : IDisposable
    {
        internal NativeProvisioningPlan Plan { get; }
        internal NativeExecutionIdentity Identity { get; set; } = null!;
        internal NativeStorageBoundary Storage { get; set; } = null!;
        internal WindowsNetworkEnforcement Network { get; set; } = null!;
        internal TrustedNativeImage Executable { get; set; } = null!;
        internal List<TrustedNativeImage> Images { get; } = [];
        internal Admission(NativeProvisioningPlan plan) => Plan = plan;
        public void Dispose()
        {
            for (int index = Images.Count - 1; index >= 0; index--) Images[index].Dispose();
            Images.Clear();
            Network?.Dispose();
            Storage?.Dispose();
            Identity?.Dispose();
        }
    }
}
