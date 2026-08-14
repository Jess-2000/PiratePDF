namespace BlackFlag.Engine.Plugins.Activation;

public enum PluginActivationIssueKind
{
    SdkIncompatible,
    MissingDependency,
    DependencyVersionIncompatible,
    DependencyCycle,
    DependencyBlocked,
    DependencyDepthExceeded,
    PluginLimitExceeded
}
