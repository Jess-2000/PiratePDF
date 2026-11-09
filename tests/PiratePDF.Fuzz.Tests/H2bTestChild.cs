#nullable enable annotations
using System.Reflection;
using System.Runtime.ExceptionServices;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

// Reflection preserves internal tooling contracts. This checkpoint launches no controlled or production child.
internal static class H2bTestChild
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
    internal static Type Type(string name) => typeof(ChildProcess).Assembly.GetType("PiratePDF.Fuzz.Runner." + name, true)!;
    internal static object? Call(string type, string method, params object[] arguments)
    {
        try { return Type(type).GetMethod(method, Flags)!.Invoke(null, arguments); }
        catch (TargetInvocationException failure) when (failure.InnerException is not null)
        { ExceptionDispatchInfo.Capture(failure.InnerException).Throw(); throw; }
    }

    internal static object New(string type, params object[] arguments)
    {
        try { return Activator.CreateInstance(Type(type), Flags, null, arguments, null)!; }
        catch (TargetInvocationException failure) when (failure.InnerException is not null)
        { ExceptionDispatchInfo.Capture(failure.InnerException).Throw(); throw; }
    }

    internal static object? Value(object value, string property) => value.GetType().GetProperty(property, Flags)!.GetValue(value);
    internal static object? CallInstance(object value, string method) => value.GetType().GetMethod(method, Flags)!.Invoke(value, null);
}
