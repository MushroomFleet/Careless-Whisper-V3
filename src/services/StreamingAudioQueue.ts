export class StreamingAudioQueue {
  private audioContext: AudioContext | null = null;
  private queue: { audio: ArrayBuffer; text: string; index: number }[] = [];
  private isPlaying = false;
  private currentSource: AudioBufferSourceNode | null = null;
  private nextStartTime = 0;
  private onChunkStart?: (chunkIndex: number, text: string) => void;
  private onChunkEnd?: (chunkIndex: number) => void;
  private onQueueUpdate?: (queueSize: number) => void;
  private onComplete?: () => void;
  private maxQueueSize = 10; // Increased default from 5 to 10
  private dynamicQueueSize = 10;
  private sampleRate = 22050;
  private averageChunkDuration = 0;
  private chunkCount = 0;

  constructor(options: {
    maxQueueSize?: number;
    sampleRate?: number;
    onChunkStart?: (chunkIndex: number, text: string) => void;
    onChunkEnd?: (chunkIndex: number) => void;
    onQueueUpdate?: (queueSize: number) => void;
    onComplete?: () => void;
  } = {}) {
    this.maxQueueSize = options.maxQueueSize || 10; // Increased default
    this.dynamicQueueSize = this.maxQueueSize;
    this.sampleRate = options.sampleRate || 22050;
    this.onChunkStart = options.onChunkStart;
    this.onChunkEnd = options.onChunkEnd;
    this.onQueueUpdate = options.onQueueUpdate;
    this.onComplete = options.onComplete;
  }

  /**
   * Initialize audio context
   */
  async initialize(): Promise<void> {
    if (!this.audioContext) {
      this.audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
      
      // Resume context if needed (browser autoplay policy)
      if (this.audioContext.state === 'suspended') {
        await this.audioContext.resume();
      }
    }
  }

  /**
   * Add audio chunk to queue and start playback if not already playing
   */
  async addChunk(audio: ArrayBuffer, text: string, index: number): Promise<boolean> {
    return this.addChunkWithRetry(audio, text, index, 3);
  }

  /**
   * Add chunk with retry logic and dynamic queue sizing
   */
  async addChunkWithRetry(audio: ArrayBuffer, text: string, index: number, maxRetries: number = 3): Promise<boolean> {
    // Calculate estimated duration for this chunk
    const estimatedDuration = this.estimateChunkDuration(audio);
    
    // Adjust queue size dynamically based on chunk characteristics
    this.adjustQueueSize(estimatedDuration);

    for (let attempt = 0; attempt < maxRetries; attempt++) {
      // Check if queue has capacity
      if (this.queue.length < this.dynamicQueueSize) {
        // Only add non-empty audio chunks
        if (audio.byteLength > 0) {
          this.queue.push({ audio, text, index });
          console.log(`Added chunk ${index} to queue: "${text}" (${audio.byteLength} bytes, ~${estimatedDuration.toFixed(1)}s)`);
          
          // Update running average of chunk duration
          this.updateAverageChunkDuration(estimatedDuration);
          
          this.onQueueUpdate?.(this.queue.length);
        }

        // Start playback if not already playing
        if (!this.isPlaying) {
          await this.startPlayback();
        }

        return true;
      }

      // Queue is full, wait before retrying
      const waitTime = Math.min(1000 * Math.pow(2, attempt), 5000); // Exponential backoff, max 5s
      console.log(`Audio queue full (${this.queue.length}/${this.dynamicQueueSize}), waiting ${waitTime}ms before retry ${attempt + 1}/${maxRetries} for chunk ${index}`);
      
      await this.waitForQueueSpace(waitTime);
    }

    console.error(`Failed to add chunk ${index} after ${maxRetries} retries, queue persistently full`);
    return false;
  }

  /**
   * Estimate duration of an audio chunk based on its size
   */
  private estimateChunkDuration(audio: ArrayBuffer): number {
    // Assume 16-bit PCM at our sample rate
    const samples = audio.byteLength / 2;
    return samples / this.sampleRate;
  }

  /**
   * Update running average of chunk duration
   */
  private updateAverageChunkDuration(duration: number): void {
    this.chunkCount++;
    this.averageChunkDuration = ((this.averageChunkDuration * (this.chunkCount - 1)) + duration) / this.chunkCount;
  }

  /**
   * Dynamically adjust queue size based on chunk characteristics
   */
  private adjustQueueSize(chunkDuration: number): void {
    let optimalSize = this.maxQueueSize;

    // For very long chunks (>8s), use larger queue to prevent blocking
    if (chunkDuration > 8) {
      optimalSize = Math.max(this.maxQueueSize, 12);
    } else if (chunkDuration > 5) {
      optimalSize = Math.max(this.maxQueueSize, 10);
    } else if (chunkDuration > 2) {
      optimalSize = Math.max(this.maxQueueSize, 8);
    }

    // Also consider average chunk duration
    if (this.averageChunkDuration > 6) {
      optimalSize = Math.max(optimalSize, 15);
    }

    if (optimalSize !== this.dynamicQueueSize) {
      console.log(`Adjusting queue size from ${this.dynamicQueueSize} to ${optimalSize} (chunk: ${chunkDuration.toFixed(1)}s, avg: ${this.averageChunkDuration.toFixed(1)}s)`);
      this.dynamicQueueSize = optimalSize;
    }
  }

  /**
   * Wait for queue space to become available
   */
  private async waitForQueueSpace(waitTime: number): Promise<void> {
    return new Promise(resolve => {
      const checkSpace = () => {
        if (this.queue.length < this.dynamicQueueSize) {
          resolve();
        } else {
          // Check again after a short interval
          setTimeout(checkSpace, Math.min(waitTime / 10, 500));
        }
      };
      
      // Start checking after the wait time
      setTimeout(checkSpace, waitTime);
    });
  }

  /**
   * Start playing queued chunks
   */
  private async startPlayback(): Promise<void> {
    if (!this.audioContext || this.isPlaying) {
      return;
    }

    this.isPlaying = true;
    this.nextStartTime = this.audioContext.currentTime;
    console.log('Starting streaming audio playback');

    await this.playNextChunk();
  }

  /**
   * Play the next chunk in the queue
   */
  private async playNextChunk(): Promise<void> {
    if (!this.audioContext || this.queue.length === 0) {
      this.isPlaying = false;
      return;
    }

    const chunk = this.queue.shift()!;
    this.onQueueUpdate?.(this.queue.length);

    try {
      // Convert audio buffer to AudioBuffer
      const audioBuffer = await this.convertToAudioBuffer(chunk.audio);
      
      if (audioBuffer.length === 0) {
        console.warn(`Chunk ${chunk.index} has no audio data, skipping`);
        // Continue to next chunk
        await this.playNextChunk();
        return;
      }

      // Create audio source
      const source = this.audioContext.createBufferSource();
      source.buffer = audioBuffer;
      source.connect(this.audioContext.destination);

      // Set up completion handler
      const onEnded = async () => {
        console.log(`Chunk ${chunk.index} playback completed`);
        this.onChunkEnd?.(chunk.index);
        this.currentSource = null;
        
        // Play next chunk or complete
        if (this.queue.length > 0) {
          await this.playNextChunk();
        } else {
          this.isPlaying = false;
          console.log('All queued chunks played');
          this.onComplete?.();
        }
      };

      source.addEventListener('ended', onEnded);

      // Schedule playback
      const startTime = Math.max(this.nextStartTime, this.audioContext.currentTime);
      source.start(startTime);
      this.currentSource = source;

      // Update next start time
      this.nextStartTime = startTime + audioBuffer.duration;

      console.log(`Playing chunk ${chunk.index}: "${chunk.text}" (${audioBuffer.duration.toFixed(2)}s)`);
      this.onChunkStart?.(chunk.index, chunk.text);

    } catch (error) {
      console.error(`Failed to play chunk ${chunk.index}:`, error);
      // Continue to next chunk on error
      await this.playNextChunk();
    }
  }

  /**
   * Convert ArrayBuffer to AudioBuffer
   */
  private async convertToAudioBuffer(audioData: ArrayBuffer): Promise<AudioBuffer> {
    if (!this.audioContext) {
      throw new Error('Audio context not initialized');
    }

    try {
      // Try to decode as complete audio file first
      return await this.audioContext.decodeAudioData(audioData.slice(0));
    } catch (error) {
      // Fallback: treat as raw PCM data
      console.log('Decoding as raw PCM data');
      return this.createPCMAudioBuffer(audioData);
    }
  }

  /**
   * Create AudioBuffer from raw PCM data
   */
  private createPCMAudioBuffer(audioData: ArrayBuffer): AudioBuffer {
    if (!this.audioContext) {
      throw new Error('Audio context not initialized');
    }

    const samples = audioData.byteLength / 2; // 16-bit samples
    const audioBuffer = this.audioContext.createBuffer(1, samples, this.sampleRate);
    const channelData = audioBuffer.getChannelData(0);
    const view = new DataView(audioData);

    for (let i = 0; i < samples; i++) {
      // Convert 16-bit signed integer to float [-1, 1]
      channelData[i] = view.getInt16(i * 2, true) / 32768;
    }

    return audioBuffer;
  }

  /**
   * Stop playback and clear queue
   */
  stop(): void {
    if (this.currentSource) {
      this.currentSource.stop();
      this.currentSource = null;
    }
    
    this.queue.length = 0;
    this.isPlaying = false;
    this.nextStartTime = 0;
    console.log('Streaming audio playback stopped');
  }

  /**
   * Get queue status
   */
  getStatus(): {
    isPlaying: boolean;
    queueSize: number;
    maxQueueSize: number;
    queueFull: boolean;
  } {
    return {
      isPlaying: this.isPlaying,
      queueSize: this.queue.length,
      maxQueueSize: this.maxQueueSize,
      queueFull: this.queue.length >= this.maxQueueSize
    };
  }

  /**
   * Check if queue has capacity
   */
  hasCapacity(): boolean {
    return this.queue.length < this.maxQueueSize;
  }

  /**
   * Complete streaming (no more chunks coming)
   */
  complete(): void {
    console.log('Streaming marked as complete');
    // If no chunks are queued and not playing, trigger completion
    if (this.queue.length === 0 && !this.isPlaying) {
      this.onComplete?.();
    }
  }

  /**
   * Cleanup resources
   */
  cleanup(): void {
    this.stop();
    if (this.audioContext) {
      this.audioContext.close();
      this.audioContext = null;
    }
  }
}
