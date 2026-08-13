namespace BlackFlag.Engine.Plugins.Runtime;

public enum PluginRuntimeIssueKind
{
    DependencyUnavailable,
    EntryPointInvalid,
    AssemblyUnavailable,
    AssemblyLoadFailed,
    EntryPointCreationFailed,
    InitializationFailed,
    DeactivationFailed,
    DisposalFailed
}
