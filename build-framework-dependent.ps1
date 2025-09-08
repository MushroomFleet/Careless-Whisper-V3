#!/usr/bin/env pwsh
# Build script for framework-dependent distribution

Write-Host "Building Careless Whisper V3 - Framework-Dependent Distribution" -ForegroundColor Green

# Clean previous builds
if (Test-Path "dist-framework-dependent") {
    Write-Host "Cleaning previous framework-dependent build..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force "dist-framework-dependent"
}

# Build framework-dependent version
Write-Host "Building framework-dependent executable..." -ForegroundColor Cyan
dotnet publish CarelessWhisperV2.csproj -c Release -r win-x64 --self-contained false -p:PublishProfile=FrameworkDependent -p:PublishReadyToRun=true -o dist-framework-dependent

if ($LASTEXITCODE -eq 0) {
    Write-Host "Framework-dependent build completed successfully!" -ForegroundColor Green
    
    # Copy distribution readme
    Copy-Item "DISTRIBUTION_README.md" "dist-framework-dependent/DISTRIBUTION_README.md"
    
    # Show build output
    Write-Host "`nBuild artifacts:" -ForegroundColor Cyan
    Get-ChildItem "dist-framework-dependent" | Format-Table Name, Length -AutoSize
    
    Write-Host "`nFramework-dependent distribution ready in: dist-framework-dependent/" -ForegroundColor Green
    Write-Host "Note: Requires .NET 8.0 Runtime to be installed on target machine" -ForegroundColor Yellow
} else {
    Write-Host "Framework-dependent build failed!" -ForegroundColor Red
    exit 1
}
