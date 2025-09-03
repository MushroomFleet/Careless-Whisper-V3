import { AIController } from '@/services/AIController';
import { StreamingAudioQueue } from '@/services/StreamingAudioQueue';
import type { 
  TabType, 
  AppState, 
  AppStatus, 
  PipelineResult,
  WhisperModelSize,
  OllamaConfig,
  TTSConfig,
  ServiceTrafficLights,
  TrafficLightStatus,
  TrafficLightState
} from '@/types';

export class SpeakToMeApp {
  private aiController: AIController;
  private state: AppState;
  private currentTab: TabType = 'speech';

  constructor() {
    this.aiController = new AIController({
      whisperModel: 'base',
      ollamaHost: 'http://127.0.0.1:11434',
      maxGpuMemory: 4096,
      enableLogging: true
    });

    this.state = {
      currentTab: 'speech',
      isProcessing: false,
      status: { type: 'idle', message: 'Ready' },
      whisper: {
        isLoaded: false,
        currentModel: 'base',
        availableModels: [],
        memoryUsage: 0
      },
      ollama: {
        isConnected: false,
        currentModel: null,
        availableModels: [],
        config: {
          temperature: 0.7,
          top_p: 0.9,
          systemPrompt: 'You are a helpful assistant.',
          maxTokens: 2048,
          keepAlive: '5m'
        },
        memoryUsage: 0
      },
      tts: {
        isLoaded: false,
        currentVoice: 'af_bella',
        availableVoices: [],
        config: {
          speed: 1.0,
          pitch: 1.0,
          volume: 1.0
        },
        memoryUsage: 0
      },
      audio: {
        isRecording: false,
        currentBlob: null,
        duration: 0,
        visualizerData: null,
        outputAudio: null
      }
    };
  }

  /**
   * Initialize the application
   */
  async initialize(): Promise<void> {
    try {
      console.log('Initializing SpeakToMeApp...');
      
      // Initialize AI Controller
      await this.aiController.initialize();
      
      // Enable auto-downloads by default
      this.aiController.enableAutoDownload({
        voicePreviews: true,
        responseAudio: true,
        inputAudio: true,
        sessionSummaries: true
      });
      
      // Setup UI
      this.setupUI();
      
      // Update initial state
      await this.updateServiceStatus();
      
      console.log('SpeakToMeApp initialized successfully');
      console.log('Auto-downloads enabled for all audio types');
    } catch (error) {
      console.error('Failed to initialize SpeakToMeApp:', error);
      throw error;
    }
  }

  /**
   * Setup the user interface
   */
  private setupUI(): void {
    this.createMainInterface();
    this.bindEventListeners();
    this.setupAudioVisualization();
    this.updateUI();
  }

