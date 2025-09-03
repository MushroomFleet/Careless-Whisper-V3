import type { PipelineResult } from '@/types';

interface SessionMetadata {
  sessionId: string;
  timestamp: Date;
  whisperModel: string;
  ollamaModel: string;
  ttsVoice: string;
  processingTimes: {
    transcription: number;
    response: number;
    tts: number;
  };
}

interface LoggingConfig {
  enableLogging: boolean;
  logPath: string;
  maxSessionsPerDay: number;
  autoDownload: boolean;
  autoDownloadTypes: {
    voicePreviews: boolean;
    responseAudio: boolean;
    inputAudio: boolean;
    sessionSummaries: boolean;
  };
}

export class LoggingService {
  private config: LoggingConfig;
  private currentSession: number = 0;
  private todayDateString: string;
  private sessionMap: Map<string, SessionMetadata> = new Map();

  constructor(config: Partial<LoggingConfig> = {}) {
    this.config = {
      enableLogging: true,
      logPath: 'logs',
      maxSessionsPerDay: 999,
      autoDownload: false, // Default to manual downloads
      autoDownloadTypes: {
        voicePreviews: false,
        responseAudio: false,
        inputAudio: false,
        sessionSummaries: false
      },
      ...config
    };
    
    this.todayDateString = this.getTodayDateString();
    this.initializeSession();
  }

  /**
   * Initialize logging session for today
   */
  private async initializeSession(): Promise<void> {
    try {
      // Create directory structure for today
      await this.createDirectoryStructure();
      
      // Get next session number
      this.currentSession = await this.getNextSessionNumber();
      
      console.log(`Logging initialized for ${this.todayDateString}, session ${this.currentSession.toString().padStart(3, '0')}`);
    } catch (error) {
      console.warn('Failed to initialize logging:', error);
    }
  }

  /**
   * Get today's date string in YYMMDD format
   */
  private getTodayDateString(): string {
    const now = new Date();
    const year = now.getFullYear().toString().slice(-2);
    const month = (now.getMonth() + 1).toString().padStart(2, '0');
    const day = now.getDate().toString().padStart(2, '0');
    return `${year}${month}${day}`;
  }

  /**
   * Create directory structure for logging
   */
  private async createDirectoryStructure(): Promise<void> {
    if (!this.config.enableLogging) return;

    const basePath = `${this.config.logPath}/${this.todayDateString}`;
    
    // Note: In a browser environment, we can't create actual directories
    // This would be implemented differently in a Node.js environment
    // For now, we'll simulate the structure in memory and use downloads
    console.log(`Would create directory structure: ${basePath}/transcripts/, ${basePath}/responses/, ${basePath}/speech/`);
  }

  /**
   * Get next session number for today
   */
  private async getNextSessionNumber(): Promise<number> {
    // In a real implementation, this would check existing files
    // For browser environment, we'll start from 1 and increment
    const storedSession = localStorage.getItem(`speak2me_session_${this.todayDateString}`);
    let sessionNum = storedSession ? parseInt(storedSession) + 1 : 1;
    
    if (sessionNum > this.config.maxSessionsPerDay) {
      sessionNum = 1; // Reset if exceeds max
    }
    
    localStorage.setItem(`speak2me_session_${this.todayDateString}`, sessionNum.toString());
    return sessionNum;
  }

  /**
   * Start a new session
   */
  async startSession(sessionId: string, whisperModel: string, ollamaModel: string, ttsVoice: string): Promise<void> {
    const metadata: SessionMetadata = {
      sessionId,
      timestamp: new Date(),
      whisperModel,
      ollamaModel,
      ttsVoice,
      processingTimes: {
        transcription: 0,
        response: 0,
        tts: 0
      }
    };

    this.sessionMap.set(sessionId, metadata);
    console.log(`Started logging session: ${sessionId}`);
  }

