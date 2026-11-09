using System.Globalization;
using System.Runtime.Versioning;
using System.Security;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace PiratePDF.Fuzz.Runner;

[SupportedOSPlatform("windows")]
internal sealed class NativeStationTrust : IDisposable
{
    private readonly List<IDisposable> leases = [];
    internal StationManifest Manifest { get; }
    internal string Digest { get; }

    internal NativeStationTrust(string digest)
    {
        NativeStationPolicy.RequireHex(digest, 64);
        Digest = digest;
        try
        {
            byte[] manifestBytes = ReadProtected(Path.Combine(NativeStationPolicy.CodeRoot, "closure.json"), 131072);
            if (Convert.ToHexStringLower(SHA256.HashData(manifestBytes)) != digest) throw new SecurityException("Operator code-closure approval mismatch.");
            Manifest = NativeStationPolicy.Decode<StationManifest>(manifestBytes);
            ValidateManifest(Manifest);
            var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (StationFile file in Manifest.Files)
            {
                string path = Resolve(Manifest, file);
                if (!expected.Add(path)) throw new SecurityException("Duplicate code dependency.");
                ProtectAncestors(path);
                var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                leases.Add(stream);
                MatchFile(file, stream.Length, StationBootstrapInterop.FileId(stream.SafeFileHandle), Convert.ToHexStringLower(SHA256.HashData(stream)));
            }
            // Every file in the two application directories and selected runtime graph is admitted.
            foreach (string path in EnumerateClosure(Manifest))
                if (!expected.Remove(path)) throw new SecurityException("Unexpected code or runtime dependency.");
            if (expected.Count != 0) throw new SecurityException("Incomplete code/runtime closure.");
            string[] fxr = Directory.GetDirectories(Path.Combine(NativeStationPolicy.RuntimeRoot, "host", "fxr"));
            if (fxr.Length is < 1 or > 32 || fxr.Any(path => !Version.TryParse(Path.GetFileName(path), out Version? version) ||
                version > Version.Parse(Manifest.HostFxrVersion)))
                throw new SecurityException("The hostfxr selected by the installed host is not independently approved.");
            ValidateRuntimeConfig(File.ReadAllBytes(Path.Combine(NativeStationPolicy.CodeRoot, "PiratePDF.NativeProvisioning.Station.runtimeconfig.json")), Manifest.RuntimeVersion);
            if (Directory.GetFiles(NativeStationPolicy.CodeRoot, "*.runtimeconfig.dev.json", SearchOption.AllDirectories).Length != 0)
                throw new SecurityException("Development dependency probing refused.");
        }
        catch { Dispose(); throw; }
    }

