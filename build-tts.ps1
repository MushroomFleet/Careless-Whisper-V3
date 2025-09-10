# Build Script for Careless Whisper V3.6.5 with TTS Integration
# Builds both framework-dependent and standalone versions with Python TTS support

param(
    [switch]$SkipTTS = $false,
    [switch]$TestOnly = $false,
    [switch]$Verbose = $false
)

Write-Host "🐱 Building Careless Whisper V3.6.5 with CarelessKitten TTS..." -ForegroundColor Cyan

# Check if we're in the project root
if (!(Test-Path "CarelessWhisperV2.csproj")) {
    Write-Host "❌ Error: Run this script from the Careless Whisper project root directory" -ForegroundColor Red
    exit 1
}

try {
    # Step 1: Setup Python environment for TTS if not skipped
    if (!$SkipTTS) {
        Write-Host "🐍 Setting up Python environment for TTS..." -ForegroundColor Green
        
        if (Test-Path "scripts\setup_python_environment.ps1") {
            & "scripts\setup_python_environment.ps1" -Verbose:$Verbose
            if ($LASTEXITCODE -ne 0) {
                Write-Host "⚠️  Python setup failed, continuing without embedded TTS" -ForegroundColor Yellow
                $SkipTTS = $true
            } else {
                Write-Host "✅ Python TTS environment ready" -ForegroundColor Green
            }
        } else {
            Write-Host "⚠️  Python setup script not found, skipping TTS setup" -ForegroundColor Yellow
            $SkipTTS = $true
        }
        
        # Step 1.1: Sync fixed KittenTTS bridge from python/ to scripts/
        if (Test-Path "python\kitten_tts_bridge.py") {
            Write-Host "🔄 Syncing fixed KittenTTS bridge script..." -ForegroundColor Blue
            Copy-Item -Path "python\kitten_tts_bridge.py" -Destination "scripts\kitten_tts_bridge.py" -Force
            Write-Host "✅ KittenTTS bridge script synchronized" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Fixed KittenTTS bridge not found in python/ directory" -ForegroundColor Yellow
        }
    }
    
    if ($TestOnly) {
        Write-Host "🧪 Test mode - skipping actual build" -ForegroundColor Yellow
        Write-Host "✅ All preparation steps completed successfully" -ForegroundColor Green
        exit 0
    }
    
    # Step 2: Build framework-dependent version
    Write-Host "🏗️  Building framework-dependent version..." -ForegroundColor Blue
    
    if (Test-Path "build-framework-dependent.ps1") {
        & "build-framework-dependent.ps1"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Framework-dependent build failed" -ForegroundColor Red
            exit 1
        } else {
            Write-Host "✅ Framework-dependent build completed" -ForegroundColor Green
        }
    } else {
        Write-Host "⚠️  Framework-dependent build script not found" -ForegroundColor Yellow
    }
    
    # Step 3: Copy Python environment to both debug and release builds if TTS enabled
    if (!$SkipTTS -and (Test-Path "python")) {
        
        # Copy to debug build directory (for development/F5 runs)
        $debugPath = "bin\Debug\net8.0-windows\win-x64"
        if (Test-Path $debugPath) {
            Write-Host "🐍 Copying Python environment to debug build..." -ForegroundColor Blue
            
            if (Test-Path "$debugPath\python") {
                Remove-Item -Path "$debugPath\python" -Recurse -Force
            }
            if (Test-Path "$debugPath\scripts") {
                Remove-Item -Path "$debugPath\scripts" -Recurse -Force
            }
            
            Copy-Item -Path "python" -Destination "$debugPath\python" -Recurse -Force
            Copy-Item -Path "scripts" -Destination "$debugPath\scripts" -Recurse -Force
            Write-Host "✅ TTS environment copied to debug build" -ForegroundColor Green
        }
        
        # Copy to framework-dependent release build
        if (Test-Path "dist-framework-dependent") {
            Write-Host "🐍 Copying Python environment to framework-dependent build..." -ForegroundColor Blue
            
            if (Test-Path "dist-framework-dependent\python") {
                Remove-Item -Path "dist-framework-dependent\python" -Recurse -Force
            }
            if (Test-Path "dist-framework-dependent\scripts") {
                Remove-Item -Path "dist-framework-dependent\scripts" -Recurse -Force
            }
            
            Copy-Item -Path "python" -Destination "dist-framework-dependent\python" -Recurse -Force
            Copy-Item -Path "scripts" -Destination "dist-framework-dependent\scripts" -Recurse -Force
            Write-Host "✅ TTS environment copied to framework-dependent build" -ForegroundColor Green
        }
    }
    
    # Step 4: Build standalone version
    Write-Host "📦 Building standalone version..." -ForegroundColor Blue
    
    if (Test-Path "build-standalone.ps1") {
        & "build-standalone.ps1"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Standalone build failed" -ForegroundColor Red
            exit 1
        } else {
            Write-Host "✅ Standalone build completed" -ForegroundColor Green
        }
    } else {
        Write-Host "⚠️  Standalone build script not found" -ForegroundColor Yellow
    }
    
    # Step 5: Generate build summary
    Write-Host ""
    Write-Host "🎉 Careless Whisper V3.6.5 Build Complete!" -ForegroundColor Green
    
    if (Test-Path "dist-framework-dependent") {
        $frameworkSize = (Get-ChildItem -Path "dist-framework-dependent" -Recurse | Measure-Object -Property Length -Sum).Sum
        $frameworkSizeMB = [math]::Round($frameworkSize / 1MB, 1)
        Write-Host "📁 Framework-dependent: dist-framework-dependent ($frameworkSizeMB MB)" -ForegroundColor White
    }
    
    if (Test-Path "dist-standalone") {
        $standaloneSize = (Get-ChildItem -Path "dist-standalone" -Recurse | Measure-Object -Property Length -Sum).Sum
        $standaloneSizeMB = [math]::Round($standaloneSize / 1MB, 1)
        Write-Host "📦 Standalone: dist-standalone ($standaloneSizeMB MB)" -ForegroundColor White
    }
    
    Write-Host ""
    Write-Host "🐱 CarelessKitten TTS Features:" -ForegroundColor Magenta
    if (!$SkipTTS) {
        Write-Host "   ✅ Embedded Python environment included" -ForegroundColor Green
        Write-Host "   ✅ KittenTTS neural voices ready" -ForegroundColor Green
        Write-Host "   ✅ Ctrl+F1 hotkey for clipboard reading" -ForegroundColor Green
        Write-Host "   ✅ Windows SAPI fallback available" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  TTS features disabled (use system Python)" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "🚀 Ready for distribution!" -ForegroundColor Cyan
    Write-Host "   Ctrl+F1: Text-to-Speech from clipboard" -ForegroundColor White
    
} catch {
    Write-Host ""
    Write-Host "❌ Build failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "🔍 Check the error details above and try again" -ForegroundColor Yellow
    exit 1
}
