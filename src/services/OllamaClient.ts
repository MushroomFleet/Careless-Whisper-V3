import type { 
  OllamaModel, 
  OllamaGenerateOptions, 
  OllamaResponse, 
  OllamaMessage,
  OllamaConfig 
} from '@/types';

export class OllamaClient {
  private client: any = null;
  private currentModel: string | null = null;
  private keepAlive = '5m';
  private host: string;
  private isConnected = false;
  private availableModels: OllamaModel[] = [];
  private config: OllamaConfig;

  constructor(host = 'http://127.0.0.1:11434', config?: Partial<OllamaConfig>) {
    this.host = host;
    this.config = {
      temperature: 0.7,
      top_p: 0.9,
      systemPrompt: 'You are a helpful assistant.',
      maxTokens: 2048,
      keepAlive: '5m',
      ...config
    };
  }

  /**
   * Initialize the Ollama client
   */
  async initialize(): Promise<void> {
    if (this.isConnected) {
      return;
    }

    try {
      console.log(`Connecting to Ollama at ${this.host}...`);
      
      // Dynamic import of ollama
      const { Ollama } = await import('ollama');
      this.client = new Ollama({ host: this.host });
      
      // Test connection by listing models with timeout
      await this.testConnectionWithTimeout(5000); // 5 second timeout
      this.isConnected = true;
      
      console.log('Successfully connected to Ollama');
    } catch (error) {
      console.error('Failed to connect to Ollama:', error);
      this.isConnected = false;
      // Don't throw error, just log it - let the app continue without Ollama
      console.warn('Ollama not available, continuing without LLM functionality');
    }
  }

