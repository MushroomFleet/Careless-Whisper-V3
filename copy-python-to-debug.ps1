# Quick fix script to copy Python environment to debug build directory
# Run this after any debug build to enable KittenTTS functionality

Write-Host "Copying Python environment to debug build directory..." -ForegroundColor Cyan

# Check if we're in the project root
if (!(Test-Path "CarelessWhisperV2.csproj")) {
    Write-Host "ERROR: Run this script from the Careless Whisper project root directory" -ForegroundColor Red
    exit 1
}

# Check if Python environment exists
if (!(Test-Path "python")) {
    Write-Host "ERROR: Python directory not found. Run scripts\setup_python_environment.ps1 first" -ForegroundColor Red
    exit 1
}

# Define debug path
$debugPath = "bin\Debug\net8.0-windows\win-x64"

# Check if debug build exists
if (!(Test-Path $debugPath)) {
    Write-Host "ERROR: Debug build directory not found at: $debugPath" -ForegroundColor Red
    Write-Host "   Build the project first (F5 in Visual Studio or dotnet build)" -ForegroundColor Yellow
    exit 1
}

try {
    # Clean existing Python/scripts directories in debug build
    if (Test-Path "$debugPath\python") {
        Write-Host "Cleaning existing Python directory..." -ForegroundColor Yellow
        Remove-Item -Path "$debugPath\python" -Recurse -Force
    }
    if (Test-Path "$debugPath\scripts") {
        Write-Host "Cleaning existing scripts directory..." -ForegroundColor Yellow
        Remove-Item -Path "$debugPath\scripts" -Recurse -Force
    }
    
    # Copy Python environment
    Write-Host "Copying python/ to debug build..." -ForegroundColor Green
    Copy-Item -Path "python" -Destination "$debugPath\python" -Recurse -Force
    
    # Copy scripts
    Write-Host "Copying scripts/ to debug build..." -ForegroundColor Green
    Copy-Item -Path "scripts" -Destination "$debugPath\scripts" -Recurse -Force
    
    # Copy KittenTTS dependencies to make embedded Python self-contained
    Write-Host "Making embedded Python self-contained..." -ForegroundColor Green
    $userSitePackages = "C:\Users\Genuine\AppData\Roaming\Python\Python311\site-packages"
    $embeddedSitePackages = "$debugPath\python\Lib\site-packages"
    
    if (Test-Path $userSitePackages) {
        New-Item -Path $embeddedSitePackages -ItemType Directory -Force | Out-Null
        Copy-Item -Path "$userSitePackages\*" -Destination $embeddedSitePackages -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "✅ Dependencies copied to embedded Python" -ForegroundColor Green
    }
    
    # Ensure our custom patches are in the embedded Python
    Copy-Item -Path "python\kitten_number_converter.py" -Destination "$debugPath\python\kitten_number_converter.py" -Force
    Copy-Item -Path "python\kitten_tts_bridge.py" -Destination "$debugPath\scripts\kitten_tts_bridge.py" -Force
    
    # Verify the copy
    $pythonExe = "$debugPath\python\python.exe"
    $bridgeScript = "$debugPath\scripts\kitten_tts_bridge.py"
    
    if ((Test-Path $pythonExe) -and (Test-Path $bridgeScript)) {
        Write-Host "SUCCESS: Python environment successfully copied to debug build!" -ForegroundColor Green
        Write-Host "Location: $debugPath\python\" -ForegroundColor White
        Write-Host "Python executable: $pythonExe" -ForegroundColor White
        Write-Host "Bridge script: $bridgeScript" -ForegroundColor White
        
        Write-Host ""
        Write-Host "KittenTTS should now work in debug mode!" -ForegroundColor Cyan
        Write-Host "Test with Ctrl+F1 after restarting CarelessWhisper" -ForegroundColor White
    } else {
        Write-Host "WARNING: Copy completed but some files may be missing" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "ERROR: Copy failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
