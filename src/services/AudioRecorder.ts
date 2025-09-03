import type { AudioRecorderConfig, AudioVisualizerConfig } from '@/types';

export class AudioRecorder {
  private mediaRecorder: MediaRecorder | null = null;
  private stream: MediaStream | null = null;
  private audioContext: AudioContext | null = null;
  private analyser: AnalyserNode | null = null;
  private microphone: MediaStreamAudioSourceNode | null = null;
  private isRecording = false;
  private chunks: Blob[] = [];
  private config: AudioRecorderConfig;
  private visualizerConfig: AudioVisualizerConfig;
  private onDataCallback?: (data: Float32Array) => void;
  private animationFrame?: number;

  constructor(
    config?: Partial<AudioRecorderConfig>,
    visualizerConfig?: Partial<AudioVisualizerConfig>
  ) {
    this.config = {
      sampleRate: 16000,
      channels: 1,
      bitsPerSample: 16,
      mimeType: 'audio/webm;codecs=opus',
      ...config
    };

    this.visualizerConfig = {
      fftSize: 256,
      smoothingTimeConstant: 0.8,
      minDecibels: -90,
      maxDecibels: -10,
      ...visualizerConfig
    };
  }

  /**
   * Initialize audio recording
   */
  async initialize(): Promise<void> {
    try {
      console.log('Initializing audio recorder...');
      
      // Don't request microphone access during initialization
      // This will be done when the user actually tries to record
      console.log('Audio recorder ready (microphone access will be requested when recording starts)');
    } catch (error) {
      console.error('Failed to initialize audio recorder:', error);
      throw new Error(`Failed to initialize audio recorder: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Start recording audio
   */
  async startRecording(): Promise<void> {
    if (!this.mediaRecorder || !this.audioContext) {
      await this.setupAudio();
    }

    if (this.isRecording) {
      console.warn('Already recording');
      return;
    }

    try {
      // Resume audio context if suspended
      if (this.audioContext!.state === 'suspended') {
        await this.audioContext!.resume();
      }

      // Clear previous chunks
      this.chunks = [];

      // Start recording
      this.mediaRecorder!.start(100); // Collect data every 100ms
      this.isRecording = true;

      // Start visualization
      this.startVisualization();

      console.log('Recording started');
    } catch (error) {
      console.error('Failed to start recording:', error);
      throw new Error(`Failed to start recording: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Setup audio recording equipment
   */
  private async setupAudio(): Promise<void> {
    try {
      console.log('Setting up audio equipment...');
      
      // Request microphone access
      this.stream = await navigator.mediaDevices.getUserMedia({
        audio: {
          sampleRate: this.config.sampleRate,
          channelCount: this.config.channels,
          echoCancellation: true,
          noiseSuppression: true,
          autoGainControl: true
        }
      });

      // Create audio context for visualization
      this.audioContext = new (window.AudioContext || (window as any).webkitAudioContext)({
        sampleRate: this.config.sampleRate
      });

      // Create analyser for audio visualization
      this.analyser = this.audioContext.createAnalyser();
      this.analyser.fftSize = this.visualizerConfig.fftSize;
      this.analyser.smoothingTimeConstant = this.visualizerConfig.smoothingTimeConstant;
      this.analyser.minDecibels = this.visualizerConfig.minDecibels;
      this.analyser.maxDecibels = this.visualizerConfig.maxDecibels;

      // Connect microphone to analyser
      this.microphone = this.audioContext.createMediaStreamSource(this.stream);
      this.microphone.connect(this.analyser);

      // Create MediaRecorder
      this.mediaRecorder = new MediaRecorder(this.stream, {
        mimeType: this.getSupportedMimeType()
      });

      this.setupMediaRecorderEvents();
      
      console.log('Audio equipment setup completed');
    } catch (error) {
      console.error('Failed to setup audio equipment:', error);
      throw new Error(`Failed to setup audio equipment: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Stop recording audio
   */
  stopRecording(): Promise<Blob> {
    return new Promise((resolve, reject) => {
      if (!this.isRecording || !this.mediaRecorder) {
        reject(new Error('Not currently recording'));
        return;
      }

      // Set up one-time event listener for stop event
      const handleStop = () => {
        this.mediaRecorder!.removeEventListener('stop', handleStop);
        
        // Create blob from chunks
        const blob = new Blob(this.chunks, { 
          type: this.getSupportedMimeType() 
        });
        
        this.isRecording = false;
        this.stopVisualization();
        
        console.log(`Recording stopped. Blob size: ${blob.size} bytes`);
        resolve(blob);
      };

      this.mediaRecorder.addEventListener('stop', handleStop);
      this.mediaRecorder.stop();
    });
  }

  /**
   * Setup MediaRecorder event handlers
   */
  private setupMediaRecorderEvents(): void {
    if (!this.mediaRecorder) return;

    this.mediaRecorder.addEventListener('dataavailable', (event) => {
      if (event.data.size > 0) {
        this.chunks.push(event.data);
      }
    });

    this.mediaRecorder.addEventListener('error', (event) => {
      console.error('MediaRecorder error:', event);
    });
  }

  /**
   * Start audio visualization
   */
  private startVisualization(): void {
    if (!this.analyser) return;

    const dataArray = new Float32Array(this.analyser.frequencyBinCount);

    const updateVisualization = () => {
      if (!this.isRecording || !this.analyser) return;

      this.analyser.getFloatFrequencyData(dataArray);
      
      if (this.onDataCallback) {
        this.onDataCallback(dataArray);
      }

      this.animationFrame = requestAnimationFrame(updateVisualization);
    };

    updateVisualization();
  }

  /**
   * Stop audio visualization
   */
  private stopVisualization(): void {
    if (this.animationFrame) {
      cancelAnimationFrame(this.animationFrame);
      this.animationFrame = undefined;
    }
  }

  /**
   * Set callback for visualization data
   */
  setVisualizationCallback(callback: (data: Float32Array) => void): void {
    this.onDataCallback = callback;
  }

  /**
   * Get supported MIME type for recording
   */
  private getSupportedMimeType(): string {
    const types = [
      'audio/webm;codecs=opus',
      'audio/webm',
      'audio/mp4',
      'audio/ogg;codecs=opus'
    ];

    for (const type of types) {
      if (MediaRecorder.isTypeSupported(type)) {
        return type;
      }
    }

    return 'audio/webm'; // Fallback
  }

  /**
   * Convert blob to ArrayBuffer
   */
  async blobToArrayBuffer(blob: Blob): Promise<ArrayBuffer> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result as ArrayBuffer);
      reader.onerror = () => reject(new Error('Failed to read blob'));
      reader.readAsArrayBuffer(blob);
    });
  }

  /**
   * Get current recording state
   */
  get recording(): boolean {
    return this.isRecording;
  }

  /**
   * Get current configuration
   */
  getConfig(): AudioRecorderConfig {
    return { ...this.config };
  }

  /**
   * Get visualizer configuration
   */
  getVisualizerConfig(): AudioVisualizerConfig {
    return { ...this.visualizerConfig };
  }

  /**
   * Check if microphone is available
   */
  static async checkMicrophoneAvailability(): Promise<boolean> {
    try {
      const devices = await navigator.mediaDevices.enumerateDevices();
      return devices.some(device => device.kind === 'audioinput');
    } catch {
      return false;
    }
  }

  /**
   * Request microphone permission
   */
  static async requestMicrophonePermission(): Promise<boolean> {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      stream.getTracks().forEach(track => track.stop());
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Get available audio input devices
   */
  static async getAudioInputDevices(): Promise<MediaDeviceInfo[]> {
    try {
      const devices = await navigator.mediaDevices.enumerateDevices();
      return devices.filter(device => device.kind === 'audioinput');
    } catch {
      return [];
    }
  }

  /**
   * Test audio levels
   */
  async testAudioLevels(duration = 3000): Promise<number[]> {
    if (!this.analyser) {
      throw new Error('Audio recorder not initialized');
    }

    const levels: number[] = [];
    const dataArray = new Float32Array(this.analyser.frequencyBinCount);
    const startTime = Date.now();

    return new Promise((resolve) => {
      const collectLevels = () => {
        if (Date.now() - startTime >= duration) {
          resolve(levels);
          return;
        }

        this.analyser!.getFloatFrequencyData(dataArray);
        
        // Calculate average level
        const sum = dataArray.reduce((a, b) => a + b, 0);
        const average = sum / dataArray.length;
        levels.push(average);

        requestAnimationFrame(collectLevels);
      };

      collectLevels();
    });
  }

  /**
   * Cleanup resources
   */
  cleanup(): void {
    this.stopVisualization();

    if (this.mediaRecorder && this.isRecording) {
      this.mediaRecorder.stop();
    }

    if (this.stream) {
      this.stream.getTracks().forEach(track => track.stop());
      this.stream = null;
    }

    if (this.audioContext) {
      this.audioContext.close();
      this.audioContext = null;
    }

    this.mediaRecorder = null;
    this.analyser = null;
    this.microphone = null;
    this.isRecording = false;
    this.chunks = [];

    console.log('Audio recorder cleaned up');
  }
}

// Default export for convenience
export default AudioRecorder;

// Factory function for easier instantiation
export function createAudioRecorder(
  config?: Partial<AudioRecorderConfig>,
  visualizerConfig?: Partial<AudioVisualizerConfig>
): AudioRecorder {
  return new AudioRecorder(config, visualizerConfig);
}