  /**
   * Create the main interface HTML
   */
  private createMainInterface(): void {
    const appContainer = document.getElementById('app');
    if (!appContainer) return;

    appContainer.innerHTML = `
      <!-- Main Container -->
      <div class="min-h-screen bg-gray-50">
        <!-- Navigation Tabs -->
        <div class="border-b border-gray-200 bg-white shadow-sm">
          <nav class="container mx-auto px-4">
            <div class="flex space-x-8">
              <button id="speech-tab" class="tab-button tab-active" data-tab="speech">
                <div class="flex items-center">
                  <div class="traffic-light-container mr-3">
                    <div id="whisper-traffic-light" class="traffic-light traffic-light-red" title="Whisper Status">
                      <div class="traffic-light-dot"></div>
                    </div>
                  </div>
                  <i class="fas fa-microphone mr-2"></i>
                  Speech-to-Prompt
                </div>
              </button>
              <button id="config-tab" class="tab-button" data-tab="config">
                <div class="flex items-center">
                  <div class="traffic-light-container mr-3">
                    <div id="ollama-traffic-light" class="traffic-light traffic-light-red" title="Ollama Status">
                      <div class="traffic-light-dot"></div>
                    </div>
                  </div>
                  <i class="fas fa-cog mr-2"></i>
                  Ollama Config
                </div>
              </button>
              <button id="tts-tab" class="tab-button" data-tab="tts">
                <div class="flex items-center">
                  <div class="traffic-light-container mr-3">
                    <div id="tts-traffic-light" class="traffic-light traffic-light-red" title="TTS Status">
                      <div class="traffic-light-dot"></div>
                    </div>
                  </div>
                  <i class="fas fa-volume-up mr-2"></i>
                  Response-to-Speech
                </div>
              </button>
            </div>
          </nav>
        </div>

        <!-- Tab Content -->
        <div class="container mx-auto px-4 py-8">
          <!-- Speech-to-Prompt Tab -->
          <div id="speech-panel" class="tab-panel">
            <div class="audio-control-panel">
              <div class="flex items-center justify-between mb-6">
                <h2 class="text-2xl font-bold text-gray-800">Voice Input</h2>
                <div id="status-indicator" class="status-indicator bg-green-100 text-green-800">
                  <i class="fas fa-circle animate-pulse mr-1"></i>
                  <span id="status-text">Ready</span>
                </div>
              </div>
              
              <!-- Audio Controls -->
              <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div class="space-y-4">
                  <label class="block text-sm font-medium text-gray-700">
                    Whisper Model
                  </label>
                  <select id="whisper-model" class="model-selector">
                    <option value="tiny">Tiny (Fastest, ~850MB)</option>
                    <option value="base" selected>Base (Balanced, ~1.7GB)</option>
                    <option value="small">Small (Better Quality, ~2GB)</option>
                  </select>
                  
                  <button id="record-btn" class="btn-primary w-full">
                    <i class="fas fa-microphone mr-2"></i>
                    <span id="record-text">Start Recording</span>
                  </button>

                  <!-- Progress Bar -->
                  <div id="progress-container" class="hidden">
                    <div class="flex justify-between text-sm text-gray-600 mb-1">
                      <span id="progress-stage">Processing...</span>
                      <span id="progress-percent">0%</span>
                    </div>
                    <div class="progress-bar">
                      <div id="progress-fill" class="progress-fill w-0"></div>
                    </div>
                  </div>
                </div>
                
                <div class="space-y-4">
                  <label class="block text-sm font-medium text-gray-700">
                    Audio Input
                  </label>
                  <div id="audio-visualizer" class="audio-visualizer">
                    <canvas id="audio-canvas" class="w-full h-32 bg-gray-100 rounded-lg"></canvas>
                  </div>

                  <!-- Memory Usage -->
                  <div class="bg-blue-50 p-4 rounded-lg">
                    <h3 class="font-semibold text-blue-800 mb-2">
                      <i class="fas fa-memory mr-1"></i>
                      Memory Usage
                    </h3>
                    <div class="space-y-2 text-sm text-blue-700">
                      <div class="flex justify-between">
                        <span>Current Usage:</span>
                        <span id="current-memory">0MB</span>
                      </div>
                      <div class="flex justify-between">
                        <span>Available GPU:</span>
                        <span id="available-memory">4GB</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              
              <!-- Transcript Display -->
              <div class="mt-6">
                <label class="block text-sm font-medium text-gray-700 mb-2">
                  Transcript
                </label>
                <textarea id="transcript" class="w-full h-24 p-3 border border-gray-300 rounded-lg resize-none custom-scrollbar"
                          placeholder="Your speech will appear here..." readonly></textarea>
              </div>
            </div>
          </div>

          <!-- Ollama Config Tab -->
          <div id="config-panel" class="tab-panel hidden">
            <div class="audio-control-panel">
              <h2 class="text-2xl font-bold text-gray-800 mb-6">Model Configuration</h2>
              
              <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <div class="space-y-4">
                  <label class="block text-sm font-medium text-gray-700">
                    Available Models
                  </label>
                  <select id="model-selector" class="model-selector">
                    <option value="llama3.1:3b-instruct-q4_0">Llama 3.1 3B (Recommended)</option>
                    <option value="phi3:3.8b-mini-instruct-4k-q4_0">Phi-3 3.8B Mini</option>
                    <option value="gemma2:2b-instruct-q4_0">Gemma2 2B (Fastest)</option>
                    <option value="qwen2:1.5b-instruct-q4_0">Qwen2 1.5B (Ultra-fast)</option>
                  </select>
                  
                  <div class="space-y-3">
                    <label class="block text-sm font-medium text-gray-700">
                      Temperature: <span id="temp-value">0.7</span>
                    </label>
                    <input type="range" id="temperature" class="w-full" 
                           min="0" max="1" step="0.1" value="0.7">
                    
                    <label class="block text-sm font-medium text-gray-700">
                      Top P: <span id="top-p-value">0.9</span>
                    </label>
                    <input type="range" id="top-p" class="w-full" 
                           min="0" max="1" step="0.05" value="0.9">
                  </div>

                  <button id="test-connection" class="btn-secondary w-full">
                    <i class="fas fa-plug mr-2"></i>
                    Test Connection
                  </button>
                </div>
                
                <div class="space-y-4">
                  <label class="block text-sm font-medium text-gray-700">
                    System Prompt
                  </label>
                  <textarea id="system-prompt" class="w-full h-32 p-3 border border-gray-300 rounded-lg custom-scrollbar"
                            placeholder="You are a helpful assistant...">You are a helpful assistant.</textarea>
                  
                  <!-- Connection Status -->
                  <div id="connection-status" class="bg-gray-50 p-4 rounded-lg">
                    <h3 class="font-semibold text-gray-800 mb-2">
                      <i class="fas fa-server mr-1"></i>
                      Connection Status
                    </h3>
                    <div class="space-y-2 text-sm text-gray-700">
                      <div class="flex justify-between">
                        <span>Ollama Server:</span>
                        <span id="server-status" class="text-red-600">Disconnected</span>
                      </div>
                      <div class="flex justify-between">
                        <span>Current Model:</span>
                        <span id="current-model">None</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Response-to-Speech Tab -->
          <div id="tts-panel" class="tab-panel hidden">
            <div class="audio-control-panel">
              <h2 class="text-2xl font-bold text-gray-800 mb-6">Voice Output</h2>
              
              <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <div class="space-y-4">
                  <label class="block text-sm font-medium text-gray-700">
                    Voice Selection
                  </label>
                  <select id="voice-selector" class="model-selector">
                    <optgroup label="🇺🇸 American Female (Premium)">
                      <option value="af_heart">Heart - A Quality ❤️</option>
                      <option value="af_bella" selected>Bella - A Quality 🔥</option>
                    </optgroup>
                    <optgroup label="🇺🇸 American Female (Standard)">
                      <option value="af_alloy">Alloy - B Quality</option>
                      <option value="af_aoede">Aoede - B Quality</option>
                      <option value="af_kore">Kore - B Quality</option>
                      <option value="af_nicole">Nicole - B Quality 🎧</option>
                      <option value="af_nova">Nova - B Quality</option>
                      <option value="af_sarah">Sarah - B Quality</option>
                      <option value="af_sky">Sky - B Quality</option>
                    </optgroup>
                    <optgroup label="🇺🇸 American Female (Basic)">
                      <option value="af_jessica">Jessica - C Quality</option>
                      <option value="af_river">River - C Quality</option>
                    </optgroup>
                    <optgroup label="🇺🇸 American Male (Standard)">
                      <option value="am_fenrir">Fenrir - B Quality</option>
                      <option value="am_michael">Michael - B Quality</option>
                      <option value="am_puck">Puck - B Quality</option>
                    </optgroup>
                    <optgroup label="🇺🇸 American Male (Basic)">
                      <option value="am_echo">Echo - C Quality</option>
                      <option value="am_eric">Eric - C Quality</option>
                      <option value="am_liam">Liam - C Quality</option>
                      <option value="am_onyx">Onyx - C Quality</option>
                      <option value="am_santa">Santa - C Quality</option>
                      <option value="am_adam">Adam - D Quality</option>
                    </optgroup>
                    <optgroup label="🇬🇧 British Female">
                      <option value="bf_emma">Emma - B Quality 🚺</option>
                      <option value="bf_isabella">Isabella - B Quality</option>
                      <option value="bf_alice">Alice - C Quality 🚺</option>
                      <option value="bf_lily">Lily - C Quality 🚺</option>
                    </optgroup>
                    <optgroup label="🇬🇧 British Male">
                      <option value="bm_george">George - B Quality</option>
                      <option value="bm_fable">Fable - B Quality 🚹</option>
                      <option value="bm_lewis">Lewis - C Quality</option>
                      <option value="bm_daniel">Daniel - C Quality 🚹</option>
                    </optgroup>
                  </select>
                  
                  <div class="space-y-3">
                    <label class="block text-sm font-medium text-gray-700">
                      Speech Speed: <span id="speed-value">1.0</span>
                    </label>
                    <input type="range" id="speech-speed" class="w-full" 
                           min="0.5" max="2.0" step="0.1" value="1.0">
                  </div>
                  
                  <button id="preview-voice" class="btn-secondary w-full">
                    <i class="fas fa-play mr-2"></i>
                    Preview Voice
                  </button>
                </div>
                
                <div class="space-y-4">
                  <!-- Silent Thinking Option -->
                  <div class="bg-blue-50 p-3 rounded-lg">
                    <div class="flex items-center justify-between">
                      <div class="flex items-center space-x-2">
                        <input type="checkbox" id="silent-thinking" checked class="rounded border-blue-300 text-blue-600 focus:ring-blue-500">
                        <label for="silent-thinking" class="text-sm font-medium text-blue-700">
                          Silent Thinking
                        </label>
                        <div class="group relative">
                          <i class="fas fa-info-circle text-blue-500 text-xs cursor-help"></i>
                          <div class="absolute bottom-full left-1/2 transform -translate-x-1/2 mb-2 px-3 py-2 bg-gray-800 text-white text-xs rounded-lg opacity-0 group-hover:opacity-100 transition-opacity duration-200 whitespace-nowrap z-10">
                            Filter out &lt;think&gt;...&lt;/think&gt; content from audio generation
                          </div>
                        </div>
                      </div>
                      <span id="thinking-status" class="text-xs text-blue-600">Filtering enabled</span>
                    </div>
                  </div>

                  <!-- Markdown Filter Option -->
                  <div class="bg-green-50 p-3 rounded-lg mt-2">
                    <div class="flex items-center justify-between">
                      <div class="flex items-center space-x-2">
                        <input type="checkbox" id="markdown-filter" checked class="rounded border-green-300 text-green-600 focus:ring-green-500">
                        <label for="markdown-filter" class="text-sm font-medium text-green-700">
                          Markdown Filter
                        </label>
                        <div class="group relative">
                          <i class="fas fa-info-circle text-green-500 text-xs cursor-help"></i>
                          <div class="absolute bottom-full left-1/2 transform -translate-x-1/2 mb-2 px-3 py-2 bg-gray-800 text-white text-xs rounded-lg opacity-0 group-hover:opacity-100 transition-opacity duration-200 whitespace-nowrap z-10">
                            Remove markdown formatting symbols from audio generation
                          </div>
                        </div>
                      </div>
                      <span id="markdown-status" class="text-xs text-green-600">Markdown filtering enabled</span>
                    </div>
                  </div>

                  <!-- Streaming Mode Option -->
                  <div class="bg-purple-50 p-3 rounded-lg mt-2">
                    <div class="flex items-center justify-between">
                      <div class="flex items-center space-x-2">
                        <input type="checkbox" id="streaming-mode" checked class="rounded border-purple-300 text-purple-600 focus:ring-purple-500">
                        <label for="streaming-mode" class="text-sm font-medium text-purple-700">
                          Streaming Mode
                        </label>
                        <div class="group relative">
                          <i class="fas fa-info-circle text-purple-500 text-xs cursor-help"></i>
                          <div class="absolute bottom-full left-1/2 transform -translate-x-1/2 mb-2 px-3 py-2 bg-gray-800 text-white text-xs rounded-lg opacity-0 group-hover:opacity-100 transition-opacity duration-200 whitespace-nowrap z-10">
                            Stream audio chunks as they generate for real-time playback
                          </div>
                        </div>
                      </div>
                      <span id="streaming-status" class="text-xs text-purple-600">Real-time streaming enabled</span>
                    </div>
                  </div>

                  <!-- Streaming Progress (hidden by default) -->
                  <div id="streaming-progress" class="bg-orange-50 p-3 rounded-lg mt-2 hidden">
                    <div class="flex items-center justify-between mb-2">
                      <span class="text-sm font-medium text-orange-700">Streaming Audio</span>
                      <span id="streaming-chunks" class="text-xs text-orange-600">Chunk 0</span>
                    </div>
                    <div class="flex items-center space-x-2">
                      <div class="flex-1 bg-orange-200 rounded-full h-2">
                        <div id="streaming-progress-bar" class="bg-orange-500 h-2 rounded-full transition-all duration-300 w-0"></div>
                      </div>
                      <span id="streaming-queue-status" class="text-xs text-orange-600">Queue: 0</span>
                    </div>
                  </div>
                  
                  <label class="block text-sm font-medium text-gray-700">
                    Response Text
                  </label>
                  <textarea id="response-text" class="w-full h-32 p-3 border border-gray-300 rounded-lg custom-scrollbar"
                            placeholder="AI response will appear here..." readonly></textarea>
                  
                  <!-- Audio Player -->
                  <div class="bg-gray-50 p-4 rounded-lg">
                    <div class="flex items-center space-x-4">
                      <button id="play-audio" class="btn-primary" disabled>
                        <i class="fas fa-play mr-2"></i>
                        Play Response
                      </button>
                      <button id="download-audio" class="btn-secondary" disabled>
                        <i class="fas fa-download mr-2"></i>
                        Download
                      </button>
                    </div>
                    <audio id="response-audio" class="w-full mt-4 hidden" controls></audio>
                  </div>

                  <!-- Auto-Download Settings -->
                  <div class="bg-purple-50 p-4 rounded-lg mt-4">
                    <h3 class="font-semibold text-purple-800 mb-3">
                      <i class="fas fa-download mr-1"></i>
                      Auto-Download Settings
                    </h3>
                    <div class="space-y-3">
                      <div class="flex items-center justify-between">
                        <span class="text-sm text-purple-700">Master Auto-Download</span>
                        <label class="relative inline-flex items-center cursor-pointer">
                          <input type="checkbox" id="auto-download-master" class="sr-only peer" checked>
                          <div class="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-purple-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-purple-600"></div>
                        </label>
                      </div>
                      <div class="grid grid-cols-2 gap-3 pl-4 border-l-2 border-purple-200">
                        <div class="flex items-center justify-between">
                          <span class="text-xs text-purple-600">Voice Previews</span>
                          <input type="checkbox" id="auto-download-previews" checked class="rounded border-purple-300 text-purple-600 focus:ring-purple-500">
                        </div>
                        <div class="flex items-center justify-between">
                          <span class="text-xs text-purple-600">Response Audio</span>
                          <input type="checkbox" id="auto-download-response" checked class="rounded border-purple-300 text-purple-600 focus:ring-purple-500">
                        </div>
                        <div class="flex items-center justify-between">
                          <span class="text-xs text-purple-600">Input Audio</span>
                          <input type="checkbox" id="auto-download-input" checked class="rounded border-purple-300 text-purple-600 focus:ring-purple-500">
                        </div>
                        <div class="flex items-center justify-between">
                          <span class="text-xs text-purple-600">Session Summaries</span>
                          <input type="checkbox" id="auto-download-summaries" checked class="rounded border-purple-300 text-purple-600 focus:ring-purple-500">
                        </div>
                      </div>
                      <div class="pt-2 border-t border-purple-200">
                        <div class="flex items-center justify-between text-xs text-purple-600">
                          <span id="auto-download-status">All downloads enabled</span>
                          <button id="refresh-download-status" class="text-purple-500 hover:text-purple-700">
                            <i class="fas fa-sync-alt text-xs"></i>
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    `;
  }

