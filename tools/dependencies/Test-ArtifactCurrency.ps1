[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ManifestPath,
    [string]$BaselinePath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $BaselinePath) { $BaselinePath = Join-Path $PSScriptRoot 'artifact-security-baseline.json' }
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$baseline = Get-Content -LiteralPath $BaselinePath -Raw | ConvertFrom-Json
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
if ($baseline.Format -ne 'PiratePDF artifact security baseline v1' -or
    $manifest.Format -ne 'PiratePDF artifact currency manifest v1') { throw 'Unsupported baseline or manifest format.' }

function Get-ContainedPath([string]$Root, [string]$Relative) {
    if ([string]::IsNullOrWhiteSpace($Relative) -or [IO.Path]::IsPathRooted($Relative)) { throw 'Evidence paths must be relative.' }
    $rootPath = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $path = [IO.Path]::GetFullPath((Join-Path $rootPath $Relative))
    if (-not $path.StartsWith($rootPath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Evidence path escapes its root: $Relative"
    }
    $cursor = $path
    while ($cursor.Length -ge $rootPath.Length) {
        if (([IO.File]::GetAttributes($cursor) -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Evidence path uses a reparse point: $Relative"
        }
        $cursor = [IO.Path]::GetDirectoryName($cursor)
    }
    return $path
}

function Get-VerifiedFile($Record) {
    $path = Get-ContainedPath $repository $Record.Path
    if ($Record.SHA256 -notmatch '^[0-9A-Fa-f]{64}$' -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $Record.SHA256) {
        throw "Evidence hash mismatch: $($Record.Path)"
    }
    return $path
}

$metadata = Get-Content -LiteralPath (Get-VerifiedFile $manifest.ReleaseMetadata) -Raw | ConvertFrom-Json
if ($metadata.'latest-sdk' -ne $baseline.SDK -or $metadata.'latest-runtime' -ne $baseline.Runtime) {
    throw 'Publisher metadata differs from the approved SDK/runtime baseline. Review servicing before qualification.'
}
if ($manifest.ReviewedOn -ne $baseline.ReviewedOn -or $manifest.Source.Commit -notmatch '^[0-9a-f]{40}$' -or
    [string]::IsNullOrWhiteSpace($manifest.Source.State)) { throw 'Missing source or dated baseline review provenance.' }
[void](Get-VerifiedFile $manifest.Source.Inputs)
$sdk = Get-VerifiedFile $manifest.SDK
$selected = & $sdk --version
if ($LASTEXITCODE -ne 0 -or $selected -ne $baseline.SDK) { throw "SDK selection must be $($baseline.SDK), found $selected." }
$global = Get-Content -LiteralPath (Join-Path $repository 'global.json') -Raw | ConvertFrom-Json
if ($global.sdk.version -ne $baseline.SDK) { throw 'global.json differs from the reviewed SDK baseline.' }
$packages = [xml](Get-Content -LiteralPath (Join-Path $repository 'Directory.Packages.props') -Raw)
foreach ($package in $packages.Project.ItemGroup.PackageVersion | Where-Object Include -like 'SkiaSharp*') {
    if ($package.Version -ne $baseline.SkiaSharp) { throw 'Central SkiaSharp selection differs from the reviewed baseline.' }
}
if (@($manifest.Accountability).Count -lt 3) { throw 'Missing SBOM/notices accountability evidence.' }
foreach ($record in $manifest.Accountability) { [void](Get-VerifiedFile $record) }
$inventory = Get-Content -LiteralPath (Get-VerifiedFile $manifest.DependencyInventory) -Raw | ConvertFrom-Json
$skiaPackages = @($inventory.Packages | Where-Object Id -like 'SkiaSharp*')
if ($skiaPackages.Count -ne 4 -or @($skiaPackages | Where-Object Version -ne $baseline.SkiaSharp).Count -gt 0) {
    throw 'Resolved SkiaSharp inventory differs from the reviewed baseline.'
}
$servicedProvenance = Get-VerifiedFile $manifest.ServicedSbomProvenance
$servicedArtifact = @($manifest.Artifacts | Where-Object Id -eq 'sbom-tool-serviced')
if ($servicedArtifact.Count -ne 1) { throw 'Missing current source-serviced SBOM tool artifact.' }
$servicedDirectory = Get-ContainedPath $repository $servicedArtifact[0].Root
& (Join-Path $PSScriptRoot 'Test-ServicedSbomTool.ps1') -ArtifactDirectory (Split-Path (Get-VerifiedFile $manifest.DependencyInventory)) `
    -ToolDirectory $servicedDirectory -ProvenancePath $servicedProvenance -BaselinePath $BaselinePath
if (-not $?) { throw 'Current SBOM tool security/provenance validation failed.' }

$auditedProjects = @{}
if (@($manifest.Audits).Count -eq 0) { throw 'Missing NuGet vulnerability audits.' }
foreach ($record in $manifest.Audits) {
    $audit = Get-Content -LiteralPath (Get-VerifiedFile $record) -Raw | ConvertFrom-Json
    if (-not $audit.PSObject.Properties['projects'] -or @($audit.projects).Count -eq 0) { throw 'Empty or failed NuGet audit.' }
    if ($audit.PSObject.Properties['logs'] -and @($audit.logs).Count -gt 0) { throw 'NuGet audit contains diagnostics. Review before qualification.' }
    foreach ($project in $audit.projects) {
        if ($project.PSObject.Properties['frameworks']) {
            foreach ($framework in $project.frameworks) {
                foreach ($kind in @('topLevelPackages', 'transitivePackages')) {
                    if ($framework.PSObject.Properties[$kind] -and @($framework.$kind).Count -gt 0) {
                        throw "NuGet audit reports vulnerable packages: $($project.path)"
                    }
                }
            }
        }
        $auditedProjects[$project.path] = $true
    }
}
if ($auditedProjects.Count -ne $manifest.ExpectedAuditProjects -or $auditedProjects.Count -lt 1) {
    throw 'NuGet audit project coverage differs from the declared repository scope.'
}

if (@($manifest.CompilerAssets).Count -eq 0) { throw 'Missing compiler restore evidence.' }
foreach ($record in $manifest.CompilerAssets) {
    $assets = Get-Content -LiteralPath (Get-VerifiedFile $record) -Raw | ConvertFrom-Json
    $compilers = @($assets.libraries.PSObject.Properties.Name | Where-Object { $_ -match '(^|\.)Microsoft\.DotNet\.ILCompiler/' })
    if ($compilers.Count -eq 0) { throw 'Compiler evidence has no resolved ILCompiler.' }
    foreach ($compiler in $compilers) {
        if ($compiler.Split('/')[-1] -ne $baseline.Compiler) { throw "Unapproved compiler input: $compiler" }
    }
    $audit = $assets.project.restore.restoreAuditProperties
    if ($audit.enableAudit -ne 'true' -or $audit.auditMode -ne 'all' -or $audit.auditLevel -ne 'low') {
        throw 'Compiler restore must retain all-package vulnerability auditing at low severity.'
    }
}

if (@($manifest.Artifacts).Count -eq 0) { throw 'No current artifacts to verify.' }
foreach ($artifact in $manifest.Artifacts) {
    if ($artifact.Classification -ne 'development-only' -or $artifact.Mode -notin @('framework-dependent', 'self-contained')) {
        throw 'This verifier qualifies development currency only. Official distribution needs the separate release contract.'
    }
    $root = Get-ContainedPath $repository $artifact.Root
    $expected = @{}
    if (@($artifact.Files).Count -eq 0 -or [string]::IsNullOrWhiteSpace($artifact.Project)) { throw 'Missing artifact file or project provenance.' }
    foreach ($file in $artifact.Files) {
        $path = Get-ContainedPath $root $file.Path
        if ($expected.ContainsKey($path)) { throw "Duplicate artifact file: $($file.Path)" }
        $expected[$path] = $true
        if ($file.SHA256 -notmatch '^[0-9A-Fa-f]{64}$' -or
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.SHA256) {
            throw "Artifact hash mismatch: $($artifact.Id)/$($file.Path)"
        }
    }
    $actual = @(Get-ChildItem -LiteralPath $root -File -Recurse -Force)
    if ($actual.Count -ne $expected.Count) { throw "Artifact file coverage differs: $($artifact.Id)" }
    foreach ($file in $actual) {
        [void](Get-ContainedPath $root $file.FullName.Substring($root.Length + 1))
        if (-not $expected.ContainsKey($file.FullName)) { throw "Unrecorded artifact file: $($file.Name)" }
        if ($file.Name -ieq 'libSkiaSharp.dll') {
            $nativeKey = $file.Name
            if ($file.FullName -match '[\\/]runtimes[\\/](win-[^\\/]+)[\\/]native[\\/]libSkiaSharp.dll$') {
                $nativeKey = 'runtimes/' + $matches[1] + '/native/libSkiaSharp.dll'
            }
            if (-not $baseline.NativeHashes.PSObject.Properties[$nativeKey] -or
                (Get-FileHash -LiteralPath $file.FullName).Hash -ne $baseline.NativeHashes.$nativeKey) {
                throw "Unapproved native binary: $($file.FullName)"
            }
        }
    }
    $configs = @($actual | Where-Object Name -like '*.runtimeconfig.json')
    if ($configs.Count -eq 0) { throw "No runtime configuration in artifact: $($artifact.Id)" }
    if ($artifact.Mode -eq 'self-contained' -and (-not $artifact.EntryRuntimeConfig -or
        -not $expected.ContainsKey((Join-Path $root $artifact.EntryRuntimeConfig)))) { throw 'Missing primary self-contained runtime configuration.' }
    foreach ($file in $configs) {
        $options = (Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json).runtimeOptions
        if ($options.tfm -ne $baseline.TargetFramework) { throw "Unapproved target framework: $($file.Name)" }
        $frameworks = @()
        foreach ($kind in @('framework', 'frameworks', 'includedFrameworks')) {
            if ($options.PSObject.Properties[$kind]) { $frameworks += @($options.$kind) }
        }
        if ($frameworks.Count -eq 0) { throw "Missing runtime requirement: $($file.Name)" }
        foreach ($framework in $frameworks) {
            if ([version]$framework.version -lt [version]$baseline.Runtime -or
                ([version]$framework.version).Major -ne 10) { throw "Insecure or incompatible runtime requirement: $($framework.version)" }
        }
        if ($options.PSObject.Properties['includedFrameworks']) {
            if ($artifact.Mode -ne 'self-contained') { throw 'Framework-dependent artifact unexpectedly embeds a runtime.' }
            foreach ($hash in $baseline.RuntimeHashes.PSObject.Properties) {
                $binary = Join-Path $file.DirectoryName $hash.Name
                if (-not (Test-Path -LiteralPath $binary -PathType Leaf) -or (Get-FileHash -LiteralPath $binary).Hash -ne $hash.Value) {
                    throw "Embedded runtime binary differs from reviewed pack: $($hash.Name)"
                }
            }
        } elseif ($artifact.Mode -eq 'self-contained' -and $file.FullName -eq (Join-Path $root $artifact.EntryRuntimeConfig)) {
            throw 'Self-contained runtime configuration is missing embedded framework identity.'
        }
    }
    Write-Output "Verified development artifact $($artifact.Id): $($actual.Count) files."
}
Write-Output "Currency verification passed for $(@($manifest.Artifacts).Count) artifacts and $($auditedProjects.Count) audited projects."
