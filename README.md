# Careless Whisper V3

**"Care Less" - Inobtrusive Augments!**

**Six-Mode Voice & AI Assistant for Windows** • Silent system tray interface • Built with .NET 8.0

Transform your voice into text instantly, get AI-powered responses, analyze screen content, or listen to clipboard text with neural voices—all with simple hotkeys. No windows, no interruptions—just speak and get results anywhere.

**Related Project:** [Careless-Canvas-NML](https://github.com/MushroomFleet/Careless-Canvas-NML) - Infinite canvas for your pastes using nested markup language

## 🚀 Get Started Now - Download V3.6.5

**Ready to boost your productivity?** 

👉 **[Download Latest Portable Release (v3.6.5)](../../releases/latest)**

**What you get:**
- ⚡ **Instant Setup** - Single portable executable, no installer needed
- 🐱 **Neural TTS Included** - 277MB total with embedded CarelessKitten TTS
- 🛡️ **Local AI Processing** - Whisper.net keeps your voice private
- 🔧 **Just Need .NET 8.0** - Free Microsoft runtime (quick install)

Extract, double-click, and start speaking! The app lives silently in your system tray with 8 neural voices ready.

---

## ✨ Core Features

### 🎙️ Six-Mode Hotkey System
Complete voice and AI workflow integration with simple hotkey combinations:

**Voice Processing**
- **F1**: **Speech-to-Text** → Hold, speak, release → Instant paste
- **Ctrl+F1**: **🐱 CarelessKitten TTS** → Neural voices read clipboard content aloud (8 expressive voices)

**AI-Powered Analysis**  
- **Shift+F2**: **Speech-to-AI** → Voice your question → Get AI response pasted
- **Ctrl+F2**: **Speech + Clipboard** → Combine clipboard content with voice prompt for enhanced AI processing

**Visual Intelligence**
- **Shift+F3**: **Vision Capture** → Drag-select screen area → AI describes image → Instant paste  
- **Ctrl+F3**: **Speech + Vision** → Voice question + screen selection → Combined AI analysis → Paste result

### 🐱 CarelessKitten Neural TTS (NEW in v3.6.5)
Revolutionary text-to-speech integration bringing 8 high-quality neural voices to your clipboard workflow:
- **8 Expressive Voices**: Premium KittenTTS neural synthesis (expr-voice-2-m/f through expr-voice-5-m/f)
- **Instant Activation**: Simple Ctrl+F1 hotkey reads any copied text aloud
- **Smart Text Processing**: Advanced num2words integration for natural pronunciation of currencies, dates, and ordinals  
- **Embedded Python Environment**: Complete portable TTS runtime with no external dependencies
- **Multi-tier Fallback**: KittenTTS → System Python → Windows SAPI for universal compatibility
- **Professional Quality**: IPA phoneme processing ensures crystal-clear neural speech synthesis

### 🤖 AI Integration - Local & Cloud
Flexible AI processing with privacy-first design:
- **OpenRouter Integration**: 300+ cutting-edge cloud models (GPT-4, Claude, Gemini, Llama, etc.)
- **Ollama Support**: Local AI models for complete privacy (Llama, Mistral, Qwen, Code Llama, custom models)
- **Dual Provider System**: Switch between cloud power and local privacy as needed
- **Streaming Responses**: Real-time AI output for faster interaction
- **Offline Capability**: Full functionality without internet when using Ollama

### 👁️ AI Vision Analysis  
Advanced screen capture and image understanding:
- **Drag-to-Select Interface**: Professional overlay with visual feedback and multi-monitor support
- **Dual Vision Modes**: Quick capture (Shift+F3) or combined speech+vision analysis (Ctrl+F3)  
- **Smart Image Processing**: Token-aware optimization and format detection for maximum API efficiency
- **Vision Model Support**: Claude 3, GPT-4 Vision, LLaVA, and other vision-capable models
- **Customizable Prompts**: Configure analysis behavior with preset options for common scenarios
- **Fast Performance**: ~30ms screen capture using optimized BitBlt API

### 📋 Enhanced Clipboard Integration
Intelligent clipboard workflow features:
- **Speech Copy Prompt**: Ctrl+F2 automatically captures clipboard content and combines with voice for enhanced AI processing
- **Context-Aware Processing**: AI receives both clipboard text and voice input for more informed responses
- **Smart Text Detection**: Automatic content optimization for both TTS synthesis and AI analysis
- **Universal Compatibility**: Works with any application that supports clipboard operations

### 🔒 Privacy & Security Features
- **Local Speech Processing**: Whisper.NET runs entirely on your machine
- **Encrypted API Storage**: Secure key management via Windows DPAPI  
- **No Data Sharing**: Voice data never leaves your computer (except explicit AI requests)
- **Optional Logging**: Transcription history can be disabled for maximum privacy
- **Open Source**: Complete transparency of data handling

### 🔧 System Integration
- **Silent Operation**: Lives quietly in system tray with zero interruption
- **Smart Notifications**: Customizable audio feedback for different operations
- **Transcription History**: Search and manage past voice-to-text conversions
- **Global Hotkeys**: System-wide operation in any Windows application
- **Efficient Resource Usage**: Optimized performance despite powerful AI capabilities

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

## ⚙️ Essential Settings

**Right-click the system tray icon** to access settings for:
- **Hotkeys**: Customize voice and AI hotkeys  
- **Audio**: Select microphone and configure recording quality
- **AI Providers**: Add OpenRouter API key or configure Ollama server
- **Voice Models**: Choose Whisper model size (speed vs. accuracy)
- **Privacy**: Control transcription history and data retention

## 🔧 Technical Overview

### Core Architecture
- **Framework**: .NET 8.0 with dependency injection and modern WPF
- **Audio Processing**: NAudio for high-quality voice recording
- **Speech Recognition**: Whisper.NET with local GGML model processing  
- **Neural TTS**: Embedded Python 3.11.9 with KittenTTS and advanced phoneme processing
- **AI Integration**: Dual provider system (300+ cloud models + local models)
- **System Integration**: SharpHook for global hotkeys, Windows DPAPI for secure storage

### AI Model Support
**Cloud Processing (OpenRouter)**
- GPT-4o, Claude-3.5, Gemini Pro, Llama, Mistral, Qwen, and 300+ specialized models

**Local Processing (Ollama)**  
- Llama 3.2, Mistral 7B/22B, Qwen 2.5, Code Llama, and custom GGUF models

**Neural Text-to-Speech (CarelessKitten)**
- 8 expressive KittenTTS voices with professional text preprocessing
- Multi-tier fallback: KittenTTS → System Python → Windows SAPI

### Performance & Size
- **Total Package**: 277MB (includes embedded Python + neural TTS + Whisper models)
- **TTS Response**: ~100-200ms for typical clipboard content
- **Screen Capture**: ~30ms using optimized BitBlt API
- **Memory Usage**: Efficient resource management despite AI capabilities

## 🛠️ Build from Source

```bash
git clone https://github.com/MushroomFleet/careless-whisper-V3
cd careless-whisper-V3
dotnet build CarelessWhisperV2.csproj
dotnet run --project CarelessWhisperV2.csproj
```

Requires .NET 8.0 SDK for development.

## 📋 Prerequisites

**Required**:
- Windows 10/11 (64-bit)
- .NET 8.0 Runtime ([Download free from Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0))
- Any microphone (built-in or external)

**Optional**:  
- OpenRouter API key for AI features (get free credits at [openrouter.ai](https://openrouter.ai))

## ⚙️ Essential Settings

**Right-click the system tray icon** to access settings for:
- **Hotkeys**: Customize voice and AI hotkeys  
- **Audio**: Select microphone and configure recording quality
- **AI Providers**: Add OpenRouter API key or configure Ollama server
- **Voice Models**: Choose Whisper model size (speed vs. accuracy)
- **Privacy**: Control transcription history and data retention

## 🔧 Technical Overview

### Core Architecture
- **Framework**: .NET 8.0 with dependency injection and modern WPF
- **Audio Processing**: NAudio for high-quality voice recording
- **Speech Recognition**: Whisper.NET with local GGML model processing  
- **Neural TTS**: Embedded Python 3.11.9 with KittenTTS and advanced phoneme processing
- **AI Integration**: Dual provider system (300+ cloud models + local models)
- **System Integration**: SharpHook for global hotkeys, Windows DPAPI for secure storage

### AI Model Support
**Cloud Processing (OpenRouter)**: GPT-4o, Claude-3.5, Gemini Pro, Llama, Mistral, Qwen, and 300+ specialized models

**Local Processing (Ollama)**: Llama 3.2, Mistral 7B/22B, Qwen 2.5, Code Llama, and custom GGUF models

**Neural Text-to-Speech (CarelessKitten)**: 8 expressive KittenTTS voices with professional text preprocessing and multi-tier fallback architecture

### Performance & Size
- **Total Package**: 277MB (includes embedded Python + neural TTS + Whisper models)
- **TTS Response**: ~100-200ms for typical clipboard content
- **Screen Capture**: ~30ms using optimized BitBlt API
- **Memory Usage**: Efficient resource management despite AI capabilities

## 🔐 Privacy & Security

- **Local Speech Processing**: All transcription happens locally via Whisper.NET
- **Encrypted API Storage**: Secure key management via Windows DPAPI  
- **No Data Sharing**: Voice data never leaves your computer (except explicit AI requests)
- **Optional Logging**: Transcription history can be disabled for maximum privacy
- **Open Source Transparency**: Complete code visibility for audit-friendly security review

## 📝 Current Status

**Version 3.6.5** - Complete six-mode voice productivity suite with CarelessKitten TTS integration

✅ **All Features Working**: Six-mode hotkey system, neural TTS, AI vision analysis, dual AI integration, clipboard workflows, transcription history, comprehensive settings UI

## 🚀 Real-World Workflows

### Voice & AI Productivity
- **F1**: Quick dictation for emails, documents, chat messages
- **Shift+F2**: "Calculate 15% tip on $47.83" or "Explain this error: undefined reference to malloc"
- **Ctrl+F2**: Copy draft email → "Make this more professional" → Get polished version

### Neural TTS & Accessibility  
- **Ctrl+F1**: Copy articles, documentation, emails → Listen while working/coding/multitasking
- **Ctrl+F1**: Copy study materials, foreign language text → Audio learning with natural pronunciation
- **Ctrl+F1**: Copy meeting notes, reports → Hands-free content consumption

### Visual Intelligence
- **Shift+F3**: Capture error dialogs, charts, UI elements → Get instant AI descriptions  
- **Ctrl+F3**: "What programming language is this?" → Drag code area → Get analysis
- **Ctrl+F3**: "What accessibility issues are here?" → Drag UI → Get audit

### Content Enhancement Workflows
- **Copy + Voice**: Copy code snippets → Ctrl+F2 → "Explain and add comments"
- **Copy + Voice**: Copy data → Ctrl+F2 → "Summarize key trends" 
- **Copy + Voice**: Copy technical docs → Ctrl+F2 → "Simplify for beginners"

## 📋 Quick Setup Guide

### Essential Steps
1. **Download** CarelessWhisperV3.6.5-portable.zip
2. **Extract** to any directory  
3. **Run** CarelessWhisperV3.6.5-portable.exe
4. **Test**: Copy text → Press Ctrl+F1 → Listen to neural TTS!

### Optional Configuration
- **Right-click tray icon** → Settings
- **Add OpenRouter API key** for AI features (get free credits at openrouter.ai)
- **Configure hotkeys** and voice preferences
- **Test microphone** and adjust audio settings

*Perfect for: Developers, writers, researchers, students, anyone who wants voice-powered productivity*

## 🤝 Contributing

This project implements production-ready .NET 8.0 patterns with comprehensive V3.0 architecture. See [docs/devteam-handoff-v3-final.md](docs/devteam-handoff-v3-final.md) for complete technical documentation.

## 📄 License

[License to be determined]

---

**Made for developers, writers, and anyone who wants instant transcription, AI assistance, and neural TTS at their fingertips.**

*V3.6.5: Where voice meets neural intelligence - Care Less, Achieve More.*
