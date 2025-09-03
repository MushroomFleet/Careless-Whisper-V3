import { WhisperTranscriber } from './WhisperTranscriber';
import { OllamaClient } from './OllamaClient';
import { KokoroTTS } from './KokoroTTS';
import { AudioRecorder } from './AudioRecorder';
import { LoggingService } from './LoggingService';
import { StreamingAudioQueue } from './StreamingAudioQueue';
import type { 
  PipelineStage, 
  PipelineResult, 
  PipelineOptions,
  WhisperModelSize,
  OllamaConfig,
  TTSConfig,
  TTSResult,
  TTSVoice,
  AppError,
  MemoryInfo
} from '@/types';

export class AIController {
  private whisper: WhisperTranscriber;
  private ollama: OllamaClient;
  private tts: KokoroTTS;
  private audioRecorder: AudioRecorder;
  private loggingService: LoggingService;
  private currentStage: string | null = null;
  private isProcessing = false;
  private maxGpuMemory: number;

  constructor(options: {
    whisperModel?: WhisperModelSize;
    ollamaHost?: string;
    ollamaConfig?: Partial<OllamaConfig>;
    ttsConfig?: Partial<TTSConfig>;
    maxGpuMemory?: number;
    enableLogging?: boolean;
  } = {}) {
    // Initialize services with memory-optimized settings
    this.whisper = new WhisperTranscriber(options.whisperModel || 'base');
    this.ollama = new OllamaClient(options.ollamaHost, options.ollamaConfig);
    this.tts = new KokoroTTS({ 
      quantization: 'q8',
      config: options.ttsConfig 
    });
    this.audioRecorder = new AudioRecorder();
    this.loggingService = new LoggingService({
      enableLogging: options.enableLogging !== false, // Default to enabled
      logPath: 'logs'
    });
    this.maxGpuMemory = options.maxGpuMemory || 4096; // 4GB default
  }

  /**
   * Initialize all AI services
   */
  async initialize(): Promise<void> {
    try {
      console.log('Initializing AI Controller...');
      
      // Try to initialize Ollama (non-blocking)
      await this.ollama.initialize();
      
      // Initialize audio recorder
      await this.audioRecorder.initialize();
      
      console.log('AI Controller initialized successfully');
    } catch (error) {
      console.error('Failed to initialize AI Controller:', error);
      // Allow the app to continue even if some services fail
      console.warn('Some services may not be available, but continuing initialization');
    }
  }

