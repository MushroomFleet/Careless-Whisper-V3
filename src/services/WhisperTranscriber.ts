import { pipeline } from '@huggingface/transformers';
import type { 
  WhisperModelSize, 
  WhisperTranscribeOptions, 
  WhisperTranscribeResult, 
  WhisperModelInfo
} from '@/types';

export class WhisperTranscriber {
  private whisper: any = null;
  private isLoaded = false;
  private currentModelSize: WhisperModelSize;
  private gpuMemoryUsage = 0;

  constructor(modelSize: WhisperModelSize = 'base', _options: Partial<WhisperTranscribeOptions> = {}) {
    this.currentModelSize = modelSize;
    this.gpuMemoryUsage = this.getMemoryRequirements(modelSize);
  }

  /**
   * Initialize the Whisper model using Transformers.js
   */
  async initialize(): Promise<void> {
    if (this.isLoaded) {
      console.log('Whisper model already loaded, skipping initialization');
      return;
    }

    try {
      console.log(`[WHISPER DEBUG] Starting initialization of ${this.currentModelSize} model...`);
      
      // Create speech recognition pipeline with Xenova/whisper-tiny.en
      // Note: Currently only supporting tiny model for optimal performance
      const modelName = this.getModelName(this.currentModelSize);
      console.log(`[WHISPER DEBUG] Using model: ${modelName}`);
      
      console.log(`[WHISPER DEBUG] Creating pipeline...`);
      this.whisper = await pipeline('automatic-speech-recognition', modelName);
      this.isLoaded = true;
      
      console.log(`[WHISPER DEBUG] Pipeline created successfully`);
      console.log(`[WHISPER DEBUG] Model type:`, typeof this.whisper);
      console.log(`[WHISPER DEBUG] Whisper ${this.currentModelSize} model loaded successfully`);
    } catch (error) {
      console.error('[WHISPER ERROR] Failed to initialize Whisper:', error);
      console.error('[WHISPER ERROR] Error details:', {
        name: error instanceof Error ? error.name : 'Unknown',
        message: error instanceof Error ? error.message : String(error),
        stack: error instanceof Error ? error.stack : 'No stack trace'
      });
      throw new Error(`Failed to initialize Whisper: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Get the HuggingFace model name for the specified size
   */
  private getModelName(size: WhisperModelSize): string {
    // Currently, Transformers.js primarily supports the tiny model
    // We can map different sizes to the same model for now
    const modelMap: Record<WhisperModelSize, string> = {
      tiny: 'Xenova/whisper-tiny.en',
      base: 'Xenova/whisper-tiny.en', // Fallback to tiny for now
      small: 'Xenova/whisper-tiny.en', // Fallback to tiny for now
      medium: 'Xenova/whisper-tiny.en', // Fallback to tiny for now
      large: 'Xenova/whisper-tiny.en' // Fallback to tiny for now
    };
    return modelMap[size] || 'Xenova/whisper-tiny.en';
  }

  /**
   * Transcribe audio buffer to text using Transformers.js
   */
  async transcribe(
    audioBuffer: ArrayBuffer, 
    options: WhisperTranscribeOptions = {}
  ): Promise<WhisperTranscribeResult> {
    console.log('[WHISPER DEBUG] transcribe() called');
    console.log('[WHISPER DEBUG] Audio buffer size:', audioBuffer.byteLength, 'bytes');
    console.log('[WHISPER DEBUG] Options:', options);
    console.log('[WHISPER DEBUG] Is loaded:', this.isLoaded);

    if (!this.isLoaded) {
      console.log('[WHISPER DEBUG] Model not loaded, initializing...');
      await this.initialize();
    }

    try {
      console.log('[WHISPER DEBUG] Starting transcription...');
      const startTime = performance.now();

      // Validate audio buffer
      console.log('[WHISPER DEBUG] Validating audio buffer...');
      this.validateAudioBuffer(audioBuffer);
      console.log('[WHISPER DEBUG] Audio buffer validation passed');

      // Convert ArrayBuffer to Blob for Transformers.js
      console.log('[WHISPER DEBUG] Converting audio buffer to blob...');
      const audioBlob = await this.convertAudioBufferToBlob(audioBuffer);
      console.log('[WHISPER DEBUG] Audio blob created:', {
        size: audioBlob.size,
        type: audioBlob.type
      });
      
      const audioUrl = URL.createObjectURL(audioBlob);
      console.log('[WHISPER DEBUG] Audio URL created:', audioUrl);
      
      try {
        // Prepare Transformers.js options
        const transformersOptions: any = {};
        
        // Handle timestamp options
        if (options.word_timestamps) {
          transformersOptions.return_timestamps = 'word';
        } else {
          transformersOptions.return_timestamps = true; // Segment-level timestamps
        }
        
        console.log('[WHISPER DEBUG] Transformers options:', transformersOptions);
        console.log('[WHISPER DEBUG] Whisper pipeline type:', typeof this.whisper);
        console.log('[WHISPER DEBUG] Calling whisper pipeline...');

        // Process transcription using Transformers.js pipeline
        const result = await this.whisper(audioUrl, transformersOptions);
        
        console.log('[WHISPER DEBUG] Pipeline returned result:', result);
        console.log('[WHISPER DEBUG] Result type:', typeof result);
        
        const endTime = performance.now();
        const duration = endTime - startTime;
        
        console.log(`[WHISPER DEBUG] Transcription completed in ${duration.toFixed(2)}ms`);
        
        console.log('[WHISPER DEBUG] Formatting result...');
        const formattedResult = this.formatResult(result);
        console.log('[WHISPER DEBUG] Formatted result:', formattedResult);
        
        return formattedResult;
      } finally {
        // Clean up blob URL
        console.log('[WHISPER DEBUG] Cleaning up blob URL');
        URL.revokeObjectURL(audioUrl);
      }
    } catch (error) {
      console.error('[WHISPER ERROR] Transcription failed:', error);
      console.error('[WHISPER ERROR] Error details:', {
        name: error instanceof Error ? error.name : 'Unknown',
        message: error instanceof Error ? error.message : String(error),
        stack: error instanceof Error ? error.stack : 'No stack trace'
      });
      throw new Error(`Transcription failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Convert ArrayBuffer to audio Blob for Transformers.js
   */
  private async convertAudioBufferToBlob(audioBuffer: ArrayBuffer): Promise<Blob> {
    console.log('[WHISPER DEBUG] convertAudioBufferToBlob() called');
    console.log('[WHISPER DEBUG] Input buffer size:', audioBuffer.byteLength);
    
    try {
      console.log('[WHISPER DEBUG] Attempting to decode audio data...');
      // The audioBuffer contains encoded audio (WebM/Opus), not raw PCM
      // We need to decode it using Web Audio API first
      const decodedAudio = await this.decodeAudioData(audioBuffer);
      console.log('[WHISPER DEBUG] Audio decoded successfully:', {
        sampleRate: decodedAudio.sampleRate,
        numberOfChannels: decodedAudio.numberOfChannels,
        length: decodedAudio.length,
        duration: decodedAudio.duration
      });
      
      // Convert the decoded audio to WAV format
      console.log('[WHISPER DEBUG] Creating WAV from audio buffer...');
      const wavBlob = this.createWavFromAudioBuffer(decodedAudio);
      console.log('[WHISPER DEBUG] WAV blob created:', {
        size: wavBlob.size,
        type: wavBlob.type
      });
      return wavBlob;
    } catch (error) {
      console.error('[WHISPER ERROR] Failed to convert audio buffer:', error);
      console.error('[WHISPER ERROR] Error details:', {
        name: error instanceof Error ? error.name : 'Unknown',
        message: error instanceof Error ? error.message : String(error)
      });
      
      // Fallback: try to use the original blob directly
      // Transformers.js might be able to handle the encoded format
      console.log('[WHISPER DEBUG] Using fallback: original blob with WebM type');
      return new Blob([audioBuffer], { type: 'audio/webm' });
    }
  }

  /**
   * Decode audio data using Web Audio API
   */
  private async decodeAudioData(audioBuffer: ArrayBuffer): Promise<AudioBuffer> {
    console.log('[WHISPER DEBUG] decodeAudioData() called');
    
    // Create temporary audio context for decoding
    const audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
    console.log('[WHISPER DEBUG] AudioContext created, sample rate:', audioContext.sampleRate);
    
    try {
      console.log('[WHISPER DEBUG] Calling decodeAudioData...');
      const decodedBuffer = await audioContext.decodeAudioData(audioBuffer.slice(0));
      console.log('[WHISPER DEBUG] Audio decoded successfully');
      return decodedBuffer;
    } catch (error) {
      console.error('[WHISPER ERROR] Failed to decode audio data:', error);
      throw error;
    } finally {
      // Clean up audio context
      console.log('[WHISPER DEBUG] Closing audio context');
      await audioContext.close();
    }
  }

  /**
   * Create WAV blob from decoded AudioBuffer
   */
  private createWavFromAudioBuffer(audioBuffer: AudioBuffer): Blob {
    const sampleRate = 16000; // Whisper expects 16kHz
    const channels = 1; // Mono
    const bitsPerSample = 16;
    
    // Resample to 16kHz mono if needed
    const resampledData = this.resampleAndConvertToMono(audioBuffer, sampleRate);
    
    // Convert float32 samples to int16
    const int16Data = new Int16Array(resampledData.length);
    for (let i = 0; i < resampledData.length; i++) {
      // Clamp to [-1, 1] and convert to 16-bit integer
      const sample = Math.max(-1, Math.min(1, resampledData[i]));
      int16Data[i] = sample * 0x7FFF;
    }
    
    // Create WAV file
    const dataLength = int16Data.length * 2; // 2 bytes per sample
    const buffer = new ArrayBuffer(44 + dataLength);
    const view = new DataView(buffer);
    
    // WAV header
    const writeString = (offset: number, string: string) => {
      for (let i = 0; i < string.length; i++) {
        view.setUint8(offset + i, string.charCodeAt(i));
      }
    };
    
    writeString(0, 'RIFF');
    view.setUint32(4, 36 + dataLength, true);
    writeString(8, 'WAVE');
    writeString(12, 'fmt ');
    view.setUint32(16, 16, true);
    view.setUint16(20, 1, true);
    view.setUint16(22, channels, true);
    view.setUint32(24, sampleRate, true);
    view.setUint32(28, sampleRate * channels * (bitsPerSample / 8), true);
    view.setUint16(32, channels * (bitsPerSample / 8), true);
    view.setUint16(34, bitsPerSample, true);
    writeString(36, 'data');
    view.setUint32(40, dataLength, true);
    
    // Copy audio data
    const output = new Uint8Array(buffer, 44);
    const int16View = new Uint8Array(int16Data.buffer);
    output.set(int16View);
    
    return new Blob([buffer], { type: 'audio/wav' });
  }

  /**
   * Resample audio to target sample rate and convert to mono
   */
  private resampleAndConvertToMono(audioBuffer: AudioBuffer, targetSampleRate: number): Float32Array {
    const originalSampleRate = audioBuffer.sampleRate;
    const channels = audioBuffer.numberOfChannels;
    const originalLength = audioBuffer.length;
    
    // Calculate new length after resampling
    const resampleRatio = targetSampleRate / originalSampleRate;
    const newLength = Math.round(originalLength * resampleRatio);
    
    // Get audio data (convert to mono if needed)
    let audioData: Float32Array;
    if (channels === 1) {
      audioData = audioBuffer.getChannelData(0);
    } else {
      // Mix down to mono by averaging channels
      audioData = new Float32Array(originalLength);
      for (let i = 0; i < originalLength; i++) {
        let sum = 0;
        for (let channel = 0; channel < channels; channel++) {
          sum += audioBuffer.getChannelData(channel)[i];
        }
        audioData[i] = sum / channels;
      }
    }
    
    // Resample if needed
    if (Math.abs(resampleRatio - 1) < 0.001) {
      // No resampling needed
      return audioData;
    }
    
    // Simple linear interpolation resampling
    const resampledData = new Float32Array(newLength);
    for (let i = 0; i < newLength; i++) {
      const originalIndex = i / resampleRatio;
      const index = Math.floor(originalIndex);
      const fraction = originalIndex - index;
      
      if (index >= originalLength - 1) {
        resampledData[i] = audioData[originalLength - 1];
      } else {
        // Linear interpolation
        resampledData[i] = audioData[index] * (1 - fraction) + audioData[index + 1] * fraction;
      }
    }
    
    return resampledData;
  }

  /**
   * Change the model size (requires reloading)
   */
  async setModelSize(size: WhisperModelSize): Promise<void> {
    if (this.currentModelSize === size) {
      return;
    }

    console.log(`Switching from ${this.currentModelSize} to ${size} model...`);
    
    // Unload current model
    this.unload();
    
    // Update size and memory usage
    this.currentModelSize = size;
    this.gpuMemoryUsage = this.getMemoryRequirements(size);
    
    // Reinitialize with new model
    await this.initialize();
  }

  /**
   * Get memory requirements for a model size (ONNX quantized models)
   */
  getMemoryRequirements(size: WhisperModelSize): number {
    // Updated requirements for ONNX quantized models from Transformers.js
    const requirements: Record<WhisperModelSize, number> = {
      tiny: 39,     // MB - Xenova/whisper-tiny.en ONNX model
      base: 39,     // MB - Currently using tiny model as fallback
      small: 39,    // MB - Currently using tiny model as fallback  
      medium: 39,   // MB - Currently using tiny model as fallback
      large: 39     // MB - Currently using tiny model as fallback
    };
    return requirements[size] || requirements.tiny;
  }

  /**
   * Get available models with their specifications
   */
  getAvailableModels(): WhisperModelInfo[] {
    return [
      { size: 'tiny', memoryMB: 39, description: 'ONNX tiny model - fast, good quality', downloadSize: '39MB' },
      { size: 'base', memoryMB: 39, description: 'Uses tiny model (fallback)', downloadSize: '39MB' },
      { size: 'small', memoryMB: 39, description: 'Uses tiny model (fallback)', downloadSize: '39MB' },
    ];
  }

  /**
   * Check if model is loaded
   */
  get loaded(): boolean {
    return this.isLoaded;
  }

  /**
   * Get current model size
   */
  get modelSize(): WhisperModelSize {
    return this.currentModelSize;
  }

  /**
   * Get current memory usage
   */
  get memoryUsage(): number {
    return this.isLoaded ? this.gpuMemoryUsage : 0;
  }

  /**
   * Unload the model to free memory
   */
  unload(): void {
    if (this.isLoaded) {
      console.log(`Unloading Whisper ${this.currentModelSize} model...`);
      this.whisper = null;
      this.isLoaded = false;
      this.gpuMemoryUsage = 0;
    }
  }

  /**
   * Format the Transformers.js result to our interface
   */
  private formatResult(rawResult: any): WhisperTranscribeResult {
    // Handle Transformers.js output format
    if (typeof rawResult === 'string') {
      return { text: rawResult };
    }

    if (rawResult && typeof rawResult === 'object') {
      const result: WhisperTranscribeResult = {
        text: rawResult.text || '',
        language: 'en' // Transformers.js whisper-tiny.en is English-only
      };

      // Handle chunks from Transformers.js (both segment and word-level timestamps)
      if (rawResult.chunks && Array.isArray(rawResult.chunks)) {
        // Check if chunks contain word-level or segment-level timestamps
        const isWordLevel = rawResult.chunks.some((chunk: any) => 
          chunk.text && chunk.text.trim().split(' ').length === 1
        );

        if (isWordLevel) {
          // Word-level timestamps - group into segments
          result.segments = this.groupWordsIntoSegments(rawResult.chunks);
        } else {
          // Segment-level timestamps
          result.segments = rawResult.chunks.map((chunk: any) => ({
            start: chunk.timestamp?.[0] || 0,
            end: chunk.timestamp?.[1] || 0,
            text: chunk.text || '',
            words: undefined // No word-level data in segment mode
          }));
        }
      }

      return result;
    }

    throw new Error('Invalid result format from Transformers.js Whisper');
  }

  /**
   * Group word-level chunks into segments for better organization
   */
  private groupWordsIntoSegments(words: any[]): Array<{
    start: number;
    end: number;
    text: string;
    words: Array<{
      start: number;
      end: number;
      word: string;
      probability: number;
    }>;
  }> {
    if (!words || words.length === 0) {
      return [];
    }

    const segments = [];
    const segmentSize = 10; // Group every ~10 words into a segment
    
    for (let i = 0; i < words.length; i += segmentSize) {
      const segmentWords = words.slice(i, i + segmentSize);
      const start = segmentWords[0]?.timestamp?.[0] || 0;
      const end = segmentWords[segmentWords.length - 1]?.timestamp?.[1] || 0;
      const text = segmentWords.map(w => w.text || '').join('');
      
      const wordsData = segmentWords.map(word => ({
        start: word.timestamp?.[0] || 0,
        end: word.timestamp?.[1] || 0,
        word: word.text || '',
        probability: 0.95 // Transformers.js doesn't provide confidence, use default
      }));

      segments.push({
        start,
        end,
        text,
        words: wordsData
      });
    }

    return segments;
  }

  /**
   * Validate audio buffer format
   */
  private validateAudioBuffer(buffer: ArrayBuffer): void {
    if (!buffer || buffer.byteLength === 0) {
      throw new Error('Invalid audio buffer: empty or null');
    }

    // Basic size validation (minimum ~1 second of audio at 16kHz)
    const minSize = 16000 * 2; // 16kHz * 2 bytes per sample
    if (buffer.byteLength < minSize) {
      console.warn('Audio buffer may be too short for reliable transcription');
    }

    // Maximum size validation (prevent memory issues)
    const maxSize = 100 * 1024 * 1024; // 100MB
    if (buffer.byteLength > maxSize) {
      throw new Error('Audio buffer too large (>100MB)');
    }
  }

  /**
   * Get transcription performance stats
   */
  getPerformanceStats(): {
    modelSize: WhisperModelSize;
    memoryUsage: number;
    estimatedSpeed: string;
    quality: string;
  } {
    const stats = {
      tiny: { speed: '600-1000 wpm', quality: 'Basic' },
      base: { speed: '400-600 wpm', quality: 'Good' },
      small: { speed: '200-400 wpm', quality: 'Better' },
      medium: { speed: '100-200 wpm', quality: 'Very Good' },
      large: { speed: '50-100 wpm', quality: 'Excellent' }
    };

    return {
      modelSize: this.currentModelSize,
      memoryUsage: this.memoryUsage,
      estimatedSpeed: stats[this.currentModelSize]?.speed || 'Unknown',
      quality: stats[this.currentModelSize]?.quality || 'Unknown'
    };
  }

}

// Default export for convenience
export default WhisperTranscriber;

// Factory function for easier instantiation
export function createWhisperTranscriber(
  modelSize: WhisperModelSize = 'base',
  options?: Partial<WhisperTranscribeOptions>
): WhisperTranscriber {
  return new WhisperTranscriber(modelSize, options);
}
