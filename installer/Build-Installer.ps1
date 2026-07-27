[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'driver\EFucoserFocuserDriver\EFucoserFocuserDriver.csproj'
$driverPath = Join-Path $repositoryRoot 'driver\EFucoserFocuserDriver\bin\Release\ASCOM.EFucoser.Focuser.dll'
$scriptPath = Join-Path $PSScriptRoot 'EFucoserASCOMSetup.iss'
$outputPath = Join-Path $repositoryRoot 'dist\EFucoserASCOMSetup.exe'

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) {
    throw 'Visual Studio Build Tools were not found.'
}

$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' |
    Select-Object -First 1
if (-not $msbuild) {
    throw 'MSBuild was not found.'
}

$isccCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    throw 'Inno Setup 6 was not found. Install package JRSoftware.InnoSetup with winget.'
}

& $msbuild $projectPath /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU /verbosity:minimal
if ($LASTEXITCODE -ne 0) {
    throw "Driver build failed with exit code $LASTEXITCODE."
}
if (-not (Test-Path $driverPath)) {
    throw "Driver output was not created: $driverPath"
}

& $iscc $scriptPath
if ($LASTEXITCODE -ne 0) {
    throw "Installer build failed with exit code $LASTEXITCODE."
}
if (-not (Test-Path $outputPath)) {
    throw "Installer output was not created: $outputPath"
}

$file = Get-Item $outputPath
$hash = Get-FileHash $outputPath -Algorithm SHA256
Write-Output "Installer: $($file.FullName)"
Write-Output "Size: $($file.Length) bytes"
Write-Output "SHA256: $($hash.Hash)"
