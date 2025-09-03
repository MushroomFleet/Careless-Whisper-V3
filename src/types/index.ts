// Core AI Service Types
export interface WhisperTranscribeOptions {
  word_timestamps?: boolean;
  language?: string;
  model?: WhisperModelSize;
  temperature?: number;
}

export interface WhisperTranscribeResult {
  text: string;
  language?: string;
  segments?: Array<{
    start: number;
    end: number;
    text: string;
    words?: Array<{
      start: number;
      end: number;
      word: string;
      probability: number;
    }>;
  }>;
}

export type WhisperModelSize = 'tiny' | 'base' | 'small' | 'medium' | 'large';

export interface WhisperModelInfo {
  size: WhisperModelSize;
  memoryMB: number;
  description: string;
  downloadSize: string;
}

// Ollama Service Types
export interface OllamaModel {
  name: string;
  size: number;
  digest: string;
  modified_at: string;
  details?: {
    format: string;
    family: string;
    families: string[];
    parameter_size: string;
    quantization_level: string;
  };
}

export interface OllamaGenerateOptions {
  temperature?: number;
  top_p?: number;
  top_k?: number;
  repeat_penalty?: number;
  seed?: number;
  num_predict?: number;
  stop?: string[];
}

export interface OllamaMessage {
  role: 'system' | 'user' | 'assistant';
  content: string;
}

export interface OllamaResponse {
  message: {
    role: string;
    content: string;
  };
  done: boolean;
  total_duration?: number;
  load_duration?: number;
  prompt_eval_count?: number;
  prompt_eval_duration?: number;
  eval_count?: number;
  eval_duration?: number;
}

// TTS Service Types
export interface TTSVoice {
  id: string;
  description: string;
  gender: 'male' | 'female';
  accent: string;
}

export interface TTSGenerateOptions {
  speed?: number;
  pitch?: number;
  volume?: number;
  voice?: string;
}

export interface TTSResult {
  audio: ArrayBuffer;
  duration: number;
  sampleRate: number;
}

// Streaming TTS Types
export interface AudioChunk {
  text: string;
  phonemes: string[];
  audio: ArrayBuffer;
  isComplete: boolean;
  chunkIndex: number;
  totalChunks?: number;
}

export type StreamingCallback = (chunk: AudioChunk) => void | Promise<void>;

export interface StreamingTTSOptions extends TTSGenerateOptions {
  enableStreaming?: boolean;
  chunkSize?: number;
  onProgress?: (progress: number) => void;
  onChunk?: StreamingCallback;
  onComplete?: () => void;
  onError?: (error: Error) => void;
}

// Application State Types
export interface AppState {
  currentTab: TabType;
  isProcessing: boolean;
  status: AppStatus;
  whisper: WhisperState;
  ollama: OllamaState;
  tts: TTSState;
  audio: AudioState;
}

export type TabType = 'speech' | 'config' | 'tts';

export interface AppStatus {
  type: 'idle' | 'loading' | 'recording' | 'processing' | 'success' | 'error';
  message: string;
  progress?: number;
}

export type TrafficLightStatus = 'red' | 'orange' | 'green';

export interface ServiceTrafficLights {
  whisper: TrafficLightStatus;
  ollama: TrafficLightStatus;
  tts: TrafficLightStatus;
}

export interface TrafficLightState {
  status: TrafficLightStatus;
  message: string;
  tooltip?: string;
}

export interface WhisperState {
  isLoaded: boolean;
  currentModel: WhisperModelSize;
  availableModels: WhisperModelInfo[];
  memoryUsage: number;
}

export interface OllamaState {
  isConnected: boolean;
  currentModel: string | null;
  availableModels: OllamaModel[];
  config: OllamaConfig;
  memoryUsage: number;
}

export interface OllamaConfig {
  temperature: number;
  top_p: number;
  systemPrompt: string;
  maxTokens: number;
  keepAlive: string;
}

export interface TTSState {
  isLoaded: boolean;
  currentVoice: string;
  availableVoices: TTSVoice[];
  config: TTSConfig;
  memoryUsage: number;
}

export interface TTSConfig {
  speed: number;
  pitch: number;
  volume: number;
}

export interface AudioState {
  isRecording: boolean;
  currentBlob: Blob | null;
  duration: number;
  visualizerData: Float32Array | null;
  outputAudio: HTMLAudioElement | null;
}

// Pipeline Types
export interface PipelineStage {
  name: string;
  status: 'pending' | 'running' | 'completed' | 'error';
  progress: number;
  message: string;
  duration?: number;
  error?: string;
}

export interface PipelineResult {
  transcript: string;
  response: string;
  audio: ArrayBuffer;
  stages: PipelineStage[];
  totalDuration: number;
  memoryUsage: {
    whisper: number;
    ollama: number;
    tts: number;
    total: number;
  };
}