    internal static void ValidateManifest(StationManifest manifest)
    {
        if (manifest.Version != 1 || !Version.TryParse(manifest.RuntimeVersion, out Version? runtime) || runtime.Major != 10 || runtime.Build < 0 ||
            !Version.TryParse(manifest.HostFxrVersion, out Version? fxr) || fxr.Major != 10 || fxr.Build < 0 ||
            manifest.Files is null || manifest.Files.Length is < 8 or > 512)
            throw new SecurityException("Invalid station code manifest.");
        long total = 0;
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (StationFile file in manifest.Files)
        {
            if (file is null || file.Root is not ("Code" or "Runtime") || string.IsNullOrEmpty(file.Path) ||
                file.Path.Length > 240 || file.Path.Contains(':', StringComparison.Ordinal) || file.Path.Contains('\\', StringComparison.Ordinal) ||
                file.Path.Split('/').Any(part => part is "" or "." or ".." || part.EndsWith(' ') || part.EndsWith('.')) ||
                file.Length is < 1 or > 268435456) throw new SecurityException("Invalid station dependency path/budget.");
            NativeStationPolicy.RequireHex(file.Sha256, 64); NativeStationPolicy.RequireHex(file.FileId, 48);
            if (!unique.Add(file.Root + "/" + file.Path)) throw new SecurityException("Duplicate code-closure identity.");
            total = checked(total + file.Length);
            if (total > 536870912) throw new SecurityException("Code-closure work budget exceeded.");
            _ = Resolve(manifest, file);
        }
        foreach (string required in new[] { "Code/PiratePDF.NativeProvisioning.Station.dll", "Code/PiratePDF.NativeProvisioning.Station.deps.json",
            "Code/PiratePDF.NativeProvisioning.Station.runtimeconfig.json", "Code/Provision-NativeExecution.ps1", "Code/PiratePDF.NativeProvisioning.Bootstrap.dll",
            "Code/controller/PiratePDF.Fuzz.Runner.exe", "Code/controller/PiratePDF.Fuzz.Runner.dll", "Runtime/dotnet.exe",
            "Runtime/host/fxr/" + manifest.HostFxrVersion + "/hostfxr.dll",
            "Runtime/shared/Microsoft.NETCore.App/" + manifest.RuntimeVersion + "/hostpolicy.dll",
            "Runtime/shared/Microsoft.NETCore.App/" + manifest.RuntimeVersion + "/coreclr.dll" })
            if (!manifest.Files.Any(file => file.Root + "/" + file.Path == required)) throw new SecurityException("Missing required runtime/code member.");
    }
    internal static void MatchFile(StationFile expected, long length, string identity, string digest)
    {
        if (expected.Length != length || expected.FileId != identity || expected.Sha256 != digest)
            throw new SecurityException("Protected code dependency identity mismatch.");
    }
    private static string Resolve(StationManifest manifest, StationFile file)
    {
        if (file.Root == "Runtime" && file.Path.Contains('/', StringComparison.Ordinal) &&
            !file.Path.StartsWith("host/fxr/" + manifest.HostFxrVersion + "/", StringComparison.Ordinal) &&
            !file.Path.StartsWith("shared/Microsoft.NETCore.App/" + manifest.RuntimeVersion + "/", StringComparison.Ordinal))
            throw new SecurityException("Unapproved runtime probing location.");
        return Path.Combine(file.Root == "Code" ? NativeStationPolicy.CodeRoot : NativeStationPolicy.RuntimeRoot, file.Path.Replace('/', Path.DirectorySeparatorChar));
    }
    private static List<string> EnumerateClosure(StationManifest manifest)
    {
        var paths = new List<string>();
        foreach (string root in new[] { NativeStationPolicy.CodeRoot, Path.Combine(NativeStationPolicy.RuntimeRoot, "host", "fxr", manifest.HostFxrVersion),
            Path.Combine(NativeStationPolicy.RuntimeRoot, "shared", "Microsoft.NETCore.App", manifest.RuntimeVersion) })
        {
            var pending = new Queue<string>(); pending.Enqueue(root); int directories = 0;
            while (pending.TryDequeue(out string? directory))
            {
                ProtectAncestors(directory);
                if (++directories > 128) throw new SecurityException("Dependency directory budget exceeded.");
                foreach (string child in Directory.EnumerateFileSystemEntries(directory))
                {
                    if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0) throw new SecurityException("Redirected closure member.");
                    if (Directory.Exists(child)) pending.Enqueue(child);
                    else if (child != Path.Combine(NativeStationPolicy.CodeRoot, "closure.json")) paths.Add(child);
                    if (paths.Count + pending.Count > 512) throw new SecurityException("Dependency enumeration budget exceeded.");
                }
            }
        }
        foreach (string file in Directory.EnumerateFiles(NativeStationPolicy.RuntimeRoot)) paths.Add(file);
        if (paths.Count > 512) throw new SecurityException("Runtime root budget exceeded.");
        return paths;
    }
    internal static void ValidateRuntimeConfig(byte[] bytes, string version)
    {
        _ = NativeStationPolicy.Decode<System.Text.Json.JsonElement>(bytes); // Reject duplicate keys before host-policy comparisons.
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(bytes);
        var root = document.RootElement;
        if (root.EnumerateObject().Count() != 1 || !root.TryGetProperty("runtimeOptions", out var options) ||
            !options.TryGetProperty("framework", out var framework) || framework.EnumerateObject().Count() != 2 || framework.GetProperty("name").GetString() != "Microsoft.NETCore.App" ||
            framework.GetProperty("version").GetString() != version || options.GetProperty("rollForward").GetString() != "Disable" ||
            options.EnumerateObject().Any(property => property.Name is not ("tfm" or "framework" or "rollForward" or "configProperties")))
            throw new SecurityException("Unapproved framework configuration/probing.");
        if (options.TryGetProperty("configProperties", out var config) && config.EnumerateObject().Any(property => property.Value.ValueKind != System.Text.Json.JsonValueKind.False ||
            property.Name is not ("System.Reflection.Metadata.MetadataUpdater.IsSupported" or "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization")))
            throw new SecurityException("Unapproved runtime setting.");
    }
    internal byte[] ReadProtected(string path, int maximum)
    {
        ProtectAncestors(path);
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read); leases.Add(stream);
        if (stream.Length is < 1 || stream.Length > maximum) throw new SecurityException("Protected record exceeds budget.");
        byte[] bytes = new byte[checked((int)stream.Length)]; stream.ReadExactly(bytes); return bytes;
    }
    internal static void ProtectAncestors(string path)
    {
        string? current = Path.GetFullPath(path);
        bool first = true;
        while (current is not null)
        {
            FileAttributes attributes = File.GetAttributes(current);
            if ((attributes & FileAttributes.ReparsePoint) != 0) throw new SecurityException("Reparse code/store ancestor refused.");
            bool directory = (attributes & FileAttributes.Directory) != 0;
            FileSystemSecurity security = directory ? new DirectoryInfo(current).GetAccessControl(AccessControlSections.Owner | AccessControlSections.Access)
                : new FileInfo(current).GetAccessControl(AccessControlSections.Owner | AccessControlSections.Access);
            VerifyPathDescriptor(new RawSecurityDescriptor(security.GetSecurityDescriptorBinaryForm(), 0),
                current.Equals(NativeStationPolicy.CodeRoot, StringComparison.OrdinalIgnoreCase) || current.Equals(NativeStationPolicy.StoreRoot, StringComparison.OrdinalIgnoreCase), first);
            current = Path.GetDirectoryName(current);
            first = false;
        }
    }
    internal static void VerifyPathDescriptor(RawSecurityDescriptor descriptor, bool protectedRoot, bool member)
    {
        static bool Trusted(string? sid) => sid is "S-1-5-18" or "S-1-5-32-544" or "S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464";
        if (!Trusted(descriptor.Owner?.Value) || descriptor.DiscretionaryAcl is null ||
            (protectedRoot && !descriptor.ControlFlags.HasFlag(ControlFlags.DiscretionaryAclProtected)))
            throw new SecurityException("Unprotected or untrusted code/store ownership.");
        foreach (GenericAce ace in descriptor.DiscretionaryAcl)
        {
            if (ace is not CommonAce common || common.IsCallback ||
                common.AceQualifier is not (AceQualifier.AccessAllowed or AceQualifier.AccessDenied) ||
                (common.AceFlags & (AceFlags.SuccessfulAccess | AceFlags.FailedAccess)) != 0)
                throw new SecurityException("Unsupported code/store ACL.");
            // An inheritance template grants nothing on this object. Descendants
            // are inspected independently, where inherited applicable grants still refuse.
            if ((common.AceFlags & AceFlags.InheritOnly) != 0) continue;
            // Denies cannot grant mutation authority. Do not cancel an applicable
            // allow with a deny using guessed membership or ordering; refuse conservatively.
            if (common.AceQualifier == AceQualifier.AccessAllowed && !Trusted(common.SecurityIdentifier.Value) &&
                (common.AccessMask & (member ? 0x500d0156 : 0x500d0040)) != 0)
                throw new SecurityException("Untrusted code/store mutation authority.");
        }
    }
    internal static StationPeer Peer(int pid, SafeProcessHandle handle)
    {
        string[] facts = StationBootstrapInterop.Facts(handle);
        if (StationBootstrapInterop.Exited(handle)) throw new SecurityException("Station peer exited during admission.");
        return new(pid, long.Parse(facts[0], CultureInfo.InvariantCulture), facts[1], facts[2], facts[3], facts[4],
            int.Parse(facts[5], CultureInfo.InvariantCulture), int.Parse(facts[6], CultureInfo.InvariantCulture),
            bool.Parse(facts[7]), bool.Parse(facts[8]), bool.Parse(facts[9]), bool.Parse(facts[10]), bool.Parse(facts[11]));
    }
    internal void VerifyImage(SafeProcessHandle process, bool helper)
    {
        string expected = helper ? Path.Combine(NativeStationPolicy.RuntimeRoot, "dotnet.exe") : Path.Combine(NativeStationPolicy.CodeRoot, "controller", "PiratePDF.Fuzz.Runner.exe");
        if (!StationBootstrapInterop.Image(process).Equals(expected, StringComparison.OrdinalIgnoreCase)) throw new SecurityException("Unapproved station peer image.");
        // The helper's payload is bound by the independent parent's actual child record;
        // a generic dotnet host path alone is never sufficient authorization.
        if (!Manifest.Files.Any(file => Resolve(Manifest, file).Equals(expected, StringComparison.OrdinalIgnoreCase))) throw new SecurityException("Peer image absent from admitted closure.");
    }
    public void Dispose() { for (int i = leases.Count - 1; i >= 0; i--) leases[i].Dispose(); leases.Clear(); }
}
