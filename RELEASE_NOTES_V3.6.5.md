# 🐱 Careless Whisper V3.6.5 - CarelessKitten TTS Release

## 🎉 Major Milestone: First Release with Embedded Neural TTS

**Careless Whisper V3.6.5** introduces **CarelessKitten TTS** - a revolutionary neural text-to-speech system that transforms the simple **Ctrl+F1** hotkey into a gateway to 8 high-quality expressive voices.

---

## 🚀 What's New in V3.6.5

### 🐱 CarelessKitten TTS Integration
The flagship feature of v3.6.5, bringing neural text-to-speech directly to your clipboard workflow:

- **🎙️ 8 Expressive Neural Voices**: Premium KittenTTS voices (expr-voice-2-m/f through expr-voice-5-m/f)
- **⚡ Instant Activation**: Simple **Ctrl+F1** hotkey reads clipboard content aloud
- **🧠 Intelligent Text Processing**: Advanced num2words integration for currency, dates, ordinals
- **🔊 Crystal Clear Audio**: Neural synthesis produces natural, intelligible speech
- **📋 Smart Clipboard Integration**: Automatic text detection and optimization
- **🛡️ Offline Operation**: Complete TTS processing happens locally

### 🏗️ Technical Achievements

**Embedded Python Environment**:
- Portable Python 3.11.9 deployment with zero external dependencies
- Bundled eSpeak integration for phoneme processing
- Advanced dependency resolution bypassing circular dependency issues
- Complete num2words library integration for professional text normalization

**Comprehensive Monkey Patching System**:
- Full phonemizer backend replacement with custom IPA phoneme generation
- Advanced eSpeak detection bypassing for seamless bundled eSpeak integration
- System path isolation preventing conflicts with system Python installations
- Sophisticated module interception ensuring consistent neural voice quality

**Multi-tier Fallback Architecture**:
- **Primary**: Embedded Python KittenTTS (neural voices)
- **Secondary**: System Python KittenTTS (if available)
- **Tertiary**: Windows SAPI (universal compatibility)

---

## 📦 Release Assets

### Primary Release
- **📁 CarelessWhisperV3.6.5-portable.zip** (277.6 MB compressed)
  - Complete standalone application with embedded Python
  - No installation required - extract and run
  - Includes all TTS dependencies and neural voice models
  - Compatible with Windows 10/11 x64

### What's Included
- **🎯 CarelessWhisperV3.6.5-portable.exe** - Main application (269MB)
- **🐍 Embedded Python Environment** - Complete TTS runtime
- **🔊 eSpeak Integration** - Bundled phonemizer with data files
- **📖 Documentation** - Distribution and installation guides

---

## 🎯 Six-Mode Hotkey System

Careless Whisper V3.6.5 now provides six distinct modes of operation:

1. **F1** - **Speech-to-Text**: Hold, speak, release → Instant paste
2. **🆕 Ctrl+F1** - **🐱 CarelessKitten TTS**: Read clipboard aloud with neural voices
3. **Shift+F2** - **Speech-Prompt-to-AI**: Voice questions → AI responses
4. **Ctrl+F2** - **Speech Copy Prompt**: Combine clipboard + voice → Enhanced AI processing  
5. **Shift+F3** - **Vision Capture**: Screen selection → AI image analysis
6. **Ctrl+F3** - **Speech + Vision**: Combined voice + image analysis

---

## 🔧 Installation & Usage

### Quick Start
1. **Download** `CarelessWhisperV3.6.5-portable.zip`
2. **Extract** to any directory
3. **Run** `CarelessWhisperV3.6.5-portable.exe`
4. **Test TTS**: Copy any text → Press **Ctrl+F1** → Listen!

