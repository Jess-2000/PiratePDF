[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Inspect', 'Install', 'Verify', 'Recover', 'Remove', 'StationLeaseOnlyV1')]
    [string]$Operation,
    [ValidatePattern('^[a-f0-9]{8}$')]
    [string]$RunId,
    [string]$Root,
    [ValidateRange(16, 4096)]
    [int]$CapacityMiB = 64,
    [switch]$DryRun,
    [ValidatePattern('^[a-f0-9]{32}$')]
    [string]$AuthorizationId,
    [ValidatePattern('^[a-f0-9]{64}$')]
    [string]$ApprovedClosureDigest,
    [int]$ControllerProcessId,
    [long]$ControllerCreationTime
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Operation -eq 'StationLeaseOnlyV1') {
    # This branch is an operator entry, never a requester-triggered UAC path.
    # The installed script, parent PowerShell and Windows are trusted
    # administrative bootstrap prerequisites. No Add-Type input comes from IPC.
    $codeRoot = Join-Path ([Environment]::GetFolderPath('CommonProgramFiles')) 'PiratePDF\NativeProvisioning\StationV1'
    $storeRoot = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'PiratePDF\NativeProvisioning\StationV1'
    $runtimeRoot = Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'dotnet'
    if ($DryRun) {
        [pscustomobject]@{ Operation=$Operation; ChangesPerformed=$false; ProductionNativeExecution='Disabled';
            CodeRoot=$codeRoot; AuthorizationStore=$storeRoot; ReadyToApply=$false;
            Prerequisite='Separate approval, protected installation, independent trusted Administrator parent, exact code/runtime manifest and controller instance.' } | ConvertTo-Json
        return
    }
    if (!$AuthorizationId -or !$ApprovedClosureDigest -or !$RunId -or $ControllerProcessId -le 0 -or $ControllerCreationTime -le 0) {
        throw 'Independent operator approval must bind a code closure and exact controller instance.'
    }
    if ($AuthorizationId -cnotmatch '^[a-f0-9]{32}$' -or $ApprovedClosureDigest -cnotmatch '^[a-f0-9]{64}$' -or $RunId -cnotmatch '^[a-f0-9]{8}$') { throw 'Noncanonical station identity.' }
    $impersonation=[Security.Principal.WindowsIdentity]::GetCurrent($true)
    if ($null -ne $impersonation) { $impersonation.Dispose(); throw 'An impersonating administrative parent is not a trusted bootstrap context.' }
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    try {
        if (!(New-Object Security.Principal.WindowsPrincipal($identity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
            throw 'Requires a separately approved, independently trusted Administrator PowerShell. No self-elevation is performed.'
        }
    } finally { $identity.Dispose() }
    $leases = New-Object 'Collections.Generic.List[IDisposable]'
    $controllerHandle = $null; $helperHandle = $null
    $ownershipStarted = $false; $originalFailure = $null; $helperCleanup = $null; $lifetimeEvidence = $null
    $cleanupFailure = $null; $leaseCloseFailures = 0
    function Assert-StationDescriptor($Descriptor, [bool]$RequireProtected, [bool]$Member) {
        $trusted = @('S-1-5-18','S-1-5-32-544','S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464')
        if ($Descriptor.Owner.Value -notin $trusted -or $null -eq $Descriptor.DiscretionaryAcl -or
            ($RequireProtected -and !([int]$Descriptor.ControlFlags -band [int][Security.AccessControl.ControlFlags]::DiscretionaryAclProtected))) {
            throw 'Unprotected bootstrap owner/DACL.'
        }
        $mutation = if ($Member) { 0x500d0156 } else { 0x500d0040 }
        foreach ($ace in $Descriptor.DiscretionaryAcl) {
            if ($ace -isnot [Security.AccessControl.CommonAce] -or $ace.IsCallback -or
                $ace.AceQualifier -notin @([Security.AccessControl.AceQualifier]::AccessAllowed,[Security.AccessControl.AceQualifier]::AccessDenied) -or
                ([int]$ace.AceFlags -band ([int][Security.AccessControl.AceFlags]::SuccessfulAccess -bor [int][Security.AccessControl.AceFlags]::FailedAccess))) {
                throw 'Unsupported bootstrap ACE.'
            }
            # This template is not effective here; each descendant is checked separately.
            if ([int]$ace.AceFlags -band [int][Security.AccessControl.AceFlags]::InheritOnly) { continue }
            if ($ace.AceQualifier -eq [Security.AccessControl.AceQualifier]::AccessAllowed -and
                $ace.SecurityIdentifier.Value -notin $trusted -and ($ace.AccessMask -band $mutation) -ne 0) {
                throw 'Untrusted bootstrap mutation authority.'
            }
        }
    }
    function Assert-StationPath([string]$Path, [bool]$RequireProtected = $false) {
        $current = [IO.Path]::GetFullPath($Path); $first = $true
        while ($current) {
            $item = Get-Item -LiteralPath $current -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Redirected bootstrap path.' }
            $acl = Get-Acl -LiteralPath $current
            $descriptor = New-Object Security.AccessControl.RawSecurityDescriptor($acl.GetSecurityDescriptorBinaryForm(),0)
            # A higher ancestor's ability to create unrelated children is not
            # authority to replace this protected path. DELETE_CHILD still refuses.
            Assert-StationDescriptor $descriptor ($RequireProtected -and $first) $first
            $current = [IO.Path]::GetDirectoryName($current); $first = $false
        }
    }
    function Read-StationBytes([string]$Path, [long]$Maximum) {
        Assert-StationPath $Path
        $stream = [IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        $leases.Add($stream)
        if ($stream.Length -lt 1 -or $stream.Length -gt $Maximum) { throw 'Bootstrap file budget exceeded.' }
        $bytes = New-Object byte[] ([int]$stream.Length)
        $offset = 0
        while ($offset -lt $bytes.Length) { $read=$stream.Read($bytes,$offset,$bytes.Length-$offset); if ($read -le 0) { throw 'Truncated bootstrap file.' }; $offset += $read }
        return ,$bytes
    }
    function Get-StationDigest([byte[]]$Bytes) {
        $sha=[Security.Cryptography.SHA256]::Create()
        try { return [BitConverter]::ToString($sha.ComputeHash($Bytes)).Replace('-','').ToLowerInvariant() } finally { $sha.Dispose() }
    }
    function Get-StationPeer([int]$ProcessId, $Handle) {
        $facts=[PiratePDF.Fuzz.Runner.StationBootstrapInterop]::Facts($Handle)
        return [ordered]@{ Pid=$ProcessId; CreationTime=[long]$facts[0]; User=$facts[1]; Logon=$facts[2]; Authentication=$facts[3]; TokenId=$facts[4];
            Session=[int]$facts[5]; Integrity=[int]$facts[6]; Elevated=[bool]::Parse($facts[7]); Administrator=[bool]::Parse($facts[8]);
            Primary=[bool]::Parse($facts[9]); AppContainer=[bool]::Parse($facts[10]); UIAccess=[bool]::Parse($facts[11]) }
    }
    try {
        Assert-StationPath $codeRoot $true; Assert-StationPath $storeRoot $true
        if (![string]::Equals($PSCommandPath,(Join-Path $codeRoot 'Provision-NativeExecution.ps1'),[StringComparison]::OrdinalIgnoreCase)) {
            throw 'Only the independently admitted protected installed script may launch this operation.'
        }
        $manifestBytes=Read-StationBytes (Join-Path $codeRoot 'closure.json') 131072
        if ((Get-StationDigest $manifestBytes) -cne $ApprovedClosureDigest) { throw 'Independent closure approval mismatch.' }
        $manifest=[Text.Encoding]::UTF8.GetString($manifestBytes) | ConvertFrom-Json
        if ($manifest.Version -ne 1 -or $manifest.RuntimeVersion -notmatch '^10\.0\.[0-9]+$' -or $manifest.HostFxrVersion -notmatch '^10\.0\.[0-9]+$' -or
            $manifest.Files.Count -lt 8 -or $manifest.Files.Count -gt 512 -or ($manifest.PSObject.Properties.Name | Where-Object { $_ -notin @('Version','RuntimeVersion','HostFxrVersion','Files') })) {
            throw 'Unsupported station manifest.'
        }
        $expected=New-Object 'Collections.Generic.Dictionary[string,object]' ([StringComparer]::OrdinalIgnoreCase)
        $total=0L
        foreach ($file in $manifest.Files) {
            if ($file.Root -notin @('Code','Runtime') -or $file.Path.Length -gt 240 -or $file.Path -match '[:\\]' -or
                @($file.Path.Split('/') | Where-Object { $_ -in @('','.','..') -or $_.EndsWith(' ') -or $_.EndsWith('.') }).Count -ne 0 -or
                $file.Sha256 -cnotmatch '^[0-9a-f]{64}$' -or $file.FileId -cnotmatch '^[0-9a-f]{48}$' -or $file.Length -lt 1 -or $file.Length -gt 268435456 -or
                ($file.PSObject.Properties.Name | Where-Object { $_ -notin @('Root','Path','Sha256','FileId','Length') })) { throw 'Invalid closure member.' }
            if ($file.Root -eq 'Runtime' -and $file.Path.Contains('/') -and
                !$file.Path.StartsWith('host/fxr/'+$manifest.HostFxrVersion+'/',[StringComparison]::Ordinal) -and
                !$file.Path.StartsWith('shared/Microsoft.NETCore.App/'+$manifest.RuntimeVersion+'/',[StringComparison]::Ordinal)) { throw 'Unapproved runtime dependency location.' }
            $base=if ($file.Root -eq 'Code') { $codeRoot } else { $runtimeRoot }
            $path=Join-Path $base $file.Path.Replace('/','\')
            if ($expected.ContainsKey($path)) { throw 'Duplicate closure path.' }
            $bytes=Read-StationBytes $path $file.Length
            if ($bytes.Length -ne $file.Length -or (Get-StationDigest $bytes) -cne $file.Sha256) { throw 'Code substitution.' }
            $total += $file.Length; if ($total -gt 536870912) { throw 'Closure work budget exceeded.' }
            $expected.Add($path,$file)
        }
        foreach ($required in @('PiratePDF.NativeProvisioning.Station.dll','PiratePDF.NativeProvisioning.Station.deps.json',
            'PiratePDF.NativeProvisioning.Station.runtimeconfig.json','Provision-NativeExecution.ps1','PiratePDF.NativeProvisioning.Bootstrap.dll',
            'controller\PiratePDF.Fuzz.Runner.exe','controller\PiratePDF.Fuzz.Runner.dll')) {
            if (!$expected.ContainsKey((Join-Path $codeRoot $required))) { throw 'Missing fixed application closure member.' }
        }
        foreach ($required in @('dotnet.exe',('host\fxr\'+$manifest.HostFxrVersion+'\hostfxr.dll'),
            ('shared\Microsoft.NETCore.App\'+$manifest.RuntimeVersion+'\hostpolicy.dll'),('shared\Microsoft.NETCore.App\'+$manifest.RuntimeVersion+'\coreclr.dll'))) {
            if (!$expected.ContainsKey((Join-Path $runtimeRoot $required))) { throw 'Missing exact host/runtime member.' }
        }
        $discovered=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
        $queue=New-Object 'Collections.Generic.Queue[string]'
        $queue.Enqueue($codeRoot); $queue.Enqueue((Join-Path $runtimeRoot ('host\fxr\'+$manifest.HostFxrVersion)))
        $queue.Enqueue((Join-Path $runtimeRoot ('shared\Microsoft.NETCore.App\'+$manifest.RuntimeVersion)))
        $directories=0
        while ($queue.Count -gt 0) {
            $directory=$queue.Dequeue(); Assert-StationPath $directory
            $directories++; if ($directories -gt 128) { throw 'Closure directory budget exceeded.' }
            foreach ($entry in [IO.Directory]::EnumerateFileSystemEntries($directory)) {
                if (([IO.File]::GetAttributes($entry) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Redirected dependency.' }
                if ([IO.Directory]::Exists($entry)) { $queue.Enqueue($entry) }
                elseif ($entry -ne (Join-Path $codeRoot 'closure.json')) { if (!$discovered.Add($entry)) { throw 'Duplicate closure enumeration.' } }
                if ($discovered.Count+$queue.Count -gt 512) { throw 'Closure member budget exceeded.' }
            }
        }
        foreach ($file in [IO.Directory]::EnumerateFiles($runtimeRoot)) { [void]$discovered.Add($file) }
        if ($discovered.Count -ne $expected.Count) { throw 'Unexpected/missing dependency.' }
        foreach ($file in $discovered) { if (!$expected.ContainsKey($file)) { throw 'Unapproved dependency.' } }
        $fxrDirectories=@([IO.Directory]::EnumerateDirectories((Join-Path $runtimeRoot 'host\fxr')))
        if ($fxrDirectories.Count -gt 32) { throw 'hostfxr directory budget exceeded.' }
        foreach ($directory in $fxrDirectories) {
            if ([version][IO.Path]::GetFileName($directory) -gt [version]$manifest.HostFxrVersion) { throw 'A different hostfxr would be selected.' }
        }
        foreach ($prefix in @('PiratePDF.NativeProvisioning.Station','controller\PiratePDF.Fuzz.Runner')) {
            $configText=[Text.Encoding]::UTF8.GetString((Read-StationBytes (Join-Path $codeRoot ($prefix+'.runtimeconfig.json')) 4096))
            # Fixed SDK-generated schema needs no escaped property names/values.
            # Reject ambiguous duplicate host-selection fields before ConvertFrom-Json.
            if ($configText.Contains('\')) { throw 'Escaped runtime configuration is not admitted.' }
            foreach ($key in @('runtimeOptions','tfm','framework','name','version','rollForward','configProperties','System.Reflection.Metadata.MetadataUpdater.IsSupported','System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization')) {
                if ([regex]::Matches($configText,'"'+[regex]::Escape($key)+'"\s*:').Count -gt 1) { throw 'Duplicate runtime setting.' }
            }
            $configRoot=$configText | ConvertFrom-Json
            if (@($configRoot.PSObject.Properties).Count -ne 1) { throw 'Unexpected runtime document.' }
            $config=$configRoot.runtimeOptions
            if ($config.rollForward -cne 'Disable' -or $config.framework.name -cne 'Microsoft.NETCore.App' -or $config.framework.version -cne $manifest.RuntimeVersion -or
                @($config.framework.PSObject.Properties).Count -ne 2 -or
                ($config.PSObject.Properties.Name | Where-Object { $_ -notin @('tfm','framework','rollForward','configProperties') })) { throw 'Unapproved runtime/probing configuration.' }
            if ($config.PSObject.Properties.Name -contains 'configProperties') {
                foreach ($property in $config.configProperties.PSObject.Properties) {
                    if ($property.Name -notin @('System.Reflection.Metadata.MetadataUpdater.IsSupported','System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization') -or
                        $property.Value -isnot [bool] -or $property.Value) { throw 'Unapproved runtime configuration value.' }
                }
            }
        }
        $depsText=[Text.Encoding]::UTF8.GetString((Read-StationBytes (Join-Path $codeRoot 'PiratePDF.NativeProvisioning.Station.deps.json') 4096))
        if ($depsText.Contains('\')) { throw 'Escaped dependency configuration is not admitted.' }
        $deps=$depsText | ConvertFrom-Json
        if (@($deps.libraries.PSObject.Properties).Count -ne 1 -or $deps.libraries.PSObject.Properties.Name -cne 'PiratePDF.NativeProvisioning.Station/1.0.0') { throw 'Privileged dependency graph is not closed.' }
        if ($deps.runtimeTarget.name -cne '.NETCoreApp,Version=v10.0' -or @($deps.targets.PSObject.Properties).Count -ne 1) { throw 'Unexpected runtime target.' }
        $target=$deps.targets.PSObject.Properties['.NETCoreApp,Version=v10.0'].Value
        if (@($target.PSObject.Properties).Count -ne 1) { throw 'Unexpected runtime library.' }
        $own=$target.PSObject.Properties['PiratePDF.NativeProvisioning.Station/1.0.0'].Value
        if (@($own.PSObject.Properties).Count -ne 1 -or @($own.runtime.PSObject.Properties).Count -ne 1 -or
            $own.runtime.PSObject.Properties.Name -cne 'PiratePDF.NativeProvisioning.Station.dll') { throw 'Unapproved native/resource/probing dependency.' }
        if (@(Get-ChildItem -LiteralPath $codeRoot -Filter '*.runtimeconfig.dev.json' -Recurse -File).Count -ne 0) { throw 'Development probing is prohibited.' }
        if ('PiratePDF.Fuzz.Runner.StationBootstrapInterop' -as [type]) { throw 'Bootstrap interop was already loaded; require a fresh trusted parent.' }
        Add-Type -LiteralPath (Join-Path $codeRoot 'PiratePDF.NativeProvisioning.Bootstrap.dll')
        # The fixed, hash-admitted native declarations give the parent OS file and
        # token identities before the helper CLR exists. Retain every read lease.
        [PiratePDF.Fuzz.Runner.StationBootstrapInterop]::NoImpersonation()
        foreach ($lease in $leases) {
            if ($lease -is [IO.FileStream] -and $expected.ContainsKey($lease.Name) -and
                [PiratePDF.Fuzz.Runner.StationBootstrapInterop]::FileId($lease.SafeFileHandle) -cne $expected[$lease.Name].FileId) { throw 'Bootstrap file identity changed.' }
        }
        $controllerHandle=[PiratePDF.Fuzz.Runner.StationBootstrapInterop]::OpenPeer($ControllerProcessId,$false)
        $controller=Get-StationPeer $ControllerProcessId $controllerHandle
        if ($controller.CreationTime -ne $ControllerCreationTime -or $controller.Elevated -or $controller.Administrator -or !$controller.Primary -or
            $controller.Integrity -ne 8192 -or $controller.Session -le 0 -or $controller.AppContainer -or $controller.UIAccess -or
            [PiratePDF.Fuzz.Runner.StationBootstrapInterop]::Exited($controllerHandle) -or
            [PiratePDF.Fuzz.Runner.StationBootstrapInterop]::Image($controllerHandle) -ine (Join-Path $codeRoot 'controller\PiratePDF.Fuzz.Runner.exe')) { throw 'Controller role/instance/image mismatch.' }
        $authorizationPath=Join-Path $storeRoot ($AuthorizationId+'.authorization.json')
        if ([IO.File]::Exists($authorizationPath) -or [IO.File]::Exists((Join-Path $storeRoot ($AuthorizationId+'.claimed')))) { throw 'Authorization identity was already used.' }
        $helperProcessId=0
        # Protective ownership is rooted before later role/authorization work can throw.
        $ownershipStarted = $true
        $helperHandle=[PiratePDF.Fuzz.Runner.StationBootstrapInterop]::LaunchStationHelper($AuthorizationId,[ref]$helperProcessId,$leases)
        $helperPeer=Get-StationPeer $helperProcessId $helperHandle
        if (!$helperPeer.Elevated -or !$helperPeer.Administrator -or !$helperPeer.Primary -or
            $helperPeer.Integrity -ne 12288 -or $helperPeer.Session -ne $controller.Session -or $helperPeer.User -ne $controller.User -or
            $helperPeer.AppContainer -or $helperPeer.UIAccess) { throw 'Actual helper instance/token mismatch; no authorization will be issued.' }
        $now=[DateTimeOffset]::UtcNow
        $record=[ordered]@{ Version=1; Operation='StationLeaseOnlyV1'; Id=$AuthorizationId; RunId=$RunId; Policy='station-lease-v1';
            StationRights=131074; DesktopRights=131201; CodeDigest=$ApprovedClosureDigest; NotBefore=$now.ToString('o'); Expires=$now.AddSeconds(30).ToString('o');
            Controller=$controller; Helper=$helperPeer }
        $recordBytes=[Text.Encoding]::UTF8.GetBytes(($record | ConvertTo-Json -Depth 5 -Compress))
        $recordStream=New-Object IO.FileStream($authorizationPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None,4096,[IO.FileOptions]::WriteThrough)
        try { $recordStream.Write($recordBytes,0,$recordBytes.Length); $recordStream.Flush($true) } finally { $recordStream.Dispose() }
        Assert-StationPath $authorizationPath
    } catch { $originalFailure = $_
    } finally {
        if ($ownershipStarted) {
            $failureClass=if ($originalFailure) { $originalFailure.Exception.GetType().Name } else { 'None' }
            try { $helperCleanup=[PiratePDF.Fuzz.Runner.StationBootstrapInterop]::CompleteOwnedHelper([bool]$originalFailure,$failureClass,$leases) }
            catch { $cleanupFailure = $_ } # Never replace a preexisting launch/authorization error.
            if (!$originalFailure -and $helperCleanup -and $helperCleanup.ExitVerified -and !$helperCleanup.TerminationAttempted -and
                $helperCleanup.ExitCode -eq 0 -and $helperCleanup.CodeLeasesReleased) {
                try { $lifetimeEvidence=[PiratePDF.Fuzz.Runner.StationBootstrapInterop]::ObserveStationLifetime($RunId,$controllerHandle,$controller.Session,$helperCleanup) }
                catch { $lifetimeEvidence=[pscustomobject]@{ State='ObservationFailure'; Error=$_.Exception.GetType().Name; HResult=$_.Exception.HResult; DestructionProved=$false } }
            }
        }
        # Unverified exit roots the handle AND code leases in the resident trusted parent.
        if (!$ownershipStarted -or ($helperCleanup -and !$helperCleanup.OwnershipEstablished)) {
            for ($i=$leases.Count-1;$i -ge 0;$i--) {
                try { $leases[$i].Dispose() } catch { $leaseCloseFailures++; if (!$cleanupFailure) { $cleanupFailure=$_ } }
            }
        }
        if ($controllerHandle) { $controllerHandle.Dispose() }
    }
    if ($helperCleanup) {
        $helperOutcome=if ($helperCleanup.ExitVerified -and !$helperCleanup.TerminationAttempted -and $helperCleanup.ExitCode -eq 0) {
            @('AcknowledgedHelperClosed','0','ObjectLifetimeUnqualified')
        } elseif ($helperCleanup.ExitVerified) { [PiratePDF.Fuzz.Runner.StationBootstrapInterop]::DecodeFailure($helperCleanup.ExitCode)
        } else { @('UnverifiedHelperExit','0','UNKNOWN/BLOCKED') }
        $cleanupError=if ($cleanupFailure) { $cleanupFailure.Exception.GetType().Name } else { $null }
        [pscustomobject]@{ Operation=$Operation; Authorization=$AuthorizationId; HelperLifecycle=$helperCleanup; LifetimeEvidence=$lifetimeEvidence;
            CleanupError=$cleanupError; AdditionalLeaseCloseFailures=$leaseCloseFailures;
            State=$helperOutcome[0]; Win32Error=$helperOutcome[1]; Ownership=$helperOutcome[2];
            FinalObjectLifetime='UNKNOWN/BLOCKED'; ProductionNativeExecution='Disabled' } | ConvertTo-Json -Depth 4
    }
    if ($originalFailure) { throw $originalFailure }
    if ($cleanupFailure) { throw $cleanupFailure }
    if (!$helperCleanup -or !$helperCleanup.ExitVerified -or !$helperCleanup.CodeLeasesReleased -or
        $helperCleanup.TerminationAttempted -or $helperCleanup.ExitCode -ne 0) {
        throw 'UNKNOWN/BLOCKED: helper lifecycle failed; protective ownership may remain in this trusted parent. No retry or worker admission.'
    }
    if (!$lifetimeEvidence -or !$lifetimeEvidence.DestructionProved) {
        throw 'UNKNOWN/BLOCKED: owner closure and namespace observations do not prove final USER-object destruction. Acceptance requires a separate decision.'
    }
    return # Never exit the resident trusted parent that may own protective leases.
}

if ($Operation -eq 'Inspect' -or $Operation -eq 'Verify') {
    [pscustomobject]@{
        Schema = 'piratepdf-h2b-provisioning-checkpoint-v1'
        Operation = $Operation
        Qualified = $false
        ChangesPerformed = $false
        Blockers = @(
            'Live ACL/token provider source exists; restricted-account qualification is pending.'
            'Persistent per-account WFP provider source exists; brokered DNS and local IPC denial are not established.'
            'Fixed-VHDX identity/geometry provider source exists; privileged capacity qualification is pending.'
            'No integrated restricted creation-time Job launch is qualified.'
            'Install/Recover/Remove ownership journals and lifecycle orchestration remain incomplete; application is refused.'
        )
    } | ConvertTo-Json -Depth 4
    if ($Operation -eq 'Verify') { exit 2 }
    exit 0
}

if ([string]::IsNullOrWhiteSpace($RunId) -or [string]::IsNullOrWhiteSpace($Root)) {
    throw 'A specific run identity and trusted absolute root are required for a reviewable plan.'
}
if ($Root -notmatch '^[A-Za-z]:[\\/]' -or $Root.StartsWith('\\') -or
    $Root.Substring(2).Contains(':') -or $Root -match '[\x00-\x1f]') {
    throw 'Expected an ordinary absolute local planning root.'
}
$planRoot = [IO.Path]::GetFullPath($Root)
$account = 'pph2b_' + $RunId
[pscustomobject]@{
    Schema = 'piratepdf-h2b-provisioning-plan-v1'
    Operation = $Operation
    DryRun = [bool]$DryRun
    ReadyToApply = $false
    ChangesPerformed = $false
    Account = $account
    RestrictingGroup = 'pph2b_r_' + $RunId
    BackingFile = [IO.Path]::Combine($planRoot, $RunId + '.vhdx')
    CapacityBytes = [long]$CapacityMiB * 1MB
    Network = 'Eight per-account IPv4/IPv6 WFP ALE block layers; loopback included; separate denial of unapproved local IPC and proxied DNS must be demonstrated.'
    RequiredPrivileges = 'Explicit Administrator approval for local account/logon policy, scoped ACLs, VHDX creation/attach/format, and WFP installation. No service or developer privilege grant.'
    AclPlan = 'Read/execute only for approved image/runtime closure; read/write only on the bounded volume. Exact runtime resources are unresolved; no system ACL expansion is permitted automatically.'
    CredentialPlan = 'No credential arguments, environment, logs or plan fields. The explicit .NET verification helper consumes an interactive SecureString; no credential is persisted.'
    Recovery = 'Retain uncertain backing files and evidence; inspect live ownership before any detach, format, reuse or deletion.'
    Rollback = 'Remove only verified owned account/group/filter identities after the job is empty and evidence is retained. Leave ambiguous resources untouched.'
    PersistentChanges = 'Account/group, logon policy, explicit ACLs, backing disk and WFP objects; none are created by this checkpoint utility.'
} | ConvertTo-Json -Depth 4

if ($DryRun) { exit 0 }
throw 'Apply is unavailable at this security checkpoint. No system changes were performed. Complete and review the live providers before requesting Administrator approval.'
