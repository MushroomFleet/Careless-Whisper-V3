#!/usr/bin/env python3
# kitten_tts_bridge.py - Bridge script with comprehensive phonemizer patching

import argparse
import json
import sys
import tempfile
import os
from pathlib import Path
import subprocess
import types

# Monkey patches for dependency issues
try:
    # Set up bundled eSpeak before any imports
    script_dir = Path(__file__).parent.absolute()
    python_dir = script_dir.parent / "python" if script_dir.name == "scripts" else script_dir
    espeak_exe = python_dir / "espeak" / "espeak.exe"
    espeak_data = python_dir / "espeak" / "espeak-data"
    
    if espeak_exe.exists():
        # Set eSpeak executable and data paths
        os.environ['ESPEAK_EXE'] = str(espeak_exe)
        
        # Set data path - eSpeak looks for this
        if espeak_data.exists():
            os.environ['ESPEAK_DATA_PATH'] = str(espeak_data)
        
        # Remove system python paths from sys.path to force bundled packages
        original_paths = sys.path[:]
        filtered_paths = [p for p in sys.path if 'AppData' not in p and 'site-packages' not in p or 'Lib\\site-packages' in p]
        sys.path[:] = filtered_paths
        
        # Add eSpeak directory to PATH so it can be found by subprocess calls
        current_path = os.environ.get('PATH', '')
        path_parts = current_path.split(os.pathsep)
        filtered_path = [p for p in path_parts if 'eSpeak' not in p and 'espeak' not in p]
        os.environ['PATH'] = str(espeak_exe.parent) + os.pathsep + os.pathsep.join(filtered_path)
        
        # Also set phonemizer-specific environment variables
        os.environ['PHONEMIZER_ESPEAK_EXECUTABLE'] = str(espeak_exe)
        if espeak_data.exists():
            os.environ['PHONEMIZER_ESPEAK_PATH'] = str(espeak_data)
    
    # Real num2words is now available in site-packages, no need for custom implementation
    
    # Create comprehensive phonemizer patches
    class PatchedEspeakWrapper:
        data_path = None
        
        @staticmethod
        def set_data_path(path):
            PatchedEspeakWrapper.data_path = path
        
        def __init__(self, *args, **kwargs):
            pass
        
        def phonemize(self, text, *args, **kwargs):
            # Basic phonemization - just return the text
            return text
    
    # Create a fully functional EspeakBackend that bypasses all detection
    class PatchedEspeakBackend:
        def __init__(self, language='en-us', preserve_punctuation=False, with_stress=False, 
                     tie=False, language_switch='keep-flags', words_mismatch='ignore'):
            self.language = language
            self.preserve_punctuation = preserve_punctuation
            self.with_stress = with_stress
            self.tie = tie
            self.language_switch = language_switch
            self.words_mismatch = words_mismatch
            self.espeak_exe = str(espeak_exe) if espeak_exe.exists() else 'espeak'
        
        def phonemize(self, text_list, separator=' ', strip=False, njobs=1, **kwargs):
            """Phonemize using our bundled eSpeak directly to produce real IPA phonemes"""
            if isinstance(text_list, str):
                text_list = [text_list]
            
            results = []
            for text in text_list:
                # Use our bundled eSpeak to get proper IPA phonemes (not just text!)
                cmd = [self.espeak_exe, '-q', '--ipa']
                if espeak_data.exists():
                    cmd.extend(['--path', str(espeak_data)])
                cmd.append(text)
                
                try:
                    result = subprocess.run(cmd, capture_output=True, text=True, timeout=30, encoding='utf-8')
                    if result.returncode == 0 and result.stdout.strip():
                        phonemes = result.stdout.strip()
                        # Clean up the IPA output (remove extra whitespace, newlines)
                        phonemes = ' '.join(phonemes.split())
                        results.append(phonemes)
                    else:
                        # If eSpeak fails, we need to provide some basic phoneme mapping
                        # This is critical - returning plain text breaks KittenTTS neural network
                        basic_phonemes = self._text_to_basic_ipa(text)
                        results.append(basic_phonemes)
                except Exception as e:
                    # Emergency fallback with basic phoneme mapping
                    basic_phonemes = self._text_to_basic_ipa(text)
                    results.append(basic_phonemes)
            
            return results
        
        def _text_to_basic_ipa(self, text):
            """Basic fallback text-to-IPA conversion when eSpeak fails"""
            # Very basic character mapping for emergencies
            # This is not perfect but better than returning plain English
            basic_map = {
                'a': 'æ', 'e': 'ɛ', 'i': 'ɪ', 'o': 'ɔ', 'u': 'ʊ',
                'th': 'θ', 'sh': 'ʃ', 'ch': 'ʧ', 'ng': 'ŋ'
            }
            
            result = text.lower()
            for eng, ipa in basic_map.items():
                result = result.replace(eng, ipa)
            
            return result
        
        @staticmethod
        def is_available():
            return True
        
        @staticmethod  
        def version():
            return "1.48.03"
    
    # Create mock misaki modules
    en_module = types.ModuleType('misaki.en')
    en_module.clean_text = lambda text: text.strip()
    en_module.normalize_text = lambda text: text.lower()
    en_module.process_text = lambda text: text
    
    espeak_module = types.ModuleType('misaki.espeak')
    espeak_module.EspeakWrapper = PatchedEspeakWrapper
    
    misaki_module = types.ModuleType('misaki')
    misaki_module.en = en_module
    misaki_module.espeak = espeak_module
    
    # Install all mock modules before any imports
    sys.modules['misaki'] = misaki_module
    sys.modules['misaki.en'] = en_module
    sys.modules['misaki.espeak'] = espeak_module
    
    # Create comprehensive phonemizer module patches
    phonemizer_backend_module = types.ModuleType('phonemizer.backend')
    phonemizer_backend_module.EspeakBackend = PatchedEspeakBackend
    
    phonemizer_backend_espeak_module = types.ModuleType('phonemizer.backend.espeak')
    phonemizer_backend_espeak_module.EspeakBackend = PatchedEspeakBackend
    
    phonemizer_module = types.ModuleType('phonemizer')
    phonemizer_module.backend = phonemizer_backend_module
    
    # Install phonemizer patches
    sys.modules['phonemizer'] = phonemizer_module
    sys.modules['phonemizer.backend'] = phonemizer_backend_module  
    sys.modules['phonemizer.backend.espeak'] = phonemizer_backend_espeak_module
    sys.modules['phonemizer.backend.EspeakBackend'] = PatchedEspeakBackend
    
    # Now try to import KittenTTS
    from kittentts import KittenTTS
    
