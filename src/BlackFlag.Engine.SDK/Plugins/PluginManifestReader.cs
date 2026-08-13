using System.Text.Json;

namespace BlackFlag.Engine.SDK.Plugins;

/// <summary>Reads and validates bounded version-1 plugin manifests without executing plugin code.</summary>
public static class PluginManifestReader
{
    public const int CurrentManifestVersion = 1;

    public static PluginManifest Read(ReadOnlySpan<byte> utf8Json, PluginManifestLimits? limits = null)
    {
        limits ??= new PluginManifestLimits();
        limits.Validate();
        if (utf8Json.Length == 0) throw new PluginManifestException("Plugin manifest is empty.");
        if (utf8Json.Length > limits.MaximumBytes) throw new PluginManifestException("Plugin manifest exceeds the configured size limit.");

        try
        {
            using JsonDocument document = JsonDocument.Parse(utf8Json.ToArray(), new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 16
            });
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw new PluginManifestException("Plugin manifest root must be an object.");
            int manifestVersion = RequiredInt32(root, "manifestVersion");
            if (manifestVersion != CurrentManifestVersion) throw new PluginManifestException($"Unsupported plugin manifest version {manifestVersion}.");

            PluginId id = new(RequiredString(root, "id", PluginId.MaximumLength));
            string name = RequiredString(root, "name", 256);
            Version version = RequiredVersion(root, "version");
            Version minimumSdkVersion = RequiredVersion(root, "minimumSdkVersion");
            string entryPoint = RequiredString(root, "entryPoint", 512);
            PluginCapability[] required = ReadCapabilities(root, "requiredCapabilities", limits.MaximumCapabilities);
            PluginCapability[] optional = ReadCapabilities(root, "optionalCapabilities", limits.MaximumCapabilities);
            if (required.Length + optional.Length > limits.MaximumCapabilities)
                throw new PluginManifestException("Plugin manifest declares too many capabilities.");
            if (required.Intersect(optional).Any())
                throw new PluginManifestException("A capability cannot be both required and optional.");

            return new PluginManifest(id, name, version, minimumSdkVersion, entryPoint, required, optional);
        }
        catch (PluginManifestException) { throw; }
        catch (JsonException ex) { throw new PluginManifestException("Plugin manifest contains invalid JSON.", ex); }
        catch (ArgumentException ex) { throw new PluginManifestException("Plugin manifest contains an invalid value.", ex); }
    }

    private static string RequiredString(JsonElement root, string name, int maximumLength)
    {
        if (!root.TryGetProperty(name, out JsonElement element) || element.ValueKind != JsonValueKind.String)
            throw new PluginManifestException($"Plugin manifest property '{name}' is required and must be a string.");
        string? value = element.GetString();
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength)
            throw new PluginManifestException($"Plugin manifest property '{name}' is empty or exceeds its maximum length.");
        return value;
    }

    private static int RequiredInt32(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out JsonElement element) || !element.TryGetInt32(out int value))
            throw new PluginManifestException($"Plugin manifest property '{name}' is required and must be an integer.");
        return value;
    }

    private static Version RequiredVersion(JsonElement root, string name)
    {
        string value = RequiredString(root, name, 64);
        return Version.TryParse(value, out Version? version) && version.Major >= 0
            ? version
            : throw new PluginManifestException($"Plugin manifest property '{name}' is not a valid version.");
    }

    private static PluginCapability[] ReadCapabilities(JsonElement root, string name, int maximumCount)
    {
        if (!root.TryGetProperty(name, out JsonElement element)) return [];
        if (element.ValueKind != JsonValueKind.Array) throw new PluginManifestException($"Plugin manifest property '{name}' must be an array.");
        var capabilities = new List<PluginCapability>();
        foreach (JsonElement item in element.EnumerateArray())
        {
            if (capabilities.Count >= maximumCount) throw new PluginManifestException("Plugin manifest declares too many capabilities.");
            if (item.ValueKind != JsonValueKind.String) throw new PluginManifestException($"Plugin manifest property '{name}' must contain only strings.");
            string? value = item.GetString();
            if (string.IsNullOrWhiteSpace(value)) throw new PluginManifestException($"Plugin manifest property '{name}' contains an empty capability.");
            capabilities.Add(new PluginCapability(value));
        }
        if (capabilities.Count != capabilities.Distinct().Count()) throw new PluginManifestException($"Plugin manifest property '{name}' contains duplicate capabilities.");
        return capabilities.ToArray();
    }
}
