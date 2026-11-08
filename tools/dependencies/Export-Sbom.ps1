[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$CycloneDxPath,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$CycloneDxPath = (Resolve-Path -LiteralPath $CycloneDxPath).Path
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$toolDirectory = Split-Path $CycloneDxPath
$provenance = Join-Path $toolDirectory 'serviced-tool-provenance.json'
if (-not (Test-Path -LiteralPath $provenance -PathType Leaf)) {
    throw 'Original or unknown SBOM producer is blocked. Build the reviewed source-serviced tool first.'
}
# Refuse the vulnerable vendor bundle before executing it. Only the reviewed source-serviced variant is admitted.
& (Join-Path $PSScriptRoot 'Test-ServicedSbomTool.ps1') -ArtifactDirectory $OutputDirectory -ToolDirectory $toolDirectory -ProvenancePath $provenance
if (-not $?) { throw 'Serviced SBOM producer validation failed.' }
$version = & $CycloneDxPath --version
if ($LASTEXITCODE -ne 0 -or $version -notmatch '^6\.3\.1-piratepdf\.b14b\.1(?:\+|$)') { throw 'The reviewed source-serviced CycloneDX variant is required.' }
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

# Preserve the original vendor bundle as reviewed, explicitly blocked historical evidence.
$vendorPath = Join-Path $root 'docs/dependencies/history/b12-20261031/generation-tool.json'
$vendor = Get-Content -LiteralPath $vendorPath -Raw | ConvertFrom-Json
$vendor.Scope = 'historical-blocked-vendor-tool'
$vendor | Add-Member NoteProperty BlockedAdvisories @('GHSA-846f-f764-76mx','GHSA-2fmv-rv2c-59x7')
$vendor | Add-Member NoteProperty CurrentExecutionPermitted $false
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'generation-tool.json'),
    (($vendor | ConvertTo-Json -Depth 15) -replace "`r`n", "`n") + "`n", (New-Object Text.UTF8Encoding($false)))
[IO.File]::Copy($provenance, (Join-Path $OutputDirectory 'generation-tool-serviced.json'), $true)
& (Join-Path $PSScriptRoot 'Test-DependencyArtifacts.ps1') -ArtifactDirectory $OutputDirectory
if (-not $?) { throw 'SBOM validation failed.' }
