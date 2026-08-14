using BlackFlag.Engine.SDK.Plugins;
namespace BlackFlag.Engine.Plugins.Runtime;

public sealed record PluginRuntimeIssue(PluginRuntimeIssueKind Kind, PluginId PluginId, string Message);
