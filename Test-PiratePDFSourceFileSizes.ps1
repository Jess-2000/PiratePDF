[CmdletBinding()]
param(
    [ValidateRange(1, 100000)]
    [int]$ReviewThreshold = 800,

    [ValidateRange(1, 100000)]
    [int]$HardLimit = 1000
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($ReviewThreshold -gt $HardLimit) {
    throw "ReviewThreshold must be less than or equal to HardLimit."
}

function Get-RepositoryRoot {
    $Root = (& git rev-parse --show-toplevel 2>$null)

    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($Root)) {
        throw "This script must be run from inside the PiratePDF Git repository."
    }

    return [System.IO.Path]::GetFullPath($Root.Trim())
}

function Test-IsGeneratedSource {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo]$File
    )

    $Name = $File.Name

    return $Name.EndsWith(".g.cs", [System.StringComparison]::OrdinalIgnoreCase) -or
        $Name.EndsWith(".generated.cs", [System.StringComparison]::OrdinalIgnoreCase) -or
        $Name.EndsWith(".designer.cs", [System.StringComparison]::OrdinalIgnoreCase) -or
        $Name.Equals("AssemblyInfo.cs", [System.StringComparison]::OrdinalIgnoreCase)
}

$RepositoryRoot = Get-RepositoryRoot
$Roots = @(
    "src",
    "tests",
    "tools",
    "samples"
)

$Files = New-Object System.Collections.Generic.List[System.IO.FileInfo]

foreach ($RelativeRoot in $Roots) {
    $SourceRoot = Join-Path $RepositoryRoot $RelativeRoot
    if (-not (Test-Path -LiteralPath $SourceRoot -PathType Container)) {
        continue
    }

    Get-ChildItem -LiteralPath $SourceRoot -Recurse -File -Filter "*.cs" |
        Where-Object {
            $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and
            -not (Test-IsGeneratedSource $_)
        } |
        ForEach-Object { $Files.Add($_) }
}

Get-ChildItem -LiteralPath $RepositoryRoot -Recurse -File -Filter "*.ps1" |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|.git)[\\/]' } |
    ForEach-Object { $Files.Add($_) }

$Results = foreach ($File in $Files) {
    $LineCount = @(Get-Content -LiteralPath $File.FullName).Count
    $RelativePath = $File.FullName.Substring($RepositoryRoot.Length).TrimStart([char[]]"\/")

    [pscustomobject]@{
        Lines = $LineCount
        Path = $RelativePath
    }
}

$Reviewed = @($Results | Where-Object { $_.Lines -ge $ReviewThreshold } | Sort-Object -Property @{ Expression = 'Lines'; Descending = $true }, Path)
$Violations = @($Results | Where-Object { $_.Lines -gt $HardLimit } | Sort-Object -Property @{ Expression = 'Lines'; Descending = $true }, Path)

Write-Host "PiratePDF source-file size check"
Write-Host "Review threshold: $ReviewThreshold lines"
Write-Host "Hard limit:       $HardLimit lines"
Write-Host "Files inspected:  $($Results.Count)"

if ($Reviewed.Count -gt 0) {
    Write-Host ""
    Write-Host "Files at or above the review threshold:"
    $Reviewed | Format-Table Lines, Path -AutoSize | Out-Host
}

if ($Violations.Count -gt 0) {
    Write-Host ""
    Write-Error "$($Violations.Count) hand-authored source file(s) exceed the $HardLimit-line hard limit."
    exit 1
}

Write-Host ""
Write-Host "Source-file size check passed."
exit 0
