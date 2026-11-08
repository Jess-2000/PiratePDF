[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
if (-not $RepositoryRoot) { $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path }
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
[void][IO.Directory]::CreateDirectory($OutputDirectory)
[void][IO.Directory]::CreateDirectory((Join-Path $OutputDirectory 'licenses'))
$utf8 = New-Object Text.UTF8Encoding($false)
Add-Type -AssemblyName System.IO.Compression.FileSystem
$reviewRoot = Join-Path $RepositoryRoot 'docs/dependencies'
$review = Get-Content -LiteralPath (Join-Path $reviewRoot 'license-review.json') -Raw | ConvertFrom-Json
$servicedReview = Get-Content -LiteralPath (Join-Path $reviewRoot 'serviced-tool-review.json') -Raw | ConvertFrom-Json
# Retained upstream evidence is an input, never fetched from moving URLs in CI.
foreach ($evidence in @(@($review.Components) + @($servicedReview.NewComponent) | ForEach-Object { $_.ManifestEvidence; $_.LegalTexts })) {
    if ($evidence.Artifact -notmatch '^(licenses|evidence)/[a-f0-9]{64}\.txt$') { throw 'Invalid reviewed evidence path.' }
    $source = Join-Path $reviewRoot $evidence.Artifact
    if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ine $evidence.Sha256) { throw 'Reviewed evidence hash mismatch.' }
    $destination = Join-Path $OutputDirectory $evidence.Artifact
    [void][IO.Directory]::CreateDirectory((Split-Path $destination))
    if ($source -ine $destination) { [IO.File]::Copy($source, $destination, $true) }
}
if ($reviewRoot -ine $OutputDirectory) { [IO.File]::Copy((Join-Path $reviewRoot 'license-review.json'), (Join-Path $OutputDirectory 'license-review.json'), $true) }
if ($reviewRoot -ine $OutputDirectory) { [IO.File]::Copy((Join-Path $reviewRoot 'serviced-tool-review.json'), (Join-Path $OutputDirectory 'serviced-tool-review.json'), $true) }

function Write-Json($Path, $Value) {
    [IO.File]::WriteAllText($Path, (($Value | ConvertTo-Json -Depth 35) -replace "`r`n", "`n") + "`n", $utf8)
}

function Read-PackageXml($Path) {
    $settings = New-Object Xml.XmlReaderSettings
    $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $settings.MaxCharactersInDocument = 16777216
    $reader = [Xml.XmlReader]::Create($Path, $settings)
    try { $document = New-Object Xml.XmlDocument; $document.XmlResolver = $null; $document.Load($reader); return ,$document }
    finally { $reader.Dispose() }
}

function Public-Url($Value) {
    if (-not $Value) { return $null }
    $uri = $null
    if (-not [Uri]::TryCreate([string]$Value, [UriKind]::Absolute, [ref]$uri) -or
        $uri.Scheme -notin @('https', 'http') -or $uri.UserInfo -or
        ($uri.Query -and $uri.Query -notmatch '^\?(?i:linkid|id)=[0-9]+$') -or $uri.Fragment) {
        throw 'Package metadata contains a nonpublic or credential-bearing URL; review before exporting.'
    }
    return $uri.AbsoluteUri
}

function Package-File($Root, $Relative) {
    $full = [IO.Path]::GetFullPath((Join-Path $Root $Relative))
    $prefix = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $full.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Package file escapes package root.' }
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { throw "Missing package file: $Relative" }
    return $full
}

function Archive-Sha512($Path) {
    $stream = [IO.File]::OpenRead($Path)
    $algorithm = [Security.Cryptography.SHA512]::Create()
    try { return [Convert]::ToBase64String($algorithm.ComputeHash($stream)) }
    finally { $algorithm.Dispose(); $stream.Dispose() }
}