  /**
   * Bind event listeners
   */
  private bindEventListeners(): void {
    // Tab switching
    document.querySelectorAll('.tab-button').forEach(button => {
      button.addEventListener('click', (e) => {
        // Handle clicking on nested elements within the tab button
        let target = e.target as HTMLElement;
        let tabButton = target.closest('.tab-button') as HTMLElement;
        
        if (tabButton) {
          const tabId = tabButton.getAttribute('data-tab') as TabType;
          console.log('Switching to tab:', tabId); // Debug log
          this.switchTab(tabId);
        }
      });
    });

    // Recording controls
    const recordBtn = document.getElementById('record-btn');
    recordBtn?.addEventListener('click', () => this.toggleRecording());

    // Model selectors
    const whisperModelSelect = document.getElementById('whisper-model') as HTMLSelectElement;
    whisperModelSelect?.addEventListener('change', (e) => {
      this.updateWhisperModel((e.target as HTMLSelectElement).value as WhisperModelSize);
    });

    const ollamaModelSelect = document.getElementById('model-selector') as HTMLSelectElement;
    ollamaModelSelect?.addEventListener('change', (e) => {
      this.updateOllamaModel((e.target as HTMLSelectElement).value);
    });

    // Configuration controls
    const temperatureSlider = document.getElementById('temperature') as HTMLInputElement;
    temperatureSlider?.addEventListener('input', (e) => {
      const value = parseFloat((e.target as HTMLInputElement).value);
      document.getElementById('temp-value')!.textContent = value.toString();
      this.updateOllamaConfig({ temperature: value });
    });

    const topPSlider = document.getElementById('top-p') as HTMLInputElement;
    topPSlider?.addEventListener('input', (e) => {
      const value = parseFloat((e.target as HTMLInputElement).value);
      document.getElementById('top-p-value')!.textContent = value.toString();
      this.updateOllamaConfig({ top_p: value });
    });

    const speedSlider = document.getElementById('speech-speed') as HTMLInputElement;
    speedSlider?.addEventListener('input', (e) => {
      const value = parseFloat((e.target as HTMLInputElement).value);
      document.getElementById('speed-value')!.textContent = value.toString();
      this.updateTTSConfig({ speed: value });
    });

    // System prompt
    const systemPromptTextarea = document.getElementById('system-prompt') as HTMLTextAreaElement;
    systemPromptTextarea?.addEventListener('change', (e) => {
      this.updateOllamaConfig({ systemPrompt: (e.target as HTMLTextAreaElement).value });
    });

    // Test connection
    const testConnectionBtn = document.getElementById('test-connection');
    testConnectionBtn?.addEventListener('click', () => this.testConnection());

    // Voice preview
    const previewVoiceBtn = document.getElementById('preview-voice');
    previewVoiceBtn?.addEventListener('click', () => this.previewVoice());

    // Audio playback
    const playAudioBtn = document.getElementById('play-audio');
    playAudioBtn?.addEventListener('click', () => this.playResponseAudio());

    const downloadAudioBtn = document.getElementById('download-audio');
    downloadAudioBtn?.addEventListener('click', () => this.downloadResponseAudio());

    // Auto-download settings controls
    const masterToggle = document.getElementById('auto-download-master') as HTMLInputElement;
    masterToggle?.addEventListener('change', (e) => {
      this.toggleAutoDownload((e.target as HTMLInputElement).checked);
    });

    const previewsToggle = document.getElementById('auto-download-previews') as HTMLInputElement;
    previewsToggle?.addEventListener('change', (e) => {
      this.updateAutoDownloadType('voicePreviews', (e.target as HTMLInputElement).checked);
    });

    const responseToggle = document.getElementById('auto-download-response') as HTMLInputElement;
    responseToggle?.addEventListener('change', (e) => {
      this.updateAutoDownloadType('responseAudio', (e.target as HTMLInputElement).checked);
    });

    const inputToggle = document.getElementById('auto-download-input') as HTMLInputElement;
    inputToggle?.addEventListener('change', (e) => {
      this.updateAutoDownloadType('inputAudio', (e.target as HTMLInputElement).checked);
    });

    const summariesToggle = document.getElementById('auto-download-summaries') as HTMLInputElement;
    summariesToggle?.addEventListener('change', (e) => {
      this.updateAutoDownloadType('sessionSummaries', (e.target as HTMLInputElement).checked);
    });

    const refreshStatusBtn = document.getElementById('refresh-download-status');
    refreshStatusBtn?.addEventListener('click', () => this.updateAutoDownloadStatus());

    // Silent thinking toggle
    const silentThinkingToggle = document.getElementById('silent-thinking') as HTMLInputElement;
    silentThinkingToggle?.addEventListener('change', (e) => {
      this.updateSilentThinkingStatus((e.target as HTMLInputElement).checked);
    });

    // Markdown filter toggle
    const markdownFilterToggle = document.getElementById('markdown-filter') as HTMLInputElement;
    markdownFilterToggle?.addEventListener('change', (e) => {
      this.updateMarkdownFilterStatus((e.target as HTMLInputElement).checked);
    });

    // Streaming mode toggle
    const streamingModeToggle = document.getElementById('streaming-mode') as HTMLInputElement;
    streamingModeToggle?.addEventListener('change', (e) => {
      this.updateStreamingModeStatus((e.target as HTMLInputElement).checked);
    });
  }

