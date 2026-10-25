param([Parameter(Mandatory=$true)][string]$GameBin)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$fixturePrefix = Join-Path $repoRoot 'build\compatibility-integration-'
$fixture = [IO.Path]::GetFullPath($fixturePrefix + [Guid]::NewGuid().ToString('N'))
if (!$fixture.StartsWith($fixturePrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected fixture path.' }
$binaryNames = @('FarCry6.exe', 'FC_m64d3d12.dll', 'EMP.dll')
$customNames = @('uplay_r2.ini', 'amd_ags_x64.dll', 'upc_r2_loader64.dll', 'dbdata.dll', 'GamerProfile.xml')
$handles = @()
New-Item -ItemType Directory -Path $fixture | Out-Null
try {
    # Copy only the three binaries. Never write into the supplied installation.
    foreach ($name in $binaryNames) {
        Copy-Item -LiteralPath (Join-Path $GameBin $name) -Destination (Join-Path $fixture $name)
    }
    $customIni = "[Settings]`r`nUsername=Custom Player`r`nEmail=player@example.invalid`r`nUserId=custom-profile`r`nLanguage=fr-FR`r`nSaveType=2`r`nSavePath=D:\My Saves\Far Cry 6`r`n"
    [IO.File]::WriteAllText((Join-Path $fixture 'uplay_r2.ini'), $customIni)
    foreach ($name in $customNames | Where-Object { $_ -ne 'uplay_r2.ini' }) {
        [IO.File]::WriteAllText((Join-Path $fixture $name), 'custom file not modified by launcher')
    }
    $before = @{}
    foreach ($name in $customNames) {
        $before[$name] = (Get-FileHash -LiteralPath (Join-Path $fixture $name)).Hash
    }
    $validator = Add-Type -Path (Join-Path $repoRoot 'src\GameCompatibility.cs') -PassThru
    $verify = $validator.GetMethod('Verify', [Reflection.BindingFlags]'Static,NonPublic')
    foreach ($name in $customNames) {
        $handles += [IO.File]::Open((Join-Path $fixture $name), [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    }
    $verify.Invoke($null, @($fixture))
    Write-Output 'PASS full validation never accesses locked settings or unpatched dependencies'
    foreach ($handle in $handles) { $handle.Dispose() }
    $handles = @()
    foreach ($name in $customNames) {
        if ((Get-FileHash -LiteralPath (Join-Path $fixture $name)).Hash -ne $before[$name]) { throw "Changed custom file: $name" }
    }
    Write-Output 'PASS all five custom files remain unchanged'
    foreach ($name in $binaryNames) {
        $target = Join-Path $fixture $name
        $backup = $target + '.supported'
        Move-Item -LiteralPath $target -Destination $backup
        [IO.File]::WriteAllText($target, 'unsupported binary fixture')
        $rejected = $false
        try { $verify.Invoke($null, @($fixture)) }
        catch {
            if ($_.Exception.InnerException -is [InvalidOperationException] -and $_.Exception.InnerException.Message.Contains($name)) { $rejected = $true }
            else { throw }
        }
        if (!$rejected) { throw "Changed $name was accepted." }
        Write-Output "PASS actual validator rejects changed $name"
        Remove-Item -LiteralPath $target
        Move-Item -LiteralPath $backup -Destination $target
    }
}
finally {
    foreach ($handle in $handles) { $handle.Dispose() }
    $resolved = [IO.Path]::GetFullPath($fixture)
    if (!$resolved.StartsWith($fixturePrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected fixture cleanup path.' }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
