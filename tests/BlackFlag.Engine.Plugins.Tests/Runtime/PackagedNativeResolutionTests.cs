#nullable enable
using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using BlackFlag.Engine.Plugins.Runtime;

namespace BlackFlag.Engine.Plugins.Tests.Runtime;

[CollectionDefinition("Native resolution working directory", DisableParallelization = true)]
public sealed class NativeResolutionCollection;

[Collection("Native resolution working directory")]
public sealed class PackagedNativeResolutionTests
{
    [Fact]
    public void RuntimeManagedSkiaLoadsPackagedAssetFromUnrelatedWorkingDirectory()
    {
        using var package = new NativePackage();
        string working = Path.Combine(package.Root, "working");
        Directory.CreateDirectory(working);
        File.WriteAllText(Path.Combine(working, NativePackage.SkiaLibraryName), "unrelated invalid native image");
        (int exitCode, string output) = package.InvokeChild("SkiaBitmap", working,
            Path.Combine(AppContext.BaseDirectory, "SkiaSharp.dll"));
        Assert.Equal(0, exitCode);
        Assert.Contains("RESULT=1", output, StringComparison.Ordinal);
        if (OperatingSystem.IsWindows())
            Assert.Contains("NATIVE=" + NativePackage.SkiaAssetPath, output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RuntimeManagedSkiaWithoutNativeAssetFailsInFreshProcess()
    {
        using var package = new NativePackage();
        string skia = Path.Combine(package.Package, "SkiaSharp.dll");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "SkiaSharp.dll"), skia);
        (int exitCode, string output) = package.InvokeChild("SkiaBitmap", package.Root, skia, isolated: true);
        Assert.True(exitCode == 2, $"Expected missing native dependency (exit 2), got {exitCode}. Child output: {output}");
        Assert.Contains("MISSING=DllNotFoundException", output, StringComparison.Ordinal);
    }

