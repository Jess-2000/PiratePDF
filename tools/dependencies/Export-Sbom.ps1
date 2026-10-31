[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$CycloneDxPath,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$CycloneDxPath = (Resolve-Path -LiteralPath $CycloneDxPath).Path
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$version = & $CycloneDxPath --version
if ($LASTEXITCODE -ne 0 -or $version -notmatch '^6\.3\.1(?:\+|$)') { throw 'CycloneDX 6.3.1 is required.' }
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$jobs = @(
    @{Project='PiratePDF.slnx'; Name='solution'; Recurse=$false},
    @{Project='src/BlackFlag.Engine.Rendering.Skia/BlackFlag.Engine.Rendering.Skia.csproj'; Name='engine'; Recurse=$true},
    @{Project='tools/PiratePDF.Fuzz.SharpFuzz/PiratePDF.Fuzz.SharpFuzz.csproj'; Name='sharpfuzz'; Recurse=$true}
)
foreach ($job in $jobs) {
    $arguments = @((Join-Path $root $job.Project), '--disable-package-restore', '--no-serial-number',
        '--spec-version', '1.6', '--output-format', 'Xml', '--set-name', ('PiratePDF-' + $job.Name),
        '--set-type', 'Library', '--set-version', 'working-tree', '-o', $OutputDirectory, '--filename', ($job.Name + '.cdx.xml'))
    if ($job.Recurse) { $arguments += '--recursive' }
    & $CycloneDxPath @arguments
    if ($LASTEXITCODE -ne 0) { throw "CycloneDX generation failed for $($job.Name)." }
    $path = Join-Path $OutputDirectory ($job.Name + '.cdx.xml')
    $settings = New-Object Xml.XmlReaderSettings
    $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $settings.MaxCharactersInDocument = 16777216
    $reader = [Xml.XmlReader]::Create($path, $settings)
    try { $document = New-Object Xml.XmlDocument; $document.XmlResolver = $null; $document.Load($reader) }
    finally { $reader.Dispose() }
    $ns = New-Object Xml.XmlNamespaceManager($document.NameTable)
    $ns.AddNamespace('b', 'http://cyclonedx.org/schema/bom/1.6')
    # Remove only generation time, not package identity/hash/license/graph evidence.
    foreach ($node in @($document.SelectNodes('/b:bom/b:metadata/b:timestamp', $ns))) { [void]$node.ParentNode.RemoveChild($node) }
    $writerSettings = New-Object Xml.XmlWriterSettings
    $writerSettings.Encoding = New-Object Text.UTF8Encoding($false)
    $writerSettings.Indent = $true
    $writerSettings.NewLineChars = "`n"
    $writer = [Xml.XmlWriter]::Create($path, $writerSettings)
    try { $document.Save($writer) } finally { $writer.Dispose() }
}

# The approved generator is a development tool, not a runtime package. Record
# its shipped dependency graph and files without claiming per-library licenses.
$store = Join-Path (Split-Path $CycloneDxPath) '.store/cyclonedx/6.3.1/cyclonedx/6.3.1'
$spec = Join-Path $store 'CycloneDX.nuspec'
if (-not (Test-Path -LiteralPath $spec)) { throw 'Pinned tool store metadata missing.' }
$metadata = Get-Content -LiteralPath (Join-Path $store '.nupkg.metadata') -Raw | ConvertFrom-Json
$depsPaths = @(Get-ChildItem -LiteralPath (Join-Path $store 'tools') -Recurse -File -Filter 'CycloneDX.deps.json')
if ($depsPaths.Count -eq 0) { throw 'Tool dependency graph missing.' }
$bundled = @()
$review = Get-Content -LiteralPath (Join-Path $OutputDirectory 'license-review.json') -Raw | ConvertFrom-Json
foreach ($depsPath in @($depsPaths | Sort-Object FullName)) {
    $deps = Get-Content -LiteralPath $depsPath.FullName -Raw | ConvertFrom-Json
    $target = $depsPath.FullName.Substring($store.Length + 1).Replace('\', '/')
    $bundled += [ordered]@{Graph=$target;Dependencies=@($deps.libraries.PSObject.Properties | Sort-Object Name | ForEach-Object {
        $identity = $_.Name
        $component = @($review.Components | Where-Object Identity -EQ $identity)
        if ($component.Count -ne 1) { throw "Missing bundled-component review: $identity" }
        [ordered]@{Identity=$identity;Type=$_.Value.type;Provenance=('license-review.json#' + $identity);ReviewStatus=$component[0].Status}
    })}
}
$files = @(Get-ChildItem -LiteralPath (Join-Path $store 'tools') -Recurse -File |
    Where-Object Extension -In @('.dll', '.json') | Sort-Object FullName | ForEach-Object {
    [ordered]@{Name=$_.FullName.Substring($store.Length + 1).Replace('\', '/');Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
})
$tool = [ordered]@{
    Id='CycloneDX';Version='6.3.1';Scope='development';Source='https://api.nuget.org/v3/index.json'
    Approval='Owner approval in B12 conversation, 2026-10-31';DeclaredLicense='Apache-2.0'
    Upstream='https://github.com/CycloneDX/cyclonedx-dotnet';Commit='6938a02734b0c30e1a59976f5bd592f1ee8be1eb'
    RestoreContentHashSha512=$metadata.contentHash
    ArchiveSha512Hex=(Get-FileHash -LiteralPath (Join-Path $store 'cyclonedx.6.3.1.nupkg') -Algorithm SHA512).Hash.ToLowerInvariant()
    BundledDependencies=$bundled;BundledFiles=$files
    FileCoverage='Bundled runtime DLL/config files; optional XML documentation and PDBs excluded. Archive hash covers the complete package.'
    LicenseEvidence='license-review.json; retained version-specific upstream and package legal texts'
    OwnerDecision=$review.OwnerDecision
    Limitations=@('Package declaration is unchanged; separately reviewed bundled licenses and binary correspondence are recorded in license-review.json.',
        'Evidence and owner-confirmed use do not constitute a general legal-compliance opinion.', 'Tool is not part of the engine or a desktop distribution.')
}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'generation-tool.json'),
    (($tool | ConvertTo-Json -Depth 15) -replace "`r`n", "`n") + "`n", (New-Object Text.UTF8Encoding($false)))
& (Join-Path $PSScriptRoot 'Test-DependencyArtifacts.ps1') -ArtifactDirectory $OutputDirectory
if (-not $?) { throw 'SBOM validation failed.' }
