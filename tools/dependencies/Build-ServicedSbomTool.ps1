[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [string]$DotnetPath = 'dotnet',
    [string]$SourceArchive
)

$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT -or -not [Environment]::Is64BitProcess) {
    throw 'Build the reviewed Windows x64 serviced tool from 64-bit Windows PowerShell.'
}
$repository = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$baseline = Get-Content (Join-Path $PSScriptRoot 'artifact-security-baseline.json') -Raw | ConvertFrom-Json
$tool = $baseline.SbomTool
$output = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
$allowed = Join-Path $repository 'artifacts/fuzz'
if (-not $output.StartsWith($allowed + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
    (Test-Path -LiteralPath $output)) { throw 'Use a new output directory under artifacts/fuzz for the serviced tool build.' }
$sdk = & $DotnetPath --version
if ($LASTEXITCODE -ne 0 -or $sdk -ne $baseline.SDK) { throw 'Select the exact approved SDK before building the serviced tool.' }
[void][IO.Directory]::CreateDirectory($output)
$archive = Join-Path $output 'source.zip'
$url = 'https://api.github.com/repos/CycloneDX/cyclonedx-dotnet/zipball/' + $tool.UpstreamCommit
if ($SourceArchive) { Copy-Item -LiteralPath $SourceArchive -Destination $archive }
else { Invoke-WebRequest -Uri $url -OutFile $archive -UseBasicParsing }
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash -ne $tool.SourceArchiveSHA512) { throw 'Pinned CycloneDX source archive checksum differs.' }
Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $output 'source')
$roots = @(Get-ChildItem (Join-Path $output 'source') -Directory)
if ($roots.Count -ne 1) { throw 'Unexpected source archive layout.' }
$source = $roots[0].FullName
$properties = Join-Path $source 'Directory.Packages.props'
$original = [IO.File]::ReadAllText($properties)
if ($original.Contains('Include="System.Security.Cryptography.Pkcs"')) { throw 'Source dependency pin changed upstream. Review before rebuilding.' }
$patched = $original.Replace('  <ItemGroup>', ('  <ItemGroup>' + "`n" + '    <!-- PiratePDF B14B servicing modification: pin the owner-approved security patch. -->' + "`n" + '    <PackageVersion Include="System.Security.Cryptography.Pkcs" Version="' + $tool.Pkcs + '" />'))
if ($patched -eq $original) { throw 'Unable to apply the reviewed Pkcs-only dependency patch.' }
[IO.File]::WriteAllText($properties, $patched, (New-Object Text.UTF8Encoding($false)))
$project = Join-Path $source 'CycloneDX/CycloneDX.csproj'
$common = @('-c', 'Release', '-f', $baseline.TargetFramework, '-r', 'win-x64', '--self-contained', 'false',
    '-p:TargetFrameworks=net10.0', '-p:GeneratePackageOnBuild=false', '-p:PackAsTool=false',
    ('-p:PackageId=' + $tool.Id), ('-p:Version=' + $tool.Version), ('-p:PackageVersion=' + $tool.Version),
    ('-p:InformationalVersion=' + $tool.Version), '-p:IncludeSourceRevisionInInformationalVersion=false',
    '-p:EnableSourceControlManagerQueries=false', '-p:EnableSourceLink=false',
    '-p:Deterministic=true', '-p:ContinuousIntegrationBuild=true', '-p:TreatWarningsAsErrors=true',
    '-p:NuGetAudit=true', '-p:NuGetAuditMode=all', '-p:NuGetAuditLevel=low',
    '-p:PublishTrimmed=false', '-p:PublishAot=false', '-p:RollForward=LatestPatch',
    ('-p:RuntimeFrameworkVersion=' + $baseline.Runtime),
    ('-p:RestoreConfigFile=' + (Join-Path $PSScriptRoot 'NuGet.Config')))