  /**
   * Process audio through the complete pipeline: Speech → LLM → TTS
   */
  async processAudioPipeline(
    audioBuffer: ArrayBuffer, 
    options: PipelineOptions = {}
  ): Promise<PipelineResult> {
    if (this.isProcessing) {
      throw new Error('Pipeline is already processing');
    }

    this.isProcessing = true;
    const startTime = performance.now();
    const stages: PipelineStage[] = [
      { name: 'Speech Recognition', status: 'pending', progress: 0, message: 'Initializing...' },
      { name: 'LLM Processing', status: 'pending', progress: 0, message: 'Waiting...' },
      { name: 'Speech Synthesis', status: 'pending', progress: 0, message: 'Waiting...' }
    ];

    let transcript = '';
    let response = '';
    let audio: ArrayBuffer;

    // Start logging session
    const sessionId = this.loggingService.generateSessionId();
    const whisperModel = this.whisper.modelSize;
    const ollamaModel = this.ollama.loadedModel || 'unknown';
    const ttsVoice = options.ttsOptions?.voice || 'af_bella';

    await this.loggingService.startSession(sessionId, whisperModel, ollamaModel, ttsVoice);

    try {
      // Stage 1: Speech Recognition
      this.currentStage = 'transcription';
      stages[0].status = 'running';
      stages[0].message = 'Transcribing audio...';
      
      const stageStartTime = performance.now();
      
      // Load Whisper if needed (unload other models to free memory)
      await this.optimizeMemoryForStage('whisper');
      await this.whisper.initialize();
      
      transcript = (await this.whisper.transcribe(audioBuffer, options.whisperOptions)).text;
      
      const stageEndTime = performance.now();
      const transcriptionTime = stageEndTime - stageStartTime;
      
      stages[0].status = 'completed';
      stages[0].progress = 100;
      stages[0].duration = transcriptionTime;
      stages[0].message = `Transcribed: "${transcript.substring(0, 50)}..."`;

      if (!transcript.trim()) {
        throw new Error('No speech detected in audio');
      }

      // Log transcription result
      const inputAudioBlob = new Blob([audioBuffer], { type: 'audio/wav' });
      await this.loggingService.logTranscription(sessionId, transcript, transcriptionTime, inputAudioBlob);

      // Stage 2: LLM Processing
      this.currentStage = 'generation';
      stages[1].status = 'running';
      stages[1].message = 'Generating response...';
      
      const llmStartTime = performance.now();
      
      // Load Ollama model (unload Whisper to free memory)
      await this.optimizeMemoryForStage('ollama');
      
      // Ensure a model is loaded
      if (!this.ollama.loadedModel) {
        await this.ollama.loadModel('llama3.1:3b-instruct-q4_0');
      }
      
      response = await this.ollama.generate(transcript, options.ollamaOptions);
      
      const llmEndTime = performance.now();
      const responseTime = llmEndTime - llmStartTime;
      
      stages[1].status = 'completed';
      stages[1].progress = 100;
      stages[1].duration = responseTime;
      stages[1].message = `Generated: "${response.substring(0, 50)}..."`;

      if (!response.trim()) {
        throw new Error('Empty response from LLM');
      }

      // Log Ollama response
      await this.loggingService.logResponse(sessionId, transcript, response, responseTime);

      // Stage 3: Text-to-Speech (Streaming)
      this.currentStage = 'synthesis';
      stages[2].status = 'running';
      stages[2].message = 'Applying text filters...';
      
      const ttsStartTime = performance.now();
      
      // Apply text filters before TTS (think filter first, then markdown filter)
      const filteredResponse = this.applyTextFilters(response, options);
      console.log('Text filtering applied:', {
        originalLength: response.length,
        filteredLength: filteredResponse.length,
        silentThinking: options.silentThinking !== false,
        markdownFilter: options.markdownFilter !== false
      });
      
      // Load TTS (unload Ollama to free memory)
      await this.optimizeMemoryForStage('tts');
      await this.tts.initialize();
      
      stages[2].message = 'Synthesizing speech...';
      
      // Use streaming TTS for better handling of long text
      const audioChunks: ArrayBuffer[] = [];
      let chunkCount = 0;
      
      await this.tts.streamSpeech(
        filteredResponse, 
        options.ttsOptions?.voice || 'af_bella',
        async (chunk) => {
          if (chunk.isComplete) {
            console.log(`TTS streaming completed with ${chunkCount} chunks`);
            stages[2].progress = 100;
            stages[2].message = 'Speech synthesis completed';
          } else {
            // Collect audio chunks
            if (chunk.audio.byteLength > 0) {
              audioChunks.push(chunk.audio);
              chunkCount++;
            }
            
            // Update progress based on chunk index
            if (chunk.totalChunks && chunk.totalChunks > 0) {
              const progress = Math.min(95, (chunk.chunkIndex / chunk.totalChunks) * 100);
              stages[2].progress = progress;
              stages[2].message = `Synthesizing speech... (${Math.round(progress)}%)`;
            } else {
              // Progressive feedback without total known
              const progress = Math.min(90, chunkCount * 10);
              stages[2].progress = progress;
              stages[2].message = `Synthesizing speech... (chunk ${chunkCount})`;
            }
            
            console.log(`Received TTS chunk ${chunk.chunkIndex}: ${chunk.text} (${chunk.audio.byteLength} bytes)`);
          }
        }
      );
      
      // Combine all audio chunks into final buffer
      audio = this.combineAudioChunks(audioChunks);
      
      const ttsEndTime = performance.now();
      const ttsTime = ttsEndTime - ttsStartTime;
      
      stages[2].status = 'completed';
      stages[2].progress = 100;
      stages[2].duration = ttsTime;
      stages[2].message = `Speech synthesis completed (${chunkCount} chunks, ${audio.byteLength} bytes)`;

      // Log generated speech
      await this.loggingService.logGeneratedSpeech(sessionId, audio, ttsVoice, ttsTime);

      const totalEndTime = performance.now();
      const totalDuration = totalEndTime - startTime;

      // Create session summary
      await this.loggingService.logPipelineResult(
        { transcript, response, audio, stages, totalDuration, memoryUsage: this.getMemoryUsage() },
        sessionId,
        {
          transcription: transcriptionTime,
          response: responseTime,
          tts: ttsTime
        }
      );

      console.log(`Pipeline completed in ${totalDuration.toFixed(2)}ms`);

      return {
        transcript,
        response,
        audio,
        stages,
        totalDuration,
        memoryUsage: this.getMemoryUsage()
      };

    } catch (error) {
      console.error(`Pipeline failed at ${this.currentStage}:`, error);
      
      // Mark current stage as failed
      const currentStageIndex = this.currentStage === 'transcription' ? 0 : 
                               this.currentStage === 'generation' ? 1 : 2;
      stages[currentStageIndex].status = 'error';
      stages[currentStageIndex].error = error instanceof Error ? error.message : 'Unknown error';

      // End session on error
      await this.loggingService.endSession(sessionId);

      throw this.createAppError('processing', `Pipeline failed at ${this.currentStage}`, error);
    } finally {
      this.isProcessing = false;
      this.currentStage = null;
      
      // End session when processing completes
      await this.loggingService.endSession(sessionId);
    }
  }

