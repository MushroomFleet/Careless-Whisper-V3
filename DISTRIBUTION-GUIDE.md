# Careless Whisper V3.1 - Distribution Guide

**Choose Your Distribution: Standalone vs Framework-Dependent**

## 🚀 Two Distribution Options Available

We provide **two versions** of Careless Whisper V3.1 to meet different user needs:

### 📦 Option 1: Standalone Distribution
- **Size**: 329MB
- **Setup**: Extract and run (zero installation)
- **Requirements**: Windows 10/11 x64 only
- **Best for**: General users, portable use, maximum simplicity

### 🔧 Option 2: Framework-Dependent Distribution  
- **Size**: 82MB (75% smaller!)
- **Setup**: Install .NET 8.0 Runtime + extract app
- **Requirements**: Windows 10/11 x64 + .NET 8.0 Runtime
- **Best for**: Technical users, IT deployments, smaller downloads

---

## 🤔 Which Version Should You Choose?

### Choose **Standalone** if you want:
- ✅ **Zero setup hassle** - just extract and run
- ✅ **Maximum compatibility** - works on any Windows 10/11 machine
- ✅ **Portable usage** - copy to USB, run anywhere
- ✅ **No dependencies** - nothing else to install or manage
- ✅ **Foolproof installation** - perfect for non-technical users

### Choose **Framework-Dependent** if you want:
- ✅ **Smaller download** - 82MB vs 329MB (save bandwidth)
- ✅ **Faster updates** - only app files update, not entire runtime
- ✅ **Professional deployment** - better for IT environments
- ✅ **Shared runtime** - efficient if you use other .NET apps
- ✅ **Better performance** - slightly faster startup and execution

---

## 📥 Download Links

### Standalone Distribution (329MB)
**📁 Folder**: `dist-standalone/`
- `CarelessWhisperV2.exe` (255MB) - Complete standalone application
- `ggml-tiny.bin` (77MB) - Whisper AI model
- `README-STANDALONE.md` - Installation guide

### Framework-Dependent Distribution (82MB)  
**📁 Folder**: `dist-framework-dependent/`
- `CarelessWhisperV2.exe` (465KB) - Main application
- `ggml-tiny.bin` (77MB) - Whisper AI model
- `*.dll` files (~4MB) - Dependencies
- `runtimes/` folder - Native libraries
- `README-FRAMEWORK-DEPENDENT.md` - Installation guide

---

## 📊 Detailed Comparison

| Feature | Standalone | Framework-Dependent |
|---------|------------|-------------------|
| **Download Size** | 329MB | 82MB |
| **Installation Steps** | 1 (Extract & run) | 2 (.NET + Extract) |
| **.NET Runtime Required** | ❌ No | ✅ Yes (.NET 8.0) |
| **Portability** | ✅ Fully portable | ❌ Needs runtime |
| **First-time Setup** | ⚡ Instant | 🔧 5-10 minutes |
| **Updates** | 📦 Replace entire package | 🔄 Replace app files only |
| **Performance** | ⚡ Good | ⚡ Slightly better |
| **Disk Space** | 329MB | 82MB + Runtime |
| **Network Usage** | High initial | Low ongoing |
| **Enterprise Deployment** | Simple but large | Professional |
| **USB/Portable Use** | ✅ Perfect | ❌ Needs setup |

---

## 🎯 Recommendations by Use Case

### 🏠 Home Users / Personal Use
**→ Choose Standalone**
- No technical knowledge required
- Works immediately out of the box
- Great for sharing with friends/family

### 🏢 Business / Enterprise
**→ Choose Framework-Dependent**
- IT can deploy .NET runtime via Group Policy
- Smaller network impact for multiple installs
- Better update management

### 👨‍💻 Developers / Technical Users
**→ Choose Framework-Dependent**
- You likely already have .NET installed
- Appreciate smaller download sizes
- Want better performance and update efficiency

### 🎒 Portable / Traveling Use
**→ Choose Standalone**
- Works on any machine without setup
- Perfect for USB stick deployment
- No dependency concerns

### 📚 Educational / Training
**→ Choose Standalone**
- Quick demo setup
- No admin rights needed for .NET install
- Students can use immediately

---

## 🛠️ Installation Quick Start

### Standalone Version
1. Download and extract `dist-standalone/`
2. Double-click `CarelessWhisperV2.exe`
3. Look for system tray icon
4. **Ready to use!**

### Framework-Dependent Version
1. **First**: Install .NET 8.0 Runtime from Microsoft
2. Download and extract `dist-framework-dependent/`
3. Double-click `CarelessWhisperV2.exe`
4. Look for system tray icon
5. **Ready to use!**

---

## 🔧 Technical Details

### Standalone Build Technical Info
- **Build Type**: Self-contained single file
- **Includes**: Complete .NET 8.0 runtime
- **Target**: win-x64
- **Optimization**: ReadyToRun enabled
- **Native Libraries**: Extracted at runtime

### Framework-Dependent Build Technical Info
- **Build Type**: Framework-dependent
- **Requires**: Microsoft.WindowsDesktop.App 8.0.x
- **Target**: win-x64  
- **Optimization**: ReadyToRun enabled
- **Native Libraries**: Included in runtimes folder

---

## 📋 Feature Comparison

Both versions include **identical functionality**:

### 🎤 Core Features
- **Speech-to-Text** (F1 key) - Local Whisper AI processing
- **AI Assistant** (Shift+F2) - OpenRouter integration
- **System Tray Integration** - Minimized operation
- **Global Hotkeys** - Works in any application
- **Audio Device Selection** - Multiple microphone support

### 🔧 Settings & Configuration  
- **OpenRouter API Integration** - Multiple AI models
- **Whisper Model Selection** - Performance vs accuracy
- **Hotkey Customization** - Change default keys
- **Auto-start Options** - Launch with Windows
- **Theme Selection** - Light/dark modes

### 🔒 Privacy & Security
- **Local Speech Processing** - Whisper runs offline
- **No Telemetry** - No usage tracking
- **API Key Security** - Encrypted local storage
- **Minimal Permissions** - Only microphone access

---

## 📞 Support & Documentation

### Get Help
- **GitHub Issues**: https://github.com/MushroomFleet/careless-whisper-V2
- **Documentation**: See README files in each distribution folder

### Version-Specific Issues
- **Standalone**: Check `README-STANDALONE.md`
- **Framework-Dependent**: Check `README-FRAMEWORK-DEPENDENT.md`
- **.NET Runtime Issues**: Microsoft documentation

---

## 🚀 Ready to Choose?

**Not sure?** → Go with **Standalone** (easier, more compatible)

**Technical user?** → Try **Framework-Dependent** (smaller, better performance)

**Enterprise deployment?** → Use **Framework-Dependent** (professional approach)

Both versions deliver the same powerful voice-to-text and AI assistant experience! 🎙️✨
