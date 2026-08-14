namespace BlackFlag.Engine.Plugins.Runtime;

public enum PluginRuntimeIssueKind
{
    DependencyUnavailable,
    RequiredCapabilityUnavailable,
    SecurityPolicyDenied,
    EntryPointInvalid,
    AssemblyUnavailable,
    AssemblyLoadFailed,
    EntryPointCreationFailed,
    InitializationFailed,
    ExtensionRegistrationFailed,
    DeactivationFailed,
    DisposalFailed
}
