# Careless Whisper V3

**"Care Less" - Inobtrusive Augments!**

**Dual-Mode Voice-to-Text & AI Assistant for Windows** • Silent system tray interface • Built with .NET 8.0

Transform your voice into text instantly OR get AI-powered responses with simple hotkeys. No windows, no interruptions—just speak and get results anywhere.

**4/9/25:** [Careless-Canvas-NML](https://github.com/MushroomFleet/Careless-Canvas-NML)
- Specially designed infinite canvas for your pastes 
- takes advantage of NML (nested markup language) to save your work.

## 🚀 Get Started Now - Download V3.0.1

**Ready to boost your productivity?** 

👉 **[Download Latest Portable Release (v3.0.1)](../../releases/latest)**

**What you get:**
- ⚡ **Instant Setup** - Single portable executable, no installer needed
- 🎯 **Super Lightweight** - Only 120MB total (including tiny GGML weights)
- 🛡️ **Local AI Processing** - Whisper.net keeps your voice private
- 🔧 **Just Need .NET 8.0** - Free Microsoft runtime (quick install)

Extract, double-click, and start speaking! The app lives silently in your system tray.

---

## ✨ Core Features

### 🎙️ Dual Hotkey System
- **F1**: **Speech-to-Text** → Hold, speak, release → Instant paste
- **Shift+F2**: **Speech-Prompt-to-AI** → Voice your question → Get AI response pasted

### 🤖 AI Integration with 300+ Models
- **OpenRouter API**: Access to **300+ cutting-edge LLM models**
- **GPT-4, Claude, Gemini, Llama** and hundreds more
- **Customizable Prompts**: Configure system behavior for your needs
- **Streaming Responses**: Real-time AI output for faster interaction

### 📋 Revived Transcription History
- **System Tray Access** - Right-click tray icon → View transcription history
- **Session Logging** - Track all your voice-to-text conversions
- **Search & Review** - Find past transcriptions quickly
- **Privacy Controls** - Enable/disable history as needed

### 🔊 Custom Audio Notifications
- **Smart Feedback** - Know when recording starts/stops
- **Custom Sounds** - Use your own audio files
- **Per-Mode Alerts** - Different sounds for transcription vs AI responses
- **Volume Control** - Adjust to your preference

### 🔒 Privacy & Security
- **Local Speech Processing**: Whisper runs entirely on your machine (120MB GGML weights)
- **Encrypted API Storage**: Secure OpenRouter key management via Windows DPAPI
- **No Data Sharing**: Your voice stays private (except for LLM requests you explicitly make)

### 👻 Silent Operation
- **System Tray Interface**: Lives quietly in background
- **Zero Interruption**: Works seamlessly with any Windows application
- **Minimal Resource Usage**: Efficient despite powerful AI capabilities

## 🎯 Perfect For

### Traditional Speech-to-Text
- **Quick Notes**: Capture thoughts without breaking workflow
- **Dictation**: Write emails, documents, messages hands-free
- **Accessibility**: Voice input for any Windows application

### AI-Powered Assistance
- **Research**: Ask questions and get instant answers from 300+ models
- **Writing**: Get help with content, grammar, style
- **Coding**: Voice-ask programming questions
- **Creative**: Brainstorm ideas, get suggestions
- **Productivity**: Quick calculations, translations, explanations

## 📋 Prerequisites

### Required
- **Windows 10/11** (64-bit)
- **.NET 8.0 Runtime** ([Download free from Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Any microphone** (built-in or external)

### Optional
- **OpenRouter API key** (for AI features - get free credits at [openrouter.ai](https://openrouter.ai))

## ⚙️ Configuration

Right-click the system tray icon to access comprehensive settings:

### General Settings
- Auto-start with Windows
- Theme selection (Light/Dark/System)
- Logging and retention policies

### Hotkeys
- Customize push-to-talk keys (default: F1)
- Configure AI hotkey (default: Shift+F2)
- Modifier key requirements

### Audio Settings
- Microphone selection and testing
- Sample rate and buffer size optimization
- Real-time audio quality validation

### Whisper Settings
- Model selection (Tiny → Medium for speed vs. accuracy)
- Language preference (auto-detect or specific)
- GPU acceleration toggle

### OpenRouter (V3.0)
- **API Key Management**: Secure encrypted storage
- **Model Selection**: Choose from **300+ LLM models**
- **System Prompts**: Customize AI behavior
- **Advanced Parameters**: Temperature, max tokens, streaming

### Audio Notifications (V3.0)
- **Notification Control**: Enable/disable audio feedback
- **Custom Sounds**: Use your own WAV/MP3 files
- **Volume Control**: Adjust notification volume
- **Per-Mode Settings**: Different sounds for speech-to-text vs. AI responses

### Transcription History (V3.0.1)
- **History Management**: View, search, and manage past transcriptions
- **Data Retention**: Configure how long to keep history
- **Export Options**: Save transcriptions to files
- **Privacy Controls**: Clear history or disable logging

## 🔧 Technical Details

### Architecture
- **Framework**: Clean .NET 8.0 with dependency injection
- **Audio Processing**: NAudio for high-quality recording
- **Speech Recognition**: Whisper.NET with GGML weights (120MB total)
- **Global Hotkeys**: SharpHook for system-wide key detection
- **AI Integration**: OpenRouter API with HTTP client for 300+ models
- **Secure Storage**: Windows DPAPI for API key encryption
- **UI Framework**: Modern WPF with system tray (H.NotifyIcon)

### Whisper Models (Local Processing)
- **Tiny**: Fastest, good for simple speech (~39M parameters, ~39MB)
- **Base**: Balanced speed/accuracy (recommended, ~74M parameters, ~74MB)
- **Small**: Better accuracy (~244M parameters, ~244MB)
- **Medium**: High accuracy (~769M parameters, ~769MB)

*Despite the power, total installation remains lightweight at ~120MB with tiny model included.*

### OpenRouter Models (Cloud Processing)
Access to **300+ models** including:
- **GPT-4o, Claude-3.5, Gemini Pro** for premium quality
- **Llama, Mistral, Qwen, Command** for cost-effective processing
- **Specialized models** for coding, reasoning, creative tasks, and more

## 🛠️ Build from Source

```bash
git clone https://github.com/MushroomFleet/careless-whisper-V2
cd careless-whisper-V2
dotnet build CarelessWhisperV2.csproj
dotnet run --project CarelessWhisperV2.csproj
```

Requires .NET 8.0 SDK for development.

## 🔐 Privacy & Security

### Local Processing (Speech-to-Text)
- **No internet required**: All transcription happens locally via Whisper
- **No data collection**: Your voice never leaves your computer
- **Optional logging**: Transcription history saved locally (can be disabled)

### Cloud Processing (AI Features)
- **User Control**: AI features are opt-in with explicit API key setup
- **Transparent Requests**: Only voice prompts you explicitly make are sent to OpenRouter
- **No Voice Storage**: Audio is transcribed locally before sending text prompts
- **API Security**: Keys encrypted with Windows DPAPI

### Open Source Transparency
- **Full Source Available**: Complete transparency of data handling
- **Audit-Friendly**: Clean architecture for security review
- **No Hidden Telemetry**: Everything the app does is visible in code

## 📝 Status

**Current Version**: **3.0.1** - Latest stable release

✅ **Working**: 
- Dual-mode speech processing (local + AI)
- Complete settings UI with all configuration options
- Secure OpenRouter integration with **300+ models**
- Custom audio notification system
- **Revived transcription history** with search and management
- Enhanced transcription with multiple Whisper models

🎯 **V3.0.1 Achievements**:
- **Dual Hotkey System**: F1 for direct transcription, Shift+F2 for AI assistance
- **300+ OpenRouter Models**: Massive selection of cutting-edge LLMs
- **Transcription History Revival**: Complete session logging with system tray access
- **Custom Audio Notifications**: Configurable sound feedback
- **Enhanced Security**: Encrypted API key management
- **Comprehensive Settings**: Full-featured configuration UI

## 🚀 Use Cases & Examples

### Content Creation
- **Shift+F2**: "Help me write a professional email declining a meeting"
- **Shift+F2**: "Suggest three blog post titles about AI productivity"

### Development
- **Shift+F2**: "Explain this error: undefined reference to malloc"
- **Shift+F2**: "Write a Python function to sort a list by date"

### Research & Learning
- **Shift+F2**: "What are the key benefits of microservices architecture?"
- **Shift+F2**: "Translate this to Spanish: The meeting is at 3 PM"

### Quick Tasks
- **F1**: Direct dictation for emails, documents, chat messages
- **Shift+F2**: "Calculate 15% tip on $47.83" or "What's the capital of Slovenia?"

## 🤝 Contributing

This project implements production-ready .NET 8.0 patterns with comprehensive V3.0 architecture. See [docs/devteam-handoff-v3-final.md](docs/devteam-handoff-v3-final.md) for complete technical documentation.

## 📄 License

[License to be determined]

---

**Made for developers, writers, and anyone who wants both instant transcription AND AI assistance at their fingertips.**

*V3.0.1: Where voice meets intelligence - Care Less, Achieve More.*


