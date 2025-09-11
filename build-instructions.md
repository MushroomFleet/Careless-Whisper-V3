# Careless Whisper V3.7.0 - Build Instructions

**Developer/Maintainer Guide for Building Distribution Releases**

This document provides step-by-step instructions for building both portable (standalone) and framework-dependent distribution releases of Careless Whisper V3.7.0.

---

## 📋 Prerequisites

### Required Software
- **.NET 8.0 SDK** (not just runtime) - [Download from Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Windows 10/11** (64-bit) - Required for Windows-specific features
- **PowerShell 7.0+** - For running build scripts
- **Git** (optional) - For version control operations

### Required Files
Ensure these files are present in the project root:
- `CarelessWhisperV2.csproj` - Main project file
- `ggml-tiny.bin` - Whisper model file (77MB)
- `build-standalone.ps1` - Standalone build script
- `build-framework-dependent.ps1` - Framework-dependent build script
- `DISTRIBUTION_README.md` - User distribution guide

### System Configuration
- **Developer Mode** enabled in Windows (recommended)
- **Execution Policy** set to allow PowerShell scripts:
  ```powershell
  Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
  ```

---

## 🚀 Quick Build Commands

### Option 1: Use Existing PowerShell Scripts (Recommended)

#### Standalone Distribution
```powershell
.\build-standalone.ps1
```

#### (optional - fix for unauthorized)
```powershell
Unblock-File -Path ".\build-standalone.ps1"
Unblock-File -Path ".\build-framework-dependent.ps1"
```

#### Framework-Dependent Distribution
```powershell
.\build-framework-dependent.ps1
```

### Option 2: Manual Commands (Advanced)

#### Standalone Distribution (Self-Contained)
```bash
# Clean previous builds
Remove-Item -Recurse -Force "dist-standalone" -ErrorAction SilentlyContinue

# Build standalone
dotnet publish CarelessWhisperV2.csproj -c Release -r win-x64 --self-contained true -p:PublishProfile=Standalone -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true -o dist-standalone

# Copy distribution readme
Copy-Item "DISTRIBUTION_README.md" "dist-standalone/DISTRIBUTION_README.md"
```

#### Framework-Dependent Distribution
```bash
# Clean previous builds
Remove-Item -Recurse -Force "dist-framework-dependent" -ErrorAction SilentlyContinue

# Build framework-dependent
dotnet publish CarelessWhisperV2.csproj -c Release -r win-x64 --self-contained false -p:PublishProfile=FrameworkDependent -p:PublishReadyToRun=true -o dist-framework-dependent

# Copy distribution readme
Copy-Item "DISTRIBUTION_README.md" "dist-framework-dependent/DISTRIBUTION_README.md"
```

---

## 📁 Expected Build Outputs

### Standalone Distribution (`dist-standalone/`)
```
dist-standalone/
├── CarelessWhisperV2.exe         (~87MB - Single file with everything)
├── ggml-tiny.bin                 (77MB - Whisper model)
├── DISTRIBUTION_README.md        (User guide)
└── CarelessWhisperV2.pdb         (Debug symbols, optional)

Total Size: ~157MB
```

### Framework-Dependent Distribution (`dist-framework-dependent/`)
```
dist-framework-dependent/
├── CarelessWhisperV2.exe         (~139KB - Main executable)
├── CarelessWhisperV2.dll         (~2MB - Main assembly)
├── ggml-tiny.bin                 (77MB - Whisper model)
├── *.dll files                   (~9MB - Dependencies)
├── runtimes/win-x64/             (Native libraries)
│   ├── ggml-base-whisper.dll
│   ├── ggml-cpu-whisper.dll
│   ├── ggml-whisper.dll
│   ├── whisper.dll
│   └── SharpHook.dll
├── DISTRIBUTION_README.md        (User guide)
└── CarelessWhisperV2.pdb         (Debug symbols, optional)

Total Size: ~86MB
```

---

## 🔧 Detailed Build Process

### Step 1: Environment Preparation

1. **Open PowerShell as Administrator** (recommended)
2. **Navigate to project root**:
   ```powershell
   cd "c:\Projects\careless-whisper-3.7.0\Careless-Whisper-V3-370\Careless-Whisper-V3-370"
   ```
3. **Verify .NET SDK**:
   ```bash
   dotnet --version
   # Should show 8.0.x or higher
   ```
4. **Clean any previous builds**:
   ```bash
   dotnet clean CarelessWhisperV2.csproj
   ```

### Step 2: Build Configuration Verification

1. **Check project file version** in `CarelessWhisperV2.csproj`:
   ```xml
   <Version>3.7.0</Version>
   <AssemblyVersion>3.7.0.0</AssemblyVersion>
   <FileVersion>3.7.0.0</FileVersion>
   ```

2. **Verify required files exist**:
   ```powershell
   # Check for Whisper model
   if (!(Test-Path "ggml-tiny.bin")) { 
       Write-Error "ggml-tiny.bin not found!" 
   }
   
   # Check project file
   if (!(Test-Path "CarelessWhisperV2.csproj")) { 
       Write-Error "Project file not found!" 
   }
   ```

### Step 3: Build Standalone Distribution

1. **Run the build command**:
   ```bash
   dotnet publish CarelessWhisperV2.csproj -c Release -r win-x64 --self-contained true -p:PublishProfile=Standalone -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true -o dist-standalone
   ```

2. **Verify build success**:
   ```powershell
   # Check exit code
   if ($LASTEXITCODE -ne 0) {
       Write-Error "Build failed with exit code $LASTEXITCODE"
       exit 1
   }
   
   # Check output files
   Get-ChildItem "dist-standalone" | Format-Table Name, Length -AutoSize
   ```

3. **Copy distribution files**:
   ```powershell
   Copy-Item "DISTRIBUTION_README.md" "dist-standalone/"
   ```

### Step 4: Build Framework-Dependent Distribution

1. **Run the build command**:
   ```bash
   dotnet publish CarelessWhisperV2.csproj -c Release -r win-x64 --self-contained false -p:PublishProfile=FrameworkDependent -p:PublishReadyToRun=true -o dist-framework-dependent
   ```

2. **Verify build success**:
   ```powershell
   # Check exit code
   if ($LASTEXITCODE -ne 0) {
       Write-Error "Build failed with exit code $LASTEXITCODE"
       exit 1
   }
   
   # Check output files
   Get-ChildItem "dist-framework-dependent" | Format-Table Name, Length -AutoSize
   ```

3. **Copy distribution files**:
   ```powershell
   Copy-Item "DISTRIBUTION_README.md" "dist-framework-dependent/"
   ```

---

## ✅ Build Verification

### Automated Verification Script
```powershell
# Verify standalone build
if (Test-Path "dist-standalone/CarelessWhisperV2.exe") {
    $standaloneSize = (Get-Item "dist-standalone/CarelessWhisperV2.exe").Length / 1MB
    Write-Host "✅ Standalone EXE: $($standaloneSize.ToString("F1")) MB" -ForegroundColor Green
    
    # Check if it's actually self-contained (should be large)
    if ($standaloneSize -lt 50) {
        Write-Warning "⚠️ Standalone EXE seems too small - verify self-contained build"
    }
} else {
    Write-Error "❌ Standalone build failed - EXE not found"
}

# Verify framework-dependent build
if (Test-Path "dist-framework-dependent/CarelessWhisperV2.exe") {
    $frameworkSize = (Get-Item "dist-framework-dependent/CarelessWhisperV2.exe").Length / 1KB
    Write-Host "✅ Framework-dependent EXE: $($frameworkSize.ToString("F1")) KB" -ForegroundColor Green
    
    # Check if dependencies are present
    $dllCount = (Get-ChildItem "dist-framework-dependent/*.dll").Count
    Write-Host "✅ Dependencies found: $dllCount DLL files" -ForegroundColor Green
    
    # Check for runtime folder
    if (Test-Path "dist-framework-dependent/runtimes") {
        Write-Host "✅ Native runtime libraries present" -ForegroundColor Green
    } else {
        Write-Warning "⚠️ Runtime folder missing"
    }
} else {
    Write-Error "❌ Framework-dependent build failed - EXE not found"
}

# Verify Whisper model is present in both builds
@("dist-standalone", "dist-framework-dependent") | ForEach-Object {
    if (Test-Path "$_/ggml-tiny.bin") {
        $modelSize = (Get-Item "$_/ggml-tiny.bin").Length / 1MB
        Write-Host "✅ $($_): Whisper model present ($($modelSize.ToString("F1")) MB)" -ForegroundColor Green
    } else {
        Write-Error "❌ $($_): Whisper model missing"
    }
}
```

### Manual Testing
1. **Test standalone version**:
   ```bash
   cd dist-standalone
   .\CarelessWhisperV2.exe --debug
   # Should start without requiring .NET runtime
   ```

2. **Test framework-dependent version**:
   ```bash
   cd dist-framework-dependent
   .\CarelessWhisperV2.exe --debug
   # Should start if .NET 8.0 runtime is installed
   ```

---

## 🚨 Common Issues & Troubleshooting

### Build Failures

#### "Project not found" Error
**Cause**: Wrong directory or missing project file
**Solution**:
```bash
# Verify you're in the correct directory
Get-Location
# Should show path ending with the project folder

# Check project file exists
Test-Path "CarelessWhisperV2.csproj"
# Should return True
```

#### "Whisper native libraries not found" Error
**Cause**: Missing Whisper.net runtime dependencies
**Solution**:
```bash
# Restore NuGet packages
dotnet restore CarelessWhisperV2.csproj

# Clear NuGet cache if needed
dotnet nuget locals all --clear
```

#### "Self-contained build too small" Warning
**Cause**: Build might not be truly self-contained
**Solution**:
```bash
# Verify publish parameters
dotnet publish CarelessWhisperV2.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -v normal -o dist-standalone
# Check verbose output for issues
```

### Runtime Issues

#### "ggml-tiny.bin not found" Error
**Cause**: Whisper model file missing from output
**Solution**:
- Verify `ggml-tiny.bin` exists in project root
- Check `CarelessWhisperV2.csproj` includes:
  ```xml
  <Content Include="ggml-tiny.bin" CopyToOutputDirectory="PreserveNewest" />
  ```

#### Native Library Issues
**Cause**: Platform-specific dependencies missing
**Solution**:
- Ensure build targets `win-x64` specifically
- Verify `runtimes` folder is included in framework-dependent builds

---

## 📦 Release Preparation

### Pre-Release Checklist

1. **Version Consistency Check**:
   ```bash
   # Verify version in project file
   Select-String -Path "CarelessWhisperV2.csproj" -Pattern "<Version>"
   
   # Verify version in README
   Select-String -Path "README.md" -Pattern "Current Version"
   
   # Should both show 3.7.0
   ```

2. **Dependency Update Check**:
   ```bash
   # List outdated packages
   dotnet list CarelessWhisperV2.csproj package --outdated
   ```

3. **Clean Build Test**:
   ```bash
   # Full clean and rebuild
   dotnet clean CarelessWhisperV2.csproj
   dotnet build CarelessWhisperV2.csproj -c Release
   ```

### Post-Build Steps

1. **Create distribution packages**:
   ```powershell
   # Create ZIP files for distribution
   Compress-Archive -Path "dist-standalone\*" -DestinationPath "CarelessWhisper-V3.7.0-Standalone.zip"
   Compress-Archive -Path "dist-framework-dependent\*" -DestinationPath "CarelessWhisper-V3.7.0-FrameworkDependent.zip"
   ```

2. **Generate checksums**:
   ```powershell
   # Create SHA256 checksums for verification
   Get-FileHash "CarelessWhisper-V3.7.0-Standalone.zip" -Algorithm SHA256 | Out-File "checksums.txt"
   Get-FileHash "CarelessWhisper-V3.7.0-FrameworkDependent.zip" -Algorithm SHA256 | Out-File "checksums.txt" -Append
   ```

3. **Size verification**:
   ```powershell
   # Check final package sizes
   Get-ChildItem "*.zip" | Format-Table Name, @{Name="Size (MB)"; Expression={[math]::Round($_.Length/1MB,1)}}
   
   # Expected sizes:
   # Standalone: ~80-90MB (compressed)
   # Framework-dependent: ~45-55MB (compressed)
   ```

---

## 🔍 Build Parameter Reference

### Standalone Build Parameters
| Parameter | Value | Purpose |
|-----------|-------|---------|
| `-c Release` | Release configuration | Optimized build |
| `-r win-x64` | Windows 64-bit target | Platform targeting |
| `--self-contained true` | Include .NET runtime | Portable executable |
| `-p:PublishProfile=Standalone` | Custom profile | Triggers single-file mode |
| `-p:PublishSingleFile=true` | Single file output | Merges all DLLs into EXE |
| `-p:IncludeNativeLibrariesForSelfExtract=true` | Include native libs | Whisper.net support |
| `-p:PublishReadyToRun=true` | AOT compilation | Faster startup |
| `-o dist-standalone` | Output directory | Where files go |

### Framework-Dependent Build Parameters
| Parameter | Value | Purpose |
|-----------|-------|---------|
| `-c Release` | Release configuration | Optimized build |
| `-r win-x64` | Windows 64-bit target | Platform targeting |
| `--self-contained false` | Exclude .NET runtime | Smaller size |
| `-p:PublishProfile=FrameworkDependent` | Custom profile | Framework mode |
| `-p:PublishReadyToRun=true` | AOT compilation | Faster startup |
| `-o dist-framework-dependent` | Output directory | Where files go |

---

## 🧪 Testing Builds

### Standalone Testing
1. **Copy to clean Windows machine** (or VM without .NET 8.0)
2. **Run without installation**:
   ```bash
   # Should work immediately
   .\CarelessWhisperV2.exe
   ```
3. **Test core features**:
   - F1 (Speech-to-Text)
   - Ctrl+F1 (Text-to-Speech) - **NEW in v3.7.0**
   - Settings window opens
   - System tray integration works

### Framework-Dependent Testing
1. **Ensure .NET 8.0 Runtime installed**:
   ```bash
   dotnet --list-runtimes | Select-String "Microsoft.WindowsDesktop.App 8.0"
   # Should show installed runtime
   ```
2. **Run application**:
   ```bash
   .\CarelessWhisperV2.exe
   ```
3. **Test all features** (same as standalone)

---

## 📋 Distribution Checklist

### Before Building
- [ ] Version updated to 3.7.0 in all files
- [ ] All TTS features tested and working
- [ ] No debug code or test files in project
- [ ] `ggml-tiny.bin` present and correct size (77MB)
- [ ] All NuGet packages restored and up-to-date

### After Building
- [ ] Both distributions built successfully
- [ ] File sizes match expectations
- [ ] EXE files run without errors
- [ ] All hotkeys work (F1, Shift+F2, Ctrl+F2, Shift+F3, Ctrl+F3, Ctrl+F1)
- [ ] TTS functionality working (Ctrl+F1 + Escape)
- [ ] Settings window opens and saves properly
- [ ] System tray integration functional
- [ ] Distribution README copied to both folders

### Release Preparation
- [ ] ZIP files created with correct names
- [ ] Checksums generated
- [ ] File sizes documented
- [ ] Release notes prepared
- [ ] GitHub release drafted (if applicable)

---

## 🐛 Advanced Troubleshooting

### Build Performance Issues
```bash
# Use parallel build for faster compilation
dotnet publish CarelessWhisperV2.csproj -c Release -r win-x64 --self-contained true -p:PublishProfile=Standalone -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true -o dist-standalone --verbosity minimal -maxcpucount
```

### Verbose Build Output
```bash
# For debugging build issues
dotnet publish CarelessWhisperV2.csproj -c Release -r win-x64 --self-contained true -p:PublishProfile=Standalone -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true -o dist-standalone --verbosity detailed
```

### Clean Everything and Rebuild
```bash
# Nuclear option - clean everything
Remove-Item -Recurse -Force "bin", "obj", "dist-*" -ErrorAction SilentlyContinue
dotnet nuget locals all --clear
dotnet restore CarelessWhisperV2.csproj
dotnet build CarelessWhisperV2.csproj -c Release
```

---

## 📊 Build Metrics

### Expected Build Times
- **Standalone**: 2-4 minutes (depending on hardware)
- **Framework-dependent**: 1-2 minutes
- **Clean + Restore**: 1-2 minutes additional

### Expected File Sizes
- **Standalone EXE**: 80-90MB
- **Framework-dependent EXE**: 130-150KB
- **Whisper model**: Exactly 77MB
- **Total standalone package**: ~160MB
- **Total framework-dependent package**: ~90MB

---

## 🚀 Release Workflow

### Development → Distribution
1. **Complete feature development** ✅
2. **Update version numbers** ✅ (3.7.0)
3. **Run build-instructions.md** ← You are here
4. **Test both distributions**
5. **Create release packages (ZIP)**
6. **Generate checksums**
7. **Create GitHub release**
8. **Update download links**

### Continuous Integration Notes
For automated builds, ensure:
- Build agents have .NET 8.0 SDK
- PowerShell execution policy allows scripts
- Sufficient disk space for both distributions
- Network access for NuGet package restoration

---

**Build Success**: Both distributions should work identically - same features, same performance, just different deployment models.

**Questions?** Check existing `build-standalone.ps1` and `build-framework-dependent.ps1` scripts for working examples.
