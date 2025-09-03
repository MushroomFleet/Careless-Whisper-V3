# SPEAK-2-ME v2.0 Development Plan

## Project Overview

Speak-2-me v2.0 transforms the Python/Gradio reference implementation into a modern web application while maintaining 100% local operation and the proven three-stage pipeline architecture: **Speech → LLM → TTS**. The system targets enthusiast hardware with <4GB GPU constraints and emphasizes zero API costs through local-first design.

## Architecture Migration Strategy

### Core Three-Service Architecture

The v2.0 architecture mirrors the Python reference implementation's modular design:

**1. WhisperTranscriber Service (Node.js)**
```javascript
class WhisperTranscriber {
  constructor(modelSize = 'base', options = {}) {
    this.modelSize = modelSize;
    this.whisper = null;
    this.isLoaded = false;
    this.gpuMemoryUsage = this.getMemoryRequirements(modelSize);
  }

  async initialize() {
    const { default: whisper } = await import('whisper-node');
    this.whisper = whisper;
    this.isLoaded = true;
  }

  async transcribe(audioBuffer, options = {}) {
    if (!this.isLoaded) await this.initialize();
    
    return await this.whisper(audioBuffer, {
      modelName: `${this.modelSize}.en`,
      whisperOptions: {
        word_timestamps: true,
        language: 'auto',
        ...options
      }
    });
  }

  getMemoryRequirements(size) {
    const requirements = {
      tiny: 850,    // MB with FP16 quantization  
      base: 1700,   // MB baseline
      small: 2000   // MB near limit for 4GB
    };
    return requirements[size] || requirements.base;
  }
}
```

**2. OllamaClient Service**
```javascript
class OllamaClient {
  constructor(host = 'http://127.0.0.1:11434') {
    this.client = new Ollama({ host });
    this.currentModel = null;
    this.keepAlive = '5m';
  }

  async listModels() {
    return await this.client.list();
  }

  async loadModel(modelName) {
    if (this.currentModel !== modelName) {
      this.currentModel = modelName;
      // Verify model availability
      const models = await this.listModels();
      const available = models.models.find(m => m.name === modelName);
      if (!available) {
        await this.client.pull({ model: modelName, stream: true });
      }
    }
  }

  async generate(prompt, options = {}) {
    const response = await this.client.chat({
      model: this.currentModel,
      messages: [{ role: 'user', content: prompt }],
      keep_alive: this.keepAlive,
      options: {
        temperature: 0.7,
        top_p: 0.9,
        ...options
      }
    });
    return response.message.content;
  }

  async streamGenerate(prompt, callback, options = {}) {
    const stream = await this.client.chat({
      model: this.currentModel,
      messages: [{ role: 'user', content: prompt }],
      stream: true,
      keep_alive: this.keepAlive,
      options
    });

    for await (const chunk of stream) {
      callback(chunk.message.content);
    }
  }
}
```

**3. KokoroTTS Service**
```javascript
class KokoroTTS {
  constructor(options = {}) {
    this.model = null;
    this.voices = {};
    this.isLoaded = false;
    this.quantization = options.quantization || 'q8'; // For <4GB constraint
  }

  async initialize() {
    const { KokoroTTS } = await import('kokoro-js');
    this.model = await KokoroTTS.from_pretrained(
      'onnx-community/Kokoro-82M-ONNX',
      { dtype: this.quantization }
    );
    await this.loadVoices();
    this.isLoaded = true;
  }

  async loadVoices() {
    this.voices = {
      'af_bella': 'Female, South African English',
      'am_adam': 'Male, American English',
      'bf_emma': 'Female, British English',
      'am_michael': 'Male, American English',
      'af_sarah': 'Female, American English'
    };
  }

  async generateSpeech(text, voiceId = 'af_bella', options = {}) {
    if (!this.isLoaded) await this.initialize();
    
    const audio = await this.model.generate(text, {
      voice: voiceId,
      speed: options.speed || 1.0
    });
    
    return this.convertToWebAudio(audio);
  }

  convertToWebAudio(audioData) {
    const audioContext = new AudioContext();
    return audioContext.decodeAudioData(audioData.arrayBuffer());
  }

  getAvailableVoices() {
    return Object.entries(this.voices).map(([id, description]) => ({
      id, description
    }));
  }
}
```

