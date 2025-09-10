# File Structure Guide for GitHub Upload

This document lists all files in the project with upload recommendations for GitHub.

**Legend:**
- ✅ = Should be uploaded to GitHub
- ❌ = Should NOT be uploaded to GitHub (build artifacts, logs, temp files)

## Root Directory

```
d:/TOOLS/carelesswhisper361-plus/Careless-Whisper-V3/
├── ✅ App.xaml                          # WPF application definition
├── ✅ App.xaml.cs                       # WPF application code-behind
├── ✅ AssemblyInfo.cs                   # Assembly metadata
├── ✅ build-framework-dependent.ps1     # Build script for framework-dependent release
├── ✅ build-standalone.ps1              # Build script for standalone release  
├── ✅ build-tts.ps1                     # CarelessKitten TTS build script
├── ✅ careless-whisper-V2.sln           # Visual Studio solution file
├── ❌ Careless-Whisper-v3.6.3-portable-lite.zip # Release package (build artifact)
├── ✅ CarelessWhisperV2.csproj          # Project file
├── ✅ copy-python-to-debug.ps1          # TTS development utility script
├── ✅ DISTRIBUTION_README.md            # Distribution documentation
├── ✅ DISTRIBUTION-GUIDE.md             # Distribution guide
├── ✅ do-not-upload.md                  # This file - GitHub upload guide
├── ❌ ggml-base.bin                     # Whisper model binary (large file)
├── ❌ ggml-tiny.bin                     # Whisper model binary (large file)
├── ✅ RELEASE_NOTES_V3.6.5.md           # V3.6.5 release documentation
├── ✅ GITHUB_FILE_STRUCTURE_GUIDE.md    # GitHub structure guide
├── ✅ index.html                        # Web interface
├── ✅ LICENSE                           # License file
├── ✅ MainWindow.xaml                   # Main window XAML
├── ✅ MainWindow.xaml.cs                # Main window code-behind
├── ✅ package-lock.json                 # NPM lock file
├── ✅ package.json                      # NPM package configuration
├── ✅ postcss.config.js                 # PostCSS configuration
├── ✅ Program.cs                        # Application entry point
├── ✅ README.md                         # Main documentation
├── ✅ tailwind.config.js                # Tailwind CSS configuration
├── ✅ test-transformers.html            # Test HTML file
├── ✅ test-whisper.js                   # Test JavaScript file
├── ✅ TestMinimal.cs                    # Test source code
├── ✅ TestTranscriptionLogger.cs        # Test source code
├── ✅ tsconfig.json                     # TypeScript configuration
├── ✅ tsconfig.node.json                # TypeScript Node configuration
└── ✅ vite.config.ts                    # Vite configuration
```

## Build Output (DO NOT UPLOAD)

```
├── ❌ bin/                              # Entire build output directory
│   ├── ❌ Debug/                        # Debug build artifacts
│   │   └── ❌ net8.0-windows/           # Framework-specific builds
│   │       └── ❌ win-x64/              # Platform-specific builds
│   │           ├── ❌ *.dll             # Dynamic link libraries
│   │           ├── ❌ *.exe             # Executable files
│   │           ├── ❌ *.pdb             # Debug symbols
│   │           ├── ❌ *.json            # Runtime configuration
│   │           └── ❌ **/               # All subdirectories
│   └── ❌ Release/                      # Release build artifacts (v3.6.2)
│       └── ❌ net8.0-windows/           # Framework-specific builds
│           └── ❌ win-x64/              # Platform-specific builds
│               ├── ❌ *.dll             # Dynamic link libraries
│               ├── ❌ *.exe             # Executable files
│               ├── ❌ *.pdb             # Debug symbols
│               ├── ❌ *.json            # Runtime configuration
│               └── ❌ **/               # All subdirectories
```

## Build Cache (DO NOT UPLOAD)