  /**
   * Log transcription result
   */
  async logTranscription(sessionId: string, transcript: string, processingTime: number, audioBlob?: Blob): Promise<void> {
    if (!this.config.enableLogging) return;

    try {
      const metadata = this.sessionMap.get(sessionId);
      if (metadata) {
        metadata.processingTimes.transcription = processingTime;
      }

      const sessionNum = this.currentSession.toString().padStart(3, '0');
      const timestamp = new Date().toISOString();
      
      // Create transcript content with metadata
      const transcriptContent = this.formatTranscript(transcript, timestamp, metadata);
      
      // Save transcript file
      await this.saveTextFile(
        `transcripts/session_${sessionNum}_transcript.txt`,
        transcriptContent
      );

      // Optionally save the input audio
      if (audioBlob) {
        await this.saveAudioFile(
          `speech/session_${sessionNum}_input_audio.wav`,
          audioBlob
        );
      }

      console.log(`Logged transcription for session ${sessionId}`);
    } catch (error) {
      console.error('Failed to log transcription:', error);
    }
  }

  /**
   * Log Ollama response
   */
  async logResponse(sessionId: string, prompt: string, response: string, processingTime: number): Promise<void> {
    if (!this.config.enableLogging) return;

    try {
      const metadata = this.sessionMap.get(sessionId);
      if (metadata) {
        metadata.processingTimes.response = processingTime;
      }

      const sessionNum = this.currentSession.toString().padStart(3, '0');
      const timestamp = new Date().toISOString();
      
      // Create response content with metadata
      const responseContent = this.formatResponse(prompt, response, timestamp, metadata);
      
      // Save response file
      await this.saveTextFile(
        `responses/session_${sessionNum}_response.txt`,
        responseContent
      );

      console.log(`Logged response for session ${sessionId}`);
    } catch (error) {
      console.error('Failed to log response:', error);
    }
  }

  /**
   * Log generated speech audio
   */
  async logGeneratedSpeech(sessionId: string, audioBuffer: ArrayBuffer, voiceId: string, processingTime: number): Promise<void> {
    if (!this.config.enableLogging) return;

    try {
      const metadata = this.sessionMap.get(sessionId);
      if (metadata) {
        metadata.processingTimes.tts = processingTime;
      }

      const sessionNum = this.currentSession.toString().padStart(3, '0');
      
      // Convert ArrayBuffer to Blob
      const audioBlob = new Blob([audioBuffer], { type: 'audio/wav' });
      
      // Save generated audio
      await this.saveAudioFile(
        `speech/session_${sessionNum}_generated_audio.wav`,
        audioBlob
      );

      console.log(`Logged generated speech for session ${sessionId}`);
    } catch (error) {
      console.error('Failed to log generated speech:', error);
    }
  }

  /**
   * Log voice preview audio
   */
  async logVoicePreview(voiceId: string, audioBuffer: ArrayBuffer): Promise<void> {
    if (!this.config.enableLogging) return;

    try {
      const timestamp = Date.now();
      const audioBlob = new Blob([audioBuffer], { type: 'audio/wav' });
      
      // Save voice preview
      await this.saveAudioFile(
        `speech/preview_voice_${voiceId}_${timestamp}.wav`,
        audioBlob
      );

      console.log(`Logged voice preview for ${voiceId}`);
    } catch (error) {
      console.error('Failed to log voice preview:', error);
    }
  }

  /**
   * Log complete pipeline result
   */
  async logPipelineResult(result: PipelineResult, sessionId: string, processingTimes: {
    transcription: number;
    response: number;
    tts: number;
  }): Promise<void> {
    if (!this.config.enableLogging) return;

    // Update session metadata
    const metadata = this.sessionMap.get(sessionId);
    if (metadata) {
      metadata.processingTimes = processingTimes;
    }

    // Log transcript
    await this.logTranscription(sessionId, result.transcript, processingTimes.transcription);
    
    // Log response (we need the original prompt, which should be passed separately)
    await this.logResponse(sessionId, result.transcript, result.response, processingTimes.response);
    
    // Log generated audio
    await this.logGeneratedSpeech(sessionId, result.audio, 'default', processingTimes.tts);

    // Create session summary
    await this.createSessionSummary(sessionId);
  }

  /**
   * Create session summary file
   */
  private async createSessionSummary(sessionId: string): Promise<void> {
    const metadata = this.sessionMap.get(sessionId);
    if (!metadata) return;

    const sessionNum = this.currentSession.toString().padStart(3, '0');
    const summary = this.formatSessionSummary(metadata);
    
    await this.saveTextFile(
      `session_${sessionNum}_summary.txt`,
      summary
    );
  }