  /**
   * Apply text filters before TTS generation
   */
  private applyTextFilters(text: string, options: PipelineOptions): string {
    let filtered = text;
    
    // Step 1: Think filter (if enabled)
    if (options.silentThinking !== false) {
      const beforeThink = filtered;
      filtered = this.filterThinkingTags(filtered);
      if (beforeThink !== filtered) {
        console.log('Think filter applied:', {
          removed: beforeThink.length - filtered.length,
          originalLength: beforeThink.length,
          filteredLength: filtered.length
        });
      }
    }
    
    // Step 2: Markdown filter (if enabled)
    if (options.markdownFilter !== false) {
      const beforeMarkdown = filtered;
      filtered = this.filterMarkdownSymbols(filtered);
      if (beforeMarkdown !== filtered) {
        console.log('Markdown filter applied:', {
          removed: beforeMarkdown.length - filtered.length,
          originalLength: beforeMarkdown.length,
          filteredLength: filtered.length
        });
      }
    }
    
    return filtered;
  }

  /**
   * Filter out thinking tags from text
   */
  private filterThinkingTags(text: string): string {
    if (!text) return text;
    
    // Regular expression to match <think>...</think> blocks (including nested tags)
    const thinkRegex = /<think\b[^>]*>.*?<\/think>/gis;
    
    let filteredText = text.replace(thinkRegex, '');
    
    // Clean up any extra whitespace that might be left after removing thinking blocks
    filteredText = filteredText.replace(/\n\s*\n\s*\n/g, '\n\n'); // Remove multiple empty lines
    filteredText = filteredText.trim(); // Remove leading/trailing whitespace
    
    return filteredText;
  }

