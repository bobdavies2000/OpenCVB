#Requires -Version 5.1
<#
.SYNOPSIS
  Download and install ZED SDK 5.3 for OpenCVB (CUDA 12 build matches CUDA 12.x toolkits).

.DESCRIPTION
  1. Downloads ZED_SDK_Windows_cuda12_v5.3.0.exe into ZED_SDK/installer/
  2. Runs the Stereolabs installer (admin required)
  3. Copies sl_zed64.dll (+ peers) into ZED_SDK/native/bin for project-local deployment

.PARAMETER SkipInstall
  Only download the installer; do not run setup.

.PARAMETER SkipCopy
  Skip copying DLLs into ZED_SDK/native/bin after install.
#>
param(
    [switch]$SkipInstall,
    [switch]$SkipCopy
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$installerDir = Join-Path $PSScriptRoot 'installer'
$nativeBin = Join-Path $PSScriptRoot 'native\bin'
# Resolves via Stereolabs CDN (direct .exe URL redirects to stereolabs.com HTML).
$installerUrl = 'https://download.stereolabs.com/zedsdk/5.3/cu12/win'
$installerPath = Join-Path $installerDir 'ZED_SDK_Windows_cuda12_v5.3.0.exe'
$systemSdkBin = 'C:\Program Files (x86)\ZED SDK\bin'
$encodingMarker = 'SVO_ENCODING_PRESET'

New-Item -ItemType Directory -Force -Path $installerDir, $nativeBin | Out-Null

function Test-ZedSdk53Native([string]$binDir) {
    $dll = Join-Path $binDir 'sl_zed64.dll'
    if (-not (Test-Path $dll)) { return $false }
    $text = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($dll))
    return $text.Contains($encodingMarker)
}

Write-Host "OpenCVB ZED SDK 5.3 installer (CUDA 12)"
Write-Host "  Repo: $repoRoot"
Write-Host "  URL:  $installerUrl"

function Test-ValidInstaller([string]$path) {
    if (-not (Test-Path $path)) { return $false }
    if ((Get-Item $path).Length -lt 10MB) { return $false }
    $head = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($path)[0..1])
    return $head -ne '<!'
}

if (-not (Test-ValidInstaller $installerPath)) {
    if (Test-Path $installerPath) { Remove-Item $installerPath -Force }
    Write-Host "Downloading installer (large file, follow redirects)..."
    $curl = Get-Command curl.exe -ErrorAction SilentlyContinue
    if ($curl) {
        & curl.exe -fL $installerUrl -o $installerPath
    } else {
        Invoke-WebRequest -Uri $installerUrl -OutFile $installerPath -UseBasicParsing -MaximumRedirection 10
    }
    if (-not (Test-ValidInstaller $installerPath)) {
        throw "Download failed or returned HTML. Try manually from https://www.stereolabs.com/developers/release (CUDA 12)."
    }
    Write-Host "Saved: $installerPath ($((Get-Item $installerPath).Length) bytes)"
} else {
    Write-Host "Installer already present: $installerPath"
}

if (-not $SkipInstall) {
    if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Host "Re-launching elevated to run ZED SDK setup..."
        $args = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
        if ($SkipCopy) { $args += ' -SkipCopy' }
        Start-Process powershell.exe -Verb RunAs -ArgumentList $args -Wait
        exit $LASTEXITCODE
    }

    Write-Host "Running ZED SDK 5.3 setup (silent)..."
    $proc = Start-Process -FilePath $installerPath -ArgumentList '/S' -Wait -PassThru
    if ($proc.ExitCode -ne 0) {
        Write-Warning "Installer exit code $($proc.ExitCode). If setup failed, run manually: $installerPath"
    } else {
        Write-Host "ZED SDK setup finished."
    }
    Write-Host "A reboot may be required after first ZED SDK install (Stereolabs docs)."
}

if (-not $SkipInstall -and -not $SkipCopy) {
    if (-not (Test-Path $systemSdkBin)) {
        throw "ZED SDK bin not found at $systemSdkBin. Install SDK first or run without -SkipInstall."
    }
    if (-not (Test-ZedSdk53Native $systemSdkBin)) {
        throw "Installed sl_zed64.dll at $systemSdkBin does not look like ZED SDK 5.3 (missing $encodingMarker)."
    }

    Write-Host "Copying native DLLs to $nativeBin ..."
    Get-ChildItem $systemSdkBin -Filter '*.dll' | ForEach-Object {
        Copy-Item $_.FullName (Join-Path $nativeBin $_.Name) -Force
    }
    Write-Host "Project-native ZED SDK ready: $nativeBin"
}

if (Test-ZedSdk53Native $nativeBin) {
    Write-Host "OK: ZED SDK 5.3 native binaries verified under ZED_SDK/native/bin"
} elseif (Test-ZedSdk53Native $systemSdkBin) {
    Write-Host "OK: ZED SDK 5.3 installed under Program Files (rebuild MainUI to copy sl_zed64.dll to output)."
} else {
    Write-Warning "ZED SDK 5.3 native DLLs not verified yet."
}

Write-Host "Next: rebuild OpenCVB (x64), e.g. MainUI or OpenCVB.sln"
