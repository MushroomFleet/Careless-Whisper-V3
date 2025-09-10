# 🐱 CarelessKitten TTS Integration Guide
## Careless Whisper V3.6.5

### New Hotkey: Ctrl+F1 - Text-to-Speech

**CarelessKitten** brings high-quality neural text-to-speech to Careless Whisper using KittenTTS technology.

### How to Use

1. **Copy any text** to your clipboard from any application
2. **Press Ctrl+F1** to have KittenTTS read the text aloud
3. **Adjust settings** in the new TTS tab in Settings window

### Features

- **8 Expressive Voices**: Male and female KittenTTS neural voices
- **Speed Control**: Adjustable speech speed from 0.5x to 2.0x
- **Volume Control**: Independent TTS volume settings
- **Text Limiting**: Configurable maximum text length (default: 5000 characters)
- **Fallback Support**: Windows SAPI fallback if KittenTTS unavailable

### Complete Hotkey Map

| Hotkey | Function |
|--------|----------|
| **F1** | Speech to clipboard (transcribe audio) |
| **🆕 Ctrl+F1** | **Text-to-Speech from clipboard** |
| **Shift+F2** | Speech + LLM prompt |
| **Ctrl+F2** | Speech + clipboard + LLM |
| **Shift+F3** | Vision capture |
| **Ctrl+F3** | Speech + vision capture |

### Architecture Overview

The TTS integration follows Careless Whisper's established patterns:

- **Process-based Python execution** for optimal performance
- **Service-oriented architecture** with clean dependency injection
- **Event-driven hotkey system** integrated with existing SharpHook implementation
- **NAudio-based playback** leveraging existing audio infrastructure

### Installation Requirements

**Automatic (Recommended):**
- TTS works out-of-the-box with embedded Python distribution
- KittenTTS package included in portable builds

**Manual Setup (if needed):**
1. Install Python 3.8+ on your system
2. Install KittenTTS: `pip install https://github.com/KittenML/KittenTTS/releases/download/0.1/kittentts-0.1.0-py3-none-any.whl`
3. Restart Careless Whisper

### Configuration

Access TTS settings through **Settings → 🐱 TTS tab**:

- **Voice Selection**: Choose from 8 expressive neural voices
- **Speech Speed**: Adjust playback speed (0.5x - 2.0x)
- **Volume**: Independent volume control for TTS audio
- **Max Text Length**: Limit text processing for performance
- **Fallback Options**: Windows SAPI backup configuration

### Performance Notes

- **First Use**: Initial model loading may take 10-15 seconds
- **Subsequent Uses**: Near-instantaneous generation (~100-200ms)
- **Text Limits**: Automatically truncates very long clipboard content
- **Memory Usage**: ~500MB additional RAM during TTS operations

### Troubleshooting

**TTS not working?**
1. Check Python installation via Settings → TTS → Refresh Status
2. Verify clipboard contains text before pressing Ctrl+F1
3. Check TTS settings are enabled
4. Try the test function in settings

**Poor audio quality?**
1. Try different voices in TTS settings
2. Adjust speech speed (slower = clearer)
3. Enable SAPI fallback for testing

**Python/KittenTTS errors?**
1. Use embedded Python distribution (recommended)
2. Install system Python 3.8+ if needed
3. Manually install KittenTTS package
4. Check firewall/antivirus blocking Python subprocess

### Technical Implementation

**Key Components:**
- `KittenTtsEngine`: Python subprocess TTS integration
- `AudioPlaybackService`: NAudio-based audio playback
- `TtsHotkeyHandler`: Ctrl+F1 clipboard reading logic
- `PythonEnvironmentManager`: Python installation management

**File Structure:**
```
Services/Tts/           # TTS service layer
Services/Python/        # Python environment management  
scripts/               # Python bridge scripts
python/                # Embedded Python (in builds)
```

### Future Enhancements

Planned features for future versions:
- Voice caching for frequently used text
- SSML markup support
- Custom voice training
- Batch TTS processing
- Audio effects and filters

---

**🐱 CarelessKitten** - Bringing expressive neural speech to your fingertips!
