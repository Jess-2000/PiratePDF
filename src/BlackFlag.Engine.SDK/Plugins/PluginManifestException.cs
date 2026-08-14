namespace BlackFlag.Engine.SDK.Plugins;

/// <summary>Reports a deterministic plugin-manifest validation failure.</summary>
public sealed class PluginManifestException : Exception
{
    public PluginManifestException(string message) : base(message) { }
    public PluginManifestException(string message, Exception innerException) : base(message, innerException) { }
}
