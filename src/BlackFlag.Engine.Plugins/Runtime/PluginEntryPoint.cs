using System.Reflection;
namespace BlackFlag.Engine.Plugins.Runtime;

internal readonly record struct PluginEntryPoint(string TypeName, string AssemblyName)
{
    internal static bool TryParse(string value, out PluginEntryPoint entryPoint)
    {
        entryPoint = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        int comma = value.IndexOf(',');
        if (comma <= 0) return false;
        string typeName = value[..comma].Trim();
        string assemblyText = value[(comma + 1)..].Trim();
        if (typeName.Length == 0 || assemblyText.Length == 0) return false;
        try
        {
            var name = new AssemblyName(assemblyText);
            if (string.IsNullOrWhiteSpace(name.Name) || name.Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
            entryPoint = new(typeName, name.Name);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or FileLoadException) { return false; }
    }
}