foreach ($run in @(1, 2)) {
    $build = Join-Path $output "build-$run"
    $publish = Join-Path $output "publish-$run"
    $arguments = @('publish', $project) + $common + @('--artifacts-path', $build, '-o', $publish,
        ('-p:PathMap=' + $source + '=/_/CycloneDX%2C' + $build + '=/_/Build'))
    & $DotnetPath @arguments *> (Join-Path $output "build-$run.log")
    if ($LASTEXITCODE -ne 0) { throw "Serviced tool build $run failed. Inspect build-$run.log." }
    Copy-Item -LiteralPath (Join-Path $source 'LICENSE'), (Join-Path $source 'NOTICE') -Destination $publish
    [IO.File]::WriteAllText((Join-Path $publish 'SERVICING-NOTICE.txt'),
        ("PiratePDF development-only serviced CycloneDX variant " + $tool.Version + ".`n" +
        "Built from upstream commit " + $tool.UpstreamCommit + ".`n" +
        "PiratePDF modification: pin System.Security.Cryptography.Pkcs " + $tool.Pkcs + "; build net10.0 with explicit development identity and runtime floor.`n" +
        "Original upstream copyright, LICENSE and NOTICE are retained. Dependency legal evidence is in the repository's docs/dependencies records.`n"),
        (New-Object Text.UTF8Encoding($false)))
}
$first = Join-Path $output 'publish-1'
$second = Join-Path $output 'publish-2'
$files = @(Get-ChildItem $first -Recurse -File | Where-Object Extension -in @('.dll', '.json', '.exe') | Sort-Object FullName | ForEach-Object {
    $relative = $_.FullName.Substring($first.Length + 1).Replace('\', '/')
    $hash = (Get-FileHash -LiteralPath $_.FullName).Hash
    if ((Get-FileHash -LiteralPath (Join-Path $second $relative)).Hash -ne $hash) { throw "Isolated tool rebuild differs: $relative" }
    [ordered]@{Name=$relative;Sha256=$hash.ToLowerInvariant()}
})
$deps = Get-Content (Join-Path $first 'CycloneDX.deps.json') -Raw | ConvertFrom-Json
$packages = @($deps.libraries.PSObject.Properties | Where-Object { $_.Value.type -eq 'package' } | ForEach-Object { $_.Name } | Sort-Object)
$vendor = Get-Content (Join-Path $repository 'docs/dependencies/history/b12-20261031/generation-tool.json') -Raw | ConvertFrom-Json
$expected = @($vendor.BundledDependencies | Where-Object Graph -eq 'tools/net10.0/any/CycloneDX.deps.json' |
    ForEach-Object { $_.Dependencies } | Where-Object Type -eq 'package' | ForEach-Object { $_.Identity.Replace('System.Security.Cryptography.Pkcs/8.0.1', 'System.Security.Cryptography.Pkcs/' + $tool.Pkcs) } | Sort-Object)
if (Compare-Object $expected $packages) { throw 'Serviced tool has dependency changes beyond the approved Pkcs patch.' }
$assets = Get-Content (Join-Path $output 'build-1/obj/CycloneDX/project.assets.json') -Raw | ConvertFrom-Json
$pkcsArchive = Join-Path $assets.project.restore.packagesPath ('system.security.cryptography.pkcs/' + $tool.Pkcs + '/system.security.cryptography.pkcs.' + $tool.Pkcs + '.nupkg')
if ((Get-FileHash $pkcsArchive -Algorithm SHA512).Hash -ine $tool.PkcsArchiveSHA512) { throw 'Restored Pkcs archive differs from the reviewed publisher package.' }
$auditPath = Join-Path $output 'audit.json'
$restoreProperties = @($common | Where-Object { $_ -like '-p:*' })
& $DotnetPath restore $project --locked-mode -r win-x64 @restoreProperties *> (Join-Path $output 'audit-restore.log')
if ($LASTEXITCODE -ne 0) { throw 'Locked serviced-tool audit restore failed.' }
& $DotnetPath list $project package --vulnerable --include-transitive --no-restore --format json `
    --framework $baseline.TargetFramework *> $auditPath
if ($LASTEXITCODE -ne 0) { throw 'Serviced tool vulnerability audit failed.' }
$audit = Get-Content $auditPath -Raw | ConvertFrom-Json
if (-not $audit.projects -or $audit.logs) { throw 'Serviced tool audit has missing scope or diagnostics.' }
foreach ($framework in $audit.projects.frameworks) {
    if ($framework.topLevelPackages -or $framework.transitivePackages) { throw 'Serviced tool audit reports vulnerable packages.' }
}
$identity = & (Join-Path $first 'CycloneDX.exe') --version
if ($LASTEXITCODE -ne 0 -or -not $identity.StartsWith($tool.Version)) { throw 'Serviced tool CLI identity differs.' }
$record = [ordered]@{
    Format='PiratePDF source-serviced SBOM tool v1';Id=$tool.Id;Version=$tool.Version;Scope='development-only'
    Upstream='https://github.com/CycloneDX/cyclonedx-dotnet';Commit=$tool.UpstreamCommit
    SourceArchiveUrl=$url;SourceArchiveSHA512=$tool.SourceArchiveSHA512
    Patch='Add the central transitive System.Security.Cryptography.Pkcs pin only; build net10.0 with explicit development identity.'
    PatchedPropertiesSHA256=(Get-FileHash $properties).Hash;SDK=$sdk;Runtime=$baseline.Runtime
    Pkcs=$tool.Pkcs;Approval=$tool.Approval;ReproducibleRuntimeFiles=$true;AuditReportedVulnerabilities=0
    Packages=$packages;Files=$files
    LegalFiles=@(@('LICENSE','NOTICE','SERVICING-NOTICE.txt') | ForEach-Object {
        [ordered]@{Name=$_;Sha256=(Get-FileHash (Join-Path $first $_)).Hash.ToLowerInvariant()}
    })
}
[IO.File]::WriteAllText((Join-Path $first 'serviced-tool-provenance.json'), (($record | ConvertTo-Json -Depth 12) -replace "`r`n", "`n") + "`n", (New-Object Text.UTF8Encoding($false)))
Write-Output "Built $($tool.Id) $($tool.Version). Executable: $(Join-Path $first 'CycloneDX.exe')"
