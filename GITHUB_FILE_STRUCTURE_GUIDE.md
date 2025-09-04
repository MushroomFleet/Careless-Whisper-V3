# 📁 GitHub Repository File Structure Guide

**Careless Whisper V3 - What to Include/Exclude from Git Repository**

This guide uses ❌ to mark files that should **NOT** be uploaded to GitHub, ensuring others can reproduce and build the project from source.

## 🎯 Repository Goals
- ✅ Keep source code and configuration files
- ✅ Enable others to build from source  
- ❌ Exclude build outputs, binaries, and large files
- ❌ Exclude auto-generated files that cause conflicts

---

## 📂 Project Root Structure

```
careless-whisper-V2/
├── 📄 App.xaml                           ✅ WPF application definition
├── 📄 App.xaml.cs                        ✅ WPF application code-behind
├── 📄 AssemblyInfo.cs                     ✅ Assembly metadata
├── 📄 careless-whisper-V2.sln             ✅ Visual Studio solution file
├── 📄 CarelessWhisperV2.csproj           ✅ .NET project file
├── 📄 DISTRIBUTION_README.md              ✅ Distribution documentation
├── 📄 DISTRIBUTION-GUIDE.md               ✅ Distribution guide
├── 📄 ggml-tiny.bin                       ❌ LARGE MODEL FILE (77MB)
├── 📄 index.html                          ✅ Web interface entry point
├── 📄 LICENSE                             ✅ License file
├── 📄 MainWindow.xaml                     ✅ Main WPF window
├── 📄 MainWindow.xaml.cs                  ✅ Main window code-behind
├── 📄 package-lock.json                   ❌ AUTO-GENERATED NPM LOCK
├── 📄 package.json                        ✅ NPM package configuration
├── 📄 postcss.config.js                   ✅ PostCSS configuration
├── 📄 Program.cs                          ✅ Application entry point
├── 📄 README.md                           ✅ Main project documentation
├── 📄 tailwind.config.js                  ✅ Tailwind CSS configuration
├── 📄 test-transformers.html              ✅ Test page for transformers
├── 📄 test-whisper.js                     ✅ Whisper testing script
├── 📄 TestTranscriptionLogger.cs          ✅ Test logger implementation
├── 📄 tsconfig.json                       ✅ TypeScript configuration
├── 📄 tsconfig.node.json                  ✅ TypeScript Node configuration
├── 📄 vite.config.ts                      ✅ Vite build configuration
└── 📄 .gitignore                          ✅ Git ignore rules (CREATE THIS!)
```

---

## 🚫 Build Output Directories (EXCLUDE ALL)

```
├── 📁 bin/                                ❌ .NET BUILD OUTPUTS
│   ├── 📁 Debug/                          ❌ Debug build artifacts
│   │   └── 📁 net8.0-windows/             ❌ Framework-specific outputs
│   │       └── 📁 win-x64/                ❌ Platform-specific binaries
│   └── 📁 Release/                        ❌ Release build artifacts
│       └── 📁 net8.0-windows/             ❌ Framework-specific outputs
│           └── 📁 win-x64/                ❌ Platform-specific binaries
│               ├── 📄 CarelessWhisperV2.deps.json    ❌ Dependency manifest
│               ├── 📄 CarelessWhisperV2.dll          ❌ Compiled assembly
│               ├── 📄 CarelessWhisperV2.exe          ❌ Executable binary
│               ├── 📄 CarelessWhisperV2.pdb          ❌ Debug symbols
│               └── 📄 CarelessWhisperV2.runtimeconfig.json ❌ Runtime config
```

```
├── 📁 obj/                                ❌ .NET INTERMEDIATE FILES
│   ├── 📄 CarelessWhisperV2.csproj.nuget.dgspec.json ❌ NuGet dependency graph
│   ├── 📄 CarelessWhisperV2.csproj.nuget.g.props     ❌ NuGet generated props
│   ├── 📄 CarelessWhisperV2.csproj.nuget.g.targets   ❌ NuGet generated targets
│   ├── 📄 project.assets.json                        ❌ Project assets cache
│   ├── 📄 project.nuget.cache                        ❌ NuGet cache file
│   ├── 📁 Debug/                          ❌ Debug intermediate files
│   └── 📁 Release/                        ❌ Release intermediate files
```