  /**
   * Setup audio visualization
   */
  private setupAudioVisualization(): void {
    const canvas = document.getElementById('audio-canvas') as HTMLCanvasElement;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Set canvas size
    canvas.width = canvas.offsetWidth;
    canvas.height = canvas.offsetHeight;

    this.aiController.setAudioVisualizationCallback((data: Float32Array) => {
      this.drawAudioVisualization(ctx, data, canvas.width, canvas.height);
    });
  }

  /**
   * Draw audio visualization
   */
  private drawAudioVisualization(
    ctx: CanvasRenderingContext2D, 
    data: Float32Array, 
    width: number, 
    height: number
  ): void {
    ctx.clearRect(0, 0, width, height);
    
    const barWidth = width / data.length;
    let x = 0;

    for (let i = 0; i < data.length; i++) {
      const barHeight = (data[i] + 140) * 2; // Normalize decibel values
      
      ctx.fillStyle = `hsl(${240 + barHeight / 2}, 50%, 50%)`;
      ctx.fillRect(x, height - barHeight, barWidth, barHeight);
      
      x += barWidth;
    }
  }

  /**
   * Switch tabs
   */
  private switchTab(tabId: TabType): void {
    console.log('switchTab called with:', tabId); // Debug log
    this.currentTab = tabId;
    this.state.currentTab = tabId;

    // Update tab buttons
    document.querySelectorAll('.tab-button').forEach(button => {
      button.classList.remove('tab-active');
    });
    const activeButton = document.querySelector(`[data-tab="${tabId}"]`);
    console.log('Active button found:', activeButton); // Debug log
    activeButton?.classList.add('tab-active');

    // Update panels
    document.querySelectorAll('.tab-panel').forEach(panel => {
      panel.classList.add('hidden');
    });
    const targetPanel = document.getElementById(`${tabId}-panel`);
    console.log('Target panel:', `${tabId}-panel`, 'Element found:', targetPanel); // Debug log
    targetPanel?.classList.remove('hidden');
  }

  /**
   * Toggle recording
   */
  private async toggleRecording(): Promise<void> {
    if (this.state.audio.isRecording) {
      await this.stopRecording();
    } else {
      await this.startRecording();
    }
  }

  /**
   * Start recording
   */
  private async startRecording(): Promise<void> {
    try {
      await this.aiController.startRecording();
      
      this.state.audio.isRecording = true;
      this.updateStatus('recording', 'Recording...');
      
      const recordBtn = document.getElementById('record-btn');
      const recordText = document.getElementById('record-text');
      
      if (recordBtn && recordText) {
        recordBtn.classList.add('btn-danger');
        recordBtn.classList.remove('btn-primary');
        recordText.innerHTML = '<i class="fas fa-stop mr-2"></i>Stop Recording';
      }

      // Add recording animation to visualizer
      const visualizer = document.getElementById('audio-visualizer');
      visualizer?.classList.add('active');
      
    } catch (error) {
      console.error('Failed to start recording:', error);
      this.updateStatus('error', 'Failed to start recording');
    }
  }

  /**
   * Stop recording and process
   */
  private async stopRecording(): Promise<void> {
    try {
      const audioBlob = await this.aiController.stopRecording();
      
      this.state.audio.isRecording = false;
      this.state.audio.currentBlob = audioBlob;
      
      // Reset recording button
      const recordBtn = document.getElementById('record-btn');
      const recordText = document.getElementById('record-text');
      
      if (recordBtn && recordText) {
        recordBtn.classList.remove('btn-danger');
        recordBtn.classList.add('btn-primary');
        recordText.innerHTML = '<i class="fas fa-microphone mr-2"></i>Start Recording';
      }

      // Remove recording animation
      const visualizer = document.getElementById('audio-visualizer');
      visualizer?.classList.remove('active');
      
      // Process the audio
      await this.processAudio(audioBlob);
      
    } catch (error) {
      console.error('Failed to stop recording:', error);
      this.updateStatus('error', 'Failed to stop recording');
    }
  }

