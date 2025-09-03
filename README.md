# Careless Whisper V3

**Dual-Mode Voice-to-Text & AI Assistant for Windows** • Silent system tray interface • Built with .NET 8.0

Transform your voice into text instantly OR get AI-powered responses with simple hotkeys. No windows, no interruptions—just speak and get results anywhere.

## ✨ Core Features

### 🎙️ Dual Hotkey System
- **F1**: **Speech-to-Text** → Hold, speak, release → Instant paste
- **Shift+F2**: **Speech-Prompt-to-AI** → Voice your question → Get AI response pasted

### 🤖 AI Integration
- **OpenRouter API**: Access to 100+ cutting-edge LLM models
- **Customizable Prompts**: Configure system behavior for your needs
- **Streaming Responses**: Real-time AI output for faster interaction

### 🔒 Privacy & Security
- **Local Speech Processing**: Whisper runs entirely on your machine
- **Encrypted API Storage**: Secure OpenRouter key management via Windows DPAPI
- **No Data Sharing**: Your voice stays private (except for LLM requests you explicitly make)

### 👻 Silent Operation
- **System Tray Interface**: Lives quietly in background
- **Audio Notifications**: Optional sound feedback for operations
- **Zero Interruption**: Works seamlessly with any Windows application

## 🚀 Quick Start

### Prerequisites
- Windows 10/11
- .NET 8.0 Runtime
- Any microphone
- OpenRouter API key (for AI features - get free credits at [openrouter.ai](https://openrouter.ai))

### Installation
1. Download the latest V3.0 release
2. Extract and run `CarelessWhisperV2.exe`
3. The app minimizes to your system tray (look for the icon near your clock)
4. Right-click tray icon → Settings → OpenRouter tab → Add your API key

### Usage

#### Speech-to-Text (Traditional Mode)
1. **Hold F1** and speak clearly
2. **Release F1** when finished
3. **Paste anywhere** with Ctrl+V
*Perfect for dictation, quick notes, and direct transcription*

#### Speech-Prompt-to-AI (New V3.0 Mode)
1. **Hold Shift+F2** and ask your question
2. **Release keys** when finished speaking
3. **Wait briefly** for AI processing
4. **Paste the AI response** with Ctrl+V
*Perfect for research, writing assistance, code help, and creative tasks*

## 🎯 Perfect For

### Traditional Speech-to-Text
- **Quick Notes**: Capture thoughts without breaking workflow
- **Dictation**: Write emails, documents, messages hands-free
- **Accessibility**: Voice input for any Windows application

### AI-Powered Assistance
- **Research**: Ask questions and get instant answers
- **Writing**: Get help with content, grammar, style
- **Coding**: Voice-ask programming questions
- **Creative**: Brainstorm ideas, get suggestions
- **Productivity**: Quick calculations, translations, explanations

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
- **Model Selection**: Choose from 100+ LLM models
- **System Prompts**: Customize AI behavior
- **Advanced Parameters**: Temperature, max tokens, streaming

### Audio Notifications (V3.0)
- **Notification Control**: Enable/disable audio feedback
- **Custom Sounds**: Use your own WAV/MP3 files
- **Volume Control**: Adjust notification volume
- **Per-Mode Settings**: Different sounds for speech-to-text vs. AI responses

## 🔧 Technical Details

### Architecture
- **Framework**: Clean .NET 8.0 with dependency injection
- **Audio Processing**: NAudio for high-quality recording
- **Speech Recognition**: Whisper.NET for local transcription
- **Global Hotkeys**: SharpHook for system-wide key detection
- **AI Integration**: OpenRouter API with HTTP client
- **Secure Storage**: Windows DPAPI for API key encryption
- **UI Framework**: Modern WPF with system tray (H.NotifyIcon)

### Whisper Models (Local Processing)
- **Tiny**: Fastest, good for simple speech (~39M parameters)
- **Base**: Balanced speed/accuracy (recommended, ~74M parameters)
- **Small**: Better accuracy (~244M parameters)
- **Medium**: High accuracy (~769M parameters)

### OpenRouter Models (Cloud Processing)
Access to 100+ models including:
- **GPT-4o, Claude-3.5, Gemini Pro** for premium quality
- **Llama, Mistral, Qwen** for cost-effective processing
- **Specialized models** for coding, reasoning, creative tasks

## 🛠️ Build from Source

```bash
git clone https://github.com/[username]/careless-whisper-v3
cd careless-whisper-v3
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

**Current Version**: 3.0.0 - Major feature release

✅ **Working**: 
- Dual-mode speech processing (local + AI)
- Complete settings UI with all configuration options
- Secure OpenRouter integration with 100+ models
- Audio notification system
- Enhanced transcription with multiple Whisper models

🎯 **V3.0 Achievements**:
- **Dual Hotkey System**: F1 for direct transcription, Shift+F2 for AI assistance
- **OpenRouter Integration**: Access to cutting-edge LLM models
- **Audio Notifications**: Configurable sound feedback
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

*V3.0: Where voice meets intelligence.*