```
├── 📁 dist-framework-dependent/           ❌ DISTRIBUTION BUILD
│   ├── 📄 CarelessWhisperV2.deps.json     ❌ Runtime dependencies
│   ├── 📄 CarelessWhisperV2.dll           ❌ Main application DLL
│   ├── 📄 CarelessWhisperV2.exe           ❌ Application executable
│   ├── 📄 CarelessWhisperV2.pdb           ❌ Debug symbols
│   ├── 📄 ggml-tiny.bin                   ❌ Whisper model (77MB)
│   ├── 📄 *.dll                           ❌ All dependency DLLs
│   └── 📁 runtimes/                       ❌ Native runtime libraries
│       └── 📁 win-x64/                    ❌ Platform-specific natives
│           └── 📄 *.dll                   ❌ Whisper native libraries
```

```
├── 📁 dist-standalone/                    ❌ STANDALONE BUILD
│   ├── 📄 CarelessWhisperV2.exe           ❌ Self-contained executable
│   ├── 📄 CarelessWhisperV2.pdb           ❌ Debug symbols
│   └── 📄 ggml-tiny.bin                   ❌ Whisper model copy
```

---

## ✅ Source Code Directories (INCLUDE ALL)

```
├── 📁 Models/                             ✅ DATA MODELS
│   ├── 📄 ApplicationSettings.cs          ✅ App configuration model
│   ├── 📄 AudioDevice.cs                  ✅ Audio device model
│   ├── 📄 ModelCache.cs                   ✅ Model caching logic
│   ├── 📄 OpenRouterSettings.cs           ✅ OpenRouter API settings
│   └── 📄 TranscriptionEntry.cs           ✅ Transcription data model
```

```
├── 📁 Services/                           ✅ BUSINESS LOGIC
│   ├── 📁 Audio/                          ✅ Audio processing
│   ├── 📁 AudioNotification/              ✅ Sound notifications
│   ├── 📁 Cache/                          ✅ Caching services
│   ├── 📁 Clipboard/                      ✅ Clipboard operations
│   ├── 📁 Environment/                    ✅ Environment detection
│   ├── 📁 Hotkeys/                        ✅ Global hotkey handling
│   ├── 📁 Logging/                        ✅ Logging infrastructure
│   ├── 📁 Network/                        ✅ Network diagnostics
│   ├── 📁 OpenRouter/                     ✅ OpenRouter API client
│   ├── 📁 Orchestration/                  ✅ Service coordination
│   ├── 📁 Settings/                       ✅ Settings management
│   └── 📁 Transcription/                  ✅ Speech-to-text services
```

```
├── 📁 Views/                              ✅ WPF USER INTERFACE
│   ├── 📄 SettingsWindow.xaml             ✅ Settings window XAML
│   ├── 📄 SettingsWindow.xaml.cs          ✅ Settings window logic
│   ├── 📄 TranscriptionHistoryWindow.xaml ✅ History window XAML
│   └── 📄 TranscriptionHistoryWindow.xaml.cs ✅ History window logic
```

```
├── 📁 Resources/                          ✅ STATIC RESOURCES
│   └── 📄 app-icon.ico                    ✅ Application icon
```

```
├── 📁 src/                                ✅ WEB FRONTEND SOURCE
│   ├── 📄 main.ts                         ✅ TypeScript entry point
│   ├── 📄 style.css                       ✅ Main stylesheet
│   ├── 📁 components/                     ✅ Web components
│   │   └── 📄 SpeakToMeApp.ts             ✅ Main app component
│   ├── 📁 services/                       ✅ Frontend services
│   │   ├── 📄 AIController.ts             ✅ AI integration
│   │   ├── 📄 AudioRecorder.ts            ✅ Audio recording
│   │   ├── 📄 KokoroTTS.ts               ✅ Text-to-speech
│   │   ├── 📄 LoggingService.ts          ✅ Frontend logging
│   │   ├── 📄 OllamaClient.ts            ✅ Ollama API client
│   │   ├── 📄 StreamingAudioQueue.ts      ✅ Audio streaming
│   │   └── 📄 WhisperTranscriber.ts       ✅ Speech recognition
│   └── 📁 types/                          ✅ TypeScript definitions
│       └── 📄 index.ts                    ✅ Type definitions
```

