using BlackFlag.Engine.SDK.Plugins;
namespace BlackFlag.Engine.Plugins.Activation;
public sealed record PluginActivationIssue(PluginActivationIssueKind Kind, PluginId PluginId, PluginId? DependencyId, string Message);