    [Fact]
    public void PackagedNativeWinsOverInvalidSameNameInWritableWorkingDirectory()
    {
        using var package = new NativePackage();
        string working = Path.Combine(package.Root, "working");
        Directory.CreateDirectory(working);
        File.WriteAllText(Path.Combine(working, NativePackage.LibraryName), "unrelated invalid native image");
        Assert.Equal(package.NativePath, package.Resolver.ResolveUnmanagedDllToPath("b11_fixture"));
        (int exitCode, string output) = package.InvokeChild("PackagedVersion", working);
        Assert.Equal(0, exitCode);
        Assert.Contains("RESULT=150", output, StringComparison.Ordinal);
        if (OperatingSystem.IsWindows())
            Assert.Contains("NATIVE=" + package.NativePath, output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingNativeDependencyRemainsUnresolvedAndPInvokeFailsPredictably()
    {
        using var package = new NativePackage();
        File.Delete(package.NativePath);
        Assert.Null(package.Resolver.ResolveUnmanagedDllToPath("b11_fixture"));
        Assert.Equal(nint.Zero, package.ResolveNative("b11_fixture"));
        (int exitCode, string output) = package.InvokeChild("PackagedVersion", package.Root);
        Assert.Equal(2, exitCode);
        Assert.Contains("MISSING=DllNotFoundException", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RedirectedDirectoryMustStayInsidePhysicalPackage(bool inside)
    {
        using var package = new NativePackage();
        string target = Path.Combine(inside ? package.Package : package.Root, "target");
        Directory.CreateDirectory(target);
        string entryName = "BlackFlag.Engine.Plugins.TestPlugin.dll";
        File.Copy(Path.Combine(package.Package, entryName), Path.Combine(target, entryName));
        string link = Path.Combine(package.Package, "redirected");
        CreateDirectoryLink(link, target);
        try
        {
            string candidate = Path.Combine(link, entryName);
            if (inside)
                Assert.Equal(Path.Combine(target, entryName), package.Validate(candidate));
            else
                Assert.IsType<FileLoadException>(Assert.Throws<TargetInvocationException>(
                    () => package.Validate(candidate)).InnerException);
        }
        finally { Directory.Delete(link); }
    }

    private static void CreateDirectoryLink(string link, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateSymbolicLink(link, target);
            return;
        }

        // Directory junctions exercise Windows reparse handling without the
        // symbolic-link privilege. No elevated execution or policy change.
        var start = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("mklink");
        start.ArgumentList.Add("/J");
        start.ArgumentList.Add(link);
        start.ArgumentList.Add(target);
        using Process process = Process.Start(start)!;
        Assert.True(process.WaitForExit(10_000));
        Assert.Equal(0, process.ExitCode);
    }

    [Fact]
    public void UnresolvedSystemLibraryRetainsRuntimeFallback()
    {
        using var package = new NativePackage();
        string name = OperatingSystem.IsWindows() ? "kernel32.dll"
            : OperatingSystem.IsMacOS() ? "/usr/lib/libSystem.B.dylib" : "libc";
        Assert.Null(package.Resolver.ResolveUnmanagedDllToPath(name));
        Assert.Equal(nint.Zero, package.ResolveNative(name));
        Assert.Equal(Environment.ProcessId, package.Invoke("ProcessId"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RedirectedPackageReplacementIsRejectedByActualLoader(bool native)
    {
        using var package = new NativePackage();
        string moved = Path.Combine(package.Root, "outside");
        Directory.Move(package.Package, moved);
        CreateDirectoryLink(package.Package, moved);
        try
        {
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
            {
                if (native) package.ResolveNative("b11_fixture");
                else package.Invoke("ProcessId");
            });
            Assert.IsType<FileLoadException>(exception.InnerException);
        }
        finally
        {
            Directory.Delete(package.Package);
            Directory.Move(moved, package.Package);
        }
    }

    [Fact]
    public void PackageRootSelectedThroughDirectoryLinkRetainsItsPhysicalIdentity()
    {
        using var package = new NativePackage();
        string link = Path.Combine(package.Root, "selected-package");
        CreateDirectoryLink(link, package.Package);
        AssemblyLoadContext? context = null;
        try
        {
            Type type = package.Context.GetType();
            string entryPath = Path.Combine(link, "BlackFlag.Engine.Plugins.TestPlugin.dll");
            context = (AssemblyLoadContext)Activator.CreateInstance(type,
                BindingFlags.Instance | BindingFlags.NonPublic, null, [entryPath], null)!;
            string resolved = (string)type.GetMethod("ValidateResolvedPath",
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(context, [package.NativePath])!;
            Assert.Equal(package.NativePath, resolved);
        }
        finally
        {
            context?.Unload();
            Directory.Delete(link);
        }
    }

    [Fact]
    public void NativeUseDoesNotPreventManagedContextCollection()
    {
        using var package = new NativePackage();
        (int exitCode, string output) = package.InvokeChild("ManagedCollection", package.Root);
        Assert.Equal(0, exitCode);
        Assert.Contains("RESULT=150", output, StringComparison.Ordinal);
        Assert.Contains("COLLECTED=True", output, StringComparison.Ordinal);
        using FileStream stream = new(package.NativePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.True(stream.CanWrite); // Process exit releases the native mapping.
    }

    [Theory]
    [InlineData("../outside.dll", false)]
    [InlineData("native/../inside.dll", true)]
    [InlineData("../plugin-sibling/outside.dll", false)]
    public void NormalizedDependencyPathsEnforceLexicalContainment(string relative, bool allowed)
    {
        using var package = new NativePackage();
        string candidate = Path.GetFullPath(Path.Combine(package.Package, relative));
        if (allowed)
            Assert.Equal(candidate, package.Validate(candidate));
        else
            Assert.IsType<FileLoadException>(Assert.Throws<TargetInvocationException>(
                () => package.Validate(candidate)).InnerException);
    }

    [Fact]
    public void RelativeDependencyIsResolvedAgainstCurrentDirectoryThenValidated()
    {
        using var package = new NativePackage();
        string previous = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = package.Package;
            Assert.Equal(package.NativePath, package.Validate(NativePackage.LibraryName));
            Assert.IsType<FileLoadException>(Assert.Throws<TargetInvocationException>(
                () => package.Validate(Path.Combine("..", "outside.dll"))).InnerException);
        }
        finally { Environment.CurrentDirectory = previous; }
    }

    [Fact]
    public void ExcessiveRedirectionFailsWithinTheResolutionBudget()
    {
        using var package = new NativePackage();
        string target = Path.Combine(package.Package, "target");
        Directory.CreateDirectory(target);
        string[] links = new string[42];
        try
        {
            for (int index = 0; index < links.Length; index++)
            {
                links[index] = Path.Combine(package.Package, "link-" + index);
                CreateDirectoryLink(links[index], index == 0 ? target : links[index - 1]);
            }
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
                () => package.Validate(Path.Combine(links[^1], "dependency.dll")));
            Assert.IsType<FileLoadException>(exception.InnerException);
        }
        finally
        {
            for (int index = links.Length - 1; index >= 0; index--)
                if (links[index] is { } link && Directory.Exists(link)) Directory.Delete(link);
        }
    }

    [Fact]
    public void NativeHandleRemainsUsableUntilContextUnloadIsRequested()
    {
        using var package = new NativePackage();
        (int exitCode, string output) = package.InvokeChild("NativeHandleLifetime", package.Root);
        Assert.Equal(0, exitCode);
        Assert.Contains("RESULT=150", output, StringComparison.Ordinal);
        Assert.Contains("UNLOADING=True", output, StringComparison.Ordinal);
    }

    private sealed class NativePackage : IDisposable
    {
        private const string AssemblyName = "BlackFlag.Engine.Plugins.TestPlugin";
        private readonly MethodInfo loadEntry;
        private readonly MethodInfo resolveNative;
        private readonly MethodInfo validate;
        private Assembly? entry;

        internal NativePackage()
        {
            Root = Path.Combine(Path.GetTempPath(), "blackflag-native-" + Guid.NewGuid().ToString("N"));
            Package = Path.Combine(Root, "plugin");
            Directory.CreateDirectory(Path.Combine(Package, "native"));
            string assemblyPath = Path.Combine(Package, AssemblyName + ".dll");
            File.Copy(Path.Combine(AppContext.BaseDirectory, AssemblyName + ".dll"), assemblyPath);
            NativePath = Path.Combine(Package, LibraryName);
            string rid = RuntimeInformation.RuntimeIdentifier;
            File.Copy(SkiaAssetPath, NativePath);
            File.WriteAllText(Path.ChangeExtension(assemblyPath, ".deps.json"), $$"""
                {
                  "runtimeTarget": { "name": ".NETCoreApp,Version=v10.0/{{rid}}" },
                  "targets": { ".NETCoreApp,Version=v10.0/{{rid}}": {
                    "{{AssemblyName}}/1.0.0": { "native": { "{{LibraryName}}": {} } }
                  } },
                  "libraries": { "{{AssemblyName}}/1.0.0": { "type": "project", "serviceable": false, "sha512": "" } }
                }
                """);
            Resolver = new AssemblyDependencyResolver(assemblyPath);
            Type type = typeof(PluginRuntimeHost).Assembly.GetType(
                "BlackFlag.Engine.Plugins.Runtime.PluginLoadContext", throwOnError: true)!;
            Context = (AssemblyLoadContext)Activator.CreateInstance(type,
                BindingFlags.Instance | BindingFlags.NonPublic, null, [assemblyPath], null)!;
            loadEntry = type.GetMethod("LoadEntryAssembly", BindingFlags.Instance | BindingFlags.NonPublic)!;
            resolveNative = type.GetMethod("LoadUnmanagedDll", BindingFlags.Instance | BindingFlags.NonPublic)!;
            validate = type.GetMethod("ValidateResolvedPath", BindingFlags.Instance | BindingFlags.NonPublic)!;
        }

        internal static string LibraryName => OperatingSystem.IsWindows() ? "b11_fixture.dll"
            : OperatingSystem.IsMacOS() ? "libb11_fixture.dylib" : "libb11_fixture.so";
        internal static string SkiaLibraryName => OperatingSystem.IsWindows() ? "libSkiaSharp.dll"
            : OperatingSystem.IsMacOS() ? "libSkiaSharp.dylib" : "libSkiaSharp.so";
        internal static string SkiaAssetPath => Path.Combine(AppContext.BaseDirectory, "runtimes",
            OperatingSystem.IsMacOS() ? "osx" : RuntimeInformation.RuntimeIdentifier, "native", SkiaLibraryName);
        internal string Root { get; }
        internal string Package { get; }
        internal string NativePath { get; }
        internal AssemblyDependencyResolver Resolver { get; }
        internal AssemblyLoadContext Context { get; }
        internal nint ResolveNative(string name) => (nint)resolveNative.Invoke(Context, [name])!;
        internal string Validate(string path) => (string)validate.Invoke(Context, [path])!;

        internal int Invoke(string method)
        {
            entry ??= (Assembly)loadEntry.Invoke(Context, [Path.Combine(Package, AssemblyName + ".dll")])!;
            return (int)entry.GetType(AssemblyName + ".NativeResolutionProbe", throwOnError: true)!
                .GetMethod(method)!.Invoke(null, null)!;
        }

        internal (int ExitCode, string Output) InvokeChild(string method, string workingDirectory,
            string? hostPath = null, bool isolated = false)
        {
            var start = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            };
            foreach (string argument in new[]
            {
                "exec", "--runtimeconfig", Path.Combine(AppContext.BaseDirectory, "BlackFlag.Engine.Plugins.Tests.runtimeconfig.json"),
                "--depsfile", isolated ? Path.Combine(Package, AssemblyName + ".deps.json")
                    : Path.Combine(AppContext.BaseDirectory, "BlackFlag.Engine.Plugins.Tests.deps.json"),
                Path.Combine(isolated ? Package : AppContext.BaseDirectory, AssemblyName + ".dll"),
                hostPath ?? typeof(PluginRuntimeHost).Assembly.Location, Path.Combine(Package, AssemblyName + ".dll"), method
            }) start.ArgumentList.Add(argument);
            using Process process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(15_000))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("Native fixture process exceeded its time limit.");
            }
            return (process.ExitCode, output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
        }

        public void Dispose()
        {
            Context.Unload();
            // Native fixture loads run in bounded children. Their exit permits
            // deterministic cleanup without freeing a borrowed runtime handle.
            Directory.Delete(Root, recursive: true);
        }
    }
}
