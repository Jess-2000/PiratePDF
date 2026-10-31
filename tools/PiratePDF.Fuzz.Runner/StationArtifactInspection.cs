using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security;
using System.Security.Cryptography;
using System.Text.Json;

namespace PiratePDF.Fuzz.Runner;

// Reads metadata without loading or executing the artifact. Not compiled into it.
internal static class StationArtifactInspection
{
    internal static string Inspect(string directory, string runtimeVersion)
    {
        string assemblyPath = Path.Combine(directory, "PiratePDF.NativeProvisioning.Station.dll");
        using var file = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        string digest = Convert.ToHexStringLower(SHA256.HashData(file)); file.Position = 0;
        using var pe = new PEReader(file); MetadataReader metadata = pe.GetMetadataReader();
        string[] types = metadata.TypeDefinitions.Select(handle => metadata.GetString(metadata.GetTypeDefinition(handle).Name)).ToArray();
        string[] definitions = metadata.MethodDefinitions.Select(handle => metadata.GetString(metadata.GetMethodDefinition(handle).Name)).ToArray();
        string[] references = metadata.MemberReferences.Select(handle => metadata.GetString(metadata.GetMemberReference(handle).Name)).ToArray();
        ValidateMetadata(types, definitions, references);
        byte[] config = ReadBounded(Path.Combine(directory, "PiratePDF.NativeProvisioning.Station.runtimeconfig.json"));
        ValidateConfig(config, runtimeVersion);
        ValidateDependencies(ReadBounded(Path.Combine(directory, "PiratePDF.NativeProvisioning.Station.deps.json")));
        using var bootstrap = new FileStream(Path.Combine(directory, "PiratePDF.NativeProvisioning.Bootstrap.dll"), FileMode.Open, FileAccess.Read, FileShare.Read);
        using var bootstrapPe = new PEReader(bootstrap);
        MetadataReader bootstrapMetadata = bootstrapPe.GetMetadataReader();
        if (bootstrapPe.PEHeaders.CorHeader?.EntryPointTokenOrRelativeVirtualAddress != 0 ||
            bootstrapMetadata.AssemblyReferences.Any(handle => bootstrapMetadata.GetString(bootstrapMetadata.GetAssemblyReference(handle).Name) is not ("mscorlib" or "System" or "System.Core")) ||
            !bootstrapMetadata.TypeDefinitions.Any(handle => bootstrapMetadata.GetString(bootstrapMetadata.GetTypeDefinition(handle).Name) == "StationBootstrapInterop"))
            throw new SecurityException("Unexpected precompiled bootstrap executable/dependency surface.");
        ValidateBootstrapMethods(bootstrapMetadata.MethodDefinitions.Select(handle => bootstrapMetadata.GetString(bootstrapMetadata.GetMethodDefinition(handle).Name)).ToArray());
        string[] files = Directory.GetFiles(directory).Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal).ToArray();
        if (files.Length != 6 || files.Any(name => name is not ("PiratePDF.NativeProvisioning.Station.dll" or "PiratePDF.NativeProvisioning.Station.pdb" or
            "PiratePDF.NativeProvisioning.Station.deps.json" or "PiratePDF.NativeProvisioning.Station.runtimeconfig.json" or "Provision-NativeExecution.ps1" or "PiratePDF.NativeProvisioning.Bootstrap.dll")) || Directory.GetDirectories(directory).Length != 0)
            throw new SecurityException("Unexpected privileged publishing output.");
        return JsonSerializer.Serialize(new { Operation = NativeStationPolicy.Operation, Artifact = assemblyPath, Sha256 = digest,
            GenericDispatcherAbsent = true, ArbitraryProcessLaunchAbsent = true, CredentialReadersAbsent = true,
            Runtime = runtimeVersion, RollForward = "Disable", Dependencies = "Only own project plus separately admitted shared framework",
            Files = files, TypeCount = types.Length, MethodCount = definitions.Length, PrecompiledBootstrap = "Fixed DLL; OS Framework references only; no launch-time compiler",
            Executed = false, ProductionNativeExecution = "Disabled" });
    }
    private static byte[] ReadBounded(string path)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length is < 2 or > 4096) throw new SecurityException("Publishing metadata exceeds its budget.");
        byte[] bytes = new byte[checked((int)file.Length)]; file.ReadExactly(bytes); return bytes;
    }
    internal static void ValidateMetadata(string[] types, string[] definitions, string[] references)
    {
        foreach (string type in types)
            if (type.Contains("Lpac", StringComparison.OrdinalIgnoreCase) || type is "H2bProbe" or "SecureString" or "AssemblyLoadContext" or "NativeProvisioningOperations")
                throw new SecurityException("Unrelated privileged entry surface.");
        foreach (string name in definitions.Concat(references))
            if (name is "LoadFromAssemblyPath" or "LoadFrom" or "LoadFile" or "Load" or "ReadKey" or "LogonUserW" or "CreateProcessW" or "CreateProcessAsUserW" or "LaunchStationHelper" or "RunReceiverAsync" or "CreateJobObjectW" or "TerminateJobObject" or "TerminateProcess" or "CompleteOwnedHelper" or "ObserveStationLifetime")
                throw new SecurityException("Generic loading, credentials or execution present in station artifact.");
        if (!types.Contains("NativeStationTransaction", StringComparer.Ordinal) || !definitions.Contains("RunHelperAsync", StringComparer.Ordinal))
            throw new SecurityException("Missing fixed station entry.");
    }
    internal static void ValidateConfig(byte[] bytes, string version)
    {
        _ = NativeStationPolicy.Decode<JsonElement>(bytes);
        if (!Version.TryParse(version, out Version? parsed) || parsed.Major != 10 || parsed.Build < 0) throw new SecurityException("Exact runtime patch required.");
        using JsonDocument document = JsonDocument.Parse(bytes);
        JsonElement root = document.RootElement;
        if (root.EnumerateObject().Count() != 1 || !root.TryGetProperty("runtimeOptions", out JsonElement options) ||
            !options.TryGetProperty("framework", out JsonElement framework) || framework.EnumerateObject().Count() != 2 ||
            framework.GetProperty("name").GetString() != "Microsoft.NETCore.App" || framework.GetProperty("version").GetString() != version ||
            options.GetProperty("rollForward").GetString() != "Disable" ||
            options.EnumerateObject().Any(property => property.Name is not ("tfm" or "framework" or "rollForward" or "configProperties")))
            throw new SecurityException("Unapproved framework configuration/probing.");
        if (options.TryGetProperty("configProperties", out JsonElement config))
            foreach (JsonProperty property in config.EnumerateObject())
                if (property.Value.ValueKind != JsonValueKind.False || property.Name is not ("System.Reflection.Metadata.MetadataUpdater.IsSupported" or "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization"))
                    throw new SecurityException("Unapproved runtime setting.");
    }
    internal static void ValidateDependencies(byte[] bytes)
    {
        _ = NativeStationPolicy.Decode<JsonElement>(bytes);
        using JsonDocument document = JsonDocument.Parse(bytes);
        JsonElement root = document.RootElement;
        const string library = "PiratePDF.NativeProvisioning.Station/1.0.0";
        if (root.GetProperty("runtimeTarget").GetProperty("name").GetString() != ".NETCoreApp,Version=v10.0" ||
            root.GetProperty("libraries").EnumerateObject().Count() != 1 || !root.GetProperty("libraries").TryGetProperty(library, out _) ||
            root.GetProperty("targets").EnumerateObject().Count() != 1 || !root.GetProperty("targets").TryGetProperty(".NETCoreApp,Version=v10.0", out JsonElement target) ||
            target.EnumerateObject().Count() != 1 || !target.TryGetProperty(library, out JsonElement own) || own.EnumerateObject().Count() != 1 ||
            !own.TryGetProperty("runtime", out JsonElement runtime) || runtime.EnumerateObject().Count() != 1 ||
            !runtime.TryGetProperty("PiratePDF.NativeProvisioning.Station.dll", out JsonElement assembly) || assembly.EnumerateObject().Any())
            throw new SecurityException("Unexpected privileged runtime dependency/probing target.");
    }
    private static void ValidateBootstrapMethods(string[] methods)
    {
        string[] properties = ["OriginalFailure", "HelperCreated", "OwnershipEstablished", "InitialWait", "InitialWaitError", "TerminationAttempted", "TerminationError",
            "FinalWait", "FinalWaitError", "ExitVerified", "ExitCode", "ExitError", "CodeLeasesReleased", "OwnershipRetained", "LeaseCloseFailures",
            "StationName", "State", "ControllerWait", "ControllerWaitError", "ControllerExit", "ControllerExitError", "KnownTransactionOwnersClosed",
            "ProbeAttempts", "OpenError", "ProbeClosed", "ProbeCloseError", "ObserverPid", "ObserverSession", "EvidenceScope", "DestructionProved", "FinalObjectLifetime"];
        foreach (string method in methods)
        {
            if ((method.StartsWith("get_", StringComparison.Ordinal) || method.StartsWith("set_", StringComparison.Ordinal)) &&
                properties.Contains(method[4..], StringComparer.Ordinal)) continue;
            if (method is not ("OpenPeer" or "FileId" or "Image" or "Exited" or "ExitCode" or "FailureExit" or "DecodeFailure" or "LaunchStationHelper" or
                "NoImpersonation" or "Facts" or "Boolean" or "Dword" or "Query" or "OpenProcess" or "GetProcessTimes" or "GetFileInformationByHandleEx" or
                "QueryFullProcessImageNameW" or "WaitForSingleObject" or "GetExitCodeProcess" or "OpenProcessToken" or "OpenThreadToken" or "GetTokenInformation" or
                "CreateProcessW" or "ComputeStringHash" or ".ctor" or "CompleteOwnedHelper" or "WaitOwned" or "TerminateOwned" or "ExitOwned" or "TerminateProcess" or
                "Complete" or "NoLaunch" or "ValidateResidentArguments" or "ObserveStationLifetime" or "Observe" or "WaitController" or "ExitController" or
                "OpenStationProbe" or "CloseStationProbe" or "OpenWindowStationW" or "CloseWindowStation")) throw new SecurityException("Unexpected bootstrap method surface.");
        }
        if (!methods.Contains("LaunchStationHelper", StringComparer.Ordinal) || !methods.Contains("CreateProcessW", StringComparer.Ordinal))
            throw new SecurityException("Missing fixed noninheriting bootstrap launch.");
    }
}