  /**
   * Process audio through pipeline
   */
  private async processAudio(audioBlob: Blob): Promise<void> {
    try {
      this.state.isProcessing = true;
      this.updateStatus('processing', 'Processing audio...');
      this.showProgress();

      // Get filter settings from UI
      const silentThinkingCheckbox = document.getElementById('silent-thinking') as HTMLInputElement;
      const markdownFilterCheckbox = document.getElementById('markdown-filter') as HTMLInputElement;
      const streamingModeCheckbox = document.getElementById('streaming-mode') as HTMLInputElement;
      const voiceSelector = document.getElementById('voice-selector') as HTMLSelectElement;

      const pipelineOptions = {
        silentThinking: silentThinkingCheckbox?.checked ?? true,
        markdownFilter: markdownFilterCheckbox?.checked ?? true,
        streamingMode: streamingModeCheckbox?.checked ?? true,
        ttsOptions: {
          voice: voiceSelector?.value || 'af_bella'
        }
      };

      console.log('Processing audio with options:', pipelineOptions);

      // Check if streaming mode is enabled
      if (pipelineOptions.streamingMode) {
        console.log('Using streaming mode for audio processing');
        await this.processAudioWithStreaming(audioBlob, pipelineOptions);
      } else {
        console.log('Using traditional mode for audio processing');
        const result = await this.aiController.processRecordedAudio(audioBlob, pipelineOptions);
        this.displayResults(result);
      }
      
      this.state.isProcessing = false;
      this.updateStatus('success', 'Processing complete');
      this.hideProgress();
      
      // Auto-switch to TTS tab to show results
      this.switchTab('tts');
      
    } catch (error) {
      console.error('Failed to process audio:', error);
      this.state.isProcessing = false;
      this.updateStatus('error', `Processing failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
      this.hideProgress();
    }
  }

  /**
   * Process audio with real-time streaming playback
   */
  private async processAudioWithStreaming(audioBlob: Blob, pipelineOptions: any): Promise<void> {
    // Create and show streaming modal
    const streamingModal = this.createStreamingModal();
    const streamingQueue = new StreamingAudioQueue({
      maxQueueSize: 12, // Increased from 3 to 12 to handle large chunks
      sampleRate: 22050,
      onChunkStart: (chunkIndex, text) => {
        this.updateStreamingProgress(chunkIndex, text, 'playing');
      },
      onChunkEnd: (chunkIndex) => {
        console.log(`Finished playing chunk ${chunkIndex}`);
      },
      onQueueUpdate: (queueSize) => {
        this.updateQueueStatus(queueSize);
      },
      onComplete: () => {
        console.log('All streaming audio completed');
        this.closeStreamingModal(streamingModal);
      }
    });

    await streamingQueue.initialize();

    try {
      // Process through pipeline with custom streaming callback
      const result = await this.processWithStreamingCallback(audioBlob, pipelineOptions, streamingQueue);
      
      // Update text displays
      this.displayResults(result);
      
      // Mark streaming as complete
      streamingQueue.complete();
      
    } catch (error) {
      console.error('Streaming audio processing failed:', error);
      streamingQueue.cleanup();
      this.closeStreamingModal(streamingModal);
      throw error;
    }
  }

  /**
   * Process audio with custom streaming callback for real-time playback
   */
  private async processWithStreamingCallback(
    audioBlob: Blob, 
    pipelineOptions: any, 
    streamingQueue: StreamingAudioQueue
  ): Promise<PipelineResult> {
    // For now, we'll use the existing pipeline but could be enhanced to integrate better
    // with the TTS streaming in the future
    const result = await this.aiController.processRecordedAudio(audioBlob, pipelineOptions);
    
    // If we have audio result, split it into chunks and queue for streaming
    if (result.audio && result.audio.byteLength > 0) {
      const chunks = this.splitAudioIntoChunks(result.audio, result.response);
      
      // Queue chunks for streaming playback
      for (let i = 0; i < chunks.length; i++) {
        const success = await streamingQueue.addChunk(
          chunks[i].audio, 
          chunks[i].text, 
          i
        );
        
        if (!success) {
          console.warn(`Failed to queue chunk ${i}, queue may be full`);
          // Wait a bit and try again
          await new Promise(resolve => setTimeout(resolve, 100));
          await streamingQueue.addChunk(chunks[i].audio, chunks[i].text, i);
        }
        
        // Small delay between chunks to prevent overwhelming the queue
        await new Promise(resolve => setTimeout(resolve, 50));
      }
    }
    
    return result;
  }

  /**
   * Split audio buffer into smaller chunks for streaming
   */
  private splitAudioIntoChunks(audioBuffer: ArrayBuffer, text: string): Array<{audio: ArrayBuffer, text: string}> {
    const chunkSize = 8192; // Adjust based on needs
    const chunks: Array<{audio: ArrayBuffer, text: string}> = [];
    
    // Split text into sentences/phrases
    const textChunks = text.match(/[^.!?]+[.!?]*\s*/g) || [text];
    
    // Calculate audio bytes per text chunk
    const bytesPerChunk = Math.ceil(audioBuffer.byteLength / textChunks.length);
    
    for (let i = 0; i < textChunks.length; i++) {
      const start = i * bytesPerChunk;
      const end = Math.min(start + bytesPerChunk, audioBuffer.byteLength);
      const chunkAudio = audioBuffer.slice(start, end);
      
      chunks.push({
        audio: chunkAudio,
        text: textChunks[i].trim()
      });
    }
    
    return chunks;
  }

  /**
   * Create streaming modal for real-time audio playback
   */
  private createStreamingModal(): HTMLElement {
    const modal = document.createElement('div');
    modal.id = 'streaming-modal';
    modal.style.cssText = `
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0, 0, 0, 0.7);
      z-index: 10000;
      display: flex;
      align-items: center;
      justify-content: center;
      backdrop-filter: blur(4px);
    `;

    const content = document.createElement('div');
    content.style.cssText = `
      background: white;
      border-radius: 16px;
      padding: 32px;
      max-width: 500px;
      width: 90%;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
      position: relative;
    `;

    content.innerHTML = `
      <div class="text-center">
        <h3 class="text-xl font-bold text-gray-800 mb-4">
          <i class="fas fa-stream text-purple-600 mr-2"></i>
          Streaming Audio
        </h3>
        
        <div class="bg-purple-50 p-4 rounded-lg mb-6">
          <div class="flex items-center justify-between mb-3">
            <span class="text-sm font-medium text-purple-700">Current Chunk</span>
            <span id="streaming-modal-chunk" class="text-sm text-purple-600">Preparing...</span>
          </div>
          
          <div class="bg-purple-200 rounded-full h-3 mb-3">
            <div id="streaming-modal-progress" class="bg-purple-600 h-3 rounded-full transition-all duration-300" style="width: 0%"></div>
          </div>
          
          <div class="flex items-center justify-between text-xs text-purple-600">
            <span id="streaming-modal-text">Initializing streaming...</span>
            <span id="streaming-modal-queue">Queue: 0</span>
          </div>
        </div>
        
        <div class="flex items-center justify-center space-x-4 text-sm text-gray-600">
          <div class="flex items-center">
            <div class="w-3 h-3 bg-green-500 rounded-full mr-2 animate-pulse"></div>
            Real-time playback active
          </div>
        </div>
        
        <button id="streaming-modal-close" class="mt-6 px-4 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 transition-colors text-sm">
          Close (audio will continue)
        </button>
      </div>
    `;

    modal.appendChild(content);
    document.body.appendChild(modal);

    // Bind close button
    const closeBtn = content.querySelector('#streaming-modal-close');
    closeBtn?.addEventListener('click', () => {
      this.closeStreamingModal(modal);
    });

    // Close on escape key
    const handleKeydown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        this.closeStreamingModal(modal);
        document.removeEventListener('keydown', handleKeydown);
      }
    };
    document.addEventListener('keydown', handleKeydown);

    return modal;
  }

  /**
   * Update streaming progress in the modal
   */
  private updateStreamingProgress(chunkIndex: number, text: string, status: 'playing' | 'queued'): void {
    const modal = document.getElementById('streaming-modal');
    if (!modal) return;

    const chunkElement = modal.querySelector('#streaming-modal-chunk');
    const textElement = modal.querySelector('#streaming-modal-text');
    const progressElement = modal.querySelector('#streaming-modal-progress') as HTMLElement;

    if (chunkElement) {
      chunkElement.textContent = `Chunk ${chunkIndex + 1} ${status}`;
    }

    if (textElement) {
      textElement.textContent = text.length > 60 ? text.substring(0, 60) + '...' : text;
    }

    if (progressElement && status === 'playing') {
      const progress = Math.min(100, (chunkIndex + 1) * 20); // Rough progress estimate
      progressElement.style.width = `${progress}%`;
    }
  }

  /**
   * Update queue status in streaming modal
   */
  private updateQueueStatus(queueSize: number): void {
    const modal = document.getElementById('streaming-modal');
    if (!modal) return;

    const queueElement = modal.querySelector('#streaming-modal-queue');
    if (queueElement) {
      queueElement.textContent = `Queue: ${queueSize}`;
    }
  }

  /**
   * Close streaming modal
   */
  private closeStreamingModal(modal: HTMLElement): void {
    if (document.body.contains(modal)) {
      modal.style.opacity = '0';
      modal.style.transform = 'scale(0.95)';
      setTimeout(() => {
        if (document.body.contains(modal)) {
          document.body.removeChild(modal);
        }
      }, 200);
    }
  }

  /**
   * Display pipeline results
   */
  private displayResults(result: PipelineResult): void {
    // Update transcript
    const transcriptElement = document.getElementById('transcript') as HTMLTextAreaElement;
    if (transcriptElement) {
      transcriptElement.value = result.transcript;
    }

    // Update response
    const responseElement = document.getElementById('response-text') as HTMLTextAreaElement;
    if (responseElement) {
      responseElement.value = result.response;
    }

    // Setup audio playback
    this.setupAudioPlayback(result.audio);
  }

  /**
   * Setup audio playback
   */
  private async setupAudioPlayback(audioBuffer: ArrayBuffer): Promise<void> {
    const audioElement = document.getElementById('response-audio') as HTMLAudioElement;
    const playButton = document.getElementById('play-audio') as HTMLButtonElement;
    const downloadButton = document.getElementById('download-audio') as HTMLButtonElement;
    
    if (audioElement && playButton && downloadButton) {
      let finalAudioBuffer = audioBuffer;
      
      // Check if Silent Thinking is enabled and regenerate TTS if needed
      const silentThinkingCheckbox = document.getElementById('silent-thinking') as HTMLInputElement;
      const silentThinkingEnabled = silentThinkingCheckbox?.checked ?? true;
      const responseElement = document.getElementById('response-text') as HTMLTextAreaElement;
      
      if (silentThinkingEnabled && responseElement?.value) {
        const originalText = responseElement.value;
        const filteredText = this.getTextForTTS(originalText);
        
        // Only regenerate if filtering actually changed the text
        if (filteredText !== originalText && filteredText.trim()) {
          try {
            console.log('Regenerating TTS with filtered text (Silent Thinking enabled)');
            
            // Get current voice settings
            const voiceSelector = document.getElementById('voice-selector') as HTMLSelectElement;
            const voiceId = voiceSelector.value;
            
            // Generate new TTS with filtered text
            const ttsResult = await this.aiController.previewTTSVoice(voiceId, filteredText);
            finalAudioBuffer = ttsResult.audio;
            
            console.log('TTS regenerated with filtered text');
          } catch (error) {
            console.warn('Failed to regenerate TTS with filtered text:', error);
            // Fall back to original audio
          }
        }
      }
      
      // Create blob URL for audio
      const audioBlob = new Blob([finalAudioBuffer], { type: 'audio/wav' });
      const audioUrl = URL.createObjectURL(audioBlob);
      
      audioElement.src = audioUrl;
      audioElement.classList.remove('hidden');
      
      playButton.disabled = false;
      downloadButton.disabled = false;
      
      // Add auto-download when response audio starts playing
      audioElement.addEventListener('playing', async () => {
        console.log('Response audio is playing');
        
        // Trigger auto-download when response audio starts playing
        try {
          const timestamp = Date.now();
          const filename = `speech/response_audio_${timestamp}.wav`;
          await this.aiController.autoDownloadResponseAudio(filename, finalAudioBuffer);
        } catch (downloadError) {
          console.warn('Failed to auto-download response audio:', downloadError);
        }
      });
      
      this.state.audio.outputAudio = audioElement;
    }
  }

  /**
   * Update service status
   */
  private async updateServiceStatus(): Promise<void> {
    const status = this.aiController.getServiceStatus();
    
    // Update memory display
    const currentMemoryElement = document.getElementById('current-memory');
    const availableMemoryElement = document.getElementById('available-memory');
    
    if (currentMemoryElement && availableMemoryElement) {
      currentMemoryElement.textContent = `${status.memory.used}MB`;
      availableMemoryElement.textContent = `${status.memory.available}MB`;
      
      // Update memory indicator color
      const memoryContainer = currentMemoryElement.closest('.bg-blue-50');
      if (memoryContainer) {
        memoryContainer.className = status.memory.warning 
          ? 'bg-yellow-50 p-4 rounded-lg' 
          : status.memory.critical 
          ? 'bg-red-50 p-4 rounded-lg'
          : 'bg-blue-50 p-4 rounded-lg';
      }
    }

    // Update connection status
    const serverStatusElement = document.getElementById('server-status');
    const currentModelElement = document.getElementById('current-model');
    
    if (serverStatusElement && currentModelElement) {
      serverStatusElement.textContent = status.ollama.connected ? 'Connected' : 'Disconnected';
      serverStatusElement.className = status.ollama.connected ? 'text-green-600' : 'text-red-600';
      currentModelElement.textContent = status.ollama.model || 'None';
    }

    // Update available models dropdown if connected
    if (status.ollama.connected) {
      await this.populateOllamaModels();
    }
  }

  /**
   * Populate Ollama models dropdown with actual available models
   */
  private async populateOllamaModels(): Promise<void> {
    try {
      const modelSelector = document.getElementById('model-selector') as HTMLSelectElement;
      if (!modelSelector) return;

      // Get available models from Ollama
      const availableModels = await this.aiController.getAvailableOllamaModels();
      
      // Clear existing options
      modelSelector.innerHTML = '';
      
      if (availableModels.length === 0) {
        modelSelector.innerHTML = '<option value="">No models found</option>';
        return;
      }

      // Add each available model
      availableModels.forEach(model => {
        const option = document.createElement('option');
        option.value = model.name;
        
        // Format the display text with size info if available
        const sizeInfo = model.size ? ` (${this.formatBytes(model.size)})` : '';
        option.textContent = `${model.name}${sizeInfo}`;
        
        modelSelector.appendChild(option);
      });

      // Update state - store simplified model info for display
      this.state.ollama.availableModels = availableModels.map(model => ({
        name: model.name,
        size: model.size || 0,
        digest: '',
        modified_at: new Date().toISOString()
      }));
      
      console.log(`Found ${availableModels.length} Ollama models`);
    } catch (error) {
      console.error('Failed to populate Ollama models:', error);
      
      // Fallback to showing an error message
      const modelSelector = document.getElementById('model-selector') as HTMLSelectElement;
      if (modelSelector) {
        modelSelector.innerHTML = '<option value="">Error loading models</option>';
      }
    }
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
   * Update UI state
   */
  private updateUI(): void {
    this.updateServiceStatus();
    this.updateStatus(this.state.status.type, this.state.status.message);
    this.updateTrafficLights();
  }

  /**
   * Update status indicator
   */
  private updateStatus(type: AppStatus['type'], message: string): void {
    const statusIndicator = document.getElementById('status-indicator');
    const statusText = document.getElementById('status-text');
    
    if (statusIndicator && statusText) {
      statusText.textContent = message;
      
      // Update status indicator styling
      statusIndicator.className = 'status-indicator px-3 py-1 rounded-full text-sm font-medium flex items-center';
      
      switch (type) {
        case 'success':
          statusIndicator.classList.add('bg-green-100', 'text-green-800');
          break;
        case 'error':
          statusIndicator.classList.add('bg-red-100', 'text-red-800');
          break;
        case 'recording':
          statusIndicator.classList.add('bg-red-100', 'text-red-800');
          break;
        case 'processing':
          statusIndicator.classList.add('bg-blue-100', 'text-blue-800');
          break;
        default:
          statusIndicator.classList.add('bg-green-100', 'text-green-800');
      }
    }

    this.state.status = { type, message };
  }

  /**
   * Show progress indicator
   */
  private showProgress(): void {
    const progressContainer = document.getElementById('progress-container');
    progressContainer?.classList.remove('hidden');
  }

  /**
   * Hide progress indicator
   */
  private hideProgress(): void {
    const progressContainer = document.getElementById('progress-container');
    progressContainer?.classList.add('hidden');
  }

  /**
   * Update progress indicator with stage and percentage
   */
  private updateProgressIndicator(stage: string, progress: number): void {
    const progressStage = document.getElementById('progress-stage');
    const progressPercent = document.getElementById('progress-percent');
    const progressFill = document.getElementById('progress-fill');

    if (progressStage) {
      progressStage.textContent = stage;
    }

    if (progressPercent) {
      progressPercent.textContent = `${Math.round(progress)}%`;
    }

    if (progressFill) {
      progressFill.style.width = `${Math.round(progress)}%`;
    }
  }

  /**
   * Update Whisper model
   */
  private async updateWhisperModel(modelSize: WhisperModelSize): Promise<void> {
    try {
      this.updateStatus('loading', `Loading ${modelSize} model...`);
      this.updateTrafficLights();
      await this.aiController.setWhisperModel(modelSize);
      this.state.whisper.currentModel = modelSize;
      this.updateStatus('idle', 'Ready');
      await this.updateServiceStatus();
      this.updateTrafficLights();
    } catch (error) {
      console.error('Failed to update Whisper model:', error);
      this.updateStatus('error', 'Failed to load model');
      this.updateTrafficLights();
    }
  }

  /**
   * Update Ollama model
   */
  private async updateOllamaModel(modelName: string): Promise<void> {
    try {
      this.updateStatus('loading', `Loading ${modelName}...`);
      this.updateTrafficLights();
      await this.aiController.setOllamaModel(modelName);
      this.state.ollama.currentModel = modelName;
      this.updateStatus('idle', 'Ready');
      await this.updateServiceStatus();
      this.updateTrafficLights();
    } catch (error) {
      console.error('Failed to update Ollama model:', error);
      this.updateStatus('error', 'Failed to load model');
      this.updateTrafficLights();
    }
  }

  /**
   * Update Ollama configuration
   */
  private updateOllamaConfig(config: Partial<OllamaConfig>): void {
    this.aiController.updateOllamaConfig(config);
    this.state.ollama.config = { ...this.state.ollama.config, ...config };
  }

  /**
   * Update TTS configuration
   */
  private updateTTSConfig(config: Partial<TTSConfig>): void {
    this.aiController.updateTTSConfig(config);
    this.state.tts.config = { ...this.state.tts.config, ...config };
  }

  /**
   * Test Ollama connection
   */
  private async testConnection(): Promise<void> {
    try {
      this.updateStatus('loading', 'Testing connection...');
      const results = await this.aiController.testServices();
      
      if (results.ollama) {
        this.updateStatus('success', 'Connection successful');
      } else {
        this.updateStatus('error', 'Connection failed');
      }
      
      await this.updateServiceStatus();
    } catch (error) {
      console.error('Connection test failed:', error);
      this.updateStatus('error', 'Connection test failed');
    }
  }

  /**
   * Preview selected voice
   */
  private async previewVoice(): Promise<void> {
    try {
      const voiceSelector = document.getElementById('voice-selector') as HTMLSelectElement;
      const voiceId = voiceSelector.value;
      
      this.updateStatus('loading', 'Generating voice preview...');
      this.showProgress();
      
      // Set up progress callback for TTS
      this.aiController.setTTSProgressCallback((stage: string, progress: number) => {
        this.updateProgressIndicator(stage, progress);
      });
      
      // Generate preview audio using the TTS service
      const previewText = "Hello, this is a preview of the selected voice. You can use this to test how your responses will sound.";
      const result = await this.aiController.previewTTSVoice(voiceId, previewText);
      
      // Clear progress callback
      this.aiController.clearTTSProgressCallback();
      this.hideProgress();
      
      console.log('Preview audio result:', {
        audioSize: result.audio.byteLength,
        duration: result.duration,
        sampleRate: result.sampleRate
      });
      
      // Log voice preview to logging service
      try {
        await this.aiController.logVoicePreview(voiceId, result.audio);
      } catch (logError) {
        console.warn('Failed to log voice preview:', logError);
      }
      
      // Create temporary audio element for preview
      const tempAudio = document.createElement('audio');
      
      // Create blob with proper WAV MIME type
      const audioBlob = new Blob([result.audio], { type: 'audio/wav' });
      console.log('Audio blob created:', {
        size: audioBlob.size,
        type: audioBlob.type
      });
      
      const audioUrl = URL.createObjectURL(audioBlob);
      console.log('Audio URL created:', audioUrl);
      
      // Create a proper audio preview modal
      const audioPreview = this.createAudioPreviewModal(audioUrl, tempAudio);
      
      // Add audio element to the modal
      tempAudio.src = audioUrl;
      tempAudio.controls = true;
      tempAudio.preload = 'auto';
      
      // Add event listeners for debugging and auto-download
      tempAudio.addEventListener('loadstart', () => console.log('Audio loading started'));
      tempAudio.addEventListener('loadeddata', () => console.log('Audio data loaded'));
      tempAudio.addEventListener('canplay', () => console.log('Audio can play'));
      tempAudio.addEventListener('playing', async () => {
        console.log('Audio is playing');
        
        // Trigger auto-download when audio starts playing
        try {
          const timestamp = Date.now();
          const filename = `speech/preview_voice_${voiceId}_${timestamp}.wav`;
          await this.aiController.autoDownloadVoicePreview(filename, result.audio);
        } catch (downloadError) {
          console.warn('Failed to auto-download voice preview:', downloadError);
        }
      });
      tempAudio.addEventListener('error', (e) => {
        console.error('Audio playback error:', e);
        console.error('Audio error details:', tempAudio.error);
      });
      
      // Try to play with user interaction handling
      try {
        console.log('Attempting to play audio...');
        const playPromise = tempAudio.play();
        
        if (playPromise !== undefined) {
          await playPromise;
          console.log('Audio playback started successfully');
        }
      } catch (playError) {
        console.error('Autoplay failed, requiring user interaction:', playError);
        console.log('Manual controls available in preview modal');
      }
      
      // Only cleanup when audio ends or user closes the modal
      let isCleanedUp = false;
      const cleanup = () => {
        if (isCleanedUp) return;
        isCleanedUp = true;
        
        if (document.body.contains(audioPreview)) {
          document.body.removeChild(audioPreview);
        }
        URL.revokeObjectURL(audioUrl);
        console.log('Audio preview cleaned up');
      };
      
      // Cleanup only on audio end or manual close
      tempAudio.addEventListener('ended', cleanup);
      
      // Auto cleanup after 2 minutes to prevent memory leaks
      setTimeout(cleanup, 120000);
      
      this.updateStatus('success', 'Voice preview ready - check audio player');
      
      // Show voice characteristics
      const voiceInfo = this.aiController.getTTSVoiceInfo(voiceId);
      if (voiceInfo) {
        console.log(`Voice: ${voiceInfo.description} (${voiceInfo.gender}, ${voiceInfo.accent})`);
      }
      
    } catch (error) {
      console.error('Voice preview failed:', error);
      this.updateStatus('error', `Voice preview failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Play response audio
   */
  private playResponseAudio(): void {
    const audioElement = this.state.audio.outputAudio;
    if (audioElement) {
      audioElement.play();
    }
  }

  /**
   * Download response audio
   */
  private downloadResponseAudio(): void {
    const audioElement = this.state.audio.outputAudio;
    if (audioElement && audioElement.src) {
      const link = document.createElement('a');
      link.href = audioElement.src;
      link.download = 'speak2me-response.wav';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    }
  }

  /**
   * Get current application state
   */
  getState(): AppState {
    return { ...this.state };
  }

  /**
   * Determine traffic light status for each service
   */
  private getTrafficLightStatus(): ServiceTrafficLights {
    const status = this.aiController.getServiceStatus();
    
    // Whisper traffic light logic
    let whisperStatus: TrafficLightStatus = 'red';
    if (this.state.status.type === 'loading' && this.state.status.message.includes('model')) {
      whisperStatus = 'orange';
    } else if (status.whisper.loaded) {
      whisperStatus = 'green';
    }

    // Ollama traffic light logic  
    let ollamaStatus: TrafficLightStatus = 'red';
    if (this.state.status.type === 'loading' && this.state.status.message.includes('Loading')) {
      ollamaStatus = 'orange';
    } else if (status.ollama.connected && status.ollama.model) {
      ollamaStatus = 'green';
    }

    // TTS traffic light logic
    let ttsStatus: TrafficLightStatus = 'red';
    if (this.state.status.type === 'loading' && this.state.status.message.includes('voice')) {
      ttsStatus = 'orange';
    } else if (status.tts.loaded) {
      ttsStatus = 'green';
    }

    return {
      whisper: whisperStatus,
      ollama: ollamaStatus,
      tts: ttsStatus
    };
  }

  /**
   * Update traffic light indicators in the UI
   */
  private updateTrafficLights(): void {
    const lights = this.getTrafficLightStatus();
    
    this.updateTrafficLight('whisper-traffic-light', lights.whisper, this.getTrafficLightTooltip('whisper', lights.whisper));
    this.updateTrafficLight('ollama-traffic-light', lights.ollama, this.getTrafficLightTooltip('ollama', lights.ollama));
    this.updateTrafficLight('tts-traffic-light', lights.tts, this.getTrafficLightTooltip('tts', lights.tts));
  }

  /**
   * Update individual traffic light
   */
  private updateTrafficLight(elementId: string, status: TrafficLightStatus, tooltip: string): void {
    const element = document.getElementById(elementId);
    if (!element) return;

    // Remove all traffic light classes
    element.classList.remove('traffic-light-red', 'traffic-light-orange', 'traffic-light-green');
    
    // Add the appropriate class
    element.classList.add(`traffic-light-${status}`);
    
    // Update tooltip
    element.setAttribute('title', tooltip);
  }

  /**
   * Get tooltip text for traffic light
   */
  private getTrafficLightTooltip(service: 'whisper' | 'ollama' | 'tts', status: TrafficLightStatus): string {
    const serviceNames = {
      whisper: 'Whisper',
      ollama: 'Ollama', 
      tts: 'TTS'
    };

    const statusMessages = {
      red: 'unavailable',
      orange: 'loading...',
      green: 'ready'
    };

    return `${serviceNames[service]}: ${statusMessages[status]}`;
  }

  /**
   * Create audio preview modal
   */
  private createAudioPreviewModal(audioUrl: string, audioElement: HTMLAudioElement): HTMLElement {
    // Create modal overlay
    const modal = document.createElement('div');
    modal.style.cssText = `
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0, 0, 0, 0.5);
      z-index: 10000;
      display: flex;
      align-items: center;
      justify-content: center;
      backdrop-filter: blur(2px);
    `;

    // Create modal content
    const modalContent = document.createElement('div');
    modalContent.style.cssText = `
      background: white;
      border-radius: 12px;
      padding: 24px;
      max-width: 400px;
      width: 90%;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
      position: relative;
    `;

    // Create close button
    const closeButton = document.createElement('button');
    closeButton.innerHTML = '×';
    closeButton.style.cssText = `
      position: absolute;
      top: 12px;
      right: 16px;
      background: none;
      border: none;
      font-size: 24px;
      color: #6b7280;
      cursor: pointer;
      width: 32px;
      height: 32px;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 6px;
      transition: all 0.2s;
    `;
    closeButton.addEventListener('mouseenter', () => {
      closeButton.style.background = '#f3f4f6';
      closeButton.style.color = '#374151';
    });
    closeButton.addEventListener('mouseleave', () => {
      closeButton.style.background = 'none';
      closeButton.style.color = '#6b7280';
    });

    // Create title
    const title = document.createElement('h3');
    title.textContent = 'Voice Preview';
    title.style.cssText = `
      margin: 0 0 16px 0;
      font-size: 18px;
      font-weight: 600;
      color: #111827;
    `;

    // Create voice info
    const voiceSelector = document.getElementById('voice-selector') as HTMLSelectElement;
    const selectedOption = voiceSelector.options[voiceSelector.selectedIndex];
    const voiceInfo = document.createElement('p');
    voiceInfo.textContent = `Voice: ${selectedOption.text}`;
    voiceInfo.style.cssText = `
      margin: 0 0 16px 0;
      color: #6b7280;
      font-size: 14px;
    `;

    // Style the audio element
    audioElement.style.cssText = `
      width: 100%;
      margin-bottom: 16px;
      border-radius: 6px;
    `;

    // Create action buttons
    const buttonContainer = document.createElement('div');
    buttonContainer.style.cssText = `
      display: flex;
      gap: 8px;
      justify-content: flex-end;
    `;

    const downloadButton = document.createElement('button');
    downloadButton.innerHTML = '<i class="fas fa-download mr-2"></i>Download';
    downloadButton.style.cssText = `
      padding: 8px 16px;
      background: #6b7280;
      color: white;
      border: none;
      border-radius: 6px;
      cursor: pointer;
      font-size: 14px;
      transition: background 0.2s;
    `;
    downloadButton.addEventListener('mouseenter', () => {
      downloadButton.style.background = '#4b5563';
    });
    downloadButton.addEventListener('mouseleave', () => {
      downloadButton.style.background = '#6b7280';
    });
    downloadButton.addEventListener('click', () => {
      const link = document.createElement('a');
      link.href = audioUrl;
      link.download = 'voice-preview.wav';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    });

    // Close modal function
    const closeModal = () => {
      if (document.body.contains(modal)) {
        document.body.removeChild(modal);
      }
    };

    // Close button event
    closeButton.addEventListener('click', closeModal);

    // Close on overlay click
    modal.addEventListener('click', (e) => {
      if (e.target === modal) {
        closeModal();
      }
    });

    // Close on Escape key
    const handleKeydown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        closeModal();
        document.removeEventListener('keydown', handleKeydown);
      }
    };
    document.addEventListener('keydown', handleKeydown);

    // Assemble modal
    buttonContainer.appendChild(downloadButton);
    modalContent.appendChild(closeButton);
    modalContent.appendChild(title);
    modalContent.appendChild(voiceInfo);
    modalContent.appendChild(audioElement);
    modalContent.appendChild(buttonContainer);
    modal.appendChild(modalContent);

    // Add to DOM
    document.body.appendChild(modal);

    // Auto-close when audio ends
    audioElement.addEventListener('ended', () => {
      setTimeout(closeModal, 1000); // Give 1 second delay before auto-close
    });

    return modal;
  }

