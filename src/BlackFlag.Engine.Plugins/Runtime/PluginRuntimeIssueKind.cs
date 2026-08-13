namespace BlackFlag.Engine.Plugins.Runtime;

public enum PluginRuntimeIssueKind
{
    DependencyUnavailable,
    RequiredCapabilityUnavailable,
    EntryPointInvalid,
    AssemblyUnavailable,
    AssemblyLoadFailed,
    EntryPointCreationFailed,
    InitializationFailed,
    ExtensionRegistrationFailed,
    DeactivationFailed,
    DisposalFailed
}
