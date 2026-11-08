using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.Loader;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BlackFlag.Engine.Plugins.TestPlugin;

// Invoked through an assembly loaded by the real plugin context, so P/Invoke
// exercises both its resolver and the runtime's unresolved-name fallback.
public static class NativeResolutionProbe
{
    public static int Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Length != 3) return 1;
        if (args[2] == "SkiaBitmap") return ProbeSkia(args[0]);
        if (args[2] == "ManagedCollection") return ProbeManagedCollection(args[0], args[1]);
        Assembly host = Assembly.LoadFrom(args[0]);
        Type contextType = host.GetType("BlackFlag.Engine.Plugins.Runtime.PluginLoadContext", throwOnError: true)!;
        var context = (AssemblyLoadContext)Activator.CreateInstance(contextType,
            BindingFlags.Instance | BindingFlags.NonPublic, null, [args[1]], null)!;
        try
        {
            if (args[2] == "NativeHandleLifetime")
            {
                nint handle = (nint)contextType.GetMethod("LoadUnmanagedDll",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(context, ["b11_fixture"])!;
                var version = Marshal.GetDelegateForFunctionPointer<VersionFunction>(
                    NativeLibrary.GetExport(handle, "sk_version_get_milestone"));
                Console.WriteLine($"RESULT={version()}");
                bool unloading = false;
                context.Unloading += _ => unloading = true;
                context.Unload();
                Console.WriteLine($"UNLOADING={unloading}");
                GC.KeepAlive(version);
                return 0;
            }
            Assembly entry = (Assembly)contextType.GetMethod("LoadEntryAssembly",
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(context, [args[1]])!;
            object result = entry.GetType(typeof(NativeResolutionProbe).FullName!, throwOnError: true)!
                .GetMethod(args[2])!.Invoke(null, null)!;
            Console.WriteLine($"RESULT={result}");
            if (OperatingSystem.IsWindows())
            {
                using Process process = Process.GetCurrentProcess();
                foreach (ProcessModule module in process.Modules)
                    if (module.ModuleName == "b11_fixture.dll") Console.WriteLine($"NATIVE={module.FileName}");
            }
            return 0;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is DllNotFoundException)
        {
            Console.WriteLine("MISSING=DllNotFoundException");
            return 2;
        }
        finally { context.Unload(); }
    }

    private static int ProbeManagedCollection(string hostPath, string entryPath)
    {
        WeakReference context = LoadAndRequestUnload(hostPath, entryPath);
        for (int attempt = 0; attempt < 10; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        Console.WriteLine($"COLLECTED={!context.IsAlive}");
        return context.IsAlive ? 3 : 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference LoadAndRequestUnload(string hostPath, string entryPath)
    {
        Type type = Assembly.LoadFrom(hostPath).GetType(
            "BlackFlag.Engine.Plugins.Runtime.PluginLoadContext", throwOnError: true)!;
        var context = (AssemblyLoadContext)Activator.CreateInstance(type,
            BindingFlags.Instance | BindingFlags.NonPublic, null, [entryPath], null)!;
        Assembly entry = (Assembly)type.GetMethod("LoadEntryAssembly",
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(context, [entryPath])!;
        object result = entry.GetType(typeof(NativeResolutionProbe).FullName!, throwOnError: true)!
            .GetMethod(nameof(PackagedVersion))!.Invoke(null, null)!;
        Console.WriteLine($"RESULT={result}");
        var weak = new WeakReference(context);
        context.Unload();
        return weak;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int VersionFunction();

    private static int ProbeSkia(string assemblyPath)
    {
        try
        {
            Type bitmapType = Assembly.LoadFrom(assemblyPath).GetType("SkiaSharp.SKBitmap", throwOnError: true)!;
            using var bitmap = (IDisposable)Activator.CreateInstance(bitmapType, [1, 1, false])!;
            Console.WriteLine($"RESULT={bitmapType.GetProperty("Width")!.GetValue(bitmap)}");
            if (OperatingSystem.IsWindows())
            {
                using Process process = Process.GetCurrentProcess();
                foreach (ProcessModule module in process.Modules)
                    if (string.Equals(module.ModuleName, "libSkiaSharp.dll", StringComparison.OrdinalIgnoreCase))
                        Console.WriteLine($"NATIVE={module.FileName}");
            }
            return 0;
        }
        catch (Exception exception) when (IsMissingNativeDependency(exception))
        {
            // Native loading may be wrapped in TypeInitializationException and/or
            // TargetInvocationException, depending on the runtime and SkiaSharp version.
            Console.WriteLine("MISSING=DllNotFoundException");
            return 2;
        }
    }

    private static bool IsMissingNativeDependency(Exception exception)
    {
        for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
            if (cause is DllNotFoundException) return true;
        return false;
    }

    public static int PackagedVersion() => SkiaVersion();

    public static int ProcessId() => OperatingSystem.IsWindows()
        ? unchecked((int)WindowsProcessId())
        : OperatingSystem.IsMacOS() ? MacProcessId() : UnixProcessId();

    [DllImport("b11_fixture", EntryPoint = "sk_version_get_milestone")]
    private static extern int SkiaVersion();

    [DllImport("kernel32.dll", EntryPoint = "GetCurrentProcessId")]
    private static extern uint WindowsProcessId();

    [DllImport("libc", EntryPoint = "getpid")]
    private static extern int UnixProcessId();

    [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "getpid")]
    private static extern int MacProcessId();
}
