#nullable enable annotations
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.Json;

namespace PiratePDF.Fuzz.Tests;

public sealed class H2bStationManifestTests
{
    private static object Manifest(string? substitution = null, string? duplicate = null)
    {
        string[] code = ["PiratePDF.NativeProvisioning.Station.dll", "PiratePDF.NativeProvisioning.Station.deps.json", "PiratePDF.NativeProvisioning.Station.runtimeconfig.json",
            "Provision-NativeExecution.ps1", "PiratePDF.NativeProvisioning.Bootstrap.dll", "controller/PiratePDF.Fuzz.Runner.exe", "controller/PiratePDF.Fuzz.Runner.dll"];
        string[] runtime = ["dotnet.exe", "host/fxr/10.0.10/hostfxr.dll", "shared/Microsoft.NETCore.App/10.0.10/hostpolicy.dll", "shared/Microsoft.NETCore.App/10.0.10/coreclr.dll"];
        var files = code.Select(path => new { Root="Code", Path=path, Sha256=new string('a',64), FileId=new string('b',48), Length=1L })
            .Concat(runtime.Select(path => new { Root="Runtime", Path=path, Sha256=new string('a',64), FileId=new string('b',48), Length=1L })).ToList();
        if (substitution is not null) files[0]=new { files[0].Root, Path=substitution, files[0].Sha256, files[0].FileId, files[0].Length };
        if (duplicate is not null) files.Add(new { Root="Code", Path=duplicate, Sha256=new string('a',64), FileId=new string('b',48), Length=1L });
        return Decode("StationManifest", JsonSerializer.Serialize(new { Version=1, RuntimeVersion="10.0.10", HostFxrVersion="10.0.10", Files=files }));
    }
    private static object Decode(string type, string json)
    {
        MethodInfo reader=H2bTestChild.Type("NativeStationPolicy").GetMethod("Decode",BindingFlags.NonPublic|BindingFlags.Static)!.MakeGenericMethod(H2bTestChild.Type(type));
        try { return reader.Invoke(null,[Encoding.UTF8.GetBytes(json)])!; }
        catch(TargetInvocationException failure) when(failure.InnerException is not null)
        { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure.InnerException).Throw(); throw; }
    }
    [Fact]
    public void CompleteFixedManifestPassesPurePolicy() => H2bTestChild.Call("NativeStationTrust","ValidateManifest",Manifest());
    [Theory]
    [InlineData("../untrusted.dll")]
    [InlineData("controller/../../escape.dll")]
    [InlineData("C:/caller.dll")]
    [InlineData("folder\\caller.dll")]
    [InlineData("folder./caller.dll")]
    [InlineData("replaced.dll")]
    public void DependencySubstitutionAndTraversalRefuse(string path) =>
        Assert.Throws<SecurityException>(()=>H2bTestChild.Call("NativeStationTrust","ValidateManifest",Manifest(path)));
    [Fact]
    public void DuplicateManifestMembersRefuse() =>
        Assert.Throws<SecurityException>(()=>H2bTestChild.Call("NativeStationTrust","ValidateManifest",Manifest(duplicate:"PiratePDF.NativeProvisioning.Station.dll")));
    [Theory]
    [InlineData("{\"Version\":1,\"Version\":2}")]
    [InlineData("null")]
    public void DuplicatePropertiesAndNullDocumentsRefuse(string json) => Assert.Throws<SecurityException>(()=>Decode("StationMessage",json));
    [Fact]
    public void UnknownProtocolFieldsRefuse() => Assert.Throws<JsonException>(()=>Decode("StationMessage","{\"Version\":1,\"Command\":\"arbitrary\"}"));
    [Fact]
    public void OversizedDocumentsRefuse() => Assert.Throws<SecurityException>(()=>Decode("StationMessage",new string(' ',131073)));
    [Fact]
    public void UnexpectedDependencyGraphRefuses() => Assert.Throws<SecurityException>(()=>H2bTestChild.Call("StationArtifactInspection","ValidateDependencies",
        Encoding.UTF8.GetBytes("{\"runtimeTarget\":{\"name\":\".NETCoreApp,Version=v10.0\"},\"libraries\":{\"Foreign/1\":{}},\"targets\":{}}")));
    [Fact]
    public void SourceProfileIsAnExplicitClosedAllowlist()
    {
        string root=ReplayFixture.FindRepository();
        string project=File.ReadAllText(Path.Combine(root,"tools/PiratePDF.NativeProvisioning/PiratePDF.NativeProvisioning.csproj"));
        Assert.Contains("<EnableDefaultCompileItems>false</EnableDefaultCompileItems>",project,StringComparison.Ordinal);
        Assert.Contains("RequireStationRuntimeApproval",project,StringComparison.Ordinal);
        Assert.DoesNotContain("NativeStation*.cs",project,StringComparison.Ordinal);
        string entry=File.ReadAllText(Path.Combine(root,"tools/PiratePDF.NativeProvisioning/StationProvisioningEntry.cs"));
        Assert.DoesNotContain("LoadFromAssemblyPath",entry,StringComparison.Ordinal); Assert.Contains("args.Length != 2",entry,StringComparison.Ordinal);
    }
    [Theory]
    [InlineData(2L,"b","a")]
    [InlineData(1L,"c","a")]
    [InlineData(1L,"b","c")]
    public void ExactFileAdmissionRejectsLengthIdentityOrDigestSubstitution(long length,string identity,string digest)
    {
        object expected=H2bTestChild.New("StationFile","Code","fixed.dll","a","b",1L);
        Assert.Throws<SecurityException>(()=>H2bTestChild.Call("NativeStationTrust","MatchFile",expected,length,identity,digest));
    }
    [Fact]
    public void BootstrapUsesFixedPreClrEnvironmentAndDisablesAllInheritance()
    {
        string root=ReplayFixture.FindRepository();
        string source=File.ReadAllText(Path.Combine(root,"tools/PiratePDF.NativeProvisioning/StationBootstrapInterop.cs"));
        Assert.Contains("CreateProcessW(image, command, IntPtr.Zero, IntPtr.Zero, false, 0x08000400",source,StringComparison.Ordinal);
        Assert.Contains("#if !NET",source,StringComparison.Ordinal);
        Assert.DoesNotContain("Environment.GetEnvironmentVariables",source,StringComparison.Ordinal);
        string script=File.ReadAllText(Path.Combine(root,"tools/PiratePDF.Fuzz.Runner/Provision-NativeExecution.ps1"));
        Assert.DoesNotContain("Process]::Start",script,StringComparison.Ordinal);
        Assert.DoesNotContain("Add-Type -TypeDefinition",script,StringComparison.Ordinal);
        Assert.Contains("Add-Type -LiteralPath (Join-Path $codeRoot 'PiratePDF.NativeProvisioning.Bootstrap.dll')",script,StringComparison.Ordinal);
        Assert.Contains("[IO.FileMode]::CreateNew",script,StringComparison.Ordinal);
        Assert.Contains("$ControllerCreationTime",script,StringComparison.Ordinal);
        Assert.Contains("No self-elevation",script,StringComparison.Ordinal);
    }
}
