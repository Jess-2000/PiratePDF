param(
    [Parameter(Mandatory = $true)]
    [string]$ResultsDirectory,
    [ValidateSet('Portable', 'Windows', 'All')]
    [string]$Platform = 'All',
    [string]$TestAssembly = "$PSScriptRoot/bin/Release/net10.0/PiratePDF.Fuzz.Tests.dll",
    [switch]$BoundsOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$files = @(if (Test-Path -LiteralPath $ResultsDirectory -PathType Container) {
    Get-ChildItem -LiteralPath $ResultsDirectory -Filter '*.trx' -File
})
if ($files.Count -gt 64) { throw 'Test result file count exceeds 64.' }
$aggregateBytes = 0L
foreach ($file in $files) {
    if ($file.Length -gt 16MB) { throw 'Test result file exceeds the CI evidence ceiling.' }
    $aggregateBytes += $file.Length
    if ($aggregateBytes -gt 64MB) { throw 'Aggregate test result evidence exceeds 64 MiB.' }
}
if ($BoundsOnly) {
    Write-Output "Test-result bounds verified: $($files.Count) files, $aggregateBytes bytes."
    return
}
if ($files.Count -eq 0) { throw 'Expected at least one test result file.' }

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$outsideTraits = foreach ($file in @(Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'tests') -Recurse -Filter '*.cs' -File)) {
    $relative = $file.FullName.Substring($repositoryRoot.Length).TrimStart([char[]]'\/').Replace('\', '/')
    if ($relative.StartsWith('tests/PiratePDF.Fuzz.Tests/', [StringComparison]::Ordinal) -or
        $relative -match '/(?:bin|obj)/') { continue }
    if ([IO.File]::ReadAllText($file.FullName) -match '(?s)\[\s*Trait\s*\(\s*(?:TestPlatform\.Key|"Platform")\s*,') { $relative }
}
if ($outsideTraits) {
    throw "Platform traits outside PiratePDF.Fuzz.Tests are not routed: $($outsideTraits -join ', '). Update the Windows job and coverage catalog first."
}

# Trusted test metadata, not fuzz input. Count every inline row so an empty/misfiltered category fails CI.
$catalogJson = & dotnet $TestAssembly platform-catalog
if ($LASTEXITCODE -ne 0) { throw 'Could not enumerate the platform contract.' }
$catalog = $catalogJson | ConvertFrom-Json
if (@($catalog | Where-Object Platform -eq 'Portable').Count -eq 0 -or
    @($catalog | Where-Object Platform -eq 'Windows').Count -eq 0) { throw 'Both platform routes must remain populated.' }
$expected = @{}
foreach ($test in $catalog) {
    if ($Platform -eq 'All' -or $test.Platform -eq $Platform) { $expected.Add($test.Method, [int]$test.Cases) }
}
$observed = @{}
$caseIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($file in $files) {
    $settings = [Xml.XmlReaderSettings]::new()
    $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $settings.MaxCharactersInDocument = 16MB
    $reader = [Xml.XmlReader]::Create($file.FullName, $settings)
    try {
        $document = [Xml.XmlDocument]::new()
        $document.XmlResolver = $null
        $document.Load($reader)
    }
    finally { $reader.Dispose() }
    foreach ($result in $document.GetElementsByTagName('UnitTestResult')) {
        $name = $result.GetAttribute('testName')
        if (!$name.StartsWith('PiratePDF.Fuzz.Tests.', [StringComparison]::Ordinal)) { continue }
        $method = $name.Split('(')[0]
        if (!$expected.ContainsKey($method)) { throw "Unexpected test route: $method" }
        if ($result.GetAttribute('outcome') -ne 'Passed') { throw "Test did not pass: $method" }
        if (!$caseIds.Add($result.GetAttribute('testId'))) { throw "Duplicate execution evidence: $method" }
        if (!$observed.ContainsKey($method)) { $observed[$method] = 0 }
        $observed[$method]++
    }
}
foreach ($method in $expected.Keys) {
    if (!$observed.ContainsKey($method) -or $observed[$method] -ne $expected[$method]) {
        throw "Missing or mismatched test cases: $method"
    }
}
$total = ($expected.Values | Measure-Object -Sum).Sum
Write-Output "Platform coverage verified: $Platform, $total passed cases, $($expected.Count) methods, zero skipped."