```
├── ❌ obj/                              # Entire build cache directory
│   ├── ❌ *.json                        # NuGet specification files
│   ├── ❌ *.props                       # Build properties
│   ├── ❌ *.targets                     # Build targets
│   ├── ❌ *.cache                       # Build cache files
│   ├── ❌ Debug/                        # Debug cache
│   │   └── ❌ net8.0-windows/           # Framework cache
│   │       └── ❌ win-x64/              # Platform cache
│   │           ├── ❌ *.cs              # Generated source files
│   │           ├── ❌ *.cache           # Assembly cache
│   │           ├── ❌ *.editorconfig    # Generated config
│   │           └── ❌ ref/              # Reference assemblies
│   └── ❌ Release/                      # Release cache (v3.6.2)
│       └── ❌ net8.0-windows/           # Framework cache
│           └── ❌ win-x64/              # Platform cache
│               ├── ❌ *.cs              # Generated source files
│               ├── ❌ *.cache           # Assembly cache
│               ├── ❌ *.editorconfig    # Generated config
│               └── ❌ ref/              # Reference assemblies
```

## Distribution Output (DO NOT UPLOAD)

```
├── ❌ dist-framework-dependent/         # Framework-dependent distribution
│   ├── ❌ CarelessWhisperV2.deps.json   # Dependency configuration
│   ├── ❌ CarelessWhisperV2.dll         # Application library
│   ├── ❌ CarelessWhisperV2.exe         # Application executable
│   ├── ❌ CarelessWhisperV2.pdb         # Debug symbols
│   ├── ❌ CarelessWhisperV2.runtimeconfig.json # Runtime configuration
│   ├── ❌ DISTRIBUTION_README.md        # Distribution documentation (copy)
│   ├── ❌ ggml-tiny.bin                 # Whisper model binary (copy)
│   ├── ❌ *.dll                         # All dependency libraries
│   └── ❌ runtimes/                     # Runtime-specific assemblies
│       ├── ❌ win-arm64/                # ARM64 Windows runtime
│       ├── ❌ win-x64/                  # x64 Windows runtime  
│       └── ❌ win-x86/                  # x86 Windows runtime
├── ❌ dist-standalone/                  # Standalone distribution (V3.6.5)
│   ├── ❌ CarelessWhisperV3.6.5-portable.exe # Self-contained executable (269MB)
│   ├── ❌ CarelessWhisperV3.6.5-portable.zip # Compressed distribution package
│   ├── ❌ CarelessWhisperV2.pdb         # Debug symbols
│   ├── ❌ DISTRIBUTION_README.md        # Distribution documentation (copy)
│   ├── ❌ ggml-tiny.bin                 # Whisper model binary (copy)
│   ├── ❌ python/                       # Embedded Python environment (copy)
│   └── ❌ scripts/                      # TTS bridge scripts (copy)
```

## CarelessKitten TTS Components (V3.6.5)

### TTS Scripts and Setup (UPLOAD - Source Code)
```
├── ✅ scripts/                          # TTS automation scripts
│   ├── ✅ kitten_tts_bridge.py          # CarelessKitten TTS bridge script
│   └── ✅ setup_python_environment.ps1  # Python environment setup script
```

### Python Environment (DO NOT UPLOAD - Large Binaries)
```
├── ❌ python/                           # Embedded Python 3.11.9 environment (~20MB+)
│   ├── ❌ python.exe                    # Python executable
│   ├── ❌ pythonw.exe                   # Python windowed executable
│   ├── ❌ *.pyd                         # Python extension modules
│   ├── ❌ *.dll                         # Python runtime libraries
│   ├── ❌ python311.zip                 # Python standard library
│   ├── ❌ Lib/                          # Python library directory
│   │   ├── ❌ site-packages/            # Installed packages
│   │   │   ├── ❌ kittentts/            # KittenTTS neural TTS package
│   │   │   ├── ❌ num2words/            # Professional text normalization
│   │   │   ├── ❌ phonemizer/           # IPA phoneme processing
│   │   │   └── ❌ */                    # All other packages
│   │   └── ❌ */                        # Standard library modules
│   └── ❌ espeak/                       # Bundled eSpeak phonemizer
│       ├── ❌ espeak.exe                # eSpeak executable
│       └── ❌ espeak-data/              # eSpeak language data
```