  /**
   * Toggle master auto-download setting
   */
  private toggleAutoDownload(enabled: boolean): void {
    if (enabled) {
      // Enable all auto-download types
      this.aiController.enableAutoDownload({
        voicePreviews: true,
        responseAudio: true,
        inputAudio: true,
        sessionSummaries: true
      });
      
      // Update all checkbox states
      const checkboxes = [
        'auto-download-previews',
        'auto-download-response', 
        'auto-download-input',
        'auto-download-summaries'
      ];
      
      checkboxes.forEach(id => {
        const checkbox = document.getElementById(id) as HTMLInputElement;
        if (checkbox) checkbox.checked = true;
      });
    } else {
      // Disable all auto-downloads
      this.aiController.disableAutoDownload();
      
      // Update all checkbox states
      const checkboxes = [
        'auto-download-previews',
        'auto-download-response',
        'auto-download-input', 
        'auto-download-summaries'
      ];
      
      checkboxes.forEach(id => {
        const checkbox = document.getElementById(id) as HTMLInputElement;
        if (checkbox) checkbox.checked = false;
      });
    }
    
    this.updateAutoDownloadStatus();
  }

  /**
   * Update specific auto-download type
   */
  private updateAutoDownloadType(
    type: 'voicePreviews' | 'responseAudio' | 'inputAudio' | 'sessionSummaries',
    enabled: boolean
  ): void {
    // Get current status
    const currentStatus = this.aiController.getAutoDownloadStatus();
    
    // Update the specific type
    const newTypes = {
      ...currentStatus.types,
      [type]: enabled
    };
    
    // Enable with updated types
    this.aiController.enableAutoDownload(newTypes);
    
    // Update master toggle based on individual settings
    const allEnabled = Object.values(newTypes).every(val => val);
    const masterToggle = document.getElementById('auto-download-master') as HTMLInputElement;
    if (masterToggle) {
      masterToggle.checked = allEnabled;
    }
    
    this.updateAutoDownloadStatus();
  }