### Prerequisites
- **Windows 10/11** (64-bit)
- **.NET 8.0 Runtime** ([Download from Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Microphone** (for speech input features)

### CarelessKitten TTS Usage
- **Copy any text** from anywhere (documents, web pages, emails)
- **Press Ctrl+F1** - Neural TTS begins instantly
- **Listen** to high-quality speech with natural pronunciation
- **Automatic processing** of numbers, currencies, dates, and technical terms

---

## 🎨 Neural Voice Showcase

### Available Voices
- **expr-voice-2-m** - Male Voice #2 (Expressive)
- **expr-voice-2-f** - Female Voice #2 (Expressive)  
- **expr-voice-3-m** - Male Voice #3 (Expressive)
- **expr-voice-3-f** - Female Voice #3 (Expressive)
- **expr-voice-4-m** - Male Voice #4 (Expressive)
- **expr-voice-4-f** - Female Voice #4 (Expressive)
- **expr-voice-5-m** - Male Voice #5 (Expressive)
- **expr-voice-5-f** - Female Voice #5 (Expressive)

### Voice Quality Features
- **Natural Pronunciation**: Proper handling of currencies ($25.50 → "twenty-five dollars and fifty cents")
- **Smart Ordinals**: 1st → "first", 2nd → "second", 3rd → "third"  
- **Date Processing**: Natural date and time pronunciation
- **Technical Terms**: Clear pronunciation of technical documentation
- **Speed Control**: Adjustable speech rate for comfort

---

## 🛠️ For Developers

### Build Environment
- **Framework**: .NET 8.0 with comprehensive dependency injection
- **TTS Integration**: Python subprocess architecture for optimal performance
- **Neural Processing**: KittenTTS with ONNX runtime and HuggingFace model loading
- **Text Preprocessing**: Advanced num2words and phonemizer integration

### Technical Innovations
- **Circular Dependency Resolution**: Custom solutions for num2words/docopt conflicts  
- **System Path Isolation**: Prevents conflicts between embedded and system Python
- **IPA Phoneme Processing**: Ensures neural network receives proper phonetic input
- **Advanced Monkey Patching**: Comprehensive phonemizer backend replacement
- **Portable Deployment**: Fully self-contained with embedded dependencies

---

## 🐛 Known Issues

- **Unicode Warning**: Minor cosmetic warning during TTS generation (doesn't affect functionality)
- **Dependency Complexity**: num2words installation requires manual source integration due to docopt conflicts
- **File Size**: Larger distribution due to embedded Python environment (acceptable trade-off for neural quality)

---

## 🎯 Compatibility

### Fully Tested
- **Windows 11** - Primary development platform
- **Windows 10** - Verified compatibility
- **.NET 8.0** - Required runtime

### Performance
- **TTS Response Time**: ~100-200ms for typical clipboard content
- **Memory Usage**: Moderate increase due to embedded Python (acceptable for neural quality)
- **CPU Usage**: Efficient neural processing with optimized model loading

---

## 🤝 Acknowledgments

**CarelessKitten TTS** represents months of intensive development solving complex integration challenges:
- Advanced dependency resolution for Python packaging conflicts
- Sophisticated phonemizer patching for eSpeak integration  
- Neural TTS quality optimization through proper IPA phoneme processing
- Comprehensive fallback architecture ensuring universal compatibility

This release establishes Careless Whisper as not just a transcription tool, but a complete voice-enabled productivity suite.

---

## 📋 Upgrade Notes

### From V3.6.3 → V3.6.5
- **Full application replacement** recommended
- **Settings preserved** - existing configuration remains intact
- **New hotkey available** - Ctrl+F1 for TTS functionality  
- **No breaking changes** - all existing features remain functional

### First-Time Users
- **Complete package** - no additional downloads required
- **Instant functionality** - TTS works immediately after extraction
- **Zero configuration** - neural voices available out-of-the-box

---

**🐱 CarelessKitten TTS: Where neural voices meet effortless productivity**

*The future of accessible, high-quality text-to-speech is here - built into the tool you already trust for voice transcription and AI assistance.*
