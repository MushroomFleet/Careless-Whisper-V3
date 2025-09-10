# Setup Python Environment for CarelessKitten TTS
# Careless Whisper V3.6.5 - TTS Integration Script

param(
    [string]$OutputDir = "python",
    [switch]$SkipKittenTTS = $false,
    [switch]$Verbose = $false
)

Write-Host "Setting up CarelessKitten TTS Python Environment..." -ForegroundColor Cyan

# Check if we're in the project root
if (!(Test-Path "CarelessWhisperV2.csproj")) {
    Write-Host "Error: Run this script from the Careless Whisper project root directory" -ForegroundColor Red
    exit 1
}

# Clean existing python directory if it exists
if (Test-Path $OutputDir) {
    Write-Host "Cleaning existing Python directory..." -ForegroundColor Yellow
    Remove-Item -Path $OutputDir -Recurse -Force
}

# Create python directory
New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null

try {
    # Step 1: Download Python embeddable package
    Write-Host "Downloading Python 3.11 embeddable package..." -ForegroundColor Green
    
    $pythonUrl = "https://www.python.org/ftp/python/3.11.9/python-3.11.9-embed-amd64.zip"
    $pythonZip = Join-Path $OutputDir "python-embed.zip"
    
    # Download Python
    Invoke-WebRequest -Uri $pythonUrl -OutFile $pythonZip -UseBasicParsing
    
    # Extract Python
    Write-Host "Extracting Python to $OutputDir..." -ForegroundColor Green
    Expand-Archive -Path $pythonZip -DestinationPath $OutputDir -Force
    Remove-Item -Path $pythonZip
    
    # Step 2: Enable pip in embedded Python
    Write-Host "Configuring embedded Python..." -ForegroundColor Green
    
    $pthFile = Join-Path $OutputDir "python311._pth"
    if (Test-Path $pthFile) {
        # Uncomment import site to enable pip
        $pthContent = Get-Content $pthFile
        $pthContent = $pthContent -replace "#import site", "import site"
        $pthContent | Set-Content $pthFile
        Write-Host "Enabled site-packages in Python configuration" -ForegroundColor Green
    }
    
    # Step 3: Download and install get-pip.py
    Write-Host "Installing pip..." -ForegroundColor Green
    
    $pipUrl = "https://bootstrap.pypa.io/get-pip.py"
    $getPipPath = Join-Path $OutputDir "get-pip.py"
    
    Invoke-WebRequest -Uri $pipUrl -OutFile $getPipPath -UseBasicParsing
    
    # Install pip
    $pythonExe = Join-Path $OutputDir "python.exe"
    & $pythonExe $getPipPath --user --no-warn-script-location
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Pip installed successfully" -ForegroundColor Green
        Remove-Item -Path $getPipPath
    } else {
        Write-Host "WARNING: Pip installation returned exit code $LASTEXITCODE" -ForegroundColor Yellow
    }
    
    # Step 4: Install KittenTTS if not skipped
    if (!$SkipKittenTTS) {
        Write-Host "Installing KittenTTS..." -ForegroundColor Magenta
        
        $kittenTtsUrl = "https://github.com/KittenML/KittenTTS/releases/download/0.1/kittentts-0.1.0-py3-none-any.whl"
        
        # Install KittenTTS directly from URL
        & $pythonExe -m pip install $kittenTtsUrl --user --no-warn-script-location
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "KittenTTS installed successfully" -ForegroundColor Green
        } else {
            Write-Host "ERROR: KittenTTS installation failed with exit code $LASTEXITCODE" -ForegroundColor Red
            Write-Host "You may need to install KittenTTS manually later" -ForegroundColor Yellow
        }
    }
    
    # Step 5: Verify installation
    Write-Host "Verifying Python environment..." -ForegroundColor Blue
    
    # Test basic Python functionality
    $testResult = & $pythonExe -c "import sys; print(f'Python {sys.version}')" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Python verification: $testResult" -ForegroundColor Green
    } else {
        Write-Host "ERROR: Python verification failed" -ForegroundColor Red
    }
    
    # Test KittenTTS if not skipped
    if (!$SkipKittenTTS) {
        Write-Host "Testing KittenTTS import..." -ForegroundColor Magenta
        $kittenTestResult = & $pythonExe -c "try:`n    from kittentts import KittenTTS`n    print('KittenTTS import successful')`nexcept ImportError as e:`n    print(f'KittenTTS import failed: {e}')" 2>&1
        
        if ($kittenTestResult -match "successful") {
            Write-Host "KittenTTS verification: $kittenTestResult" -ForegroundColor Green
        } else {
            Write-Host "WARNING: KittenTTS verification: $kittenTestResult" -ForegroundColor Yellow
        }
    }
    
    # Step 6: Copy bridge script
    Write-Host "Copying bridge script..." -ForegroundColor Blue
    
    $bridgeSource = "scripts\kitten_tts_bridge.py"
    $bridgeTarget = Join-Path $OutputDir "kitten_tts_bridge.py"
    
    if (Test-Path $bridgeSource) {
        Copy-Item -Path $bridgeSource -Destination $bridgeTarget
        Write-Host "Bridge script copied to Python directory" -ForegroundColor Green
    } else {
        Write-Host "WARNING: Bridge script not found at $bridgeSource" -ForegroundColor Yellow
    }
    
    # Step 7: Test the complete TTS pipeline
    if (!$SkipKittenTTS) {
        Write-Host "Testing complete TTS pipeline..." -ForegroundColor Magenta
        
        $testScript = Join-Path $OutputDir "kitten_tts_bridge.py"
        if (Test-Path $testScript) {
            $pipelineTest = & $pythonExe $testScript --list-voices 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Complete TTS pipeline test successful" -ForegroundColor Green
                if ($Verbose) {
                    Write-Host "Pipeline output: $pipelineTest" -ForegroundColor Gray
                }
            } else {
                Write-Host "WARNING: TTS pipeline test failed: $pipelineTest" -ForegroundColor Yellow
            }
        }
    }
    
    # Step 8: Generate summary
    Write-Host ""
    Write-Host "Python Environment Setup Complete!" -ForegroundColor Green
    Write-Host "Location: $OutputDir" -ForegroundColor White
    Write-Host "Python executable: $pythonExe" -ForegroundColor White
    Write-Host "KittenTTS: $(if ($SkipKittenTTS) { 'Skipped' } else { 'Installed' })" -ForegroundColor White
    
    # Calculate directory size
    $dirSize = (Get-ChildItem -Path $OutputDir -Recurse | Measure-Object -Property Length -Sum).Sum
    $dirSizeMB = [math]::Round($dirSize / 1MB, 1)
    Write-Host "Size: $dirSizeMB MB" -ForegroundColor White
    
    Write-Host ""
    Write-Host "CarelessKitten TTS is ready for deployment!" -ForegroundColor Cyan
    Write-Host "   Users can now press Ctrl+F1 to read clipboard content aloud" -ForegroundColor White
    
} catch {
    Write-Host ""
    Write-Host "ERROR: Setup failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Check your internet connection and try again" -ForegroundColor Yellow
    Write-Host "For manual setup, see docs/TTS_INTEGRATION_GUIDE.md" -ForegroundColor White
    exit 1
}
