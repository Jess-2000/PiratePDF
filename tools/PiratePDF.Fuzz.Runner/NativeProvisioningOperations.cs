using System.Runtime.Versioning;
using System.Security;
using System.Security.Cryptography;
using System.Text.Json;

namespace PiratePDF.Fuzz.Runner;

/// <summary>Explicit provisioning bridge. Inspection never implies approval to launch a child.</summary>
[SupportedOSPlatform("windows")]
internal static class NativeProvisioningOperations
{
    private static readonly string[] PlannedChanges = ["Dedicated account and restricting group", "Batch-only logon policy", "Protected staging ACLs", "Fixed NTFS VHDX", "Eight persistent per-account WFP filters"];
    internal static string Execute(string operation, string path, string approvedHash, SecureString password)
    {
        NativeFilesystemAdmission.VerifyProtectedPath(path);
        using var lease = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (lease.Length > 65536 || !Convert.ToHexString(SHA256.HashData(lease)).Equals(approvedHash, StringComparison.OrdinalIgnoreCase))
            throw new SecurityException("Approved configuration identity mismatch.");
        lease.Position = 0;
        using var reader = new StreamReader(lease);
        NativeProvisioningPlan plan = NativeExecutionEnvironment.ParseConfiguration(reader.ReadToEnd());
        if (operation == "Inspect") return JsonSerializer.Serialize(new { Configuration = "Valid", Qualified = false, plan.RunId, plan.NetworkOwner,
            PlannedChanges });
        if (operation == "Backing") return JsonSerializer.Serialize(WindowsVirtualDisk.InspectBacking(plan.BackingPath));
        if (operation == "Verify")
        {
            using var identity = NativeExecutionIdentity.Open(plan.Account, password, plan.AccountSid, plan.RestrictingSid);
            NativeExecutionIdentity.VerifyLogonRights(plan.AccountSid);
            using var disk = new WindowsVirtualDisk(plan.BackingPath, plan.BackingIdentity, plan.DiskId, plan.MountRoot, plan.VolumeName, plan.Capacity);
            using var network = new WindowsNetworkEnforcement(plan.NetworkOwner, plan.AccountSid, plan.RestrictingSid);
            network.Verify();
            foreach (NativeImageIdentity image in plan.Images)
            {
                NativeFilesystemAdmission.Verify(identity.Token, image.Path, false);
                using var held = new TrustedNativeImage(image.Path, image.Sha256, image.MaximumBytes);
            }
            foreach (string resource in plan.RuntimeResources) NativeFilesystemAdmission.VerifyRuntime(identity.Token, resource);
            foreach (string excluded in plan.ExcludedResources) NativeFilesystemAdmission.VerifyDenied(identity.Token, excluded);
            return JsonSerializer.Serialize(new { Configuration = "LiveReadbackVerified", Qualified = false,
                Pending = "Controlled child, descendant, IPC, DNS, memory, storage and cleanup qualification remains mandatory." });
        }
        throw new SecurityException("State-changing lifecycle requires an approved ownership and recovery plan; no changes performed.");
    }
}