function Assert-ArchivedFile($Archive, $Relative, $ExpectedSha256) {
    $zip = [IO.Compression.ZipFile]::OpenRead($Archive)
    try {
        $entries = @($zip.Entries | Where-Object FullName -IEQ $Relative.Replace('\', '/'))
        if ($entries.Count -ne 1) { throw "Missing or ambiguous archived file: $Relative" }
        $entry = $entries[0]
        $stream = $entry.Open()
        $algorithm = [Security.Cryptography.SHA256]::Create()
        try { $hash = ([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-', '').ToLowerInvariant() }
        finally { $algorithm.Dispose(); $stream.Dispose() }
        if ($hash -ne $ExpectedSha256) { throw "Package cache/archive file mismatch: $Relative" }
    }
    finally { $zip.Dispose() }
}

$projects = @(& git -C $RepositoryRoot ls-files --cached --others --exclude-standard -- '*.csproj' | Sort-Object -Unique)
if ($LASTEXITCODE -ne 0 -or $projects.Count -eq 0) { throw 'Project inventory failed.' }
$packages = @{}
$projectRecords = @()
$relationships = @{}
foreach ($project in $projects) {
    $project = $project.Replace('\', '/')
    $assetsPath = Join-Path $RepositoryRoot ((Split-Path $project) + '/obj/project.assets.json')
    if (-not (Test-Path -LiteralPath $assetsPath)) { throw "Restore assets missing: $project" }
    $assets = Get-Content -LiteralPath $assetsPath -Raw | ConvertFrom-Json
    $scope = if ($project.StartsWith('src/', [StringComparison]::Ordinal)) { 'production' } else { 'development' }
    $projectPackages = @()
    foreach ($library in @($assets.libraries.PSObject.Properties | Sort-Object Name)) {
        if ($library.Value.type -ne 'package') { continue }
        $identity = $library.Name
        $projectPackages += $identity
        if (-not $packages.ContainsKey($identity)) {
            $root = $null
            foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
                $candidate = Join-Path $folder $library.Value.path
                if (Test-Path -LiteralPath (Join-Path $candidate '.nupkg.metadata')) { $root = $candidate; break }
            }
            if (-not $root) { throw "Package cache missing: $identity" }
            $metadata = Get-Content -LiteralPath (Join-Path $root '.nupkg.metadata') -Raw | ConvertFrom-Json
            if ($metadata.contentHash -ne $library.Value.sha512) { throw "Restore content hash mismatch: $identity" }
            $specs = @(Get-ChildItem -LiteralPath $root -Filter '*.nuspec' -File)
            if ($specs.Count -ne 1) { throw "Ambiguous package manifest: $identity" }
            $manifest = (Read-PackageXml $specs[0].FullName).package.metadata
            if ("$($manifest.id)/$($manifest.version)" -ine $identity) { throw "Package identity mismatch: $identity" }
            $archive = Package-File $root ($library.Value.path.Split('/')[-2] + '.' + $library.Value.path.Split('/')[-1] + '.nupkg')
            $archiveHash = Archive-Sha512 $archive
            if ($archiveHash -ne (Get-Content -LiteralPath ($archive + '.sha512') -Raw).Trim()) { throw "Archive hash mismatch: $identity" }
            $specHash = (Get-FileHash -LiteralPath $specs[0].FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            Assert-ArchivedFile $archive $specs[0].Name $specHash
            $notices = @()
            foreach ($relative in @($library.Value.files | Where-Object {
                [IO.Path]::GetFileName($_) -match '(?i)^(LICENSE|NOTICE|COPYING|THIRD[-_. ]?PARTY)' -or
                ($manifest.license -and $manifest.license.type -eq 'file' -and $_ -eq $manifest.license.InnerText)
            } | Sort-Object -Unique)) {
                $file = Package-File $root $relative
                $hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
                Assert-ArchivedFile $archive $relative $hash
                $destination = 'licenses/' + $hash + '.txt'
                [IO.File]::Copy($file, (Join-Path $OutputDirectory $destination), $true)
                $notices += [ordered]@{ PackagePath=$relative.Replace('\', '/'); Sha256=$hash; Artifact=$destination }
            }
            $issues = @()
            if (-not $manifest.license) { $issues += 'No declared license expression/file; legacy URL needs version-specific review.' }
            if ($notices.Count -eq 0) { $issues += 'No license/notice text included in package; attribution text needs upstream review.' }
            if (-not $manifest.repository.url) { $issues += 'No repository URL declared in package manifest.' }
            $packages[$identity] = [ordered]@{
                Id=[string]$manifest.id; Version=[string]$manifest.version
                Source=(Public-Url $metadata.source); RestoreContentHashSha512=[string]$library.Value.sha512
                ArchiveSha512Base64=$archiveHash; NuspecSha256=$specHash
                LicenseType=[string]$manifest.license.type; LicenseDeclaration=[string]$manifest.license.InnerText
                LicenseUrl=(Public-Url $manifest.licenseUrl); ProjectUrl=(Public-Url $manifest.projectUrl)
                RepositoryUrl=(Public-Url $manifest.repository.url); RepositoryCommit=[string]$manifest.repository.commit
                Authors=[string]$manifest.authors; Copyright=[string]$manifest.copyright
                NoticeFiles=$notices; ReviewIssues=$issues; Uses=@(); NativeAssets=@(); CacheRoot=$root; ArchivePath=$archive
            }
        }
        elseif ($packages[$identity].RestoreContentHashSha512 -ne $library.Value.sha512) { throw "Inconsistent restore hash: $identity" }
        foreach ($target in $assets.targets.PSObject.Properties) {
            $selected = $target.Value.PSObject.Properties[$identity].Value
            if (-not $selected) { continue }
            $framework = $target.Name.Split('/')[0]
            $direct = $assets.project.frameworks.PSObject.Properties[$framework].Value.dependencies.PSObject.Properties.Name -icontains $packages[$identity].Id
            $packages[$identity].Uses += [ordered]@{ Project=$project; Target=$target.Name; Scope=$scope; Reference= $(if($direct){'direct'}else{'transitive'}) }
            foreach ($dependency in $selected.dependencies.PSObject.Properties) {
                $resolved = @($target.Value.PSObject.Properties | Where-Object { $_.Value.type -eq 'package' -and $_.Name.Split('/')[0] -ieq $dependency.Name })
                if ($resolved.Count -ne 1) { continue } # Project references are first-party, not NuGet packages.
                $edge = "$identity>$($resolved[0].Name)"
                $relationships[$edge] = [ordered]@{ From=$identity; To=$resolved[0].Name }
            }
            foreach ($asset in $selected.runtimeTargets.PSObject.Properties) {
                if ($asset.Value.assetType -ne 'native' -or $asset.Name.EndsWith('/_._', [StringComparison]::Ordinal)) { continue }
                if (@($packages[$identity].NativeAssets | Where-Object PackagePath -eq $asset.Name).Count -gt 0) { continue }
                $file = Package-File $packages[$identity].CacheRoot $asset.Name
                $hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
                Assert-ArchivedFile $packages[$identity].ArchivePath $asset.Name $hash
                $packages[$identity].NativeAssets += [ordered]@{
                    PackagePath=$asset.Name; Rid=$asset.Value.rid; Size=(Get-Item -LiteralPath $file).Length
                    Sha256=$hash
                    WindowsX64=($asset.Value.rid -eq 'win-x64'); DistributionQualified=$false
                }
            }
        }
    }
    $projectRecords += [ordered]@{ Path=$project; Scope=$scope; Packages=@($projectPackages | Sort-Object -Unique) }
}

$packageRecords = @()
$noticeLines = @('PiratePDF / BlackFlag Engine - THIRD-PARTY NOTICES', '',
    'Generated from restored NuGet package metadata and packaged notice files.',
    'Scope: repository engine, tests and tools; NOT a desktop distribution.',
    'Declarations and included texts are evidence, not legal-compliance approval.',
    'Review issues must be resolved before redistributing affected components.', '')
foreach ($key in @($packages.Keys | Sort-Object)) {
    $record = $packages[$key]
    $record.Remove('CacheRoot')
    $record.Remove('ArchivePath')
    $record.Uses = @($record.Uses | Sort-Object { $_.Project }, { $_.Target })
    $record.NativeAssets = @($record.NativeAssets | Sort-Object { $_.PackagePath })
    $reviewed = @($review.Components | Where-Object Identity -IEQ $key)
    if ($reviewed.Count -gt 1) { throw 'Duplicate reviewed identity.' }
    if ($reviewed.Count -eq 1) {
        $archiveHex = ([BitConverter]::ToString([Convert]::FromBase64String($record.ArchiveSha512Base64))).Replace('-', '').ToLowerInvariant()
        if ($archiveHex -ne $reviewed[0].ArchiveSha512 -or $reviewed[0].Status -ne 'verified-evidence') { throw 'Package review does not match restored archive or requires a decision.' }
        $record.PackagedMetadataFindings = $record.ReviewIssues
        $record.ReviewIssues = @()
        $record.ReviewedLicense = $reviewed[0].ReviewedLicense
        $record.ReviewedLegalTexts = $reviewed[0].LegalTexts
        $record.ReviewEvidence = 'license-review.json#' + $key
    }
    $packageRecords += $record
    $noticeLines += @($key, ('=' * $key.Length), "Source: $($record.Source)",
        "License declaration: $($record.LicenseDeclaration)", "Declared copyright: $($record.Copyright)",
        "Upstream: $($record.ProjectUrl)", "Repository: $($record.RepositoryUrl)")
    foreach ($notice in $record.NoticeFiles) { $noticeLines += "Included text: $($notice.Artifact) (original: $($notice.PackagePath))" }
    if ($reviewed.Count -eq 1) {
        $noticeLines += "Reviewed license: $($reviewed[0].ReviewedLicense) (original package declaration above is unchanged)"
        foreach ($notice in $reviewed[0].LegalTexts) { $noticeLines += "Reviewed text: $($notice.Artifact) (source: $($notice.Source))" }
        $noticeLines += "Redistribution: $($reviewed[0].Obligations)"
    }
    foreach ($issue in $record.ReviewIssues) { $noticeLines += "REVIEW REQUIRED: $issue" }
    $noticeLines += ''
}
$noticeLines += @('HISTORICAL UPSTREAM SBOM TOOL - BLOCKED FROM CURRENT EXECUTION', 'See license-review.json for original bundled identities, archive and binary correspondence.', $review.OwnerDecision, '')
foreach ($component in @($review.Components | Where-Object Usage -eq 'sbom-tool')) {
    $noticeLines += @($component.Identity, "Reviewed license: $($component.ReviewedLicense)", "Redistribution: $($component.Obligations)")
    foreach ($notice in $component.LegalTexts) { $noticeLines += "Reviewed text: $($notice.Artifact) (source: $($notice.Source))" }
    $noticeLines += ''
}
$noticeLines += @('CURRENT SOURCE-SERVICED SBOM TOOL - DEVELOPMENT ONLY',
    'PiratePDF.CycloneDX.Serviced 6.3.1-piratepdf.b14b.1; upstream Apache-2.0 source, original LICENSE/NOTICE and modification notice retained.',
    'See generation-tool-serviced.json and serviced-tool-review.json. Unchanged exact components retain the original reviewed obligations above.',
    $servicedReview.NewComponent.Identity, ('Reviewed license: ' + $servicedReview.NewComponent.ReviewedLicense),
    ('Redistribution: ' + $servicedReview.NewComponent.Obligations))
foreach ($notice in $servicedReview.NewComponent.LegalTexts) { $noticeLines += "Reviewed text: $($notice.Artifact) (source: $($notice.Source))" }
$noticeLines += ''
$central = @()
foreach ($relative in @(& git -C $RepositoryRoot ls-files --cached --others --exclude-standard -- '*Directory.Packages.props' | Sort-Object -Unique)) {
    $document = Read-PackageXml (Join-Path $RepositoryRoot $relative)
    $central += [ordered]@{ Path=$relative; Versions=@($document.Project.ItemGroup.PackageVersion | Sort-Object Include | ForEach-Object { [ordered]@{Id=$_.Include;Version=$_.Version} }) }
}
$toolManifest = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'dotnet-tools.json') -Raw | ConvertFrom-Json
$tools = @($toolManifest.tools.PSObject.Properties | Sort-Object Name | ForEach-Object { [ordered]@{Id=$_.Name;Version=$_.Value.version;Scope='development';Evidence='dotnet-tools.json; not a production dependency or proof of installation'} })
$inventory = [ordered]@{
    Format='PiratePDF resolved NuGet inventory v1'; Scope='Repository resolution, not desktop distribution'; SchemaValidation='See CycloneDX SBOM validation separately'
    Projects=$projectRecords; Packages=$packageRecords; Relationships=@($relationships.Keys | Sort-Object | ForEach-Object { $relationships[$_] })
    CentralVersions=$central; DeclaredDotnetTools=$tools
    Limitations=@('No desktop entry project or distribution exists.', 'SDK/runtime packs and OS system libraries are not NuGet package components in this inventory.',
        'Bundled third-party components in native binaries are represented by package notice texts, not individually resolved NuGet graphs.',
        'Package metadata and hashes do not establish signature trust or legal approval.', 'CycloneDX generation tool provenance is recorded separately.')
}
Write-Json (Join-Path $OutputDirectory 'resolved-dependencies.json') $inventory
$native = @($packageRecords | ForEach-Object {
    $package = $_
    foreach ($asset in $package.NativeAssets) {
        [ordered]@{ Package="$($package.Id)/$($package.Version)";Source=$package.Source;RepositoryUrl=$package.RepositoryUrl
            RepositoryCommit=$package.RepositoryCommit;DeclaredPackageLicense=$package.LicenseDeclaration
            Path=$asset.PackagePath;Rid=$asset.Rid;Size=$asset.Size;Sha256=$asset.Sha256
            Kind=$(if($asset.PackagePath.EndsWith('.pdb', [StringComparison]::OrdinalIgnoreCase)){'debug-symbols'}else{'native-library'})
            WindowsX64=$asset.WindowsX64;DistributionQualified=$false }
    }
})
Write-Json (Join-Path $OutputDirectory 'native-assets.json') $native
# Normalize only the generated notice summary, not retained upstream evidence or metadata.
# Empty optional fields must not leave a trailing space; emit one final newline.
$cleanNoticeLines = @($noticeLines | ForEach-Object { ([string]$_).TrimEnd(' ', "`t") })
while ($cleanNoticeLines.Count -gt 0 -and $cleanNoticeLines[-1] -eq '') {
    $cleanNoticeLines = @($cleanNoticeLines | Select-Object -SkipLast 1)
}
$noticeText = ($cleanNoticeLines -join "`n") + "`n"
if ($noticeText -match '(?m)[ \t]+$' -or $noticeText.EndsWith("`n`n")) {
    throw 'Generated third-party notices contain trailing whitespace or extra blank lines at EOF.'
}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'THIRD-PARTY-NOTICES.txt'), $noticeText, $utf8)
Write-Host "Inventoried $($projects.Count) projects, $($packageRecords.Count) resolved packages."
