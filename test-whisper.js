// Simple test to verify Whisper implementation
import { WhisperTranscriber } from './src/services/WhisperTranscriber.ts';

async function testWhisper() {
  try {
    console.log('Creating WhisperTranscriber instance...');
    const transcriber = new WhisperTranscriber('tiny');
    
    console.log('Model size:', transcriber.modelSize);
    console.log('Memory requirements:', transcriber.getMemoryRequirements('tiny'), 'MB');
    console.log('Available models:', transcriber.getAvailableModels());
    console.log('Performance stats:', transcriber.getPerformanceStats());
    
    console.log('✓ WhisperTranscriber created successfully');
    console.log('Note: Actual transcription would require audio input and model download');
    
  } catch (error) {
    console.error('✗ Error creating WhisperTranscriber:', error);
  }
}

testWhisper();
