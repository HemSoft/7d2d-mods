# HemSoft QoL Mod Deployment Script
# Builds and deploys the mod to the 7 Days to Die Mods folder

param(
    [switch]$NoBuild,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$AssemblyName = "HemSoft_QoL"  # DLL name (matches .csproj)
$ModName = "S_HemSoft_QoL"       # Deployed folder name
$GamePath = "C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die"
$ModsPath = "$GamePath\Mods"
$DestPath = "$ModsPath\$ModName"

Write-Host "=== HemSoft QoL Mod Deployment ===" -ForegroundColor Cyan

# Build if not skipped
if (-not $NoBuild) {
    Write-Host "Building $Configuration..." -ForegroundColor Yellow
    Push-Location $ScriptDir
    dotnet build -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Pop-Location
        throw "Build failed"
    }
    Pop-Location
    Write-Host "Build successful" -ForegroundColor Green
}

# Verify game path exists
if (-not (Test-Path $GamePath)) {
    throw "7 Days to Die not found at: $GamePath"
}

# Create mod folder if needed
if (-not (Test-Path $DestPath)) {
    Write-Host "Creating mod folder..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $DestPath -Force | Out-Null
}

# Copy files
Write-Host "Deploying to $DestPath..." -ForegroundColor Yellow

Copy-Item "$ScriptDir\ModInfo.xml" $DestPath -Force
Copy-Item "$ScriptDir\bin\$AssemblyName.dll" $DestPath -Force
Copy-Item "$ScriptDir\Config" $DestPath -Recurse -Force

Write-Host ""
Write-Host "=== Deployment Complete ===" -ForegroundColor Green
Write-Host "Mod installed to: $DestPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Files deployed:" -ForegroundColor White
Get-ChildItem $DestPath -Recurse | ForEach-Object {
    $rel = $_.FullName.Replace($DestPath, "").TrimStart("\")
    if ($_.PSIsContainer) {
        Write-Host "  [DIR] $rel" -ForegroundColor DarkGray
    } else {
        Write-Host "  $rel ($([math]::Round($_.Length/1KB, 1)) KB)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Remember: Launch 7D2D with EAC DISABLED for DLL mods!" -ForegroundColor Yellow
