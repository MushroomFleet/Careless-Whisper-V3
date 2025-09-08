#!/usr/bin/env pwsh
# Build script for standalone distribution

Write-Host "Building Careless Whisper V3 - Standalone Distribution" -ForegroundColor Green

# Clean previous builds
if (Test-Path "dist-standalone") {
    Write-Host "Cleaning previous standalone build..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force "dist-standalone"
}

# Build standalone version
Write-Host "Building standalone executable..." -ForegroundColor Cyan
dotnet publish CarelessWhisperV2.csproj -c Release -r win-x64 --self-contained true -p:PublishProfile=Standalone -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true -o dist-standalone

if ($LASTEXITCODE -eq 0) {
    Write-Host "Standalone build completed successfully!" -ForegroundColor Green
    
    # Copy distribution readme
    Copy-Item "DISTRIBUTION_README.md" "dist-standalone/DISTRIBUTION_README.md"
    
    # Show build output
    Write-Host "`nBuild artifacts:" -ForegroundColor Cyan
    Get-ChildItem "dist-standalone" | Format-Table Name, Length -AutoSize
    
    Write-Host "`nStandalone distribution ready in: dist-standalone/" -ForegroundColor Green
} else {
    Write-Host "Standalone build failed!" -ForegroundColor Red
    exit 1
}
