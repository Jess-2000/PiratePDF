#nullable enable

using System.Linq;
using System.Reflection;
using BlackFlag.Engine.SDK.Plugins;

namespace BlackFlag.Engine.SDK.Tests.Plugins;

public sealed class PluginSdkIsolationTests
{
    [Fact]
    public void SdkAssemblyDoesNotReferenceOtherBlackFlagAssemblies()
    {
        Assembly sdk = typeof(PluginManifest).Assembly;

        string[] blackFlagReferences = sdk.GetReferencedAssemblies()
            .Select(static reference => reference.Name ?? string.Empty)
            .Where(static name => name.StartsWith("BlackFlag.Engine.", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(blackFlagReferences);
    }

    [Fact]
    public void PublicSdkSurfaceDoesNotExposeOtherBlackFlagAssemblies()
    {
        Assembly sdk = typeof(PluginManifest).Assembly;
        var leaked = new SortedSet<string>(StringComparer.Ordinal);

        foreach (Type type in sdk.GetExportedTypes())
        {
            Inspect(type, type.FullName ?? type.Name, sdk, leaked);
            foreach (ConstructorInfo constructor in type.GetConstructors())
                foreach (ParameterInfo parameter in constructor.GetParameters())
                    Inspect(parameter.ParameterType, $"{type.FullName} constructor", sdk, leaked);
            foreach (PropertyInfo property in type.GetProperties())
                Inspect(property.PropertyType, $"{type.FullName}.{property.Name}", sdk, leaked);
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                Inspect(method.ReturnType, $"{type.FullName}.{method.Name} return", sdk, leaked);
                foreach (ParameterInfo parameter in method.GetParameters())
                    Inspect(parameter.ParameterType, $"{type.FullName}.{method.Name} parameter", sdk, leaked);
            }
        }

        Assert.Empty(leaked);
    }

    private static void Inspect(Type type, string location, Assembly sdk, ISet<string> leaked)
    {
        if (type.IsByRef || type.IsPointer || type.IsArray)
        {
            Type? element = type.GetElementType();
            if (element is not null) Inspect(element, location, sdk, leaked);
            return;
        }
        if (type.IsGenericType)
            foreach (Type argument in type.GetGenericArguments()) Inspect(argument, location, sdk, leaked);
        if (type.IsGenericParameter)
        {
            foreach (Type constraint in type.GetGenericParameterConstraints()) Inspect(constraint, location, sdk, leaked);
            return;
        }
        Assembly assembly = type.Assembly;
        string? assemblyName = assembly.GetName().Name;
        if (assembly != sdk && assemblyName is not null && assemblyName.StartsWith("BlackFlag.Engine.", StringComparison.Ordinal))
            leaked.Add($"{location} exposes {type.FullName} from {assemblyName}");
    }
}
