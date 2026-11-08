[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$ArtifactDirectory)
$ErrorActionPreference = 'Stop'
$ArtifactDirectory = (Resolve-Path -LiteralPath $ArtifactDirectory).Path
$inventory = Get-Content -LiteralPath (Join-Path $ArtifactDirectory 'resolved-dependencies.json') -Raw | ConvertFrom-Json
if ($inventory.Projects.Count -eq 0 -or $inventory.Packages.Count -eq 0) { throw 'Empty dependency inventory.' }
$expected = @($inventory.Packages | ForEach-Object { "$($_.Id)/$($_.Version)" } | Sort-Object -Unique)
if ($expected.Count -ne $inventory.Packages.Count) { throw 'Duplicate package identity in inventory.' }
$review = Get-Content -LiteralPath (Join-Path $ArtifactDirectory 'license-review.json') -Raw | ConvertFrom-Json
$tool = Get-Content -LiteralPath (Join-Path $ArtifactDirectory 'generation-tool.json') -Raw | ConvertFrom-Json
if ($tool.Scope -ne 'historical-blocked-vendor-tool' -or $tool.CurrentExecutionPermitted -ne $false) {
    throw 'The vulnerable original SBOM bundle must remain blocked historical evidence.'
}
& (Join-Path $PSScriptRoot 'Test-ServicedSbomTool.ps1') -ArtifactDirectory $ArtifactDirectory
if (-not $?) { throw 'Current serviced SBOM producer evidence failed validation.' }
$reviewIds = @($review.Components.Identity | Sort-Object -Unique)
if ($reviewIds.Count -ne $review.Components.Count) { throw 'Duplicate license-review identity.' }
$toolIds = @($tool.BundledDependencies.Dependencies.Identity | Sort-Object -Unique)
$reviewedPackages = @($inventory.Packages | Where-Object { $_.PackagedMetadataFindings.Count -gt 0 } | ForEach-Object { "$($_.Id)/$($_.Version)" })
if (Compare-Object @(@($toolIds + $reviewedPackages) | Sort-Object -Unique) $reviewIds) { throw 'License-review identity coverage mismatch.' }
foreach ($component in $review.Components) {
    if ($component.Status -notin @('verified-evidence','owner-confirmed-non-revenue-use') -or
        -not $component.ReviewedLicense -or -not $component.Verification -or -not $component.Obligations -or
        $component.ArchiveSha512 -notmatch '^[a-f0-9]{128}$' -or $component.LegalTexts.Count -eq 0) { throw 'Incomplete license-review evidence.' }
    if ($component.Status -eq 'owner-confirmed-non-revenue-use' -and
        ($component.Identity -notin @('Json.More.Net/3.0.0','JsonPointer.Net/7.0.0','JsonSchema.Net/9.1.1') -or -not $review.OwnerDecision)) { throw 'Unsupported owner-decision classification.' }
    foreach ($evidence in @($component.ManifestEvidence) + @($component.LegalTexts)) {
        if ($evidence.Artifact -notmatch '^(licenses|evidence)/[a-f0-9]{64}\.txt$' -or
            $evidence.Artifact -notlike "*/$($evidence.Sha256).txt") { throw 'Invalid reviewed evidence path/hash.' }
        $uri = $null
        if (-not [Uri]::TryCreate($evidence.Source, [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -ne 'https' -or $uri.UserInfo) { throw 'Invalid reviewed evidence source.' }
        if ((Get-FileHash -LiteralPath (Join-Path $ArtifactDirectory $evidence.Artifact) -Algorithm SHA256).Hash -ine $evidence.Sha256) { throw 'Reviewed evidence hash mismatch.' }
    }
    $manifest = @($component.ManifestEvidence | Where-Object Kind -eq 'package-manifest')
    if ($manifest.Count -ne 1) { throw 'Missing or ambiguous reviewed package manifest.' }
    $manifestSettings = New-Object Xml.XmlReaderSettings
    $manifestSettings.DtdProcessing = [Xml.DtdProcessing]::Prohibit; $manifestSettings.XmlResolver = $null
    $manifestSettings.MaxCharactersInDocument = 16777216
    $manifestReader = [Xml.XmlReader]::Create((Join-Path $ArtifactDirectory $manifest[0].Artifact), $manifestSettings)
    try { $manifestXml = New-Object Xml.XmlDocument; $manifestXml.XmlResolver=$null; $manifestXml.Load($manifestReader) }
    finally { $manifestReader.Dispose() }
    $metadata = $manifestXml.package.metadata
    if ("$($metadata.id)/$($metadata.version)" -ine $component.Identity -or
        [string]$metadata.license.InnerText -cne $component.DeclaredLicense) { throw 'Reviewed manifest identity/license mismatch.' }
    $package = @($inventory.Packages | Where-Object { "$($_.Id)/$($_.Version)" -ieq $component.Identity })
    if ($package.Count -eq 1) {
        $archiveHash = ([BitConverter]::ToString([Convert]::FromBase64String($package[0].ArchiveSha512Base64))).Replace('-', '')
        if ($archiveHash -ine $component.ArchiveSha512 -or $package[0].NuspecSha256 -ine $manifest[0].Sha256) { throw 'Reviewed package/archive correspondence mismatch.' }
    }
}
$generator = @($review.Components | Where-Object Identity -eq 'CycloneDX/6.3.1')
if ($generator.Count -ne 1 -or $generator[0].ArchiveSha512 -ine $tool.ArchiveSha512Hex) { throw 'Reviewed generator archive mismatch.' }
$dlls = @($tool.BundledFiles | Where-Object { $_.Name.EndsWith('.dll') })
if ($dlls.Count -ne $review.BundledBinaryCorrespondence.Count) { throw 'Bundled binary review coverage mismatch.' }
foreach ($dll in $dlls) {
    $match = @($review.BundledBinaryCorrespondence | Where-Object File -eq $dll.Name)
    # Satellite resources sit in culture subdirectories of the same tool target.
    $graphName = (($dll.Name.Split('/')[0..2] -join '/') + '/CycloneDX.deps.json')
    $graph = @($tool.BundledDependencies | Where-Object Graph -eq $graphName)
    $origins = @($match.ArchiveMatches | Where-Object {
        $_.Identity -in $graph.Dependencies.Identity -and
        ($_.Identity -ne 'CycloneDX/6.3.1' -or $dll.Name.EndsWith('/CycloneDX.dll'))
    })
    if ($match.Count -ne 1 -or $match[0].Sha256 -ine $dll.Sha256 -or $graph.Count -ne 1 -or $origins.Count -eq 0) { throw "Bundled binary origin/hash mismatch: $($dll.Name)" }
}
foreach ($package in $inventory.Packages) {
    if (-not $package.Source -or -not $package.RestoreContentHashSha512 -or -not $package.ArchiveSha512Base64) { throw 'Missing package provenance/hash.' }
    foreach ($notice in $package.NoticeFiles) {
        if ($notice.Artifact -notmatch '^licenses/[a-f0-9]{64}\.txt$') { throw 'Invalid notice artifact path.' }
        $file = Join-Path $ArtifactDirectory $notice.Artifact
        if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ine $notice.Sha256) { throw 'Notice hash mismatch.' }
    }
}
$schemaDirectory = Join-Path $PSScriptRoot 'schema'
$schemaManifest = Get-Content -LiteralPath (Join-Path $schemaDirectory 'provenance.json') -Raw | ConvertFrom-Json
foreach ($schema in $schemaManifest) {
    if ($schema.File -notin @('bom-1.6.xsd','spdx.xsd','LICENSE.txt')) { throw 'Unexpected schema file.' }
    if ((Get-FileHash -LiteralPath (Join-Path $schemaDirectory $schema.File) -Algorithm SHA256).Hash -ine $schema.Sha256) { throw 'Schema provenance hash mismatch.' }
}
$schemas = New-Object Xml.Schema.XmlSchemaSet
$schemas.XmlResolver = $null
[void]$schemas.Add('http://cyclonedx.org/schema/spdx', (Join-Path $schemaDirectory 'spdx.xsd'))
[void]$schemas.Add('http://cyclonedx.org/schema/bom/1.6', (Join-Path $schemaDirectory 'bom-1.6.xsd'))
$schemas.Compile()
$union = @()
foreach ($name in @('solution','engine','sharpfuzz')) {
    $path = Join-Path $ArtifactDirectory ($name + '.cdx.xml')
    $settings = New-Object Xml.XmlReaderSettings
    $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $settings.MaxCharactersInDocument = 16777216
    $settings.Schemas = $schemas
    $settings.ValidationType = [Xml.ValidationType]::Schema
    $reader = [Xml.XmlReader]::Create($path, $settings)
    try { $document = New-Object Xml.XmlDocument; $document.XmlResolver = $null; $document.Load($reader) }
    finally { $reader.Dispose() }
    $components = @($document.bom.components.component)
    foreach ($component in $components) {
        $record = @($inventory.Packages | Where-Object { $_.Id -ieq $component.name -and $_.Version -eq $component.version })
        if ($record.Count -ne 1) { throw 'SBOM package does not match inventory.' }
        $expectedHash = ([BitConverter]::ToString([Convert]::FromBase64String($record[0].ArchiveSha512Base64))).Replace('-', '')
        $hash = @($component.hashes.hash | Where-Object alg -eq 'SHA-512')
        if ($hash.Count -ne 1 -or $hash[0].InnerText -ine $expectedHash) { throw 'SBOM archive hash mismatch.' }
    }
    $identities = @($components | ForEach-Object { "$($_.name)/$($_.version)" } | Sort-Object -Unique)
    foreach ($identity in $identities) { if ($identity -notin $expected) { throw "SBOM contains unrecognized package: $identity" } }
    if ($identities.Count -ne $components.Count) { throw 'Duplicate SBOM package.' }
    if ($name -eq 'engine') {
        $production = @($inventory.Packages | Where-Object { $_.Uses.Scope -contains 'production' } | ForEach-Object { "$($_.Id)/$($_.Version)" } | Sort-Object)
        if (Compare-Object $production $identities) { throw 'Engine SBOM does not match resolved production packages.' }
    }
    $union += $identities
    Write-Host "Validated $name CycloneDX 1.6: $($components.Count) packages."
}
if (Compare-Object $expected @($union | Sort-Object -Unique)) { throw 'SBOM union does not match repository resolution.' }
foreach ($file in @(Get-ChildItem -LiteralPath $ArtifactDirectory -File | Where-Object Extension -In @('.json','.xml'))) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    if ($text -match '(?i)(?<![a-z0-9])[a-z]:[\\/]|file://|/Users/|/home/|https?://[^/\s"<>]*@|[?&](token|password|apikey)=') { throw 'Private path or credential-like value in dependency artifact.' }
}
Write-Host 'Package/notice correspondence, schema validation and private-path checks passed.'
Write-Host "Reviewed $($review.Components.Count) component identities and $($dlls.Count) bundled DLL origins; evidence hashes match."
Write-Host "Packages requiring provenance/license review: $(@($inventory.Packages | Where-Object { $_.ReviewIssues.Count -gt 0 }).Count). Validation is not legal approval."
