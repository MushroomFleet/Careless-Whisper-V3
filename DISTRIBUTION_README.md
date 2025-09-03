# Careless Whisper V3 - Distribution Package

**Dual-Mode Voice-to-Text & AI Assistant for Windows**

## What's Included

This is the **framework-dependent** distribution of Careless Whisper V3. All application files and dependencies are included, but you'll need the .NET 8.0 Runtime installed on your system.

### Package Contents
- `CarelessWhisperV2.exe` - Main application executable
- `ggml-tiny.bin` - Whisper AI model file (77MB)
- `runtimes/` - Native Whisper libraries for speech recognition
- Various `.dll` files - Application dependencies
- This README with installation instructions

**Total Size**: ~85MB (much smaller than self-contained version)

## System Requirements

### Required
- **Windows 10/11** (64-bit)
- **.NET 8.0 Runtime** (see installation instructions below)
- **Microphone** (any Windows-compatible microphone)

### Optional
- **OpenRouter API Key** (for AI features - get free credits at [openrouter.ai](https://openrouter.ai))

## Installation Instructions

### Step 1: Install .NET 8.0 Runtime (if not already installed)

1. Visit: https://dotnet.microsoft.com/download/dotnet/8.0
2. Click **"Download .NET 8.0 Runtime"** (not SDK)
3. Choose **"Run desktop apps"** → **"Download x64"**
4. Install the downloaded file (~50MB download)

**Note**: If you already have .NET 8.0 installed, you can skip this step.

### Step 2: Run Careless Whisper V3

1. Extract all files from this package to a folder of your choice
2. **Keep all files together** - don't move individual files
3. Double-click `CarelessWhisperV2.exe` to run
4. The app will minimize to your system tray (look for the icon near your clock)

## First Time Setup

### Basic Configuration
1. **Right-click the system tray icon** → **Settings**
2. **General tab**: Configure auto-start, theme preferences
3. **Audio tab**: Select your microphone and test audio
4. **Whisper tab**: Choose your preferred model (Tiny is fastest)

### AI Features (Optional)
1. **OpenRouter tab**: Enter your API key for AI features
2. **Choose a model**: GPT-4, Claude, or others available
3. **Test the connection**: Use the test button to verify

## Usage

### Speech-to-Text Mode (F1)
1. **Hold F1** while speaking
2. **Release F1** when finished
3. **Paste anywhere** with Ctrl+V
*Perfect for dictation and quick notes*

### AI Assistant Mode (Shift+F2) 
1. **Hold Shift+F2** while asking a question
2. **Release keys** when finished speaking
3. **Wait for AI processing** (a few seconds)
4. **Paste AI response** with Ctrl+V
*Perfect for research, writing help, code assistance*

## Troubleshooting

### "Application failed to start" error
- **Install .NET 8.0 Runtime** (see Step 1 above)
- Ensure all files are extracted together

### "Native Library not found" error
- **Don't move files** - keep the entire folder structure intact
- The `runtimes/win-x64/` folder must be present with `.dll` files

### Microphone not working
- Check Windows microphone permissions
- Test microphone in Windows Settings → System → Sound
- Select correct microphone in Careless Whisper settings

### AI features not working
- Verify OpenRouter API key is correct
- Check internet connection
- Ensure you have API credits available

## File Structure

Keep this structure intact:
```
CarelessWhisper-V3/
├── CarelessWhisperV2.exe          (Main application)
├── ggml-tiny.bin                  (Whisper model)
├── *.dll                          (Dependencies)
├── runtimes/win-x64/              (Native libraries)
│   ├── ggml-base-whisper.dll
│   ├── ggml-cpu-whisper.dll  
│   ├── ggml-whisper.dll
│   └── whisper.dll
└── DISTRIBUTION_README.md         (This file)
```

## Version Information
- **Version**: 3.0.0
- **Build Type**: Framework-Dependent
- **Target**: Windows x64
- **.NET Version**: 8.0

## Support

For issues, feature requests, or contributions:
- **GitHub**: https://github.com/MushroomFleet/careless-whisper-V2
- **Documentation**: See included documentation files

---

**Enjoy your new voice-powered productivity tool!** 🎙️✨
