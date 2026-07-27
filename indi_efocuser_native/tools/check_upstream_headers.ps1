param(
    [Parameter(Mandatory = $true)]
    [string]$IndiCheckout
)

$ErrorActionPreference = "Stop"

$checkout = (Resolve-Path -LiteralPath $IndiCheckout).Path
$sourceRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$ucrtCompiler = "C:\msys64\ucrt64\bin\g++.exe"

if (Test-Path -LiteralPath $ucrtCompiler) {
    $compiler = $ucrtCompiler
    $env:PATH = "C:\msys64\ucrt64\bin;C:\msys64\usr\bin;" + $env:PATH
} else {
    $compiler = (Get-Command g++ -ErrorAction Stop).Source
}

$includeDirectories = @(
    (Join-Path $sourceRoot "tests\stubs"),
    $sourceRoot,
    (Join-Path $checkout "libs"),
    (Join-Path $checkout "libs\indibase"),
    (Join-Path $checkout "libs\indibase\timer"),
    (Join-Path $checkout "libs\indiclient"),
    (Join-Path $checkout "libs\indiabstractclient"),
    (Join-Path $checkout "libs\indicore"),
    (Join-Path $checkout "libs\indidevice"),
    (Join-Path $checkout "libs\indidevice\property")
)

$arguments = @(
    "-std=c++17",
    "-Wall",
    "-Wextra",
    "-Werror",
    "-fsyntax-only"
)

foreach ($directory in $includeDirectories) {
    $arguments += "-I$directory"
}

$arguments += (Join-Path $sourceRoot "efucoser.cpp")

& $compiler @arguments
if ($LASTEXITCODE -ne 0) {
    throw "EFucoser syntax check failed with exit code $LASTEXITCODE."
}

Write-Host "EFucoser passed the current INDI header syntax check."