  /**
   * Update auto-download status display
   */
  private updateAutoDownloadStatus(): void {
    const status = this.aiController.getAutoDownloadStatus();
    const statusElement = document.getElementById('auto-download-status');
    
    if (!statusElement) return;
    
    if (!status.enabled) {
      statusElement.textContent = 'Auto-downloads disabled';
      return;
    }
    
    const enabledTypes = Object.entries(status.types)
      .filter(([_, enabled]) => enabled)
      .map(([type, _]) => {
        switch (type) {
          case 'voicePreviews': return 'previews';
          case 'responseAudio': return 'responses';
          case 'inputAudio': return 'input';
          case 'sessionSummaries': return 'summaries';
          default: return type;
        }
      });
    
    if (enabledTypes.length === 4) {
      statusElement.textContent = 'All downloads enabled';
    } else if (enabledTypes.length === 0) {
      statusElement.textContent = 'No downloads enabled';
    } else {
      statusElement.textContent = `${enabledTypes.join(', ')} enabled`;
    }
  }

  /**
   * Filter out thinking tags from text
   */
  private filterThinkingTags(text: string): string {
    if (!text) return text;
    
    // Regular expression to match <think>...</think> blocks (including nested tags)
    // This handles multiple blocks and nested thinking tags
    const thinkRegex = /<think\b[^>]*>.*?<\/think>/gis;
    
    let filteredText = text.replace(thinkRegex, '');
    
    // Clean up any extra whitespace that might be left after removing thinking blocks
    filteredText = filteredText.replace(/\n\s*\n\s*\n/g, '\n\n'); // Remove multiple empty lines
    filteredText = filteredText.trim(); // Remove leading/trailing whitespace
    
    return filteredText;
  }