### TTS Source Packages (DO NOT UPLOAD - Source Archives)
```
├── ❌ espeak/                           # TTS development resources
│   ├── ❌ espeak.exe                    # eSpeak executable (binary)
│   ├── ❌ kittentts-0.1.3/             # KittenTTS source package
│   │   └── ❌ kittentts-0.1.3/         # Unpacked source
│   │       └── ❌ src/                  # Source code
│   └── ❌ num2words-0.5.14/            # num2words source package
│       └── ❌ num2words-0.5.14/         # Unpacked source
│           └── ❌ num2words/             # Library source
```

## Documentation (UPLOAD)

```
├── ✅ docs/                             # Documentation directory
│   ├── ✅ careless_kitten_devteam_handoff.md # CarelessKitten TTS handoff
│   ├── ✅ devteam-handoff-v3-final.md   # Development handoff
│   ├── ✅ devteam-handoff-v3-fix.md     # Development fixes
│   ├── ✅ devteam-plans-handoff-v3.md   # Development plans
│   ├── ✅ DOTNET-DEVTEAM-HANDOFF.md     # .NET team handoff
│   ├── ✅ ESPEAK_INSTALLATION_GUIDE.md  # eSpeak installation guide
│   ├── ✅ KITTEN_TTS_SUCCESS_REPORT.md  # TTS integration success report
│   ├── ✅ SPEAK2MEV2-DEVTEAM-HANDOFF.md # Speech feature handoff
│   ├── ✅ TTS_INTEGRATION_GUIDE.md      # TTS integration guide
│   ├── ✅ v363-Vision-plan.md           # Vision feature planning
│   ├── ✅ ollama-provider/              # Ollama documentation
│   │   └── ✅ ollama-typescript.md      # TypeScript integration
│   └── ✅ openrouter-provider/          # OpenRouter documentation
│       ├── ✅ openrouter-overview.md    # Overview
│       ├── ✅ openrouter-parameters.md  # Parameters guide
│       ├── ✅ openrouter-quickstart.md  # Quick start
│       └── ✅ openrouter-streaming.md   # Streaming guide
```

## Source Code - Models (UPLOAD)

```
├── ✅ Models/                           # Data models directory
│   ├── ✅ ApplicationSettings.cs        # Application settings model
│   ├── ✅ AudioDevice.cs                # Audio device model
│   ├── ✅ ModelCache.cs                 # Model cache implementation
│   ├── ✅ OllamaSettings.cs             # Ollama configuration
│   ├── ✅ OpenRouterSettings.cs         # OpenRouter configuration
│   └── ✅ TranscriptionEntry.cs         # Transcription data model
```

## Web Assets (UPLOAD)

```
├── ✅ public/                           # Web public assets
│   ├── ✅ manifest.json                 # Web app manifest
│   └── ✅ sw.js                         # Service worker
```

## Resources (UPLOAD)

```
├── ✅ Resources/                        # Application resources
│   └── ✅ app-icon.ico                  # Application icon
```

## Source Code - Services (UPLOAD)

