import { KokoroTTS as KokoroTTSEngine } from 'kokoro-js';
import type { 
  TTSVoice, 
  TTSGenerateOptions, 
  TTSResult, 
  TTSConfig,
  AudioChunk,
  StreamingCallback 
} from '@/types';

export class KokoroTTS {
  private model: any = null;
  private voices: Record<string, TTSVoice> = {};
  private isLoaded = false;
  private quantization: string;
  private memoryUsage = 0;
  private config: TTSConfig;
  private currentStream: AsyncGenerator<any> | null = null;
  private isStreaming = false;
  private progressCallback?: (stage: string, progress: number) => void;

  constructor(options: { quantization?: string; config?: Partial<TTSConfig> } = {}) {
    this.quantization = options.quantization || 'fp16'; // Faster than q8, uses ~160MB vs ~80MB
    this.config = {
      speed: 1.0,
      pitch: 1.0,
      volume: 1.0,
      ...options.config
    };
  }

  /**
   * Initialize the KokoroTTS model
   */
  async initialize(): Promise<void> {
    if (this.isLoaded) {
      return;
    }

    try {
      console.log('Initializing KokoroTTS model...');
      
      // Load the kokoro-js model
      this.model = await this.loadKokoroModel();
      await this.loadVoices();
      this.isLoaded = true;
      this.memoryUsage = this.quantization === 'fp16' ? 160 : 80; // FP16: ~160MB, Q8: ~80MB
      
      console.log('KokoroTTS model loaded successfully');
    } catch (error) {
      console.error('Failed to initialize KokoroTTS:', error);
      throw new Error(`Failed to initialize KokoroTTS: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Load the Kokoro model using kokoro-js
   */
  private async loadKokoroModel(): Promise<any> {
    try {
      console.log('Loading KokoroTTS model from onnx-community/Kokoro-82M-v1.0-ONNX...');
      console.log('Quantization level:', this.quantization);
      
      // Add timeout to prevent hanging
      const timeoutPromise = new Promise((_, reject) => {
        setTimeout(() => reject(new Error('Model loading timeout after 60 seconds')), 60000);
      });
      
      // Load the updated model with specified quantization
      const modelPromise = KokoroTTSEngine.from_pretrained(
        "onnx-community/Kokoro-82M-v1.0-ONNX",
        { 
          dtype: this.quantization as any, // fp32, fp16, q8, q4, q4f16
          device: "wasm", // Force WASM for better compatibility
          progress_callback: (progress: any) => {
            console.log('Model loading progress:', progress);
          }
        }
      );
      
      console.log('Waiting for model to load...');
      const model = await Promise.race([modelPromise, timeoutPromise]);
      
      console.log('KokoroTTS model loaded successfully');
      console.log('Model type:', typeof model);
      console.log('Model methods:', Object.getOwnPropertyNames(model));
      
      return model;
    } catch (error) {
      console.error('Failed to load KokoroTTS model:', error);
      console.error('Error type:', typeof error);
      console.error('Error stack:', error instanceof Error ? error.stack : 'No stack trace');
      throw new Error(`Failed to load KokoroTTS model: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Load available voices from the model
   */
  private async loadVoices(): Promise<void> {
    try {
      // Get available voices from the model
      const availableVoices = this.model.list_voices();
      console.log('Raw voices data:', availableVoices);
      console.table(availableVoices);
      
      // Handle both object and array formats
      this.voices = {};
      
      if (Array.isArray(availableVoices)) {
        // Array format - iterate over voice IDs
        for (const voiceId of availableVoices) {
          this.voices[voiceId] = this.mapVoiceIdToInfo(voiceId);
        }
      } else if (typeof availableVoices === 'object' && availableVoices !== null) {
        // Object format - extract voice data from the object
        for (const [voiceId, voiceData] of Object.entries(availableVoices)) {
          this.voices[voiceId] = this.mapVoiceDataToInfo(voiceId, voiceData as any);
        }
      } else {
        throw new Error('Unexpected voice data format');
      }
      
      console.log(`Loaded ${Object.keys(this.voices).length} voices:`, Object.keys(this.voices));
      console.log('Voice details:', this.voices);
    } catch (error) {
      console.warn('Failed to load voices from model, using enhanced fallback voices:', error);
      // Enhanced fallback with all 28 voices from the console output
      this.voices = this.getEnhancedFallbackVoices();
    }
  }

  /**
   * Map voice ID to voice information
   */
  private mapVoiceIdToInfo(voiceId: string): TTSVoice {
    // Parse voice ID to extract information (e.g., "af_bella" -> African Female Bella)
    const parts = voiceId.split('_');
    const prefix = parts[0];
    const name = parts[1] || 'Unknown';
    
    // Map prefixes to gender and accent
    const genderMap: Record<string, 'male' | 'female'> = {
      'af': 'female',
      'am': 'male',
      'bf': 'female',
      'bm': 'male'
    };
    
    const accentMap: Record<string, string> = {
      'af': 'South African',
      'am': 'American',
      'bf': 'British',
      'bm': 'British'
    };
    
    const gender = genderMap[prefix] || 'female';
    const accent = accentMap[prefix] || 'Unknown';
    
    return {
      id: voiceId,
      description: `${name.charAt(0).toUpperCase() + name.slice(1)} - ${accent} English`,
      gender,
      accent
    };
  }

  /**
   * Map voice data from object format to voice information
   */
  private mapVoiceDataToInfo(voiceId: string, voiceData: any): TTSVoice {
    // Extract information from the voice data object
    const name = voiceData.name || voiceId.split('_')[1] || 'Unknown';
    const language = voiceData.language || 'en-us';
    const gender = voiceData.gender ? voiceData.gender.toLowerCase() as 'male' | 'female' : 'female';
    const traits = voiceData.traits || '';
    const quality = voiceData.overallGrade || voiceData.targetQuality || 'B';
    
    // Map language codes to accent descriptions
    const accentMap: Record<string, string> = {
      'en-us': 'American',
      'en-gb': 'British',
      'en-au': 'Australian',
      'en-ca': 'Canadian',
      'en-za': 'South African'
    };
    
    const accent = accentMap[language] || 'American';
    
    // Create description with quality and traits
    let description = `${name} - ${accent} English`;
    if (quality && quality !== 'B') {
      description += ` (${quality} Quality)`;
    }
    if (traits) {
      description += ` ${traits}`;
    }
    
    return {
      id: voiceId,
      description,
      gender,
      accent
    };
  }

  /**
   * Get enhanced fallback voices with all 28 voices from console output
   */
  private getEnhancedFallbackVoices(): Record<string, TTSVoice> {
    return {
      // American Female Voices
      'af_heart': {
        id: 'af_heart',
        description: 'Heart - American English (A Quality) ❤️',
        gender: 'female',
        accent: 'American'
      },
      'af_alloy': {
        id: 'af_alloy',
        description: 'Alloy - American English (B Quality)',
        gender: 'female',
        accent: 'American'
      },
      'af_aoede': {
        id: 'af_aoede',
        description: 'Aoede - American English (B Quality)',
        gender: 'female',
        accent: 'American'
      },
      'af_bella': {
        id: 'af_bella',
        description: 'Bella - American English (A Quality) 🔥',
        gender: 'female',
        accent: 'American'
      },
      'af_jessica': {
        id: 'af_jessica',
        description: 'Jessica - American English (C Quality)',
        gender: 'female',
        accent: 'American'
      },
      'af_kore': {
        id: 'af_kore',
        description: 'Kore - American English (B Quality)',
        gender: 'female',
        accent: 'American'
      },
      'af_nicole': {
        id: 'af_nicole',
        description: 'Nicole - American English (B Quality) 🎧',
        gender: 'female',
        accent: 'American'
      },
      'af_nova': {
        id: 'af_nova',
        description: 'Nova - American English (B Quality)',
        gender: 'female',
        accent: 'American'
      },
      'af_river': {
        id: 'af_river',
        description: 'River - American English (C Quality)',
        gender: 'female',
        accent: 'American'
      },
      'af_sarah': {
        id: 'af_sarah',
        description: 'Sarah - American English (B Quality)',
        gender: 'female',
        accent: 'American'
      },
      'af_sky': {
        id: 'af_sky',
        description: 'Sky - American English (B Quality)',
        gender: 'female',
        accent: 'American'
      },
      
      // American Male Voices
      'am_adam': {
        id: 'am_adam',
        description: 'Adam - American English (D Quality)',
        gender: 'male',
        accent: 'American'
      },
      'am_echo': {
        id: 'am_echo',
        description: 'Echo - American English (C Quality)',
        gender: 'male',
        accent: 'American'
      },
      'am_eric': {
        id: 'am_eric',
        description: 'Eric - American English (C Quality)',
        gender: 'male',
        accent: 'American'
      },
      'am_fenrir': {
        id: 'am_fenrir',
        description: 'Fenrir - American English (B Quality)',
        gender: 'male',
        accent: 'American'
      },
      'am_liam': {
        id: 'am_liam',
        description: 'Liam - American English (C Quality)',
        gender: 'male',
        accent: 'American'
      },
      'am_michael': {
        id: 'am_michael',
        description: 'Michael - American English (B Quality)',
        gender: 'male',
        accent: 'American'
      },
      'am_onyx': {
        id: 'am_onyx',
        description: 'Onyx - American English (C Quality)',
        gender: 'male',
        accent: 'American'
      },
      'am_puck': {
        id: 'am_puck',
        description: 'Puck - American English (B Quality)',
        gender: 'male',
        accent: 'American'
      },
      'am_santa': {
        id: 'am_santa',
        description: 'Santa - American English (C Quality)',
        gender: 'male',
        accent: 'American'
      },
      
      // British Female Voices
      'bf_emma': {
        id: 'bf_emma',
        description: 'Emma - British English (B Quality) 🚺',
        gender: 'female',
        accent: 'British'
      },
      'bf_isabella': {
        id: 'bf_isabella',
        description: 'Isabella - British English (B Quality)',
        gender: 'female',
        accent: 'British'
      },
      'bf_alice': {
        id: 'bf_alice',
        description: 'Alice - British English (C Quality) 🚺',
        gender: 'female',
        accent: 'British'
      },
      'bf_lily': {
        id: 'bf_lily',
        description: 'Lily - British English (C Quality) 🚺',
        gender: 'female',
        accent: 'British'
      },
      
      // British Male Voices
      'bm_george': {
        id: 'bm_george',
        description: 'George - British English (B Quality)',
        gender: 'male',
        accent: 'British'
      },
      'bm_lewis': {
        id: 'bm_lewis',
        description: 'Lewis - British English (C Quality)',
        gender: 'male',
        accent: 'British'
      },
      'bm_daniel': {
        id: 'bm_daniel',
        description: 'Daniel - British English (C Quality) 🚹',
        gender: 'male',
        accent: 'British'
      },
      'bm_fable': {
        id: 'bm_fable',
        description: 'Fable - British English (B Quality) 🚹',
        gender: 'male',
        accent: 'British'
      }
    };
  }

  /**
   * Generate speech from text using kokoro-js (non-streaming)
   */
  async generateSpeech(
    text: string, 
    voiceId = 'af_bella', 
    _options: TTSGenerateOptions = {}
  ): Promise<TTSResult> {
    if (!this.isLoaded) {
      this.reportProgress('Initializing TTS model...', 0);
      await this.initialize();
    }

    try {
      console.log(`Generating speech: "${text.substring(0, 50)}..." with voice ${voiceId}`);
      const startTime = performance.now();

      // Validate voice
      if (!this.voices[voiceId]) {
        throw new Error(`Voice ${voiceId} not available`);
      }

      this.reportProgress('Preparing voice synthesis...', 10);

      // Prepare options for kokoro-js (only voice is supported in the current API)
      const generateOptions = {
        voice: voiceId
      };

      this.reportProgress('Generating speech...', 25);

      // Generate audio using the kokoro-js model
      const audioResult = await this.model.generate(text, generateOptions);
      
      this.reportProgress('Processing audio...', 75);
      
      const endTime = performance.now();
      const duration = endTime - startTime;
      
      console.log(`Speech generated in ${duration.toFixed(2)}ms`);
      
      // Convert the kokoro-js audio result to our format
      const result = await this.formatAudioResult(audioResult, duration);
      
      this.reportProgress('Speech generation complete', 100);
      
      return result;
    } catch (error) {
      console.error('Speech generation failed:', error);
      this.reportProgress('Speech generation failed', 0);
      throw new Error(`Speech generation failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Generate speech with streaming using the new kokoro-js streaming API
   */
  async streamSpeech(
    text: string,
    voiceId = 'af_bella',
    callback: StreamingCallback
  ): Promise<void> {
    if (!this.isLoaded) {
      await this.initialize();
    }

    if (this.isStreaming) {
      throw new Error('Already streaming audio');
    }

    // Add timeout protection to prevent hanging
    const STREAMING_TIMEOUT = 300000; // 5 minutes max (increased for better performance)
    const timeoutPromise = new Promise<never>((_, reject) => {
      setTimeout(() => {
        reject(new Error('Streaming timeout - falling back to non-streaming mode'));
      }, STREAMING_TIMEOUT);
    });

    try {
      console.log(`Starting streaming speech generation: "${text.substring(0, 50)}..." with voice ${voiceId}`);
      
      // Validate voice
      if (!this.voices[voiceId]) {
        throw new Error(`Voice ${voiceId} not available`);
      }

      this.isStreaming = true;
      
      // Create stream using the new kokoro-js streaming API with voice parameter with timeout protection
      const streamPromise = this.processStreamWithTimeout(text, voiceId, callback);
      
      // Race between streaming and timeout
      await Promise.race([streamPromise, timeoutPromise]);

    } catch (error) {
      console.error('Streaming speech generation failed:', error);
      
      // If streaming fails, try fallback to regular generation
      if (error instanceof Error && error.message.includes('timeout')) {
        console.log('Attempting fallback to non-streaming mode...');
        await this.fallbackToNonStreaming(text, voiceId, callback);
        return;
      }
      
      // Send error chunk
      const errorChunk: AudioChunk = {
        text: '',
        phonemes: [],
        audio: new ArrayBuffer(0),
        isComplete: true,
        chunkIndex: -1
      };
      
      await callback(errorChunk);
      
      throw new Error(`Streaming speech generation failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
    } finally {
      this.isStreaming = false;
      this.currentStream = null;
    }
  }

  /**
   * Process stream with timeout protection
   */
  private async processStreamWithTimeout(
    text: string,
    voiceId: string,
    callback: StreamingCallback
  ): Promise<void> {
    // Create stream using the new kokoro-js streaming API with voice parameter
    this.currentStream = this.model.stream(text, { voice: voiceId });
      
    let chunkIndex = 0;
    const startTime = performance.now();

    // Process streaming chunks
    for await (const streamChunk of this.currentStream!) {
      console.log(`Streaming chunk ${chunkIndex}:`, {
        text: streamChunk.text,
        phonemes: streamChunk.phonemes?.length || 0,
        audioType: typeof streamChunk.audio,
        audioConstructor: streamChunk.audio?.constructor?.name,
        audioKeys: streamChunk.audio ? Object.keys(streamChunk.audio) : [],
        fullChunk: streamChunk
      });
      
      const chunkText = streamChunk.text || '';
      const phonemes = streamChunk.phonemes || [];
      
      // Convert audio to ArrayBuffer with comprehensive debugging
      let audioBuffer: ArrayBuffer;
      
      if (streamChunk.audio) {
        console.log('Audio object analysis:', {
          type: typeof streamChunk.audio,
          constructor: streamChunk.audio.constructor?.name,
          keys: Object.keys(streamChunk.audio),
          isArrayBuffer: streamChunk.audio instanceof ArrayBuffer,
          isFloat32Array: streamChunk.audio instanceof Float32Array,
          isInt16Array: streamChunk.audio instanceof Int16Array,
          isUint8Array: streamChunk.audio instanceof Uint8Array,
          hasData: 'data' in streamChunk.audio,
          hasBuffer: 'buffer' in streamChunk.audio,
          hasSave: typeof streamChunk.audio.save === 'function',
          hasToArrayBuffer: typeof streamChunk.audio.toArrayBuffer === 'function'
        });
        
        try {
          // Try multiple extraction methods
          if (streamChunk.audio instanceof ArrayBuffer) {
            console.log('Audio is ArrayBuffer');
            audioBuffer = streamChunk.audio;
          } else if (streamChunk.audio.constructor?.name === 'RawAudio') {
            console.log('Audio is RawAudio object, extracting Float32Array');
            const float32Data = streamChunk.audio.audio; // Extract Float32Array from RawAudio
            const sampleRate = streamChunk.audio.sampling_rate || 24000;
            
            console.log(`RawAudio data: ${float32Data.length} samples at ${sampleRate}Hz`);
            console.log('First 10 samples:', Array.from(float32Data.slice(0, 10)));
            
            // Convert Float32 to Int16 PCM asynchronously to prevent blocking
            audioBuffer = await this.convertFloat32ToInt16Async(float32Data);
            
            console.log('Converted to Int16 successfully, buffer size:', audioBuffer.byteLength);
          } else if (streamChunk.audio instanceof Float32Array) {
            console.log('Audio is Float32Array, converting to 16-bit PCM');
            const float32Data = streamChunk.audio;
            const int16Data = new Int16Array(float32Data.length);
            for (let i = 0; i < float32Data.length; i++) {
              const clampedValue = Math.max(-1, Math.min(1, float32Data[i]));
              int16Data[i] = Math.round(clampedValue * 32767);
            }
            audioBuffer = int16Data.buffer;
          } else if (streamChunk.audio instanceof Int16Array) {
            console.log('Audio is Int16Array');
            audioBuffer = streamChunk.audio.buffer;
          } else if (streamChunk.audio instanceof Uint8Array) {
            console.log('Audio is Uint8Array');
            audioBuffer = streamChunk.audio.buffer;
          } else if (typeof streamChunk.audio.toArrayBuffer === 'function') {
            console.log('Using toArrayBuffer method');
            audioBuffer = await streamChunk.audio.toArrayBuffer();
          } else if (streamChunk.audio.data) {
            console.log('Extracting from data property');
            if (streamChunk.audio.data instanceof ArrayBuffer) {
              audioBuffer = streamChunk.audio.data;
            } else if (streamChunk.audio.data instanceof Float32Array) {
              const float32Data = streamChunk.audio.data;
              const int16Data = new Int16Array(float32Data.length);
              for (let i = 0; i < float32Data.length; i++) {
                const clampedValue = Math.max(-1, Math.min(1, float32Data[i]));
                int16Data[i] = Math.round(clampedValue * 32767);
              }
              audioBuffer = int16Data.buffer;
            } else {
              audioBuffer = new Uint8Array(streamChunk.audio.data).buffer;
            }
          } else if (streamChunk.audio.buffer) {
            console.log('Extracting from buffer property');
            audioBuffer = streamChunk.audio.buffer instanceof ArrayBuffer ? 
              streamChunk.audio.buffer : 
              new Uint8Array(streamChunk.audio.buffer).buffer;
          } else if (Array.isArray(streamChunk.audio)) {
            console.log('Audio is array, converting to Uint8Array');
            audioBuffer = new Uint8Array(streamChunk.audio).buffer;
          } else {
            console.warn('Could not extract audio data from chunk, using empty buffer');
            console.log('Chunk audio object:', streamChunk.audio);
            audioBuffer = new ArrayBuffer(0);
          }
        } catch (error) {
          console.error('Failed to extract audio from chunk:', error);
          audioBuffer = new ArrayBuffer(0);
        }
      } else {
        console.log('No audio property in chunk');
        audioBuffer = new ArrayBuffer(0);
      }

      // Create audio chunk
      const audioChunk: AudioChunk = {
        text: chunkText,
        phonemes: phonemes || [],
        audio: audioBuffer,
        isComplete: false,
        chunkIndex: chunkIndex++
      };

      // Call the callback with the chunk
      await callback(audioChunk);
    }

    // Send completion chunk
    const finalChunk: AudioChunk = {
      text: '',
      phonemes: [],
      audio: new ArrayBuffer(0),
      isComplete: true,
      chunkIndex: chunkIndex,
      totalChunks: chunkIndex
    };

    await callback(finalChunk);

    const endTime = performance.now();
    const duration = endTime - startTime;
    console.log(`Streaming completed in ${duration.toFixed(2)}ms with ${chunkIndex} chunks`);
  }

  /**
   * Fallback to non-streaming mode when streaming fails
   */
  private async fallbackToNonStreaming(
    text: string,
    voiceId: string,
    callback: StreamingCallback
  ): Promise<void> {
    try {
      console.log('Using non-streaming fallback mode');
      
      // Generate complete audio using regular generation
      const result = await this.generateSpeech(text, voiceId);
      
      // Split the text into chunks for pseudo-streaming
      const sentences = text.match(/[^.!?]+[.!?]*\s*/g) || [text];
      const audioChunkSize = Math.ceil(result.audio.byteLength / sentences.length);
      
      // Send chunks with split audio
      for (let i = 0; i < sentences.length; i++) {
        const start = i * audioChunkSize;
        const end = Math.min(start + audioChunkSize, result.audio.byteLength);
        const chunkAudio = result.audio.slice(start, end);
        
        const audioChunk: AudioChunk = {
          text: sentences[i].trim(),
          phonemes: [],
          audio: chunkAudio,
          isComplete: false,
          chunkIndex: i
        };
        
        await callback(audioChunk);
        
        // Small delay to simulate streaming
        await new Promise(resolve => setTimeout(resolve, 100));
      }
      
      // Send completion chunk
      const finalChunk: AudioChunk = {
        text: '',
        phonemes: [],
        audio: new ArrayBuffer(0),
        isComplete: true,
        chunkIndex: sentences.length,
        totalChunks: sentences.length
      };
      
      await callback(finalChunk);
      
      console.log('Fallback mode completed successfully');
    } catch (error) {
      console.error('Fallback mode also failed:', error);
      throw error;
    }
  }

  /**
   * Convert Float32Array to Int16 PCM asynchronously to prevent UI freezing
   */
  private async convertFloat32ToInt16Async(float32Data: Float32Array): Promise<ArrayBuffer> {
    return new Promise((resolve) => {
      const batchSize = 1024; // Process in small batches to prevent blocking
      const int16Data = new Int16Array(float32Data.length);
      let index = 0;

      const processBatch = () => {
        const endIndex = Math.min(index + batchSize, float32Data.length);
        
        // Process current batch
        for (let i = index; i < endIndex; i++) {
          const clampedValue = Math.max(-1, Math.min(1, float32Data[i]));
          int16Data[i] = Math.round(clampedValue * 32767);
        }
        
        index = endIndex;
        
        if (index < float32Data.length) {
          // Continue with next batch on next frame
          requestAnimationFrame(processBatch);
        } else {
          // All done, resolve with the converted buffer
          resolve(int16Data.buffer);
        }
      };

      // Start processing
      processBatch();
    });
  }

  /**
   * Cancel current streaming operation
   */
  cancelStreaming(): void {
    if (this.isStreaming && this.currentStream) {
      try {
        // If the stream has a return method, call it to clean up
        if (typeof this.currentStream.return === 'function') {
          this.currentStream.return(undefined);
        }
        console.log('Streaming cancelled');
      } catch (error) {
        console.error('Error cancelling stream:', error);
      } finally {
        this.isStreaming = false;
        this.currentStream = null;
      }
    }
  }

  /**
   * Check if currently streaming
   */
  get streaming(): boolean {
    return this.isStreaming;
  }

  /**
   * Convert kokoro-js audio result to web audio format
   */
  private async formatAudioResult(audioResult: any, _processingTime: number): Promise<TTSResult> {
    try {
      // The kokoro-js audio result should have methods to get the audio data
      // Based on the example API, we need to extract the raw audio data
      let audioData: ArrayBuffer;
      let sampleRate = 22050; // Default Kokoro sample rate
      let duration = 0;

      console.log('Audio result type:', typeof audioResult);
      console.log('Audio result properties:', Object.getOwnPropertyNames(audioResult));

      // Try to get audio data from the kokoro-js result
      if (audioResult && typeof audioResult === 'object') {
        // If the result has a toArrayBuffer method
        if (typeof audioResult.toArrayBuffer === 'function') {
          console.log('Using toArrayBuffer method');
          audioData = await audioResult.toArrayBuffer();
        }
        // If the result has a buffer property
        else if (audioResult.buffer) {
          console.log('Using buffer property');
          audioData = audioResult.buffer instanceof ArrayBuffer ? 
            audioResult.buffer : 
            new Uint8Array(audioResult.buffer).buffer;
        }
        // If the result has audio property
        else if (audioResult.audio) {
          console.log('Using audio property');
          console.log('Audio property type:', typeof audioResult.audio);
          console.log('Audio property constructor:', audioResult.audio.constructor.name);
          console.log('Audio property length/byteLength:', audioResult.audio.length || audioResult.audio.byteLength);
          
          // Check if it's an ArrayBuffer
          if (audioResult.audio instanceof ArrayBuffer) {
            console.log('Audio is ArrayBuffer');
            audioData = audioResult.audio;
          }
          // Check if it's a typed array
          else if (audioResult.audio instanceof Float32Array) {
            console.log('Audio is Float32Array, converting to 16-bit PCM');
            const float32Data = audioResult.audio;
            console.log('Float32 sample count:', float32Data.length);
            console.log('First 10 float32 samples:', Array.from(float32Data.slice(0, 10)));
            
            // Convert Float32 to Int16 PCM
            const int16Data = new Int16Array(float32Data.length);
            for (let i = 0; i < float32Data.length; i++) {
              // Clamp to [-1, 1] and convert to 16-bit signed integer
              const clampedValue = Math.max(-1, Math.min(1, float32Data[i]));
              int16Data[i] = Math.round(clampedValue * 32767);
            }
            console.log('First 10 int16 samples:', Array.from(int16Data.slice(0, 10)));
            audioData = int16Data.buffer;
          }
          // Check if it's already Int16Array
          else if (audioResult.audio instanceof Int16Array) {
            console.log('Audio is Int16Array');
            console.log('First 10 samples:', Array.from(audioResult.audio.slice(0, 10)));
            audioData = audioResult.audio.buffer;
          }
          // Check if it's a regular array
          else if (Array.isArray(audioResult.audio)) {
            console.log('Audio is regular Array');
            console.log('First 10 values:', audioResult.audio.slice(0, 10));
            audioData = new Uint8Array(audioResult.audio).buffer;
          }
          // Fallback
          else {
            console.log('Audio is unknown type, trying Uint8Array conversion');
            audioData = new Uint8Array(audioResult.audio).buffer;
          }
          
          // Log the first few bytes of the final audio data
          const dataView = new DataView(audioData);
          const firstBytes = [];
          for (let i = 0; i < Math.min(20, audioData.byteLength); i++) {
            firstBytes.push(dataView.getUint8(i).toString(16).padStart(2, '0'));
          }
          console.log('First 20 bytes of audio data (hex):', firstBytes.join(' '));
        }
        // If it's directly an ArrayBuffer or typed array
        else if (audioResult instanceof ArrayBuffer) {
          console.log('Direct ArrayBuffer');
          audioData = audioResult;
        }
        else if (audioResult.constructor.name.includes('Array')) {
          console.log('Array-like object');
          audioData = new Uint8Array(audioResult).buffer;
        }
        else {
          throw new Error('Unable to extract audio data from kokoro-js result');
        }

        // Try to get sample rate and duration if available
        if (audioResult.sampleRate) {
          sampleRate = audioResult.sampleRate;
        }
        if (audioResult.duration) {
          duration = audioResult.duration;
        }
      } else {
        throw new Error('Invalid audio result from kokoro-js');
      }

      // Calculate duration if not provided (assuming 16-bit audio)
      if (duration === 0) {
        const samples = audioData.byteLength / 2; // 16-bit = 2 bytes per sample
        duration = samples / sampleRate;
      }

      console.log(`Audio formatted: ${audioData.byteLength} bytes, ${duration.toFixed(2)}s @ ${sampleRate}Hz`);

      // Convert raw PCM to proper WAV format for browser playback
      const wavBuffer = this.convertToWav(audioData, sampleRate, 1);
      console.log(`WAV conversion: ${wavBuffer.byteLength} bytes`);

      return {
        audio: wavBuffer,
        duration: duration,
        sampleRate: sampleRate
      };
    } catch (error) {
      console.error('Failed to format audio result:', error);
      throw new Error(`Failed to format audio result: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Convert audio buffer to playable web audio
   */
  async convertToWebAudio(audioData: ArrayBuffer): Promise<AudioBuffer> {
    const audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
    
    try {
      return await audioContext.decodeAudioData(audioData.slice(0));
    } catch (error) {
      // Fallback: create raw PCM audio buffer
      const sampleRate = 22050;
      const samples = audioData.byteLength / 2; // 16-bit samples
      const audioBuffer = audioContext.createBuffer(1, samples, sampleRate);
      const channelData = audioBuffer.getChannelData(0);
      const view = new DataView(audioData);
      
      for (let i = 0; i < samples; i++) {
        channelData[i] = view.getInt16(i * 2, true) / 32768; // Convert to float [-1, 1]
      }
      
      return audioBuffer;
    }
  }

  /**
   * Get available voices
   */
  getAvailableVoices(): TTSVoice[] {
    return Object.values(this.voices);
  }

  /**
   * Set progress callback for TTS operations
   */
  setProgressCallback(callback: (stage: string, progress: number) => void): void {
    this.progressCallback = callback;
  }

  /**
   * Clear progress callback
   */
  clearProgressCallback(): void {
    this.progressCallback = undefined;
  }

  /**
   * Report progress if callback is set
   */
  private reportProgress(stage: string, progress: number): void {
    if (this.progressCallback) {
      this.progressCallback(stage, progress);
    }
  }

  /**
   * Update TTS configuration
   */
  updateConfig(newConfig: Partial<TTSConfig>): void {
    this.config = { ...this.config, ...newConfig };
  }

  /**
   * Get current configuration
   */
  getConfig(): TTSConfig {
    return { ...this.config };
  }

  /**
   * Check if TTS is loaded
   */
  get loaded(): boolean {
    return this.isLoaded;
  }

  /**
   * Get current memory usage
   */
  get currentMemoryUsage(): number {
    return this.isLoaded ? this.memoryUsage : 0;
  }

  /**
   * Get quantization level
   */
  get currentQuantization(): string {
    return this.quantization;
  }

  /**
   * Unload the model to free memory
   */
  unload(): void {
    if (this.isLoaded) {
      console.log('Unloading KokoroTTS model...');
      this.model = null;
      this.isLoaded = false;
      this.memoryUsage = 0;
    }
  }

  /**
   * Preview a voice with sample text
   */
  async previewVoice(voiceId: string, sampleText = "Hello, this is a voice preview."): Promise<TTSResult> {
    return this.generateSpeech(sampleText, voiceId);
  }

  /**
   * Estimate speech duration for text
   */
  estimateDuration(text: string, wordsPerMinute = 150): number {
    const words = text.split(/\s+/).length;
    return (words / wordsPerMinute) * 60; // Convert to seconds
  }

  /**
   * Validate voice ID
   */
  isValidVoice(voiceId: string): boolean {
    return voiceId in this.voices;
  }

  /**
   * Get voice information
   */
  getVoiceInfo(voiceId: string): TTSVoice | null {
    return this.voices[voiceId] || null;
  }

  /**
   * Get performance statistics
   */
  getPerformanceStats(): {
    loaded: boolean;
    memoryUsage: number;
    quantization: string;
    voicesAvailable: number;
    estimatedSpeed: string;
  } {
    return {
      loaded: this.isLoaded,
      memoryUsage: this.memoryUsage,
      quantization: this.quantization,
      voicesAvailable: Object.keys(this.voices).length,
      estimatedSpeed: '2-5x real-time'
    };
  }

  /**
   * Test TTS functionality
   */
  async testTTS(): Promise<boolean> {
    try {
      const result = await this.generateSpeech('Test', 'af_bella');
      return result.audio.byteLength > 0;
    } catch {
      return false;
    }
  }

  /**
   * Save/download audio as WAV file
   */
  async saveAudio(audioData: ArrayBuffer, filename = 'audio.wav'): Promise<void> {
    try {
      // Convert raw audio data to WAV format
      const wavBuffer = this.convertToWav(audioData, 22050, 1);
      
      // Create blob and download
      const blob = new Blob([wavBuffer], { type: 'audio/wav' });
      const url = URL.createObjectURL(blob);
      
      // Create download link
      const link = document.createElement('a');
      link.href = url;
      link.download = filename;
      document.body.appendChild(link);
      link.click();
      
      // Cleanup
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
      
      console.log(`Audio saved as ${filename}`);
    } catch (error) {
      console.error('Failed to save audio:', error);
      throw new Error(`Failed to save audio: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Convert raw PCM audio to WAV format
   */
  private convertToWav(audioData: ArrayBuffer, sampleRate: number, channels: number): ArrayBuffer {
    const dataLength = audioData.byteLength;
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
    view.setUint32(16, 16, true); // PCM format chunk size
    view.setUint16(20, 1, true); // PCM format
    view.setUint16(22, channels, true);
    view.setUint32(24, sampleRate, true);
    view.setUint32(28, sampleRate * channels * 2, true); // byte rate
    view.setUint16(32, channels * 2, true); // block align
    view.setUint16(34, 16, true); // bits per sample
    writeString(36, 'data');
    view.setUint32(40, dataLength, true);
    
    // Copy audio data
    const samples = new Uint8Array(audioData);
    const output = new Uint8Array(buffer, 44);
    output.set(samples);
    
    return buffer;
  }

  /**
   * Generate speech and return with save capability (similar to kokoro-js API)
   */
  async generate(text: string, options: { voice?: string } = {}): Promise<{
    audio: ArrayBuffer;
    duration: number;
    sampleRate: number;
    save: (filename?: string) => Promise<void>;
  }> {
    const result = await this.generateSpeech(text, options.voice || 'af_bella');
    
    return {
      ...result,
      save: async (filename = 'audio.wav') => {
        await this.saveAudio(result.audio, filename);
      }
    };
  }
}

// Default export for convenience
export default KokoroTTS;

// Factory function for easier instantiation
export function createKokoroTTS(options?: { 
  quantization?: string; 
  config?: Partial<TTSConfig> 
}): KokoroTTS {
  return new KokoroTTS(options);
}
