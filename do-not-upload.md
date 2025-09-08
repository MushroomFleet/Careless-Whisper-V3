# File Structure Guide for GitHub Upload

This document lists all files in the project with upload recommendations for GitHub.

**Legend:**
- ✅ = Should be uploaded to GitHub
- ❌ = Should NOT be uploaded to GitHub (build artifacts, logs, temp files)

## Root Directory

```
c:/Projects/carelesswhisperV4/Careless-Whisper-V3/
├── ✅ App.xaml                          # WPF application definition
├── ✅ App.xaml.cs                       # WPF application code-behind
├── ✅ AssemblyInfo.cs                   # Assembly metadata
├── ✅ build-framework-dependent.ps1     # Build script for framework-dependent release
├── ✅ build-standalone.ps1              # Build script for standalone release
├── ✅ careless-whisper-V2.sln           # Visual Studio solution file
├── ❌ Careless-Whisper-v3.6.0-portable.zip # Release package (build artifact)
├── ❌ Careless-Whisper-v3.6.0-standalone.zip # Release package (build artifact)
├── ❌ Careless-Whisper-v3.6.1-portable.zip # Release package (build artifact)
├── ❌ Careless-Whisper-v3.6.1-standalone.zip # Release package (build artifact)
├── ✅ CarelessWhisperV2.csproj          # Project file
├── ✅ DISTRIBUTION_README.md            # Distribution documentation
├── ✅ DISTRIBUTION-GUIDE.md             # Distribution guide
├── ✅ do-not-upload.md                  # This file - GitHub upload guide
├── ❌ final_test.txt                    # Test log file
├── ❌ ggml-tiny.bin                     # Whisper model binary (large file)
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
├── ❌ startup_log.txt                   # Startup log file
├── ❌ startup_test2.txt                 # Test log file
├── ✅ tailwind.config.js                # Tailwind CSS configuration
├── ✅ test-transformers.html            # Test HTML file
├── ✅ test-whisper.js                   # Test JavaScript file
├── ✅ TestMinimal.cs                    # Test source code
├── ✅ TestTranscriptionLogger.cs        # Test source code
├── ✅ tsconfig.json                     # TypeScript configuration
├── ✅ tsconfig.node.json                # TypeScript Node configuration
├── ❌ v1_6_0_final.txt                  # Version log file
├── ❌ v1_6_0_final2.txt                 # Version log file
├── ❌ v1_6_0_final3.txt                 # Version log file
├── ❌ v1_6_0_logs.txt                   # Version log file
├── ❌ v1_6_0_test.txt                   # Version test file
├── ❌ v3_6_0_test.txt                   # Version test file
└── ✅ vite.config.ts                    # Vite configuration
```

## Build Output (DO NOT UPLOAD)

```
├── ❌ bin/                              # Entire build output directory
│   └── ❌ Debug/                        # Debug build artifacts
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
│   └── ❌ Debug/                        # Debug cache
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
├── ❌ dist-standalone/                  # Standalone distribution
│   ├── ❌ CarelessWhisperV2.exe         # Self-contained executable
│   ├── ❌ CarelessWhisperV2.pdb         # Debug symbols
│   ├── ❌ DISTRIBUTION_README.md        # Distribution documentation (copy)
│   └── ❌ ggml-tiny.bin                 # Whisper model binary (copy)
```

## Documentation (UPLOAD)

```
├── ✅ docs/                             # Documentation directory
│   ├── ✅ devteam-handoff-v3-final.md   # Development handoff
│   ├── ✅ devteam-handoff-v3-fix.md     # Development fixes
│   ├── ✅ devteam-plans-handoff-v3.md   # Development plans
│   ├── ✅ DOTNET-DEVTEAM-HANDOFF.md     # .NET team handoff
│   ├── ✅ SPEAK2MEV2-DEVTEAM-HANDOFF.md # Speech feature handoff
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
│   ├── ✅ Settings/                     # Settings services
│   │   ├── ✅ ISettingsService.cs       # Settings interface
│   │   └── ✅ JsonSettingsService.cs    # JSON settings implementation
│   └── ✅ Transcription/                # Transcription services
│       ├── ✅ ITranscriptionService.cs  # Transcription interface
│       └── ✅ WhisperTranscriptionService.cs # Whisper implementation
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

# Distribution output (3.6.1 release artifacts)
dist-framework-dependent/
dist-standalone/
*.zip

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

**Total Files to Upload: ~55+ source files**
- All source code (.cs, .xaml files)
- Project configuration files (including build scripts)
- Documentation
- Web assets and configurations
- UI definitions

**Total Files to Exclude: ~120+ build artifacts, distributions, and logs**
- Entire `bin/` and `obj/` directories
- Distribution directories (`dist-framework-dependent/`, `dist-standalone/`)
- Release packages (v3.6.0 and v3.6.1 .zip files)
- All test log files
- Large binary model files
- IDE-specific cache files

**Post-3.6.1 Release Updates:**
- ❌ 4 release packages (.zip files) - These are distribution artifacts, not source code
- ❌ 2 distribution directories with ~30+ compiled files each
- ✅ 2 build scripts for automated release generation
- ✅ Updated file structure guide

This ensures a clean repository with only essential source code and documentation while excluding build artifacts, release packages, logs, and large binary files that were generated during the 3.6.1 distribution process.
