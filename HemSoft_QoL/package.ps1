# HemSoft QoL - Nexus Package Builder
# Creates a clean ZIP file for Nexus Mods distribution

$ErrorActionPreference = "Stop"
$AssemblyName = "HemSoft_QoL"  # DLL name
$ModName = "S_HemSoft_QoL"       # Deployed folder name
$Version = "1.5.0"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir

Write-Host "=== HemSoft QoL Package Builder ===" -ForegroundColor Cyan

# Build Release
Write-Host "Building Release..." -ForegroundColor Yellow
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}
Write-Host "Build successful" -ForegroundColor Green

# Create temp directory for packaging
$TempDir = Join-Path $env:TEMP "HemSoft_QoL_Package"
$PackageDir = Join-Path $TempDir $ModName

if (Test-Path $TempDir) {
    Remove-Item $TempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $PackageDir | Out-Null

# Copy required files
Write-Host "Copying files..." -ForegroundColor Yellow

# DLL
Copy-Item "$ScriptDir\bin\$AssemblyName.dll" $PackageDir

# ModInfo.xml
Copy-Item "$ScriptDir\ModInfo.xml" $PackageDir

# Config folder
$ConfigDir = Join-Path $PackageDir "Config"
New-Item -ItemType Directory -Path $ConfigDir | Out-Null
Copy-Item "$ScriptDir\Config\HemSoftQoL.xml" $ConfigDir
Copy-Item "$ScriptDir\Config\Localization.txt" $ConfigDir

# XUi folder
$XuiDir = Join-Path $ConfigDir "XUi"
New-Item -ItemType Directory -Path $XuiDir | Out-Null
Copy-Item "$ScriptDir\Config\XUi\windows.xml" $XuiDir

# ModSettings.xml (Gears integration)
Copy-Item "$ScriptDir\ModSettings.xml" $PackageDir

# Gears folder (icon)
$GearsDir = Join-Path $PackageDir "Gears"
if (Test-Path "$ScriptDir\Gears") {
    New-Item -ItemType Directory -Path $GearsDir | Out-Null
    Copy-Item "$ScriptDir\Gears\*" $GearsDir -ErrorAction SilentlyContinue
}

# README
Copy-Item "$ScriptDir\README.md" $PackageDir

# Create ZIP in repo-level releases folder
$OutputDir = Join-Path $RepoRoot "releases"
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

$ZipName = "${AssemblyName}_v${Version}.zip"
$ZipPath = Join-Path $OutputDir $ZipName

if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

Write-Host "Creating ZIP archive..." -ForegroundColor Yellow
Compress-Archive -Path $PackageDir -DestinationPath $ZipPath

# Cleanup
Remove-Item $TempDir -Recurse -Force

Write-Host ""
Write-Host "=== Package Complete ===" -ForegroundColor Green
Write-Host "Output: $ZipPath" -ForegroundColor Cyan
Write-Host "Size: $([math]::Round((Get-Item $ZipPath).Length / 1KB, 2)) KB" -ForegroundColor Cyan
Write-Host ""
Write-Host "Ready for Nexus upload!" -ForegroundColor Green
