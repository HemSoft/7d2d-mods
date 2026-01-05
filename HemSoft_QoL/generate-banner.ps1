# Generate banner using OpenRouter image model
param(
    [string]$Prompt = "Gaming mod banner 1280x720 for 7 Days to Die zombie survival game. Dark apocalyptic theme with ruined city background. Bold white text 'HemSoft QoL' centered. Subtle inventory icons and HUD elements. Professional mod banner style.",
    [string]$OutputPath = "$PSScriptRoot\images\banner.png",
    [string]$Model = "bytedance-seed/seedream-4.5"
)

$ErrorActionPreference = "Stop"
$apiKey = $env:OPENROUTER_API_KEY
if (-not $apiKey) { Write-Error "OPENROUTER_API_KEY not set"; exit 1 }

$outputDir = Split-Path $OutputPath -Parent
if (-not (Test-Path $outputDir)) { New-Item -ItemType Directory -Path $outputDir -Force | Out-Null }

Write-Host "Generating with $Model..." -ForegroundColor Cyan

$body = @{
    model = $Model
    messages = @(@{ role = "user"; content = "Generate an image: $Prompt" })
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri "https://openrouter.ai/api/v1/chat/completions" -Method Post `
        -Headers @{ "Authorization" = "Bearer $apiKey"; "Content-Type" = "application/json" } -Body $body
    
    $msg = $response.choices[0].message
    
    # Handle images array (OpenRouter format)
    if ($msg.images) {
        $img = @($msg.images)[0]
        if ($img.image_url) {
            $imgUrl = if ($img.image_url.url) { $img.image_url.url } else { $img.image_url }
            if ($imgUrl -match '^data:image/[^;]+;base64,(.+)$') {
                [IO.File]::WriteAllBytes($OutputPath, [Convert]::FromBase64String($matches[1]))
            } else {
                Invoke-WebRequest -Uri $imgUrl -OutFile $OutputPath
            }
            Write-Host "SAVED: $OutputPath ($([math]::Round((Get-Item $OutputPath).Length/1024))KB)" -ForegroundColor Green
            exit 0
        }
        if ($img.b64_json) {
            [IO.File]::WriteAllBytes($OutputPath, [Convert]::FromBase64String($img.b64_json))
            Write-Host "SAVED: $OutputPath ($([math]::Round((Get-Item $OutputPath).Length/1024))KB)" -ForegroundColor Green
            exit 0
        }
    }
    
    # Handle inline base64 in content
    if ($msg.content -match 'base64,([A-Za-z0-9+/=]+)') {
        [IO.File]::WriteAllBytes($OutputPath, [Convert]::FromBase64String($matches[1]))
        Write-Host "SAVED: $OutputPath" -ForegroundColor Green
        exit 0
    }
    
    Write-Host "No image data found in response" -ForegroundColor Yellow
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
