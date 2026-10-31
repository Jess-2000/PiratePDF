[CmdletBinding()]
param(
    [string]$ArtifactDirectory = 'docs/dependencies',
    [Parameter(Mandatory = $true)][string]$EvidenceDirectory
)
$ErrorActionPreference = 'Stop'
$ArtifactDirectory = (Resolve-Path -LiteralPath $ArtifactDirectory).Path
$EvidenceDirectory = [IO.Path]::GetFullPath($EvidenceDirectory)
[void][IO.Directory]::CreateDirectory($EvidenceDirectory)
$validator = Join-Path $PSScriptRoot 'Test-DependencyArtifacts.ps1'
& $validator -ArtifactDirectory $ArtifactDirectory
if (-not $?) { throw 'Positive artifact validation failed.' }
foreach ($scenario in @('schema','hash','notice','coverage','private-path','upstream-evidence','review-identity')) {
    $directory = Join-Path $EvidenceDirectory ($scenario + '-' + [Guid]::NewGuid().ToString('N'))
    [void][IO.Directory]::CreateDirectory($directory)
    foreach ($file in @(Get-ChildItem -LiteralPath $ArtifactDirectory -File | Where-Object Extension -In @('.json','.xml'))) {
        [IO.File]::Copy($file.FullName, (Join-Path $directory $file.Name))
    }
    Copy-Item -LiteralPath (Join-Path $ArtifactDirectory 'licenses') -Destination (Join-Path $directory 'licenses') -Recurse
    Copy-Item -LiteralPath (Join-Path $ArtifactDirectory 'evidence') -Destination (Join-Path $directory 'evidence') -Recurse
    switch ($scenario) {
        'schema' {
            $file = Join-Path $directory 'engine.cdx.xml'; [xml]$xml = Get-Content -LiteralPath $file
            $xml.bom.components.component[0].RemoveAttribute('type'); $xml.Save($file)
        }
        'hash' {
            $file = Join-Path $directory 'engine.cdx.xml'; [xml]$xml = Get-Content -LiteralPath $file
            $xml.SelectSingleNode('//*[local-name()="hash"]').InnerText = ('0' * 128); $xml.Save($file)
        }
        'notice' {
            $notice = Get-ChildItem -LiteralPath (Join-Path $directory 'licenses') -File | Select-Object -First 1
            [IO.File]::AppendAllText($notice.FullName, 'test mutation')
        }
        'coverage' {
            $file = Join-Path $directory 'solution.cdx.xml'; [xml]$xml = Get-Content -LiteralPath $file
            [void]$xml.bom.components.RemoveChild($xml.bom.components.component[0]); $xml.Save($file)
        }
        'private-path' {
            [IO.File]::WriteAllText((Join-Path $directory 'private.json'), '{"path":"C:\\Users\\fixture\\private"}')
        }
        'upstream-evidence' {
            $evidence = Get-ChildItem -LiteralPath (Join-Path $directory 'evidence') -File | Select-Object -First 1
            [IO.File]::AppendAllText($evidence.FullName, 'test mutation')
        }
        'review-identity' {
            $file = Join-Path $directory 'license-review.json'
            $review = Get-Content -LiteralPath $file -Raw | ConvertFrom-Json
            $review.Components[0].Identity = 'fixture/0.0.0'
            [IO.File]::WriteAllText($file, ($review | ConvertTo-Json -Depth 20))
        }
    }
    # These children must fail. Capture stderr without converting the expected
    # native nonzero result into a terminating harness error before inspecting it.
    $priorPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & powershell.exe -NoProfile -File $validator -ArtifactDirectory $directory *> (Join-Path $directory 'validation.log')
        $result = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $priorPreference }
    if ($result -eq 0) { throw "Invalid fixture accepted: $scenario" }
    Write-Host "Rejected $scenario mutation (exit $result)."
}