  /**
   * Format transcript with metadata
   */
  private formatTranscript(transcript: string, timestamp: string, metadata?: SessionMetadata): string {
    let content = `=== TRANSCRIPT LOG ===\n`;
    content += `Timestamp: ${timestamp}\n`;
    if (metadata) {
      content += `Session ID: ${metadata.sessionId}\n`;
      content += `Whisper Model: ${metadata.whisperModel}\n`;
      content += `Processing Time: ${metadata.processingTimes.transcription}ms\n`;
    }
    content += `\n--- TRANSCRIPT ---\n`;
    content += transcript;
    content += `\n\n--- END TRANSCRIPT ---\n`;
    
    return content;
  }

  /**
   * Format response with metadata
   */
  private formatResponse(prompt: string, response: string, timestamp: string, metadata?: SessionMetadata): string {
    let content = `=== RESPONSE LOG ===\n`;
    content += `Timestamp: ${timestamp}\n`;
    if (metadata) {
      content += `Session ID: ${metadata.sessionId}\n`;
      content += `Ollama Model: ${metadata.ollamaModel}\n`;
      content += `Processing Time: ${metadata.processingTimes.response}ms\n`;
    }
    content += `\n--- PROMPT ---\n`;
    content += prompt;
    content += `\n\n--- RESPONSE ---\n`;
    content += response;
    content += `\n\n--- END RESPONSE ---\n`;
    
    return content;
  }

  /**
   * Format session summary
   */
  private formatSessionSummary(metadata: SessionMetadata): string {
    let content = `=== SESSION SUMMARY ===\n`;
    content += `Session ID: ${metadata.sessionId}\n`;
    content += `Date: ${this.todayDateString}\n`;
    content += `Timestamp: ${metadata.timestamp.toISOString()}\n`;
    content += `\n--- MODELS USED ---\n`;
    content += `Whisper Model: ${metadata.whisperModel}\n`;
    content += `Ollama Model: ${metadata.ollamaModel}\n`;
    content += `TTS Voice: ${metadata.ttsVoice}\n`;
    content += `\n--- PROCESSING TIMES ---\n`;
    content += `Transcription: ${metadata.processingTimes.transcription}ms\n`;
    content += `Response Generation: ${metadata.processingTimes.response}ms\n`;
    content += `Text-to-Speech: ${metadata.processingTimes.tts}ms\n`;
    content += `Total: ${Object.values(metadata.processingTimes).reduce((a, b) => a + b, 0)}ms\n`;
    content += `\n--- END SUMMARY ---\n`;
    
    return content;
  }

  /**
   * Save text file (in browser, this triggers download)
   */
  private async saveTextFile(filename: string, content: string): Promise<void> {
    try {
      const blob = new Blob([content], { type: 'text/plain' });
      const url = URL.createObjectURL(blob);
      
      // In browser environment, create download link
      const link = document.createElement('a');
      link.href = url;
      link.download = `${this.todayDateString}_${filename}`;
      
      // For automatic saving, we'd need a different approach
      // For now, log that the file would be saved
      console.log(`Would save text file: ${this.todayDateString}_${filename}`);
      
      // Optionally, automatically download (uncomment if desired)
      // document.body.appendChild(link);
      // link.click();
      // document.body.removeChild(link);
      
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Failed to save text file:', error);
    }
  }

  /**
   * Save audio file (in browser, this triggers download)
   */
  private async saveAudioFile(filename: string, audioBlob: Blob): Promise<void> {
    try {
      const url = URL.createObjectURL(audioBlob);
      
      // In browser environment, create download link
      const link = document.createElement('a');
      link.href = url;
      link.download = `${this.todayDateString}_${filename}`;
      
      // For automatic saving, we'd need a different approach
      // For now, log that the file would be saved
      console.log(`Would save audio file: ${this.todayDateString}_${filename} (${audioBlob.size} bytes)`);
      
      // Optionally, automatically download (uncomment if desired)
      // document.body.appendChild(link);
      // link.click();
      // document.body.removeChild(link);
      
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Failed to save audio file:', error);
    }
  }

  /**
   * End current session
   */
  async endSession(sessionId: string): Promise<void> {
    this.sessionMap.delete(sessionId);
    
    // Increment session counter for next session
    this.currentSession = await this.getNextSessionNumber();
    
    console.log(`Ended session: ${sessionId}`);
  }

  /**
   * Get current logging status
   */
  getLoggingStatus(): {
    enabled: boolean;
    currentDate: string;
    currentSession: number;
    activeSessions: number;
  } {
    return {
      enabled: this.config.enableLogging,
      currentDate: this.todayDateString,
      currentSession: this.currentSession,
      activeSessions: this.sessionMap.size
    };
  }

