[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ArtifactDirectory,
    [string]$ToolDirectory,
    [string]$ProvenancePath,
    [string]$BaselinePath
)
$ErrorActionPreference = 'Stop'
if (-not $BaselinePath) { $BaselinePath = Join-Path $PSScriptRoot 'artifact-security-baseline.json' }
$security = Get-Content $BaselinePath -Raw | ConvertFrom-Json
$baseline = $security.SbomTool
if (-not $ProvenancePath) { $ProvenancePath = Join-Path $ArtifactDirectory 'generation-tool-serviced.json' }
$producer = Get-Content -LiteralPath $ProvenancePath -Raw | ConvertFrom-Json
$review = Get-Content (Join-Path $ArtifactDirectory 'serviced-tool-review.json') -Raw | ConvertFrom-Json
$original = Get-Content (Join-Path $ArtifactDirectory 'license-review.json') -Raw | ConvertFrom-Json
if ($producer.Format -ne 'PiratePDF source-serviced SBOM tool v1' -or $producer.Id -ne $baseline.Id -or
    $producer.Version -ne $baseline.Version -or $producer.Commit -ne $baseline.UpstreamCommit -or
    $producer.SourceArchiveSHA512 -ne $baseline.SourceArchiveSHA512 -or
    $producer.PatchedPropertiesSHA256 -ne $baseline.PatchedPropertiesSHA256 -or
    $producer.Pkcs -ne $baseline.Pkcs -or $producer.SDK -ne $security.SDK -or $producer.Runtime -ne $security.Runtime -or -not $producer.ReproducibleRuntimeFiles -or
    $producer.AuditReportedVulnerabilities -ne 0 -or $producer.Scope -ne 'development-only') {
    throw 'Serviced SBOM producer differs from the approved source/security baseline.'
}
if ((Get-FileHash (Join-Path $ArtifactDirectory 'license-review.json')).Hash -ne $review.OriginalReviewSHA256 -or
    $review.NewComponent.Identity -ne ('System.Security.Cryptography.Pkcs/' + $baseline.Pkcs) -or
    $review.NewComponent.ArchiveSha512 -ne $baseline.PkcsArchiveSHA512) {
    throw 'Serviced-tool legal review does not match its original and patched component evidence.'
}
if ($producer.LegalFiles.Count -ne 3 -or
    ($producer.LegalFiles | Where-Object Name -eq 'LICENSE').Sha256 -ne '6c29f22a4a7385285c6f579ec9f33c5e989f00739d6b257243a0b082ec9447ae' -or
    ($producer.LegalFiles | Where-Object Name -eq 'NOTICE').Sha256 -ne '35d78ed4a70916bf503ef5cd2ca1fd1115b1631545239902988d40195b6ab558') {
    throw 'Serviced upstream license/NOTICE or modification notice evidence is missing.'
}
if (Compare-Object @('LICENSE','NOTICE','SERVICING-NOTICE.txt') @($producer.LegalFiles.Name | Sort-Object -Unique)) {
    throw 'Serviced legal-file coverage is incomplete or duplicated.'
}
foreach ($legal in $producer.LegalFiles) {
    if ($legal.Name -notin @('LICENSE','NOTICE','SERVICING-NOTICE.txt') -or
        ($ToolDirectory -and (Get-FileHash (Join-Path $ToolDirectory $legal.Name)).Hash -ine $legal.Sha256)) {
        throw 'Serviced tool legal/attribution bytes differ.'
    }
}
foreach ($evidence in @($review.NewComponent.ManifestEvidence) + @($review.NewComponent.LegalTexts)) {
    if ($evidence.Artifact -notmatch '^(licenses|evidence)/[a-f0-9]{64}\.txt$' -or
        (Get-FileHash (Join-Path $ArtifactDirectory $evidence.Artifact)).Hash -ine $evidence.Sha256) {
        throw 'Serviced-tool legal/manifest evidence hash differs.'
    }
}
$expected = @($original.Components | Where-Object Identity -in $producer.Packages | ForEach-Object Identity)
$expected += @($review.NewComponent.Identity)
if (Compare-Object @($expected | Sort-Object -Unique) @($producer.Packages | Sort-Object -Unique)) {
    throw 'Serviced-tool package graph lacks exact reviewed identity coverage.'
}
$vendor = Get-Content (Join-Path $PSScriptRoot '../../docs/dependencies/history/b12-20261031/generation-tool.json') -Raw | ConvertFrom-Json
$approvedPackages = @($vendor.BundledDependencies | Where-Object Graph -eq 'tools/net10.0/any/CycloneDX.deps.json' |
    ForEach-Object Dependencies | Where-Object Type -eq 'package' | ForEach-Object { $_.Identity.Replace('System.Security.Cryptography.Pkcs/8.0.1', 'System.Security.Cryptography.Pkcs/' + $baseline.Pkcs) } | Sort-Object)
if (Compare-Object $approvedPackages @($producer.Packages | Sort-Object)) { throw 'Unapproved serviced-tool dependency change.' }
$files = @($producer.Files)
if ($files.Count -eq 0 -or @($files | Where-Object Name -eq 'CycloneDX.dll').Count -ne 1) { throw 'Missing serviced-tool file coverage.' }
$seen = @{}
foreach ($file in $files) {
    if ($file.Name -match '(^|/)\.\.(/|$)|[:\\]|^/' -or $seen.ContainsKey($file.Name) -or $file.Sha256 -notmatch '^[a-f0-9]{64}$') {
        throw 'Invalid or duplicate serviced-tool file record.'
    }
    $seen[$file.Name] = $true
    if ($ToolDirectory -and (Get-FileHash -LiteralPath (Join-Path $ToolDirectory $file.Name)).Hash -ine $file.Sha256) {
        throw "Serviced-tool binary hash differs: $($file.Name)"
    }
    if ($file.Name.EndsWith('.dll')) {
        if ($file.Name -eq 'CycloneDX.dll') {
            if ($file.Sha256 -ne $baseline.MainDllSHA256) { throw 'Serviced source-built main DLL differs from the reviewed deterministic build.' }
        } elseif ($file.Name.EndsWith('System.Security.Cryptography.Pkcs.dll')) {
            if ($file.Sha256 -ne $baseline.PkcsWindowsDllSHA256 -or $file.Sha256 -notin $review.PkcsBinaryHashes.SHA256) { throw 'Patched Pkcs DLL does not match its exact reviewed archive.' }
        } else {
            $matches = @($original.BundledBinaryCorrespondence | Where-Object Sha256 -eq $file.Sha256 |
                ForEach-Object ArchiveMatches | Where-Object Identity -in $producer.Packages)
            if ($matches.Count -eq 0) { throw "Serviced-tool DLL has no reviewed package origin: $($file.Name)" }
        }
    }
}
if ($ToolDirectory) {
    $runtimeFiles = @(Get-ChildItem $ToolDirectory -Recurse -File | Where-Object {
        $_.Extension -in @('.dll', '.json', '.exe') -and $_.Name -ne 'serviced-tool-provenance.json'
    })
    if ($runtimeFiles.Count -ne $files.Count) { throw 'Serviced-tool runtime file coverage differs.' }
    $runtime = (Get-Content (Join-Path $ToolDirectory 'CycloneDX.runtimeconfig.json') -Raw | ConvertFrom-Json).runtimeOptions
    if ($runtime.framework.version -ne $security.Runtime -or $runtime.rollForward -ne 'LatestPatch') { throw 'Serviced-tool secure runtime floor differs.' }
}
Write-Host "Verified serviced SBOM producer $($producer.Version), $($producer.Packages.Count) package identities and $($files.Count) runtime files."