except ImportError as e:
    print(json.dumps({
        "success": False, 
        "error": f"KittenTTS setup failed: {str(e)}. Ensure all dependencies are installed."
    }), file=sys.stderr)
    sys.exit(1)
except Exception as e:
    print(json.dumps({
        "success": False, 
        "error": f"Unexpected error during setup: {str(e)}"
    }), file=sys.stderr)
    sys.exit(1)

class CarelessKittenBridge:
    """Bridge between Careless Whisper and KittenTTS with comprehensive patches."""
    
    def __init__(self):
        self.model = None
        self.supported_voices = [
            'expr-voice-2-m', 'expr-voice-2-f', 
            'expr-voice-3-m', 'expr-voice-3-f',
            'expr-voice-4-m', 'expr-voice-4-f',
            'expr-voice-5-m', 'expr-voice-5-f'
        ]
    
    def initialize_model(self):
        """Initialize KittenTTS model."""
        try:
            self.model = KittenTTS("KittenML/kitten-tts-nano-0.1")
            return True
        except Exception as e:
            self._error(f"Failed to initialize KittenTTS: {e}")
            return False
    
    def generate_audio(self, text: str, voice: str, speed: float, output_path: str):
        """Generate TTS audio and save to file."""
        if not self.model:
            if not self.initialize_model():
                return False
        
        try:
            # Validate voice
            if voice not in self.supported_voices:
                self._error(f"Unsupported voice: {voice}. Supported: {', '.join(self.supported_voices)}")
                return False
            
            # Validate speed
            if not 0.5 <= speed <= 2.0:
                self._error(f"Speed must be between 0.5 and 2.0, got: {speed}")
                return False
            
            # Generate audio
            self.model.generate_to_file(
                text=text,
                output_path=output_path,
                voice=voice,
                speed=speed
            )
            
            # Verify output file exists and has content
            if not os.path.exists(output_path):
                self._error(f"Output file not created: {output_path}")
                return False
            
            file_size = os.path.getsize(output_path)
            if file_size == 0:
                self._error(f"Output file is empty: {output_path}")
                return False
            
            self._success({
                "output_path": output_path,
                "file_size": file_size,
                "voice": voice,
                "speed": speed,
                "text_length": len(text)
            })
            return True
            
        except Exception as e:
            self._error(f"TTS generation failed: {e}")
            return False
    
    def list_voices(self):
        """List available voices."""
        voice_descriptions = {
            'expr-voice-2-m': 'Male Voice #2 - Expressive',
            'expr-voice-2-f': 'Female Voice #2 - Expressive', 
            'expr-voice-3-m': 'Male Voice #3 - Expressive',
            'expr-voice-3-f': 'Female Voice #3 - Expressive',
            'expr-voice-4-m': 'Male Voice #4 - Expressive',
            'expr-voice-4-f': 'Female Voice #4 - Expressive',
            'expr-voice-5-m': 'Male Voice #5 - Expressive',
            'expr-voice-5-f': 'Female Voice #5 - Expressive'
        }
        
        voices = [
            {"id": voice_id, "description": desc} 
            for voice_id, desc in voice_descriptions.items()
        ]
        
        self._success({"voices": voices})
    
    def _success(self, data):
        """Output success result."""
        result = {"success": True, **data}
        print(json.dumps(result))
    
    def _error(self, message):
        """Output error result."""
        result = {"success": False, "error": message}
        print(json.dumps(result), file=sys.stderr)

def main():
    parser = argparse.ArgumentParser(description="KittenTTS bridge for Careless Whisper")
    parser.add_argument("--text", required=False, help="Text to convert to speech")
    parser.add_argument("--voice", default="expr-voice-2-f", help="Voice to use")
    parser.add_argument("--speed", type=float, default=1.0, help="Speech speed (0.5-2.0)")
    parser.add_argument("--output", required=False, help="Output audio file path")
    parser.add_argument("--list-voices", action="store_true", help="List available voices")
    
    args = parser.parse_args()
    
    bridge = CarelessKittenBridge()
    
    if args.list_voices:
        bridge.list_voices()
        return
    
    if not args.text or not args.output:
        bridge._error("Both --text and --output are required for TTS generation")
        sys.exit(1)
    
    success = bridge.generate_audio(args.text, args.voice, args.speed, args.output)
    sys.exit(0 if success else 1)

if __name__ == "__main__":
    main()
