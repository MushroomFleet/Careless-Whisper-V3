# Careless Whisper V3

**"Care Less" - Inobtrusive Augments!**

**Triple-Mode Voice-to-Text & AI Assistant for Windows** • Silent system tray interface • Built with .NET 8.0

Transform your voice into text instantly, get AI-powered responses, or combine clipboard content with voice prompts—all with simple hotkeys. No windows, no interruptions—just speak and get results anywhere.

**4/9/25:** [Careless-Canvas-NML](https://github.com/MushroomFleet/Careless-Canvas-NML)
- Specially designed infinite canvas for your pastes 
- takes advantage of NML (nested markup language) to save your work.

## 🚀 Get Started Now - Download V3.6

**Ready to boost your productivity?** 

👉 **[Download Latest Portable Release (v3.6.2)](../../releases/latest)**

**What you get:**
- ⚡ **Instant Setup** - Single portable executable, no installer needed
- 🎯 **Super Lightweight** - Only 157MB total (including tiny GGML weights)
- 🛡️ **Local AI Processing** - Whisper.net keeps your voice private
- 🔧 **Just Need .NET 8.0** - Free Microsoft runtime (quick install)

Extract, double-click, and start speaking! The app lives silently in your system tray.

---

## ✨ Core Features

### 🎙️ Quintuple Hotkey System
- **F1**: **Speech-to-Text** → Hold, speak, release → Instant paste
- **Shift+F2**: **Speech-Prompt-to-AI** → Voice your question → Get AI response pasted
- **Ctrl+F2**: **Speech Copy Prompt to Paste** → Combines clipboard content with voice prompt → AI processes both together
- **Shift+F3**: **Vision Capture** → Select screen area → AI describes image → Instant paste
- **Ctrl+F3**: **Speech + Vision** → Hold, speak, release → Select screen area → AI analyzes both → Paste result

### 📝 Speech Copy Prompt to Paste (NEW in v3.6.2)
- **Intelligent Clipboard Integration**: Automatically captures existing clipboard content when Ctrl+F2 is pressed
- **Dual Input Processing**: Combines your voice transcription with clipboard text using template: `"[speech-transcription], [copy-buffer text]"`
- **Seamless Workflow**: Copy any text → Hold Ctrl+F2 → Speak your instruction → Get enhanced AI response
- **Universal Compatibility**: Works with both OpenRouter (300+ cloud models) and Ollama (local models)
- **Context-Aware Processing**: AI receives both inputs for more informed and relevant responses

### 👁️ AI Vision Analysis (NEW in v3.6.3)
- **Drag-to-Select Interface**: Visual overlay with animated selection rectangle for precise screen capture
- **Dual Vision Modes**: Quick capture (Shift+F3) or combined speech+vision analysis (Ctrl+F3)
- **Multi-Monitor Support**: Works seamlessly across different screen setups and DPI configurations
- **Smart Image Processing**: Automatic optimization for vision APIs (token-aware resizing, format detection)
- **Vision Model Integration**: Uses your selected LLM provider's vision models (Claude 3, GPT-4 Vision, LLaVA, etc.)
- **Customizable Prompts**: Configure default analysis behavior or use preset prompts for common tasks
- **Fast Capture Performance**: ~30ms screen capture using optimized BitBlt API

### 🤖 Dual AI Integration - Local & Cloud
- **OpenRouter API**: Access to **300+ cutting-edge cloud models** (GPT-4, Claude, Gemini, etc.)
- **Ollama Integration**: **Local AI models** for privacy-focused processing (Llama, Mistral, Qwen, etc.)
- **Dual Provider Choice**: Switch between cloud power and local privacy
- **Customizable Prompts**: Configure system behavior for your needs
- **Streaming Responses**: Real-time AI output for faster interaction
- **Offline Capability**: Ollama models work without internet connection

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

### Speech Copy Prompt Integration (Ctrl+F2)
- **Content Enhancement**: Copy text → Voice improvements → Get polished version
- **Code Analysis**: Copy code snippets → Voice questions → Get explanations
- **Document Processing**: Copy paragraphs → Voice instructions → Get summaries/translations
- **Data Analysis**: Copy spreadsheet data → Voice queries → Get insights
- **Email Assistance**: Copy draft emails → Voice refinements → Get professional versions

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
- Configure Speech Copy Prompt hotkey (default: Ctrl+F2)
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

### Ollama (V3.6.0)
- **Local AI Models**: Run Llama, Mistral, Qwen, and other models locally
- **Privacy First**: All processing happens on your machine
- **No Internet Required**: Works completely offline
- **Server Configuration**: Connect to local Ollama server
- **Model Management**: Automatic discovery of installed models
- **✅ Fixed**: JSON deserialization issues resolved for proper API integration

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

### Vision (V3.6.3)
- **System Prompt Customization**: Configure default analysis behavior for Shift+F3
- **Prompt Presets**: Quick-select common scenarios (OCR, detailed analysis, accessibility, UI description)
- **Image Processing Settings**: Adjust quality vs. speed balance and token optimization
- **Vision Model Compatibility**: Real-time validation of selected model's vision capabilities
- **Test Integration**: Built-in test functionality to verify screen capture and AI analysis

## 🔧 Technical Details

### Architecture
- **Framework**: Clean .NET 8.0 with dependency injection
- **Audio Processing**: NAudio for high-quality recording
- **Speech Recognition**: Whisper.NET with GGML weights (120MB total)
- **Global Hotkeys**: SharpHook for system-wide key detection
- **AI Integration**: Dual provider system - OpenRouter API (300+ cloud models) + Ollama (local models)
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

### Ollama Models (Local Processing)
Run popular open-source models locally:
- **Llama 3.2 (1B/3B)**: Fast, efficient for general tasks
- **Mistral 7B/22B**: Excellent reasoning and coding
- **Qwen 2.5**: Strong multilingual capabilities
- **Code Llama**: Specialized for programming tasks
- **Custom Models**: Support for any GGUF-compatible model

## 🛠️ Build from Source

```bash
git clone https://github.com/MushroomFleet/careless-whisper-V3
cd careless-whisper-V3
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

**Current Version**: **3.6.3** - Latest stable release with Vision Analysis

✅ **Working**: 
- **Quintuple-mode processing** (local speech + AI + clipboard + vision integration)
- **AI Vision Analysis** with drag-to-select screen capture and intelligent image processing
- Complete settings UI with Vision configuration tab
- Secure OpenRouter integration with **300+ models** including vision models
- **Ollama local AI integration** with full API support
- **Speech Copy Prompt to Paste** feature with clipboard integration
- Custom audio notification system
- **Revived transcription history** with search and management
- Enhanced transcription with multiple Whisper models

🎯 **V3.6.3 Achievements**:
- **AI Vision Analysis**: Revolutionary Shift+F3 and Ctrl+F3 hotkeys for screen capture and image analysis
- **Drag-to-Select Interface**: Professional overlay system with visual feedback and multi-monitor support
- **Speech + Vision Fusion**: Ctrl+F3 combines voice transcription with image analysis for comprehensive understanding
- **Vision Model Integration**: Seamless compatibility with Claude 3, GPT-4 Vision, LLaVA, and other vision models
- **Smart Image Processing**: Token-aware optimization and format detection for maximum API efficiency
- **Customizable Vision Prompts**: Full user control over analysis behavior with preset options
- **Fast Performance**: ~30ms screen capture with optimized BitBlt implementation

🎯 **V3.6.2 Achievements**:
- **Speech Copy Prompt to Paste**: Ctrl+F2 hotkey combining clipboard content with voice prompts
- **Intelligent Clipboard Integration**: Seamless workflow bridging copy-paste with AI assistance
- **Context-Aware AI Processing**: Enhanced prompts using both voice and clipboard content
- **Universal Provider Support**: Works with both OpenRouter and Ollama models
- **Dual AI Provider System**: OpenRouter (cloud) + Ollama (local) integration
- **Enhanced Privacy Options**: Choose between cloud and local AI processing
- **300+ OpenRouter Models**: Massive selection of cutting-edge LLMs
- **Local Model Support**: Llama, Mistral, Qwen, Code Llama, and custom models
- **Enhanced Security**: Encrypted API key management

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

### Speech Copy Prompt (Ctrl+F2) Examples
- **Copy draft email** → **Ctrl+F2**: "Make this more professional and concise"
- **Copy code snippet** → **Ctrl+F2**: "Explain what this function does and add comments"
- **Copy paragraph** → **Ctrl+F2**: "Translate this to French and improve the clarity"
- **Copy error message** → **Ctrl+F2**: "What does this error mean and how do I fix it?"
- **Copy spreadsheet data** → **Ctrl+F2**: "Summarize the key trends in this data"
- **Copy meeting notes** → **Ctrl+F2**: "Create action items from these notes"
- **Copy technical documentation** → **Ctrl+F2**: "Simplify this for a non-technical audience"
- **Copy product description** → **Ctrl+F2**: "Write marketing copy based on these features"

### Vision Capture (Shift+F3) Examples
- **Quick Description**: Capture any screen area for instant AI description
- **Error Analysis**: Select error dialogs to get explanations and solutions  
- **UI Documentation**: Capture interface elements for accessibility descriptions
- **Chart Reading**: Select graphs/charts for data interpretation
- **OCR Alternative**: Capture text in images for extraction and analysis
- **Design Feedback**: Select UI mockups for usability analysis
- **Technical Diagrams**: Capture system architectures for explanations

### Speech + Vision (Ctrl+F3) Examples  
- **Hold Ctrl+F3**: "What programming language is this?" → **Release** → **Drag** code area → Get language identification and code explanation
- **Hold Ctrl+F3**: "How do I improve this chart?" → **Release** → **Drag** chart → Get data visualization suggestions
- **Hold Ctrl+F3**: "What's wrong with this error?" → **Release** → **Drag** error dialog → Get troubleshooting steps
- **Hold Ctrl+F3**: "Explain this diagram to a beginner" → **Release** → **Drag** technical diagram → Get simplified explanation
- **Hold Ctrl+F3**: "What accessibility issues are here?" → **Release** → **Drag** UI area → Get accessibility audit
- **Hold Ctrl+F3**: "Convert this to markdown" → **Release** → **Drag** formatted text → Get markdown conversion
- **Hold Ctrl+F3**: "What's the sentiment of this content?" → **Release** → **Drag** social media post → Get sentiment analysis

## 📋 Step-by-Step Workflow Guides

### Quick Vision Analysis (Shift+F3)
1. **Press Shift+F3** - Overlay appears with crosshair cursor
2. **Drag to select** - Draw rectangle around area of interest  
3. **AI analyzes** - Image sent to your configured vision model
4. **Result ready** - Description automatically copied to clipboard
5. **Paste anywhere** - Ctrl+V to use the analysis

*Perfect for: Screenshots, UI elements, error dialogs, charts, diagrams*

### Combined Speech + Vision (Ctrl+F3)
1. **Hold Ctrl+F3** - Recording starts (speak your question/instruction)
2. **Release Ctrl+F3** - Speech transcription begins
3. **Overlay appears** - Drag to select screen area for analysis  
4. **AI processes both** - Speech transcription + image sent to vision model
5. **Combined result** - Enhanced analysis based on both inputs copied to clipboard
6. **Paste anywhere** - Ctrl+V to use the comprehensive analysis

*Perfect for: Complex analysis, specific questions about visual content, contextual understanding*

### Vision Settings Configuration
1. **Right-click tray icon** → **Settings** → **Vision tab**
2. **Customize system prompt** - Default: "Describe the image in a single line paragraph"
3. **Choose from presets** - Quick prompts for common scenarios (OCR, detailed analysis, accessibility, etc.)
4. **Adjust image quality** - Balance between speed and vision model accuracy
5. **Test functionality** - Built-in test button to verify vision capture works

## 🤝 Contributing

This project implements production-ready .NET 8.0 patterns with comprehensive V3.0 architecture. See [docs/devteam-handoff-v3-final.md](docs/devteam-handoff-v3-final.md) for complete technical documentation.

## 📄 License

[License to be determined]

---

**Made for developers, writers, and anyone who wants both instant transcription AND AI assistance at their fingertips.**

*V3.6.2: Where voice meets intelligence - Care Less, Achieve More.*
