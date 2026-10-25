param([string]$GameBin)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputDirectory = Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$testExe = Join-Path $outputDirectory 'LocationTests.exe'
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
& $compiler /nologo /target:exe /platform:x64 /main:LocationTests /reference:System.Windows.Forms.dll "/out:$testExe" @sources (Join-Path $PSScriptRoot 'tests\LocationTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
& $testExe
if ($LASTEXITCODE -ne 0) { throw 'Location tests failed.' }
$compatibilityExe = Join-Path $outputDirectory 'CompatibilityTests.exe'
& $compiler /nologo /target:exe /platform:x64 /main:CompatibilityTests /reference:System.Windows.Forms.dll "/out:$compatibilityExe" @sources (Join-Path $PSScriptRoot 'tests\CompatibilityTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Compatibility test compilation failed.' }
& $compatibilityExe
if ($LASTEXITCODE -ne 0) { throw 'Compatibility tests failed.' }
if ($GameBin) { & (Join-Path $PSScriptRoot 'tests\VerifyInstalledBuild.ps1') -GameBin $GameBin }