  /**
   * Update silent thinking status
   */
  private updateSilentThinkingStatus(enabled: boolean): void {
    const statusElement = document.getElementById('thinking-status');
    if (statusElement) {
      statusElement.textContent = enabled ? 'Filtering enabled' : 'Filtering disabled';
    }
    
    console.log(`Silent thinking ${enabled ? 'enabled' : 'disabled'}`);
  }

  /**
   * Get text for TTS generation (filtered or original)
   */
  private getTextForTTS(originalText: string): string {
    let processedText = originalText;
    
    // Step 1: Silent Thinking Filter (if enabled)
    const silentThinkingCheckbox = document.getElementById('silent-thinking') as HTMLInputElement;
    const silentThinkingEnabled = silentThinkingCheckbox?.checked ?? true;
    
    if (silentThinkingEnabled) {
      const thinkingFilteredText = this.filterThinkingTags(processedText);
      
      // Log the thinking filtering process for debugging
      if (thinkingFilteredText !== processedText) {
        console.log('Silent thinking filtering applied:');
        console.log('Original:', processedText.substring(0, 100) + '...');
        console.log('After thinking filter:', thinkingFilteredText.substring(0, 100) + '...');
      }
      
      processedText = thinkingFilteredText;
    }
    
    // Step 2: Markdown Filter (if enabled) - ALWAYS AFTER thinking filter
    const markdownFilterCheckbox = document.getElementById('markdown-filter') as HTMLInputElement;
    const markdownFilterEnabled = markdownFilterCheckbox?.checked ?? true;
    
    if (markdownFilterEnabled) {
      const markdownFilteredText = this.filterMarkdownSymbols(processedText);
      
      // Log the markdown filtering process for debugging
      if (markdownFilteredText !== processedText) {
        console.log('Markdown filtering applied:');
        console.log('Before markdown filter:', processedText.substring(0, 100) + '...');
        console.log('After markdown filter:', markdownFilteredText.substring(0, 100) + '...');
      }
      
      processedText = markdownFilteredText;
    }
    
    // If filtering results in empty text, return a default message
    if (!processedText.trim()) {
      return "I'm thinking about your request, but my response is still being processed.";
    }
    
    return processedText;
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
   * Update markdown filter status
   */
  private updateMarkdownFilterStatus(enabled: boolean): void {
    const statusElement = document.getElementById('markdown-status');
    if (statusElement) {
      statusElement.textContent = enabled ? 'Markdown filtering enabled' : 'Markdown filtering disabled';
    }
    
    console.log(`Markdown filter ${enabled ? 'enabled' : 'disabled'}`);
  }

  /**
   * Update streaming mode status
   */
  private updateStreamingModeStatus(enabled: boolean): void {
    const statusElement = document.getElementById('streaming-status');
    if (statusElement) {
      statusElement.textContent = enabled ? 'Real-time streaming enabled' : 'Real-time streaming disabled';
    }
    
    console.log(`Streaming mode ${enabled ? 'enabled' : 'disabled'}`);
    
    // Show/hide streaming progress based on mode
    const progressElement = document.getElementById('streaming-progress');
    if (progressElement && !enabled) {
      progressElement.classList.add('hidden');
    }
  }
}