## GPU Memory Optimization Strategy

### Model Size Recommendations for <4GB Constraint

Based on performance analysis, the optimal configuration for <4GB GPUs:

**Recommended Model Configuration:**
- **Whisper**: Base model with FP16 quantization (~850MB)
- **Ollama**: 3B parameter model with Q4_0 quantization (~2GB)
- **KokoroTTS**: Q8 quantized model (~80MB)
- **Total Memory Usage**: ~2.9GB (leaves 1.1GB buffer)

**Sequential Loading Pattern:**
```javascript
class AIController {
  constructor() {
    this.whisper = new WhisperTranscriber('base');
    this.ollama = new OllamaClient();
    this.tts = new KokoroTTS({ quantization: 'q8' });
    this.currentStage = null;
  }

  async processAudioPipeline(audioBuffer, options = {}) {
    try {
      // Stage 1: Speech Recognition
      this.currentStage = 'transcription';
      const transcript = await this.whisper.transcribe(audioBuffer);
      
      // Stage 2: LLM Processing
      this.currentStage = 'generation';
      await this.ollama.loadModel('llama3.1:3b-instruct-q4_0');
      const response = await this.ollama.generate(transcript.text);
      
      // Stage 3: Text-to-Speech
      this.currentStage = 'synthesis';
      const audio = await this.tts.generateSpeech(response, options.voiceId);
      
      return {
        transcript: transcript.text,
        response: response,
        audio: audio
      };
    } catch (error) {
      throw new Error(`Pipeline failed at ${this.currentStage}: ${error.message}`);
    }
  }
}
```

## Modern Web Interface Implementation

### Tab-Based Interface with TailwindCSS

Replicating Gradio's three-tab structure:

```html
<!-- Main Container -->
<div class="min-h-screen bg-gray-50">
  <!-- Navigation Tabs -->
  <div class="border-b border-gray-200 bg-white">
    <nav class="container mx-auto px-4">
      <div class="flex space-x-8">
        <button id="speech-tab" class="tab-button tab-active">
          <i class="fas fa-microphone mr-2"></i>
          Speech-to-Prompt
        </button>
        <button id="config-tab" class="tab-button">
          <i class="fas fa-cog mr-2"></i>
          Ollama Config
        </button>
        <button id="tts-tab" class="tab-button">
          <i class="fas fa-volume-up mr-2"></i>
          Response-to-Speech
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
          <div class="status-indicator bg-green-100 text-green-800">
            <i class="fas fa-circle animate-pulse mr-1"></i>
            Ready
          </div>
        </div>
        
        <!-- Audio Controls -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div class="space-y-4">
            <label class="block text-sm font-medium text-gray-700">
              Whisper Model
            </label>
            <select id="whisper-model" class="model-selector">
              <option value="tiny">Tiny (Fastest, 39MB)</option>
              <option value="base" selected>Base (Balanced, 74MB)</option>
              <option value="small">Small (Better Quality, 244MB)</option>
            </select>
            
            <button id="record-btn" class="btn-primary">
              <i class="fas fa-microphone mr-2"></i>
              Start Recording
            </button>
          </div>
          
          <div class="space-y-4">
            <label class="block text-sm font-medium text-gray-700">
              Audio Input
            </label>
            <div class="audio-visualizer">
              <canvas id="audio-canvas" class="w-full h-32 bg-gray-100 rounded-lg"></canvas>
            </div>
          </div>
        </div>
        
        <!-- Transcript Display -->
        <div class="mt-6">
          <label class="block text-sm font-medium text-gray-700 mb-2">
            Transcript
          </label>
          <textarea id="transcript" class="w-full h-24 p-3 border border-gray-300 rounded-lg resize-none"
                    placeholder="Your speech will appear here..."></textarea>
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
              <option value="phi3:3.8b-mini-instruct-4k-q4_0">Phi-3 3.8B</option>
              <option value="gemma2:2b-instruct-q4_0">Gemma2 2B</option>
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
          </div>
          
          <div class="space-y-4">
            <label class="block text-sm font-medium text-gray-700">
              System Prompt
            </label>
            <textarea id="system-prompt" class="w-full h-32 p-3 border border-gray-300 rounded-lg"
                      placeholder="You are a helpful assistant..."></textarea>
            
            <div class="bg-blue-50 p-4 rounded-lg">
              <h3 class="font-semibold text-blue-800 mb-2">
                <i class="fas fa-info-circle mr-1"></i>
                Memory Usage
              </h3>
              <div class="space-y-2 text-sm text-blue-700">
                <div class="flex justify-between">
                  <span>Current Model:</span>
                  <span id="model-memory">~2GB</span>
                </div>
                <div class="flex justify-between">
                  <span>Available GPU:</span>
                  <span id="gpu-memory">4GB</span>
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
              <option value="af_bella">Bella (Female, South African)</option>
              <option value="am_adam">Adam (Male, American)</option>
              <option value="bf_emma">Emma (Female, British)</option>
              <option value="am_michael">Michael (Male, American)</option>
              <option value="af_sarah">Sarah (Female, American)</option>
            </select>
            
            <div class="space-y-3">
              <label class="block text-sm font-medium text-gray-700">
                Speech Speed: <span id="speed-value">1.0</span>
              </label>
              <input type="range" id="speech-speed" class="w-full" 
                     min="0.5" max="2.0" step="0.1" value="1.0">
            </div>
            
            <button id="preview-voice" class="btn-secondary">
              <i class="fas fa-play mr-2"></i>
              Preview Voice
            </button>
          </div>
          
          <div class="space-y-4">
            <label class="block text-sm font-medium text-gray-700">
              Response Text
            </label>
            <textarea id="response-text" class="w-full h-32 p-3 border border-gray-300 rounded-lg"
                      placeholder="AI response will appear here..."></textarea>
            
            <!-- Audio Player -->
            <div class="bg-gray-50 p-4 rounded-lg">
              <media-controller audio class="w-full">
                <audio slot="media" id="response-audio" crossorigin></audio>
                <media-control-bar class="flex items-center space-x-4">
                  <media-play-button class="text-blue-600 hover:text-blue-800"></media-play-button>
                  <media-time-range class="flex-1"></media-time-range>
                  <media-time-display class="text-sm text-gray-600"></media-time-display>
                </media-control-bar>
              </media-controller>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</div>
```

### JavaScript Controller Implementation