```
├── ✅ Services/                         # Service layer directory
│   ├── ✅ Audio/                        # Audio services
│   │   ├── ✅ IAudioService.cs          # Audio service interface
│   │   └── ✅ NAudioService.cs          # NAudio implementation
│   ├── ✅ AudioNotification/            # Audio notification services
│   │   ├── ✅ AudioNotificationService.cs # Notification implementation
│   │   └── ✅ IAudioNotificationService.cs # Notification interface
│   ├── ✅ Cache/                        # Caching services
│   │   ├── ✅ IModelsCacheService.cs    # Cache interface
│   │   └── ✅ ModelsCacheService.cs     # Cache implementation
│   ├── ✅ Clipboard/                    # Clipboard services
│   │   ├── ✅ ClipboardService.cs       # Clipboard implementation
│   │   └── ✅ IClipboardService.cs      # Clipboard interface
│   ├── ✅ Environment/                  # Environment services
│   │   ├── ✅ EnvironmentService.cs     # Environment implementation
│   │   └── ✅ IEnvironmentService.cs    # Environment interface
│   ├── ✅ Hotkeys/                      # Hotkey services
│   │   └── ✅ PushToTalkManager.cs      # Push-to-talk implementation
│   ├── ✅ Logging/                      # Logging services
│   │   ├── ✅ FileTranscriptionLogger.cs # File logging implementation
│   │   ├── ✅ ITranscriptionLogger.cs   # Logging interface
│   │   └── ✅ TranscriptionLogger.cs    # Main logging service
│   ├── ✅ Network/                      # Network services
│   │   ├── ✅ INetworkDiagnosticsService.cs # Network interface
│   │   └── ✅ NetworkDiagnosticsService.cs # Network implementation
│   ├── ✅ Ollama/                       # Ollama AI services
│   │   ├── ✅ IOllamaService.cs         # Ollama interface
│   │   └── ✅ OllamaService.cs          # Ollama implementation
│   ├── ✅ OpenRouter/                   # OpenRouter AI services
│   │   ├── ✅ IOpenRouterService.cs     # OpenRouter interface
│   │   └── ✅ OpenRouterService.cs      # OpenRouter implementation
│   ├── ✅ Orchestration/                # Orchestration services
│   │   └── ✅ TranscriptionOrchestrator.cs # Main orchestrator
│   ├── ✅ Python/                       # Python environment services
│   │   └── ✅ *.cs                      # Python integration services
│   ├── ✅ ScreenCapture/                # Screen capture services  
│   │   └── ✅ *.cs                      # Vision capture services
│   ├── ✅ Settings/                     # Settings services
│   │   ├── ✅ ISettingsService.cs       # Settings interface
│   │   └── ✅ JsonSettingsService.cs    # JSON settings implementation
│   ├── ✅ Transcription/                # Transcription services
│   │   ├── ✅ ITranscriptionService.cs  # Transcription interface
│   │   └── ✅ WhisperTranscriptionService.cs # Whisper implementation
│   └── ✅ Tts/                          # Text-to-Speech services
│       └── ✅ *.cs                      # CarelessKitten TTS services
```

## Frontend Source (UPLOAD)

```
├── ✅ src/                              # Frontend source directory
│   ├── ✅ main.ts                       # Main TypeScript entry
│   ├── ✅ style.css                     # Styles
│   ├── ✅ components/                   # UI components
│   │   └── ✅ SpeakToMeApp.ts           # Main app component
│   ├── ✅ services/                     # Frontend services
│   │   ├── ✅ AIController.ts           # AI controller
│   │   ├── ✅ AudioRecorder.ts          # Audio recording
│   │   ├── ✅ KokoroTTS.ts              # Text-to-speech
│   │   ├── ✅ LoggingService.ts         # Frontend logging
│   │   ├── ✅ OllamaClient.ts           # Ollama client
│   │   ├── ✅ StreamingAudioQueue.ts    # Audio streaming
│   │   └── ✅ WhisperTranscriber.ts     # Whisper transcription
│   └── ✅ types/                        # TypeScript types
│       └── ✅ index.ts                  # Type definitions
```

## UI Views (UPLOAD)

```
├── ✅ Views/                            # WPF views directory
│   ├── ✅ SettingsWindow.xaml           # Settings window XAML
│   ├── ✅ SettingsWindow.xaml.cs        # Settings window code
│   ├── ✅ TranscriptionHistoryWindow.xaml # History window XAML
│   └── ✅ TranscriptionHistoryWindow.xaml.cs # History window code
```

