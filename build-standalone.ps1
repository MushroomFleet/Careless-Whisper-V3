#!/usr/bin/env pwsh
# Build script for standalone distribution with CarelessKitten TTS integration

Write-Host "🐱 Building Careless Whisper V3.6.5 - Standalone Distribution with CarelessKitten TTS" -ForegroundColor Green

# Clean previous builds
if (Test-Path "dist-standalone") {
    Write-Host "Cleaning previous standalone build..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force "dist-standalone"
}

# Step 1: Setup Python TTS environment
Write-Host "🐍 Setting up embedded Python environment for CarelessKitten TTS..." -ForegroundColor Cyan

if (Test-Path "scripts\setup_python_environment.ps1") {
    & "scripts\setup_python_environment.ps1"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠️  Python setup failed, continuing without embedded TTS" -ForegroundColor Yellow
    } else {
        Write-Host "✅ Python TTS environment ready" -ForegroundColor Green
    }
} else {
    Write-Host "⚠️  Python setup script not found" -ForegroundColor Yellow
}

# Step 1.1: Sync fixed KittenTTS bridge from python/ to scripts/
if (Test-Path "python\kitten_tts_bridge.py") {
    Write-Host "🔄 Syncing fixed KittenTTS bridge script..." -ForegroundColor Blue
    Copy-Item -Path "python\kitten_tts_bridge.py" -Destination "scripts\kitten_tts_bridge.py" -Force
    Write-Host "✅ KittenTTS bridge script synchronized" -ForegroundColor Green
} else {
    Write-Host "⚠️  Fixed KittenTTS bridge not found in python/ directory" -ForegroundColor Yellow
}

# Step 2: Build standalone version
Write-Host "🏗️  Building standalone executable..." -ForegroundColor Cyan
dotnet publish CarelessWhisperV2.csproj -c Release -r win-x64 --self-contained true -p:PublishProfile=Standalone -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true -o dist-standalone

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Standalone build completed successfully!" -ForegroundColor Green
    
    # Step 3: Copy Python TTS environment to standalone distribution
    if (Test-Path "python") {
        Write-Host "🐍 Copying Python environment to standalone distribution..." -ForegroundColor Blue
        
        if (Test-Path "dist-standalone\python") {
            Remove-Item -Path "dist-standalone\python" -Recurse -Force
        }
        if (Test-Path "dist-standalone\scripts") {
            Remove-Item -Path "dist-standalone\scripts" -Recurse -Force
        }
        
        Copy-Item -Path "python" -Destination "dist-standalone\python" -Recurse -Force
        Copy-Item -Path "scripts" -Destination "dist-standalone\scripts" -Recurse -Force
        Write-Host "✅ CarelessKitten TTS environment copied to standalone distribution" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Python environment not found - TTS features may not work" -ForegroundColor Yellow
    }
    
    # Copy distribution readme
    Copy-Item "DISTRIBUTION_README.md" "dist-standalone/DISTRIBUTION_README.md"
    
    # Rename executable to include version
    if (Test-Path "dist-standalone\CarelessWhisperV2.exe") {
        Rename-Item -Path "dist-standalone\CarelessWhisperV2.exe" -NewName "CarelessWhisperV3.6.5-portable.exe"
        Write-Host "✅ Executable renamed to CarelessWhisperV3.6.5-portable.exe" -ForegroundColor Green
    }
    
    # Show build output
    Write-Host "`n🎉 Build artifacts:" -ForegroundColor Cyan
    Get-ChildItem "dist-standalone" | Format-Table Name, Length -AutoSize
    
    $totalSize = (Get-ChildItem "dist-standalone" -Recurse | Measure-Object -Property Length -Sum).Sum
    $totalSizeMB = [math]::Round($totalSize / 1MB, 1)
    
    Write-Host "`n🚀 Careless Whisper V3.6.5 with CarelessKitten TTS ready!" -ForegroundColor Green
    Write-Host "📁 Location: dist-standalone/" -ForegroundColor Green
    Write-Host "📊 Total size: $totalSizeMB MB (includes embedded Python + neural TTS)" -ForegroundColor Green
    Write-Host "🐱 Features: 6-mode processing with 8 neural voices!" -ForegroundColor Magenta
} else {
    Write-Host "❌ Standalone build failed!" -ForegroundColor Red
    exit 1
}
