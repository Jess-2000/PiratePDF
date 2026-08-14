using BlackFlag.Engine.SDK.Plugins;

namespace BlackFlag.Engine.Plugins.Discovery;

/// <summary>Identifies a validated plugin package without loading executable code.</summary>
public sealed record PluginDescriptor(PluginManifest Manifest, string PackagePath, string ManifestPath);