export interface PipelineOptions {
  whisperOptions?: WhisperTranscribeOptions;
  ollamaOptions?: OllamaGenerateOptions;
  ttsOptions?: TTSGenerateOptions;
  silentThinking?: boolean;
  markdownFilter?: boolean;
  streamingMode?: boolean;
}

// Memory Management Types
export interface MemoryInfo {
  used: number;
  available: number;
  total: number;
  percentage: number;
  warning: boolean;
  critical: boolean;
}

export interface ModelMemoryRequirements {
  whisper: Record<WhisperModelSize, number>;
  ollama: Record<string, number>;
  tts: number;
}

// Error Types
export interface AppError {
  type: 'network' | 'model' | 'audio' | 'processing' | 'memory' | 'permission';
  message: string;
  details?: string;
  recoverable: boolean;
  action?: string;
}

// Audio Recording Types
export interface AudioRecorderConfig {
  sampleRate: number;
  channels: number;
  bitsPerSample: number;
  mimeType: string;
}

export interface AudioVisualizerConfig {
  fftSize: number;
  smoothingTimeConstant: number;
  minDecibels: number;
  maxDecibels: number;
}

// Configuration Types
export interface AppConfig {
  whisper: {
    defaultModel: WhisperModelSize;
    models: WhisperModelInfo[];
  };
  ollama: {
    host: string;
    defaultModel: string;
    timeout: number;
    retries: number;
  };
  tts: {
    defaultVoice: string;
    voices: TTSVoice[];
  };
  audio: {
    recorder: AudioRecorderConfig;
    visualizer: AudioVisualizerConfig;
  };
  memory: {
    maxUsage: number;
    warningThreshold: number;
    criticalThreshold: number;
  };
  ui: {
    theme: 'light' | 'dark' | 'auto';
    language: string;
    autoSwitchTabs: boolean;
  };
}

// Event Types
export interface AppEvent {
  type: string;
  payload?: any;
  timestamp: number;
}

export interface AudioEvent extends AppEvent {
  type: 'audio.start' | 'audio.stop' | 'audio.data' | 'audio.error';
  payload: {
    blob?: Blob;
    duration?: number;
    error?: string;
  };
}

export interface ProcessingEvent extends AppEvent {
  type: 'processing.start' | 'processing.progress' | 'processing.complete' | 'processing.error';
  payload: {
    stage?: string;
    progress?: number;
    result?: PipelineResult;
    error?: AppError;
  };
}

// Utility Types
export type EventCallback<T = any> = (event: T) => void;

export interface EventEmitter {
  on<T>(event: string, callback: EventCallback<T>): void;
  off<T>(event: string, callback: EventCallback<T>): void;
  emit<T>(event: string, data: T): void;
}

// Component Props Types
export interface TabProps {
  isActive: boolean;
  onActivate: () => void;
}

export interface AudioControlsProps {
  isRecording: boolean;
  onStartRecording: () => void;
  onStopRecording: () => void;
  disabled?: boolean;
}

export interface ModelSelectorProps {
  models: Array<{ value: string; label: string; description?: string }>;
  selected: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}

export interface StatusIndicatorProps {
  status: AppStatus;
  className?: string;
}

export interface ProgressBarProps {
  value: number;
  max: number;
  label?: string;
  className?: string;
}

export interface MemoryUsageProps {
  memory: MemoryInfo;
  breakdown?: Record<string, number>;
}

// Constants
export const WHISPER_MODELS: WhisperModelInfo[] = [
  { size: 'tiny', memoryMB: 39, description: 'Fastest, basic quality', downloadSize: '39MB' },
  { size: 'base', memoryMB: 74, description: 'Balanced speed/quality', downloadSize: '74MB' },
  { size: 'small', memoryMB: 244, description: 'Better quality, slower', downloadSize: '244MB' },
];

export const DEFAULT_VOICES: TTSVoice[] = [
  { id: 'af_bella', description: 'Bella - South African English', gender: 'female', accent: 'South African' },
  { id: 'am_adam', description: 'Adam - American English', gender: 'male', accent: 'American' },
  { id: 'bf_emma', description: 'Emma - British English', gender: 'female', accent: 'British' },
  { id: 'am_michael', description: 'Michael - American English', gender: 'male', accent: 'American' },
  { id: 'af_sarah', description: 'Sarah - American English', gender: 'female', accent: 'American' },
];

export const MEMORY_LIMITS = {
  GPU_4GB: 4096,
  GPU_8GB: 8192,
  WARNING_THRESHOLD: 0.8,
  CRITICAL_THRESHOLD: 0.95,
} as const;
