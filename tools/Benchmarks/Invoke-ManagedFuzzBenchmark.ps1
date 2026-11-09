[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
if (-not $output.StartsWith((Join-Path $repository 'artifacts/fuzz') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or (Test-Path $output)) {
    throw 'Choose a new benchmark output directory under artifacts/fuzz.'
}
[void][IO.Directory]::CreateDirectory($output)
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup>
    <Compile Include="$repository/tools/benchmarks/ManagedFuzzBenchmark.cs" />
    <ProjectReference Include="$repository/tools/PiratePDF.Fuzz.Runner/PiratePDF.Fuzz.Runner.csproj" />
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $output 'ManagedFuzzBenchmark.csproj'
[IO.File]::WriteAllText($projectPath, $project)
& dotnet build $projectPath -c Release -o (Join-Path $output 'bin') *> (Join-Path $output 'build.log')
if ($LASTEXITCODE -ne 0) { throw 'Benchmark build failed. Inspect build.log.' }
& dotnet (Join-Path $output 'bin/ManagedFuzzBenchmark.dll') $repository (Join-Path $output 'measurements.json')
if ($LASTEXITCODE -ne 0) { throw 'Benchmark execution failed.' }