```
├── 📁 docs/                               ✅ DOCUMENTATION
│   ├── 📄 devteam-handoff-v3-final.md     ✅ Technical handoff docs
│   ├── 📄 devteam-handoff-v3-fix.md       ✅ Fix documentation
│   ├── 📄 devteam-plans-handoff-v3.md     ✅ Planning documentation
│   ├── 📄 DOTNET-DEVTEAM-HANDOFF.md       ✅ .NET team handoff
│   ├── 📄 SPEAK2MEV2-DEVTEAM-HANDOFF.md   ✅ Web team handoff
│   └── 📁 openrouter-provider/            ✅ OpenRouter documentation
│       ├── 📄 openrouter-overview.md      ✅ API overview
│       ├── 📄 openrouter-parameters.md    ✅ Parameter reference
│       ├── 📄 openrouter-quickstart.md    ✅ Quick start guide
│       └── 📄 openrouter-streaming.md     ✅ Streaming documentation
```

```
├── 📁 public/                             ✅ WEB STATIC ASSETS
│   ├── 📄 manifest.json                   ✅ Web app manifest
│   └── 📄 sw.js                          ✅ Service worker
```

---

## ❌ Files to NEVER Include in Git

### 🏗️ Build Outputs
- **❌ `bin/`** - All .NET build outputs and compiled binaries
- **❌ `obj/`** - All .NET intermediate compilation files  
- **❌ `dist-framework-dependent/`** - Framework-dependent distribution
- **❌ `dist-standalone/`** - Self-contained distribution

### 📦 Package Management
- **❌ `package-lock.json`** - Auto-generated, causes merge conflicts
- **❌ `node_modules/`** - NPM dependencies (install with `npm install`)

### 🤖 AI Model Files  
- **❌ `ggml-tiny.bin`** - 77MB Whisper model (too large for Git)

### 🗂️ IDE & System Files
- **❌ `.vs/`** - Visual Studio cache (if present)
- **❌ `.vscode/settings.json`** - Local VS Code settings
- **❌ `*.user`** - User-specific project files
- **❌ `Thumbs.db`** - Windows thumbnail cache
- **❌ `.DS_Store`** - macOS file system cache

---

## 🎯 Required .gitignore File

**❗ IMPORTANT**: Create this `.gitignore` file in your project root:

```gitignore
# .NET Build Outputs
bin/
obj/
*.user
*.suo
*.cache
*.dll
*.exe
*.pdb
*.runtimeconfig.json
*.deps.json

# Distribution Folders
dist-framework-dependent/
dist-standalone/

# AI Model Files (too large for Git)
*.bin
ggml-*.bin

# Node.js Dependencies
node_modules/
package-lock.json

# IDE Files
.vs/
.vscode/settings.json
*.swp
*.swo

# System Files
Thumbs.db
.DS_Store
Desktop.ini

# Logs
*.log
logs/

# Temporary Files
*.tmp
*.temp
*~
```

---

## 🔧 Building from Source (For Contributors)

After cloning the repository, contributors should:

### 1️⃣ Install Prerequisites
```bash
# Install .NET 8.0 SDK
https://dotnet.microsoft.com/download/dotnet/8.0

# Install Node.js 18+
https://nodejs.org/
```

### 2️⃣ Restore Dependencies
```bash
# Restore .NET packages
dotnet restore

# Install NPM packages
npm install
```

### 3️⃣ Download Whisper Model
```bash
# Download ggml-tiny.bin (77MB) from official Whisper.net releases
# Place in project root directory
```

### 4️⃣ Build Application
```bash
# Build .NET application
dotnet build

# Build web frontend
npm run build

# Run application
dotnet run
```

---

## 📊 Repository Size Impact

| Include ✅ | Exclude ❌ | Size Saved |
|------------|------------|------------|
| Source code | Build outputs | ~200MB |
| Documentation | AI model files | ~77MB |
| Configuration | node_modules | ~50-100MB |
| **Total Repo** | **Excluded** | **~300MB saved** |

**Result**: Clean, focused repository under 10MB instead of 300MB+

---

## ✅ Benefits of This Structure

### 🎯 For Repository Maintainers
- **Faster cloning** - Small repository size
- **No merge conflicts** - No auto-generated files
- **Clear history** - Only meaningful changes tracked
- **Better collaboration** - No binary file conflicts

### 👥 For Contributors  
- **Easy setup** - Clear build instructions
- **Reproducible builds** - Consistent across machines
- **Focus on code** - No distraction from build artifacts
- **Standard workflow** - Familiar .NET/Node.js patterns

### 🚀 For Users
- **Download releases** - Get optimized binaries from GitHub Releases
- **Multiple formats** - Framework-dependent vs. standalone options
- **Documentation included** - Clear setup and usage guides

---

**🎯 Remember**: The goal is a clean repository where anyone can `git clone`, follow build instructions, and create their own distribution packages!