  /**
   * Filter out markdown formatting symbols from text
   */
  private filterMarkdownSymbols(text: string): string {
    if (!text) return text;
    
    let filteredText = text;
    
    // Remove headers (# ## ### etc.)
    filteredText = filteredText.replace(/^#+\s*/gm, '');
    
    // Remove bold/italic formatting (**bold** *italic* __bold__ _italic_)
    filteredText = filteredText.replace(/\*\*([^*]+)\*\*/g, '$1'); // **bold**
    filteredText = filteredText.replace(/\*([^*]+)\*/g, '$1'); // *italic*
    filteredText = filteredText.replace(/__([^_]+)__/g, '$1'); // __bold__
    filteredText = filteredText.replace(/_([^_]+)_/g, '$1'); // _italic_
    
    // Remove code blocks and inline code
    filteredText = filteredText.replace(/```[\s\S]*?```/g, ''); // Code blocks
    filteredText = filteredText.replace(/`([^`]+)`/g, '$1'); // Inline code
    filteredText = filteredText.replace(/~~~[\s\S]*?~~~/g, ''); // Alternative code blocks
    
    // Remove links [text](url) -> text
    filteredText = filteredText.replace(/\[([^\]]+)\]\([^)]+\)/g, '$1');
    
    // Remove blockquotes
    filteredText = filteredText.replace(/^>\s*/gm, '');
    
    // Remove list markers (- + * at start of line)
    filteredText = filteredText.replace(/^[\s]*[-+*]\s+/gm, '');
    
    // Remove numbered list markers (1. 2. etc.)
    filteredText = filteredText.replace(/^[\s]*\d+\.\s+/gm, '');
    
    // Remove table separators
    filteredText = filteredText.replace(/\|/g, ' ');
    
    // Remove horizontal rules
    filteredText = filteredText.replace(/^[-*_]{3,}$/gm, '');
    
    // Clean up multiple spaces and empty lines
    filteredText = filteredText.replace(/[ \t]+/g, ' '); // Multiple spaces to single space
    filteredText = filteredText.replace(/\n\s*\n\s*\n/g, '\n\n'); // Multiple empty lines to double
    filteredText = filteredText.trim(); // Remove leading/trailing whitespace
    
    return filteredText;
  }

  /**
   * Combine audio chunks into a single ArrayBuffer
   */
  private combineAudioChunks(chunks: ArrayBuffer[]): ArrayBuffer {
    if (chunks.length === 0) {
      return new ArrayBuffer(0);
    }
    
    if (chunks.length === 1) {
      return chunks[0];
    }
    
    // Calculate total size
    const totalSize = chunks.reduce((sum, chunk) => sum + chunk.byteLength, 0);
    
    // Create combined buffer
    const combined = new ArrayBuffer(totalSize);
    const combinedView = new Uint8Array(combined);
    
    let offset = 0;
    for (const chunk of chunks) {
      const chunkView = new Uint8Array(chunk);
      combinedView.set(chunkView, offset);
      offset += chunk.byteLength;
    }
    
    console.log(`Combined ${chunks.length} audio chunks into ${totalSize} bytes`);
    return combined;
  }

  /**
   * Optimize memory usage for a specific stage
   */
  private async optimizeMemoryForStage(targetStage: 'whisper' | 'ollama' | 'tts'): Promise<void> {
    const currentMemory = this.getTotalMemoryUsage();
    
    console.log(`Optimizing memory for ${targetStage} stage. Current usage: ${currentMemory}MB`);

    // Unload models not needed for current stage
    switch (targetStage) {
      case 'whisper':
        // Keep only Whisper loaded
        this.tts.unload();
        break;
      
      case 'ollama':
        // Keep only Ollama loaded
        this.whisper.unload();
        this.tts.unload();
        break;
      
      case 'tts':
        // Keep only TTS loaded
        this.whisper.unload();
        break;
    }

    // Wait for garbage collection
    if (globalThis.gc) {
      globalThis.gc();
    }
    
    const newMemory = this.getTotalMemoryUsage();
    console.log(`Memory optimized. New usage: ${newMemory}MB`);
  }

  /**
   * Get current memory usage breakdown
   */
  getMemoryUsage(): {
    whisper: number;
    ollama: number;
    tts: number;
    total: number;
  } {
    return {
      whisper: this.whisper.memoryUsage,
      ollama: this.ollama.getMemoryUsage(),
      tts: this.tts.currentMemoryUsage,
      total: this.getTotalMemoryUsage()
    };
  }

  /**
   * Get total memory usage across all services
   */
  private getTotalMemoryUsage(): number {
    return this.whisper.memoryUsage + 
           this.ollama.getMemoryUsage() + 
           this.tts.currentMemoryUsage;
  }

  /**
   * Get memory information with warnings
   */
  getMemoryInfo(): MemoryInfo {
    const used = this.getTotalMemoryUsage();
    const available = this.maxGpuMemory - used;
    const percentage = (used / this.maxGpuMemory) * 100;
    
    return {
      used,
      available,
      total: this.maxGpuMemory,
      percentage,
      warning: percentage > 80,
      critical: percentage > 95
    };
  }

  /**
   * Start audio recording
   */
  async startRecording(): Promise<void> {
    await this.audioRecorder.startRecording();
  }

  /**
   * Stop audio recording and return blob
   */
  async stopRecording(): Promise<Blob> {
    return await this.audioRecorder.stopRecording();
  }

  /**
   * Process recorded audio through pipeline
   */
  async processRecordedAudio(
    audioBlob: Blob, 
    options: PipelineOptions = {}
  ): Promise<PipelineResult> {
    const arrayBuffer = await this.audioRecorder.blobToArrayBuffer(audioBlob);
    return this.processAudioPipeline(arrayBuffer, options);
  }

  /**
   * Set audio visualization callback
   */
  setAudioVisualizationCallback(callback: (data: Float32Array) => void): void {
    this.audioRecorder.setVisualizationCallback(callback);
  }

  /**
   * Get service status
   */
  getServiceStatus(): {
    whisper: { loaded: boolean; model: WhisperModelSize; memory: number };
    ollama: { connected: boolean; model: string | null; memory: number };
    tts: { loaded: boolean; memory: number };
    audioRecorder: { available: boolean };
    memory: MemoryInfo;
  } {
    return {
      whisper: {
        loaded: this.whisper.loaded,
        model: this.whisper.modelSize,
        memory: this.whisper.memoryUsage
      },
      ollama: {
        connected: this.ollama.connected,
        model: this.ollama.loadedModel,
        memory: this.ollama.getMemoryUsage()
      },
      tts: {
        loaded: this.tts.loaded,
        memory: this.tts.currentMemoryUsage
      },
      audioRecorder: {
        available: this.audioRecorder.recording !== undefined
      },
      memory: this.getMemoryInfo()
    };
  }

  /**
   * Update Whisper model
   */
  async setWhisperModel(modelSize: WhisperModelSize): Promise<void> {
    await this.whisper.setModelSize(modelSize);
  }

  /**
   * Update Ollama model
   */
  async setOllamaModel(modelName: string): Promise<void> {
    await this.ollama.loadModel(modelName);
  }

  /**
   * Update Ollama configuration
   */
  updateOllamaConfig(config: Partial<OllamaConfig>): void {
    this.ollama.updateConfig(config);
  }

  /**
   * Update TTS configuration
   */
  updateTTSConfig(config: Partial<TTSConfig>): void {
    this.tts.updateConfig(config);
  }

  /**
   * Set TTS progress callback
   */
  setTTSProgressCallback(callback: (stage: string, progress: number) => void): void {
    this.tts.setProgressCallback(callback);
  }

  /**
   * Clear TTS progress callback
   */
  clearTTSProgressCallback(): void {
    this.tts.clearProgressCallback();
  }

  /**
   * Preview a TTS voice with sample text
   */
  async previewTTSVoice(voiceId: string, sampleText?: string): Promise<TTSResult> {
    const text = sampleText || "Hello, this is a preview of the selected voice.";
    const result = await this.tts.generate(text, { voice: voiceId });
    return {
      audio: result.audio,
      duration: result.duration,
      sampleRate: result.sampleRate
    };
  }

  /**
   * Log voice preview audio to logging service
   */
  async logVoicePreview(voiceId: string, audioBuffer: ArrayBuffer): Promise<void> {
    await this.loggingService.logVoicePreview(voiceId, audioBuffer);
  }

  /**
   * Auto-download voice preview audio
   */
  async autoDownloadVoicePreview(filename: string, audioBuffer: ArrayBuffer): Promise<void> {
    const audioBlob = new Blob([audioBuffer], { type: 'audio/wav' });
    await this.loggingService.autoDownloadAudio(filename, audioBlob, 'voicePreviews');
  }

  /**
   * Auto-download response audio
   */
  async autoDownloadResponseAudio(filename: string, audioBuffer: ArrayBuffer): Promise<void> {
    const audioBlob = new Blob([audioBuffer], { type: 'audio/wav' });
    await this.loggingService.autoDownloadAudio(filename, audioBlob, 'responseAudio');
  }

  /**
   * Enable auto-download for specific types
   */
  enableAutoDownload(types: {
    voicePreviews?: boolean;
    responseAudio?: boolean;
    inputAudio?: boolean;
    sessionSummaries?: boolean;
  } = {}): void {
    this.loggingService.enableAutoDownload(types);
  }

  /**
   * Disable auto-download
   */
  disableAutoDownload(): void {
    this.loggingService.disableAutoDownload();
  }

  /**
   * Get auto-download status
   */
  getAutoDownloadStatus(): {
    enabled: boolean;
    types: {
      voicePreviews: boolean;
      responseAudio: boolean;
      inputAudio: boolean;
      sessionSummaries: boolean;
    };
  } {
    return this.loggingService.getAutoDownloadStatus();
  }

  /**
   * Get TTS voice information
   */
  getTTSVoiceInfo(voiceId: string): TTSVoice | null {
    return this.tts.getVoiceInfo(voiceId);
  }

  /**
   * Get available TTS voices
   */
  getAvailableTTSVoices(): TTSVoice[] {
    return this.tts.getAvailableVoices();
  }

  /**
   * Get available Ollama models
   */
  async getAvailableOllamaModels(): Promise<Array<{ name: string; size?: number }>> {
    try {
      const models = await this.ollama.listModels();
      return models.map(model => ({
        name: model.name,
        size: model.size
      }));
    } catch (error) {
      console.error('Failed to get available Ollama models:', error);
      return [];
    }
  }

  /**
   * Test all services
   */
  async testServices(): Promise<{
    whisper: boolean;
    ollama: boolean;
    tts: boolean;
    audioRecorder: boolean;
  }> {
    const results = {
      whisper: false,
      ollama: false,
      tts: false,
      audioRecorder: false
    };

    try {
      // Test Ollama connection
      results.ollama = await this.ollama.testConnection();
    } catch (error) {
      console.error('Ollama test failed:', error);
    }

    try {
      // Test TTS
      results.tts = await this.tts.testTTS();
    } catch (error) {
      console.error('TTS test failed:', error);
    }

    try {
      // Test microphone availability
      results.audioRecorder = await AudioRecorder.checkMicrophoneAvailability();
    } catch (error) {
      console.error('Audio recorder test failed:', error);
    }

    // Whisper test would require actual audio, so just check if it can be loaded
    try {
      await this.whisper.initialize();
      results.whisper = this.whisper.loaded;
    } catch (error) {
      console.error('Whisper test failed:', error);
    }

    return results;
  }

  /**
   * Create standardized error object
   */
  private createAppError(
    type: AppError['type'], 
    message: string, 
    originalError?: unknown
  ): AppError {
    return {
      type,
      message,
      details: originalError instanceof Error ? originalError.message : String(originalError),
      recoverable: type !== 'memory',
      action: this.getSuggestedAction(type)
    };
  }

  /**
   * Get suggested action for error type
   */
  private getSuggestedAction(type: AppError['type']): string {
    switch (type) {
      case 'network':
        return 'Check Ollama server connection';
      case 'model':
        return 'Try a different model or restart the application';
      case 'audio':
        return 'Check microphone permissions and connection';
      case 'memory':
        return 'Use smaller models or restart the application';
      case 'permission':
        return 'Grant required permissions and reload the page';
      default:
        return 'Restart the application or try again';
    }
  }

  /**
   * Get current processing state
   */
  get processing(): boolean {
    return this.isProcessing;
  }

  /**
   * Get current pipeline stage
   */
  get currentPipelineStage(): string | null {
    return this.currentStage;
  }

  /**
   * Cleanup all resources
   */
  cleanup(): void {
    this.whisper.unload();
    this.ollama.disconnect();
    this.tts.unload();
    this.audioRecorder.cleanup();
    
    console.log('AI Controller cleaned up');
  }
}

// Default export for convenience
export default AIController;

// Factory function for easier instantiation
export function createAIController(options?: {
  whisperModel?: WhisperModelSize;
  ollamaHost?: string;
  ollamaConfig?: Partial<OllamaConfig>;
  ttsConfig?: Partial<TTSConfig>;
  maxGpuMemory?: number;
}): AIController {
  return new AIController(options);
}
