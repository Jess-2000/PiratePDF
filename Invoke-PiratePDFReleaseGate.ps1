[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-GateStep {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    Write-Host ""
    Write-Host "==> $Message"
}

function Assert-LastExitCode {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Step
    )

    if ($LASTEXITCODE -ne 0) {
        throw "$Step failed with exit code $LASTEXITCODE."
    }
}

function Get-RepositoryRoot {
    $Root = (& git rev-parse --show-toplevel 2>$null)

    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($Root)) {
        throw "This script must be run from inside the PiratePDF Git repository."
    }

    return [System.IO.Path]::GetFullPath($Root.Trim())
}

function Assert-DocumentationRepairPreviewIsClean {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ReportPath
    )

    if (-not (Test-Path -LiteralPath $ReportPath -PathType Leaf)) {
        throw "DocGen repair preview did not produce '$ReportPath'."
    }

    $Report = Get-Content -LiteralPath $ReportPath -Raw

    $ChangesMatch = [regex]::Match(
        $Report,
        '(?im)^\s*\|\s*Changes\s*\|\s*(\d+)\s*\|\s*$')

    $FilesMatch = [regex]::Match(
        $Report,
        '(?im)^\s*\|\s*Files\s*\|\s*(\d+)\s*\|\s*$')

    if (-not $ChangesMatch.Success -or -not $FilesMatch.Success) {
        throw "Unable to read the DocGen repair-preview summary from '$ReportPath'."
    }

    $Changes = [int]$ChangesMatch.Groups[1].Value
    $Files = [int]$FilesMatch.Groups[1].Value

    if ($Changes -ne 0 -or $Files -ne 0) {
        throw "Documentation repair preview found $Changes proposed change(s) across $Files file(s). Run 'dotnet run --project .\tools\PiratePDF.DocGen\PiratePDF.DocGen.csproj --configuration $Configuration -- repair --apply', review the changes, and rerun the release gate."
    }
}

$RepositoryRoot = Get-RepositoryRoot
Set-Location -LiteralPath $RepositoryRoot

$SolutionPath =
    Join-Path $RepositoryRoot "PiratePDF.slnx"

$DocGenProject =
    Join-Path $RepositoryRoot "tools\PiratePDF.DocGen\PiratePDF.DocGen.csproj"

$DocumentChangesReport =
    Join-Path $RepositoryRoot "docs\Document-Changes.md"

if (-not (Test-Path -LiteralPath $SolutionPath -PathType Leaf)) {
    throw "PiratePDF solution was not found at '$SolutionPath'."
}

if (-not (Test-Path -LiteralPath $DocGenProject -PathType Leaf)) {
    throw "PiratePDF.DocGen project was not found at '$DocGenProject'."
}

Write-Host "PiratePDF Release Gate"
Write-Host "Repository:    $RepositoryRoot"
Write-Host "Configuration: $Configuration"

Write-GateStep "Source-file size validation"
& (Join-Path $RepositoryRoot "Test-PiratePDFSourceFileSizes.ps1")
Assert-LastExitCode "Source-file size validation"

Write-GateStep "Dependency restore"
& dotnet restore $SolutionPath
Assert-LastExitCode "Restore"

Write-GateStep "$Configuration build"
& dotnet build $SolutionPath --configuration $Configuration --no-restore
Assert-LastExitCode "Build"

Write-GateStep "Test suite"
& dotnet test $SolutionPath --configuration $Configuration --no-build
Assert-LastExitCode "Tests"

Write-GateStep "Documentation generation and validation"
& dotnet run `
    --project $DocGenProject `
    --configuration $Configuration `
    -- all
Assert-LastExitCode "Documentation generation"

Write-GateStep "Documentation health analysis"
& dotnet run `
    --project $DocGenProject `
    --configuration $Configuration `
    -- health
Assert-LastExitCode "Documentation health analysis"

Write-GateStep "Documentation repair preview"
& dotnet run `
    --project $DocGenProject `
    --configuration $Configuration `
    -- repair

$RepairExitCode = $LASTEXITCODE

# Inspect the generated report even if DocGen itself returns success.
Assert-DocumentationRepairPreviewIsClean $DocumentChangesReport

if ($RepairExitCode -ne 0) {
    throw "Documentation repair preview failed with exit code $RepairExitCode."
}

Write-GateStep "Git whitespace validation"
& git diff --check
Assert-LastExitCode "git diff --check"

Write-GateStep "Repository status"
& git status --short
Assert-LastExitCode "git status"

Write-Host ""
Write-Host "PiratePDF release gate completed successfully."
Write-Host "Source file sizes:     PASS"
Write-Host "Restore:               PASS"
Write-Host "Build:                 PASS"
Write-Host "Tests:                 PASS"
Write-Host "Documentation:         PASS"
Write-Host "Repair preview:        PASS (0 proposed changes)"
Write-Host "Whitespace validation: PASS"
