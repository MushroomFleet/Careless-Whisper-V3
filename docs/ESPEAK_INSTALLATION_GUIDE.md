# 🔊 eSpeak Installation Guide for Windows

**Complete the TTS setup for CarelessWhisper with eSpeak voice synthesis**

---

## 📋 What is eSpeak?

eSpeak is a compact, open-source text-to-speech synthesizer that powers the CarelessKitten TTS feature in CarelessWhisper. Once installed, you'll be able to use **Ctrl+F1** to have any clipboard content read aloud with natural-sounding voices.

### ✨ Benefits
- **High-quality speech synthesis** with multiple voice options
- **Lightweight and fast** - minimal system impact
- **Multiple language support** - over 40 languages available
- **Free and open source** - no licensing costs

---

## 🖥️ System Requirements

- **Windows 10 or later** (Windows 11 recommended)
- **50 MB free disk space** for basic installation
- **Administrator privileges** for system installation
- **Internet connection** for downloading installer

---

## 🚀 Installation Methods

### 📦 Method 1: Official eSpeak Installer (RECOMMENDED)

This is the easiest method for most users and provides the most reliable installation.

#### Step 1: Download eSpeak
✅ **Action Required:**
1. Open your web browser
2. Navigate to: **https://espeak.sourceforge.net/download.html**
3. Look for the **"Windows"** section
4. Download **`eSpeak-1.48.04-source.zip`** (or latest version)
5. Also download **`espeak-1.48.04-Windows.exe`** (Windows installer)

💡 **Tip:** If the direct links don't work, search for "eSpeak Windows download" and use the SourceForge official page.