  /**
   * Update logging configuration
   */
  updateConfig(newConfig: Partial<LoggingConfig>): void {
    this.config = { ...this.config, ...newConfig };
    console.log('Logging configuration updated:', this.config);
  }

  /**
   * Generate unique session ID
   */
  generateSessionId(): string {
    const timestamp = Date.now();
    const random = Math.random().toString(36).substring(2, 8);
    return `session_${this.todayDateString}_${this.currentSession.toString().padStart(3, '0')}_${random}`;
  }

  /**
   * Auto-download audio file immediately
   */
  async autoDownloadAudio(filename: string, audioBlob: Blob, type: keyof LoggingConfig['autoDownloadTypes']): Promise<void> {
    if (!this.config.autoDownload || !this.config.autoDownloadTypes[type]) return;

    try {
      const url = URL.createObjectURL(audioBlob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `${this.todayDateString}_${filename}`;
      link.style.display = 'none';
      
      // Trigger automatic download
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      
      // Show notification
      this.showDownloadNotification(link.download, audioBlob.size);
      
      // Cleanup
      setTimeout(() => URL.revokeObjectURL(url), 1000);
      
      console.log(`Auto-downloaded: ${link.download}`);
    } catch (error) {
      console.error('Failed to auto-download audio:', error);
    }
  }

  /**
   * Auto-download text file immediately
   */
  async autoDownloadText(filename: string, content: string, type: keyof LoggingConfig['autoDownloadTypes']): Promise<void> {
    if (!this.config.autoDownload || !this.config.autoDownloadTypes[type]) return;

    try {
      const blob = new Blob([content], { type: 'text/plain' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `${this.todayDateString}_${filename}`;
      link.style.display = 'none';
      
      // Trigger automatic download
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      
      // Show notification
      this.showDownloadNotification(link.download, blob.size);
      
      // Cleanup
      setTimeout(() => URL.revokeObjectURL(url), 1000);
      
      console.log(`Auto-downloaded: ${link.download}`);
    } catch (error) {
      console.error('Failed to auto-download text:', error);
    }
  }

  /**
   * Show download notification
   */
  private showDownloadNotification(filename: string, size: number): void {
    // Create a temporary notification element
    const notification = document.createElement('div');
    notification.style.cssText = `
      position: fixed;
      top: 20px;
      right: 20px;
      background: #10b981;
      color: white;
      padding: 12px 16px;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      z-index: 10001;
      box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
      transform: translateX(100%);
      transition: transform 0.3s ease;
    `;
    
    notification.innerHTML = `
      <div style="display: flex; align-items: center;">
        <i class="fas fa-download" style="margin-right: 8px;"></i>
        <div>
          <div style="font-weight: 600;">Downloaded</div>
          <div style="font-size: 12px; opacity: 0.9;">${filename} (${this.formatBytes(size)})</div>
        </div>
      </div>
    `;
    
    document.body.appendChild(notification);
    
    // Animate in
    setTimeout(() => {
      notification.style.transform = 'translateX(0)';
    }, 100);
    
    // Auto-remove after 3 seconds
    setTimeout(() => {
      notification.style.transform = 'translateX(100%)';
      setTimeout(() => {
        if (document.body.contains(notification)) {
          document.body.removeChild(notification);
        }
      }, 300);
    }, 3000);
  }

  /**
   * Format bytes to human readable format
   */
  private formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  /**
   * Enable auto-download for specific types
   */
  enableAutoDownload(types: Partial<LoggingConfig['autoDownloadTypes']> = {}): void {
    this.config.autoDownload = true;
    this.config.autoDownloadTypes = {
      ...this.config.autoDownloadTypes,
      ...types
    };
    console.log('Auto-download enabled for:', types);
  }

  /**
   * Disable auto-download
   */
  disableAutoDownload(): void {
    this.config.autoDownload = false;
    console.log('Auto-download disabled');
  }

  /**
   * Get auto-download status
   */
  getAutoDownloadStatus(): {
    enabled: boolean;
    types: LoggingConfig['autoDownloadTypes'];
  } {
    return {
      enabled: this.config.autoDownload,
      types: { ...this.config.autoDownloadTypes }
    };
  }
}

// Default export
export default LoggingService;
