using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PiratePDF.Fuzz.Runner;

// This is a single station-only protocol, not native-worker admission.
internal static class NativeStationPolicy
{
    internal const string Operation = "StationLeaseOnlyV1";
    internal const uint StationCreator = 0x2000a, DesktopCreator = 0x20083;
    internal const uint StationLease = 0x20002, DesktopLease = 0x20081;
    internal const int FrameLimit = 4096, TransactionLimit = 16384;
    internal const string Revision = "station-lease-v1";
    internal static readonly JsonSerializerOptions Json = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 12
    };

    internal static string CodeRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), "PiratePDF", "NativeProvisioning", "StationV1");
    internal static string StoreRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PiratePDF", "NativeProvisioning", "StationV1");
    internal static string RuntimeRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet");
    internal static string AuthorizationPath(string id)
    {
        RequireHex(id, 32);
        return Path.Combine(StoreRoot, id + ".authorization.json");
    }
    internal static string Endpoint(string id)
    {
        RequireHex(id, 32);
        return "pph2b-station-v1-" + id;
    }
    internal static void RequireHex(string value, int length)
    {
        if (value is null || value.Length != length || value.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new SecurityException("Invalid fixed station identity.");
    }
    internal static string Descriptor(bool desktop) => "O:BAG:BAD:P(A;;RC;;;OW)(A;;" +
        (desktop ? "0x20083" : "0x2000a") + ";;;BA)S:(ML;;NW;;;ME)";

    internal static T Decode<T>(byte[] bytes)
    {
        if (bytes.Length is < 2 or > 131072) throw new SecurityException("Station document exceeds its budget.");
        using JsonDocument document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 12 });
        Unique(document.RootElement);
        return JsonSerializer.Deserialize<T>(bytes, Json) ?? throw new SecurityException("Missing station document.");
    }
    private static void Unique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new SecurityException("Duplicate station property.");
                Unique(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (JsonElement item in value.EnumerateArray()) Unique(item);
    }
    internal static void ValidateAuthorization(StationAuthorization value, string id, DateTimeOffset now)
    {
        RequireHex(id, 32); RequireHex(value.RunId, 8); RequireHex(value.CodeDigest, 64);
        if (value.Version != 1 || value.Operation != Operation || value.Id != id || value.Policy != Revision ||
            value.StationRights != StationLease || value.DesktopRights != DesktopLease ||
            value.NotBefore > now || value.Expires <= now || value.Expires - value.NotBefore > TimeSpan.FromSeconds(30) ||
            value.Expires <= value.NotBefore || value.Controller is null || value.Helper is null)
            throw new SecurityException("Station authorization is not valid for this fixed transaction.");
        ValidatePeer(value.Controller, false); ValidatePeer(value.Helper, true);
        if (value.Controller.Pid == value.Helper.Pid || value.Controller.User != value.Helper.User ||
            value.Controller.Session != value.Helper.Session)
            throw new SecurityException("Station role/session mismatch.");
    }
    internal static void ValidatePeer(StationPeer peer, bool helper)
    {
        if (peer.Pid <= 0 || peer.CreationTime <= 0 || peer.Session <= 0 || string.IsNullOrEmpty(peer.User) ||
            !peer.User.StartsWith("S-1-5-21-", StringComparison.Ordinal) || string.IsNullOrEmpty(peer.Logon) ||
            !peer.Logon.StartsWith("S-1-5-5-", StringComparison.Ordinal) || string.IsNullOrEmpty(peer.Authentication) ||
            string.IsNullOrEmpty(peer.TokenId) || peer.Integrity != (helper ? 0x3000 : 0x2000) || peer.Elevated != helper ||
            peer.Administrator != helper || peer.AppContainer || peer.UIAccess || !peer.Primary)
            throw new SecurityException("Unexpected station peer token role.");
    }
    internal static void MatchPeer(StationPeer expected, StationPeer actual)
    {
        if (expected != actual) throw new SecurityException("OS station peer instance does not match operator authorization.");
    }
    internal static Dictionary<string, string> EnvironmentFor(string windows)
    {
        if (!Path.IsPathFullyQualified(windows)) throw new SecurityException("Missing trusted Windows directory.");
        return new(StringComparer.OrdinalIgnoreCase)
        {
            ["SystemRoot"] = windows, ["WINDIR"] = windows, ["PATH"] = Path.Combine(windows, "System32"),
            ["DOTNET_ROOT"] = RuntimeRoot, ["DOTNET_MULTILEVEL_LOOKUP"] = "0",
            ["DOTNET_EnableDiagnostics"] = "0", ["DOTNET_EnableDiagnostics_IPC"] = "0",
            ["DOTNET_EnableDiagnostics_Profiler"] = "0", ["CORECLR_ENABLE_PROFILING"] = "0",
            ["COR_ENABLE_PROFILING"] = "0", ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
        };
    }
}

internal sealed record StationPeer(int Pid, long CreationTime, string User, string Logon, string Authentication,
    string TokenId, int Session, int Integrity, bool Elevated, bool Administrator, bool Primary, bool AppContainer, bool UIAccess);
internal sealed record StationAuthorization(int Version, string Operation, string Id, string RunId, string Policy,
    uint StationRights, uint DesktopRights, string CodeDigest, DateTimeOffset NotBefore, DateTimeOffset Expires,
    StationPeer Controller, StationPeer Helper);
internal sealed record StationFile(string Root, string Path, string Sha256, string FileId, long Length);
internal sealed record StationManifest(int Version, string RuntimeVersion, string HostFxrVersion, StationFile[] Files);
internal sealed record StationMessage(int Version, string Operation, string Id, string Kind, long Station = 0,
    long Desktop = 0, string State = "", int ErrorCode = 0);

internal sealed class StationLeaseFailure(string state, int errorCode, Exception? inner = null) : IOException(state, inner)
{
    internal string State { get; } = state;
    internal int ErrorCode { get; } = errorCode;
}

// Numeric remote handles are never closed. A lost/partial offer is uncertainty, not rollback.
internal sealed class StationTransferState
{
    internal string State { get; private set; } = "Authorized";
    internal int Transferred { get; private set; }
    internal void Duplicated()
    {
        if (State != "Authorized" || Transferred == 2) throw new SecurityException("Invalid station transfer transition.");
        Transferred++;
    }
    internal void Acknowledge()
    {
        if (State != "Authorized" || Transferred != 2) throw new SecurityException("Unverified or partial station acknowledgment.");
        State = "Acknowledged";
    }
    internal void Fail(string reason) => State = Transferred == 0 && !reason.StartsWith("UNKNOWN/BLOCKED", StringComparison.Ordinal)
        ? "Refused:" + reason : "UNKNOWN/BLOCKED:" + reason;
    internal void Closed(bool localClosed, bool peerClosed)
        => State = localClosed && peerClosed && State == "Acknowledged" ? "Closed" : "UNKNOWN/BLOCKED:Cleanup";
}