## Additional Files to Exclude from GitHub

### IDE and Editor Files (if present)
```
❌ .vs/                                  # Visual Studio cache
❌ .vscode/                              # VS Code settings (optional)
❌ *.user                                # User-specific settings
❌ *.suo                                 # Solution user options
```

### Node.js (if present)
```
❌ node_modules/                         # NPM dependencies
❌ npm-debug.log*                        # NPM debug logs
❌ yarn-debug.log*                       # Yarn debug logs
```

### Temporary and Log Files
```
❌ *.log                                 # All log files
❌ *.tmp                                 # Temporary files
❌ *.temp                                # Temporary files
❌ *_test*.txt                           # Test output files
❌ *_log*.txt                            # Log files
❌ *_final*.txt                          # Final test files
```

### Large Binary Files
```
❌ *.bin                                 # Binary model files (use Git LFS if needed)
❌ *.gguf                                # GGUF model files (use Git LFS if needed)
❌ *.safetensors                         # Safetensors files (use Git LFS if needed)
```

## Recommended .gitignore Entries

Add these to your `.gitignore` file:

```gitignore
# Build output
bin/
obj/

# Distribution output (v3.6.5 release artifacts)
dist-framework-dependent/
dist-standalone/
*.zip

# CarelessKitten TTS binaries and environments (V3.6.5)
python/
espeak/

# User-specific files
*.user
*.suo
.vs/

# Test and log files
*.log
*.tmp
*.temp
*_test*.txt
*_log*.txt
*_final*.txt
startup_log.txt
startup_test*.txt
final_test.txt
v*_test.txt
v*_logs.txt
v*_final*.txt

# Large model files (consider Git LFS)
*.bin
*.gguf
*.safetensors

# TTS audio test files
test_*.wav
*.wav

# Node.js (if applicable)
node_modules/
npm-debug.log*
yarn-debug.log*

# OS generated files
.DS_Store
.DS_Store?
._*
.Spotlight-V100
.Trashes
ehthumbs.db
Thumbs.db
```

## Summary

**Total Files to Upload: ~70+ source files**
- All source code (.cs, .xaml files)
- Project configuration files (including TTS build scripts)
- Documentation (including CarelessKitten TTS guides)
- Web assets and configurations
- UI definitions
- TTS bridge scripts and setup automation

**Total Files to Exclude: ~200+ build artifacts, binaries, and environments**
- Entire `bin/` and `obj/` directories (including Release builds)
- Distribution directories (`dist-framework-dependent/`, `dist-standalone/`)
- Release packages (v3.6.5 .zip and .exe files)
- **Embedded Python environment** (~20MB+ binaries)
- **TTS source packages** (kittentts, num2words archives)
- **Bundled eSpeak** (executable and data files)
- Large binary model files (ggml-tiny.bin, ggml-base.bin)
- IDE-specific cache files

**V3.6.5 CarelessKitten TTS Updates:**
- ❌ **CarelessWhisperV3.6.5-portable.exe** (269MB) - Standalone executable with embedded Python
- ❌ **CarelessWhisperV3.6.5-portable.zip** - Complete distribution package
- ❌ **python/ directory** (~20MB+) - Complete embedded Python 3.11.9 environment
- ❌ **espeak/ directory** - TTS development resources and source packages
- ✅ **scripts/kitten_tts_bridge.py** - CarelessKitten TTS bridge script
- ✅ **scripts/setup_python_environment.ps1** - Python environment automation
- ✅ **build-tts.ps1** - TTS build integration script
- ✅ **CarelessKitten TTS documentation** - Comprehensive integration guides
- ✅ **RELEASE_NOTES_V3.6.5.md** - Complete release documentation

This ensures a clean repository with essential source code and documentation while excluding the substantial binary components (embedded Python, neural TTS models, compiled executables) that make V3.6.5 a 277MB+ distribution package.
