[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ManifestPath,
    [Parameter(Mandatory = $true)][string]$EvidenceDirectory
)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$verifier = Join-Path $PSScriptRoot 'Test-ArtifactCurrency.ps1'
$baselinePath = Join-Path $PSScriptRoot 'artifact-security-baseline.json'
$evidence = [IO.Path]::GetFullPath($EvidenceDirectory)
if (-not $evidence.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Currency verification fixtures must stay in the repository evidence directory.'
}
[void][IO.Directory]::CreateDirectory($evidence)
$original = Get-Content -LiteralPath $ManifestPath -Raw
& powershell.exe -NoProfile -File $verifier -ManifestPath $ManifestPath *> (Join-Path $evidence 'positive.log')
if ($LASTEXITCODE -ne 0) { throw 'Positive currency verification failed.' }
$scenarios = @('sdk-hash', 'source', 'compiler', 'audit', 'file-hash', 'coverage', 'duplicate', 'escape', 'runtime-floor', 'runtime-binary', 'native-binary', 'sbom-source', 'sbom-pkcs', 'sbom-binary')
foreach ($scenario in $scenarios) {
    $manifest = $original | ConvertFrom-Json
    $baseline = Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json
    $directory = Join-Path $evidence ($scenario + '-' + [Guid]::NewGuid().ToString('N'))
    [void][IO.Directory]::CreateDirectory($directory)
    $expected = ''
    switch ($scenario) {
        'sdk-hash' { $manifest.SDK.SHA256 = '0' * 64; $expected = 'Evidence hash mismatch' }
        'source' { $manifest.Source.Commit = ''; $expected = 'Missing source' }
        'compiler' { $manifest.CompilerAssets = @(); $expected = 'Missing compiler' }
        'audit' { $manifest.Audits = @(); $expected = 'Missing NuGet' }
        'file-hash' { $manifest.Artifacts[0].Files[0].SHA256 = '0' * 64; $expected = 'Artifact hash mismatch' }
        'coverage' { $manifest.Artifacts[0].Files = @($manifest.Artifacts[0].Files | Select-Object -Skip 1); $expected = 'file coverage differs' }
        'duplicate' { $manifest.Artifacts[0].Files += @($manifest.Artifacts[0].Files[0]); $expected = 'Duplicate artifact file' }
        'escape' { $manifest.Artifacts[0].Files[0].Path = '../outside'; $expected = 'escapes its root' }
        'runtime-floor' {
            $artifact = @($manifest.Artifacts | Where-Object Mode -eq 'framework-dependent')[0]
            $copy = Join-Path $directory 'artifact'
            Copy-Item -LiteralPath (Join-Path $root $artifact.Root) -Destination $copy -Recurse
            $config = @(Get-ChildItem $copy -Filter *.runtimeconfig.json -File)[0]
            $data = Get-Content -LiteralPath $config.FullName -Raw | ConvertFrom-Json
            $data.runtimeOptions.framework.version = '10.0.10'
            $data | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $config.FullName
            $artifact.Root = $copy.Substring($root.Length + 1)
            $record = $artifact.Files | Where-Object Path -eq $config.Name
            $record.SHA256 = (Get-FileHash -LiteralPath $config.FullName).Hash
            $manifest.Artifacts = @($artifact) + @($manifest.Artifacts | Where-Object Id -eq 'sbom-tool-serviced')
            $expected = 'Insecure or incompatible runtime'
        }
        'runtime-binary' { $baseline.RuntimeHashes.'coreclr.dll' = '0' * 64; $expected = 'Embedded runtime binary differs' }
        'native-binary' { $baseline.NativeHashes.'libSkiaSharp.dll' = '0' * 64; $expected = 'Unapproved native binary' }
        { $_ -in @('sbom-source','sbom-pkcs') } {
            $producer = Get-Content (Join-Path $root $manifest.ServicedSbomProvenance.Path) -Raw | ConvertFrom-Json
            if ($scenario -eq 'sbom-source') { $producer.Commit = '0' * 40 } else { $producer.Pkcs = '8.0.1' }
            $producerPath = Join-Path $directory 'producer.json'
            $producer | ConvertTo-Json -Depth 30 | Set-Content $producerPath
            $manifest.ServicedSbomProvenance.Path = $producerPath.Substring($root.Length + 1)
            $manifest.ServicedSbomProvenance.SHA256 = (Get-FileHash $producerPath).Hash
            $expected = 'producer differs from the approved'
        }
        'sbom-binary' { $baseline.SbomTool.MainDllSHA256 = '0' * 64; $expected = 'source-built main DLL differs' }
    }
    $testManifest = Join-Path $directory 'manifest.json'
    $testBaseline = Join-Path $directory 'baseline.json'
    $manifest | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $testManifest
    $baseline | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $testBaseline
    $log = Join-Path $directory 'verification.log'
    # Windows PowerShell maps redirected native stderr to error records. Expected rejection must reach the exit-code assertion.
    $ErrorActionPreference = 'Continue'
    try {
        & powershell.exe -NoProfile -File $verifier -ManifestPath $testManifest -BaselinePath $testBaseline *> $log
        $code = $LASTEXITCODE
    } finally { $ErrorActionPreference = 'Stop' }
    if ($code -eq 0 -or (Get-Content -LiteralPath $log -Raw) -notmatch [regex]::Escape($expected)) {
        throw "Currency negative scenario did not reject the expected condition: $scenario"
    }
    Write-Output "Rejected $scenario mutation."
}
Write-Output 'Positive currency verification and all fourteen negative scenarios passed.'
