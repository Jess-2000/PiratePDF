namespace PiratePDF.Fuzz.Runner;

/// <summary>Mandatory per-account WFP requirements. A plan is never accepted as installed enforcement.</summary>
internal static class WindowsNetworkPolicy
{
    internal static string[] RequiredLayers() =>
    [
        "FWPM_LAYER_ALE_AUTH_CONNECT_V4", "FWPM_LAYER_ALE_AUTH_CONNECT_V6",
        "FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V4", "FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V6",
        "FWPM_LAYER_ALE_AUTH_LISTEN_V4", "FWPM_LAYER_ALE_AUTH_LISTEN_V6",
        "FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V4", "FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V6"
    ];

    internal const string IdentityCondition = "FWPM_CONDITION_ALE_USER_ID";
    internal const string Action = "FWP_ACTION_BLOCK";
    internal const string LocalPolicy = "Block IPv4/IPv6 including loopback; deny unapproved local IPC separately; DNS proxy behavior requires real enforcement tests.";
}