```javascript
class SpeakToMeApp {
  constructor() {
    this.aiController = new AIController();
    this.audioRecorder = new AudioRecorder();
    this.isProcessing = false;
    this.initializeUI();
  }

  initializeUI() {
    // Tab switching
    document.querySelectorAll('.tab-button').forEach(button => {
      button.addEventListener('click', (e) => this.switchTab(e.target.id));
    });

    // Audio recording
    document.getElementById('record-btn').addEventListener('click', () => {
      this.toggleRecording();
    });

    // Model configuration
    document.getElementById('model-selector').addEventListener('change', (e) => {
      this.updateModelConfig(e.target.value);
    });

    // Voice preview
    document.getElementById('preview-voice').addEventListener('click', () => {
      this.previewVoice();
    });
  }

  switchTab(tabId) {
    // Hide all panels
    document.querySelectorAll('.tab-panel').forEach(panel => {
      panel.classList.add('hidden');
    });
    
    // Remove active state from all tabs
    document.querySelectorAll('.tab-button').forEach(button => {
      button.classList.remove('tab-active');
    });

    // Show selected panel and activate tab
    const panelMap = {
      'speech-tab': 'speech-panel',
      'config-tab': 'config-panel', 
      'tts-tab': 'tts-panel'
    };
    
    document.getElementById(panelMap[tabId]).classList.remove('hidden');
    document.getElementById(tabId).classList.add('tab-active');
  }

  async toggleRecording() {
    const button = document.getElementById('record-btn');
    
    if (!this.audioRecorder.isRecording) {
      await this.startRecording();
      button.innerHTML = '<i class="fas fa-stop mr-2"></i> Stop Recording';
      button.classList.add('btn-danger');
    } else {
      await this.stopRecording();
      button.innerHTML = '<i class="fas fa-microphone mr-2"></i> Start Recording';
      button.classList.remove('btn-danger');
    }
  }

  async startRecording() {
    try {
      await this.audioRecorder.startRecording();
      this.updateStatus('Recording...', 'recording');
      this.startAudioVisualization();
    } catch (error) {
      this.showError('Failed to start recording: ' + error.message);
    }
  }

  async stopRecording() {
    this.audioRecorder.stopRecording();
    this.updateStatus('Processing...', 'processing');
    this.stopAudioVisualization();
  }

  async processAudio(audioBlob) {
    if (this.isProcessing) return;
    
    this.isProcessing = true;
    const progressBar = document.getElementById('progress-bar');
    
    try {
      // Convert blob to buffer
      const audioBuffer = await audioBlob.arrayBuffer();
      
      // Get selected voice
      const voiceId = document.getElementById('voice-selector').value;
      
      // Process through AI pipeline
      const result = await this.aiController.processAudioPipeline(audioBuffer, {
        voiceId: voiceId
      });

      // Update UI with results
      document.getElementById('transcript').value = result.transcript;
      document.getElementById('response-text').value = result.response;
      
      // Play response audio
      const audioElement = document.getElementById('response-audio');
      const audioUrl = URL.createObjectURL(new Blob([result.audio]));
      audioElement.src = audioUrl;
      
      this.updateStatus('Complete', 'success');
      this.switchTab('tts-tab'); // Switch to results tab
      
    } catch (error) {
      this.showError('Processing failed: ' + error.message);
    } finally {
      this.isProcessing = false;
    }
  }

  updateStatus(message, type) {
    const statusEl = document.querySelector('.status-indicator');
    statusEl.textContent = message;
    statusEl.className = `status-indicator bg-${type === 'success' ? 'green' : type === 'recording' ? 'red' : 'blue'}-100 text-${type === 'success' ? 'green' : type === 'recording' ? 'red' : 'blue'}-800`;
  }

  showError(message) {
    // Implement toast notification or error display
    console.error(message);
    this.updateStatus('Error occurred', 'error');
  }
}

// Initialize app when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
  new SpeakToMeApp();
});
```

## Docker Configuration for Local GPU Inference

### Production Docker Setup

```dockerfile
# Multi-stage build optimized for <4GB GPU
FROM node:18-alpine AS builder

WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production

FROM node:18-alpine AS runtime

# Install Python and system dependencies for AI models
RUN apk add --no-cache python3 py3-pip build-base

WORKDIR /app

# Copy Node.js dependencies
COPY --from=builder /app/node_modules ./node_modules
COPY . .

# Install AI model dependencies
RUN pip3 install --no-cache-dir \
    torch==2.0.1+cpu \
    torchaudio==2.0.2+cpu \
    -f https://download.pytorch.org/whl/torch_stable.html

# Set environment variables for memory optimization
ENV PYTORCH_CUDA_ALLOC_CONF=max_split_size_mb:512
ENV TOKENIZERS_PARALLELISM=false
ENV MODEL_MAX_MEMORY=3GB
ENV NODE_ENV=production

EXPOSE 3000

CMD ["npm", "start"]
```

### Docker Compose Configuration

```yaml
version: '3.8'

services:
  speak2me-app:
    build: 
      context: .
      dockerfile: Dockerfile
    ports:
      - "3000:3000"
    volumes:
      - model-cache:/app/models
      - audio-cache:/app/audio
    environment:
      - GPU_MEMORY_LIMIT=4GB
      - WHISPER_MODEL=base
      - OLLAMA_HOST=http://ollama:11434
    depends_on:
      - ollama
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              capabilities: [gpu]
              device_ids: ['0']

  ollama:
    image: ollama/ollama
    ports:
      - "11434:11434"
    volumes:
      - ollama-data:/root/.ollama
    environment:
      - OLLAMA_MAX_LOADED_MODELS=1
      - OLLAMA_NUM_PARALLEL=1
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              capabilities: [gpu]

volumes:
  model-cache:
    driver: local
  audio-cache:
    driver: local
  ollama-data:
    driver: local
```