#### Step 2: Run the Installer
✅ **Action Required:**
1. **Right-click** on `espeak-1.48.04-Windows.exe`
2. Select **"Run as administrator"**
3. If Windows Security prompts appear, click **"More info"** then **"Run anyway"**
4. Click **"Next"** through the installation wizard
5. **Keep default installation path** (usually `C:\Program Files (x86)\eSpeak\`)
6. Select **"Add eSpeak to system PATH"** (important!)
7. Click **"Install"**
8. Click **"Finish"**

⚠️ **Important:** Make sure to check the "Add to PATH" option during installation!

#### Step 3: Verify Installation
✅ **Action Required:**
1. Press **Windows Key + R**
2. Type `cmd` and press **Enter**
3. In the command prompt, type: `espeak "Hello, this is a test"`
4. Press **Enter**

🎯 **Expected Result:** You should hear the text spoken aloud. If you hear audio, eSpeak is installed correctly!

---

### 🛠️ Method 2: Package Manager Installation (ADVANCED)

For users comfortable with command-line tools, you can use package managers.

#### Option A: Using Chocolatey
If you have Chocolatey installed:
```powershell
# Open PowerShell as Administrator
choco install espeak
```

#### Option B: Using Scoop
If you have Scoop installed:
```powershell
# Open PowerShell
scoop bucket add extras
scoop install espeak
```

---

### 💼 Method 3: Portable Installation (ALTERNATIVE)

If you prefer not to modify system settings or lack administrator privileges.

✅ **Action Required:**
1. Download the **source zip file** from eSpeak website
2. Extract to a folder like `C:\eSpeak\` (create this folder)
3. Copy the path (e.g., `C:\eSpeak\`)
4. Add this path to your user PATH environment variable:
   - Press **Windows Key + R**, type `sysdm.cpl`
   - Click **"Environment Variables"**
   - Under **"User variables"**, find **PATH**
   - Click **"Edit"** → **"New"** → Paste your eSpeak path
   - Click **"OK"** on all dialogs

---

## 🧪 Testing with CarelessWhisper

Once eSpeak is installed, test the complete TTS pipeline:

✅ **Action Required:**
1. **Open CarelessWhisper**
2. **Copy some text** to your clipboard (Ctrl+C)
3. **Press Ctrl+F1** to activate TTS
4. **Listen** for the text to be read aloud

🎯 **Expected Result:** Your clipboard content should be spoken using one of the CarelessKitten voices.

### Voice Options Available:
- **expr-voice-2-f** - Female Voice #2 (Default)
- **expr-voice-2-m** - Male Voice #2  
- **expr-voice-3-f** - Female Voice #3
- **expr-voice-3-m** - Male Voice #3
- **expr-voice-4-f** - Female Voice #4
- **expr-voice-4-m** - Male Voice #4
- **expr-voice-5-f** - Female Voice #5
- **expr-voice-5-m** - Male Voice #5

---

## 🔧 Troubleshooting

### ❌ Problem: "espeak not installed on your system"

**Solution 1: Check PATH Environment**
✅ **Try This:**
1. Open Command Prompt (Windows Key + R → `cmd`)
2. Type: `echo %PATH%`
3. Look for eSpeak directory in the output
4. If missing, re-run installer and ensure "Add to PATH" is checked

**Solution 2: Manual PATH Addition**
✅ **Try This:**
1. Find your eSpeak installation (usually `C:\Program Files (x86)\eSpeak\`)
2. Press **Windows Key + R** → type `sysdm.cpl` → Enter
3. Click **"Environment Variables"**
4. Under **"System variables"**, find **PATH**
5. Click **"Edit"** → **"New"**
6. Add your eSpeak directory path
7. Click **"OK"** on all dialogs
8. **Restart CarelessWhisper**

### ❌ Problem: Command Prompt Says "espeak is not recognized"

**Solution: Restart Required**
✅ **Try This:**
1. **Close all Command Prompt windows**
2. **Restart CarelessWhisper completely**
3. **Test again** with Ctrl+F1

**Still Not Working?**
✅ **Try This:**
1. **Restart your computer**
2. Test eSpeak in Command Prompt: `espeak "test"`
3. If working in Command Prompt, test CarelessWhisper again

### ❌ Problem: No Audio Output

**Solution: Check Audio Settings**
✅ **Try This:**
1. **Test system audio** with other applications
2. **Check volume levels** (both system and eSpeak)
3. **Test eSpeak directly**: Open Command Prompt → `espeak "volume test"`
4. **Check default audio device** in Windows Sound settings

### ❌ Problem: Installation Failed / Access Denied

**Solution: Administrator Rights**
✅ **Try This:**
1. **Right-click** the installer
2. Select **"Run as administrator"**
3. If still failing, **temporarily disable antivirus**
4. Try the **Portable Installation method** instead

---

## 🌍 Advanced: Additional Languages & Voices

eSpeak supports many languages and voice variants:

### Popular Language Codes:
- **en** - English (default)
- **es** - Spanish
- **fr** - French  
- **de** - German
- **it** - Italian
- **pt** - Portuguese

### Testing Different Voices:
```cmd
espeak -v en+f3 -s 150 "This is a female voice"
espeak -v en+m4 -s 120 "This is a male voice"
```

💡 **Note:** CarelessWhisper uses its own voice system, but you can experiment with eSpeak voices directly via command line.

---

## 🆘 Getting Help

### ✅ Quick Verification Checklist:
- [ ] eSpeak installer downloaded from official source
- [ ] Installed with "Run as administrator"  
- [ ] "Add to PATH" option was selected during install
- [ ] Command prompt test works: `espeak "test"`
- [ ] CarelessWhisper restarted after eSpeak installation
- [ ] Clipboard has text content before pressing Ctrl+F1

### 📞 Support Resources:

**CarelessWhisper TTS Issues:**
- Check the `docs/TTS_INTEGRATION_GUIDE.md` file
- Review CarelessWhisper GitHub issues

**eSpeak General Issues:**
- Official documentation: https://espeak.sourceforge.net/
- eSpeak GitHub: https://github.com/espeak-ng/espeak-ng

**Windows PATH Issues:**
- Search "Windows environment variables" in Start menu
- Use `where espeak` command to locate installation

---

## ✅ Installation Complete!

🎉 **Congratulations!** You've successfully installed eSpeak for CarelessWhisper TTS.

### What's Next:
1. **Test the feature**: Copy text → Press **Ctrl+F1**
2. **Adjust settings**: Explore voice options in CarelessWhisper settings
3. **Enjoy**: Use TTS to have documents, articles, or any text read aloud

### 🔊 Pro Tips:
- **Long documents**: Copy sections at a time for better performance
- **Speed control**: TTS speed can be adjusted in CarelessWhisper settings
- **Voice selection**: Try different voice profiles to find your preference
- **Language mixing**: eSpeak handles multiple languages in the same text

---

*This guide was created for CarelessWhisper v3.6+ with CarelessKitten TTS integration.*

**Last Updated:** January 2025
