using System.Reflection;
using System.Security;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PiratePDF.Fuzz.Runner;

namespace PiratePDF.Fuzz.Tests;

[Trait("Platform", "Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class H2bStationAclApplicabilityTests
{
    private static IEnumerable<object[]> Descriptors
    {
        get
        {
            MethodInfo method = typeof(H2bStationAclApplicabilityTests).GetMethod(nameof(EffectiveMutationRefusesButAncestorTemplateDoesNot));
            return method.GetCustomAttributes<InlineDataAttribute>().SelectMany(attribute => attribute.GetData(method));
        }
    }

    [Theory]
    [InlineData("O:BAG:BAD:P(A;OICIIO;GA;;;CO)(A;;GR;;;BU)", false, false, true)]
    [InlineData("O:BAG:BAD:P(A;OICIIO;0xe0010000;;;AU)(A;;GR;;;BU)", false, false, true)]
    [InlineData("O:BAG:BAD:P(A;ID;GW;;;BU)", true, true, false)]
    [InlineData("O:BAG:BAD:P(A;;GW;;;BU)", true, true, false)]
    [InlineData("O:BAG:BAD:P(A;;0x4;;;BU)", true, true, false)]
    [InlineData("O:BAG:BAD:P(A;;0x40;;;BU)", false, false, false)]
    [InlineData("O:BAG:BAD:P(A;;SD;;;BU)", true, true, false)]
    [InlineData("O:BAG:BAD:P(A;;WD;;;BU)", true, true, false)]
    [InlineData("O:BAG:BAD:P(A;;WO;;;BU)", true, true, false)]
    [InlineData("O:BAG:BAD:NO_ACCESS_CONTROL", true, true, false)]
    [InlineData("O:WDG:BAD:P(A;;GR;;;BU)", true, true, false)]
    [InlineData("O:BAG:BAD:(A;;GR;;;BU)", true, true, false)]
    [InlineData("O:BAG:BAD:(A;ID;GR;;;BU)", false, true, true)]
    [InlineData("O:BAG:BAD:P(D;;GW;;;BU)(A;;GR;;;BU)", true, true, true)]
    [InlineData("O:BAG:BAD:P(D;;GW;;;BU)(A;;GW;;;BU)", true, true, false)]
    [InlineData("O:BAG:BAD:P(OA;;GW;00112233-4455-6677-8899-aabbccddeeff;;BU)", true, true, false)]
    public void EffectiveMutationRefusesButAncestorTemplateDoesNot(string sddl, bool protect, bool member, bool allowed)
    {
        void Verify() => H2bTestChild.Call("NativeStationTrust", "VerifyPathDescriptor", new RawSecurityDescriptor(sddl), protect, member);
        if (allowed) Verify(); else Assert.Throws<SecurityException>(Verify);
    }

    [Fact]
    public void CallbackAceRemainsUnsupported()
    {
        var descriptor = new RawSecurityDescriptor("O:BAG:BAD:P(A;;GR;;;BU)");
        descriptor.DiscretionaryAcl!.InsertAce(0, new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 2, new("S-1-5-32-545"), true, new byte[] { 0, 0, 0, 0 }));
        Assert.Throws<SecurityException>(() => H2bTestChild.Call("NativeStationTrust", "VerifyPathDescriptor", descriptor, true, true));
    }

    [Fact]
    public async Task ActualPreClrFunctionAgreesWithManagedAdmission()
    {
        string root = ReplayFixture.FindRepository();
        string script = File.ReadAllText(Path.Combine(root, "tools/PiratePDF.Fuzz.Runner/Provision-NativeExecution.ps1"));
        int start = script.IndexOf("    function Assert-StationDescriptor(", StringComparison.Ordinal);
        int end = script.IndexOf("    function Assert-StationPath(", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        string source = Convert.ToBase64String(Encoding.UTF8.GetBytes(script[start..end]));
        string data = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(Descriptors.Select(row =>
            new { Sddl = row[0], Protect = row[1], Member = row[2] })));
        string command = "$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseInput([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + source + "')),[ref]$tokens,[ref]$errors);" +
            "if($errors.Count){throw 'Parse failure'};$fn=@($ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq 'Assert-StationDescriptor'},$true));" +
            "if($fn.Count -ne 1){throw 'Function count'};. ([scriptblock]::Create($fn[0].Extent.Text));" +
            "$cases=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + data + "'))|ConvertFrom-Json;" +
            "$values=@(foreach($case in $cases){try{Assert-StationDescriptor ([Security.AccessControl.RawSecurityDescriptor]::new($case.Sddl)) $case.Protect $case.Member;$true}catch{$false}});ConvertTo-Json -InputObject $values -Compress";
        string directory = Path.Combine(root, "artifacts", "fuzz", "station-acl-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string host = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
            ChildResult result = await ChildProcess.RunAsync(host,
                ["-NoProfile", "-NonInteractive", "-EncodedCommand", Convert.ToBase64String(Encoding.Unicode.GetBytes(command))],
                directory, TimeSpan.FromSeconds(10), Path.Combine(directory, "absent.lease"));
            Assert.Equal(0, result.ExitCode);
            Assert.True(result.CleanupSucceeded);
            Assert.False(result.TimedOut);
            Assert.False(result.OutputTruncated);
            Assert.Equal(Descriptors.Select(row => (bool)row[3]), JsonSerializer.Deserialize<bool[]>(result.Output));
        }
        finally { Directory.Delete(directory, true); } // Only the fresh, owned fixture directory.
    }

    [Fact]
    public void ReparseSubstitutionIsRejectedBeforeOwnerAdmission()
    {
        string root = Path.Combine(ReplayFixture.FindRepository(), "artifacts", "fuzz", "station-acl-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string target = Path.Combine(root, "owned-target"); Directory.CreateDirectory(target);
            string link = Path.Combine(root, "link"); Directory.CreateSymbolicLink(link, target);
            SecurityException error = Assert.Throws<SecurityException>(() => H2bTestChild.Call("NativeStationTrust", "ProtectAncestors", link));
            Assert.Contains("Reparse", error.Message, StringComparison.Ordinal);
            Assert.True(Directory.Exists(target));
        }
        finally { Directory.Delete(root, true); } // Never a protected or foreign deployment root.
    }
}