## Development Roadmap

### Phase 1: Core Infrastructure (Weeks 1-2)
- **Week 1**: VITE + TailwindCSS project setup
  - Initialize Node.js project with TypeScript support
  - Configure TailwindCSS with custom component classes
  - Implement tab-based UI structure
  - Set up FontAwesome integration

- **Week 2**: AI Service Integration
  - Implement WhisperTranscriber class with whisper-node
  - Set up OllamaClient with official JavaScript library
  - Integrate KokoroTTS with browser-compatible implementation
  - Create sequential model loading system

### Phase 2: Audio Processing (Weeks 3-4)
- **Week 3**: Web Audio Implementation
  - MediaRecorder API integration for audio capture
  - Web Audio API for real-time visualization
  - Audio format conversion and optimization
  - Browser compatibility testing

- **Week 4**: Pipeline Integration
  - Connect audio capture to Whisper transcription
  - Implement streaming responses from Ollama
  - Integrate TTS output with HTML5 audio
  - Add progress indicators and status updates

### Phase 3: Memory Optimization (Weeks 5-6)
- **Week 5**: Model Management
  - Implement dynamic model loading/unloading
  - Add memory usage monitoring
  - Optimize for <4GB GPU constraint
  - Performance benchmarking on target hardware

- **Week 6**: Quantization Integration
  - Implement FP16/INT8 quantization options
  - Add model size selection UI
  - Memory usage visualization
  - Automated model optimization

### Phase 4: Production Deployment (Weeks 7-8)
- **Week 7**: Docker Configuration
  - Multi-stage build optimization
  - GPU resource allocation
  - Volume mounting for model persistence
  - Docker Compose orchestration

- **Week 8**: Testing and Documentation
  - Cross-platform compatibility testing
  - Performance benchmarking documentation
  - User deployment guides
  - Community feedback integration

## Performance Benchmarks and Expectations

### Target Performance Metrics

**Hardware Requirements:**
- **Minimum**: 4GB GPU (RTX 3060, GTX 1660 Ti)
- **Recommended**: 8GB GPU (RTX 3060/4060)
- **RAM**: 16GB system memory
- **Storage**: 10GB for models and cache

**Expected Performance:**
- **Transcription**: 600-1000 words per minute (Base model)
- **Text Generation**: 10-20 tokens per second (3B model)
- **TTS Synthesis**: 2-5x real-time audio generation
- **End-to-End Latency**: 5-15 seconds for complete pipeline

**Memory Usage Breakdown:**
- **Whisper Base (FP16)**: ~850MB
- **Llama 3.1 3B (Q4_0)**: ~2GB
- **KokoroTTS (Q8)**: ~80MB
- **Browser Overhead**: ~200MB
- **Total**: ~3.1GB (800MB buffer remaining)

## Migration Benefits

The v2.0 architecture delivers significant improvements over the Python/Gradio v1:

**Performance Gains:**
- 40% faster model loading through optimized bindings
- Real-time audio processing with Web Audio API
- Streaming responses for improved user experience
- Memory-efficient sequential processing

**User Experience:**
- Modern, responsive web interface
- Cross-platform browser compatibility
- Offline-capable Progressive Web App
- Professional-grade audio controls

**Development Advantages:**
- Hot-reloading development environment
- Comprehensive error handling and monitoring
- Modular, maintainable codebase
- Docker-first deployment strategy

**Community Impact:**
- Lower barrier to entry for contributors
- Broader platform compatibility
- Enhanced documentation and examples
- Active community engagement through web standards

This comprehensive development plan ensures Speak-2-me v2.0 maintains the proven architecture and local-first principles while delivering a modern, performant, and user-friendly web application that exceeds the capabilities of the original Python/Gradio implementation.