  /**
   * List available models
   */
  async listModels(): Promise<OllamaModel[]> {
    if (!this.isConnected) {
      await this.initialize();
    }

    try {
      const response = await this.client.list();
      this.availableModels = response.models || [];
      return this.availableModels;
    } catch (error) {
      console.error('Failed to list models:', error);
      throw new Error(`Failed to list models: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Refresh the list of available models
   */
  async refreshAvailableModels(): Promise<void> {
    await this.listModels();
  }

  /**
   * Load a specific model
   */
  async loadModel(modelName: string): Promise<void> {
    if (!this.isConnected) {
      await this.initialize();
    }

    if (this.currentModel === modelName) {
      console.log(`Model ${modelName} already loaded`);
      return;
    }

    try {
      console.log(`Loading model: ${modelName}`);
      
      // Check if model is available
      const models = await this.listModels();
      const available = models.find(m => m.name === modelName);
      
      if (!available) {
        console.log(`Model ${modelName} not found locally, pulling...`);
        await this.pullModel(modelName);
      }

      // Test the model by sending a simple request
      await this.client.chat({
        model: modelName,
        messages: [{ role: 'user', content: 'Hello' }],
        keep_alive: this.keepAlive,
        stream: false
      });

      this.currentModel = modelName;
      console.log(`Model ${modelName} loaded successfully`);
    } catch (error) {
      console.error(`Failed to load model ${modelName}:`, error);
      throw new Error(`Failed to load model ${modelName}: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Pull a model from the Ollama registry
   */
  async pullModel(modelName: string, onProgress?: (progress: number) => void): Promise<void> {
    if (!this.isConnected) {
      await this.initialize();
    }

    try {
      console.log(`Pulling model: ${modelName}`);
      
      const stream = await this.client.pull({ 
        model: modelName, 
        stream: true 
      });

      for await (const chunk of stream) {
        if (chunk.status) {
          console.log(`Pull status: ${chunk.status}`);
          
          if (chunk.completed && chunk.total && onProgress) {
            const progress = (chunk.completed / chunk.total) * 100;
            onProgress(progress);
          }
        }
      }

      console.log(`Model ${modelName} pulled successfully`);
      await this.refreshAvailableModels();
    } catch (error) {
      console.error(`Failed to pull model ${modelName}:`, error);
      throw new Error(`Failed to pull model ${modelName}: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Generate a response from the loaded model
   */
  async generate(
    prompt: string, 
    options: OllamaGenerateOptions = {}
  ): Promise<string> {
    if (!this.currentModel) {
      throw new Error('No model loaded. Call loadModel() first.');
    }

    try {
      console.log('Generating response...');
      const startTime = performance.now();

      const messages: OllamaMessage[] = [];
      
      // Add system prompt if configured
      if (this.config.systemPrompt) {
        messages.push({
          role: 'system',
          content: this.config.systemPrompt
        });
      }

      // Add user prompt
      messages.push({
        role: 'user',
        content: prompt
      });

      const response = await this.client.chat({
        model: this.currentModel,
        messages,
        keep_alive: this.keepAlive,
        stream: false,
        options: {
          temperature: options.temperature ?? this.config.temperature,
          top_p: options.top_p ?? this.config.top_p,
          top_k: options.top_k,
          repeat_penalty: options.repeat_penalty,
          seed: options.seed,
          num_predict: options.num_predict ?? this.config.maxTokens,
          stop: options.stop,
          ...options
        }
      });

      const endTime = performance.now();
      const duration = endTime - startTime;
      
      console.log(`Response generated in ${duration.toFixed(2)}ms`);
      
      return response.message.content;
    } catch (error) {
      console.error('Generation failed:', error);
      throw new Error(`Generation failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Generate a streaming response from the loaded model
   */
  async *streamGenerate(
    prompt: string, 
    options: OllamaGenerateOptions = {}
  ): AsyncGenerator<string> {
    if (!this.currentModel) {
      throw new Error('No model loaded. Call loadModel() first.');
    }

    try {
      console.log('Starting streaming generation...');

      const messages: OllamaMessage[] = [];
      
      // Add system prompt if configured
      if (this.config.systemPrompt) {
        messages.push({
          role: 'system',
          content: this.config.systemPrompt
        });
      }

      // Add user prompt
      messages.push({
        role: 'user',
        content: prompt
      });

      const stream = await this.client.chat({
        model: this.currentModel,
        messages,
        keep_alive: this.keepAlive,
        stream: true,
        options: {
          temperature: options.temperature ?? this.config.temperature,
          top_p: options.top_p ?? this.config.top_p,
          top_k: options.top_k,
          repeat_penalty: options.repeat_penalty,
          seed: options.seed,
          num_predict: options.num_predict ?? this.config.maxTokens,
          stop: options.stop,
          ...options
        }
      });

      for await (const chunk of stream) {
        if (chunk.message?.content) {
          yield chunk.message.content;
        }
      }

      console.log('Streaming generation completed');
    } catch (error) {
      console.error('Streaming generation failed:', error);
      throw new Error(`Streaming generation failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Update configuration
   */
  updateConfig(newConfig: Partial<OllamaConfig>): void {
    this.config = { ...this.config, ...newConfig };
    this.keepAlive = newConfig.keepAlive || this.keepAlive;
  }

  /**
   * Get current configuration
   */
  getConfig(): OllamaConfig {
    return { ...this.config };
  }

  /**
   * Get connection status
   */
  get connected(): boolean {
    return this.isConnected;
  }

  /**
   * Get currently loaded model
   */
  get loadedModel(): string | null {
    return this.currentModel;
  }

  /**
   * Get available models
   */
  get models(): OllamaModel[] {
    return [...this.availableModels];
  }

  /**
   * Get estimated memory usage for current model
   */
  getMemoryUsage(): number {
    if (!this.currentModel) {
      return 0;
    }

    // Estimate memory usage based on model name
    const model = this.availableModels.find(m => m.name === this.currentModel);
    if (model?.size) {
      return Math.round(model.size / (1024 * 1024)); // Convert to MB
    }

    // Fallback estimates based on model name patterns
    const name = this.currentModel.toLowerCase();
    if (name.includes('3b')) return 2000;
    if (name.includes('7b')) return 4000;
    if (name.includes('13b')) return 8000;
    if (name.includes('30b')) return 16000;
    if (name.includes('70b')) return 40000;
    
    return 2000; // Default estimate
  }

  /**
   * Get recommended models for <4GB GPU constraint
   */
  getRecommendedModels(): Array<{ name: string; description: string; memoryMB: number }> {
    return [
      {
        name: 'llama3.1:3b-instruct-q4_0',
        description: 'Llama 3.1 3B (Recommended for 4GB)',
        memoryMB: 2000
      },
      {
        name: 'phi3:3.8b-mini-instruct-4k-q4_0',
        description: 'Phi-3 3.8B Mini',
        memoryMB: 2400
      },
      {
        name: 'gemma2:2b-instruct-q4_0',
        description: 'Gemma2 2B (Fastest)',
        memoryMB: 1500
      },
      {
        name: 'qwen2:1.5b-instruct-q4_0',
        description: 'Qwen2 1.5B (Ultra-fast)',
        memoryMB: 1000
      }
    ];
  }

  /**
   * Test connection to Ollama server with timeout
   */
  private async testConnectionWithTimeout(timeoutMs: number): Promise<void> {
    return new Promise(async (resolve, reject) => {
      const timeout = setTimeout(() => {
        reject(new Error(`Connection timeout after ${timeoutMs}ms`));
      }, timeoutMs);

      try {
        const response = await this.client.list();
        this.availableModels = response.models || [];
        clearTimeout(timeout);
        resolve();
      } catch (error) {
        clearTimeout(timeout);
        reject(error);
      }
    });
  }

  /**
   * Test connection to Ollama server
   */
  async testConnection(): Promise<boolean> {
    try {
      await this.initialize();
      return this.isConnected;
    } catch {
      return false;
    }
  }

  /**
   * Disconnect from Ollama
   */
  disconnect(): void {
    this.client = null;
    this.isConnected = false;
    this.currentModel = null;
    this.availableModels = [];
    console.log('Disconnected from Ollama');
  }

  /**
   * Get performance statistics
   */
  getPerformanceStats(): {
    model: string | null;
    memoryUsage: number;
    connected: boolean;
    modelsAvailable: number;
  } {
    return {
      model: this.currentModel,
      memoryUsage: this.getMemoryUsage(),
      connected: this.isConnected,
      modelsAvailable: this.availableModels.length
    };
  }
}

// Default export for convenience
export default OllamaClient;

// Factory function for easier instantiation
export function createOllamaClient(
  host?: string,
  config?: Partial<OllamaConfig>
): OllamaClient {
  return new OllamaClient(host, config);
}
