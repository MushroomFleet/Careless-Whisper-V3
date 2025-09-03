import './style.css';
import { SpeakToMeApp } from './components/SpeakToMeApp';

// Initialize the application when DOM is loaded
document.addEventListener('DOMContentLoaded', async () => {
  try {
    console.log('Initializing Speak-2-Me v2.0...');
    
    // Hide loading screen
    const loadingElement = document.getElementById('loading');
    
    // Initialize the main application
    const app = new SpeakToMeApp();
    await app.initialize();
    
    // Hide loading screen with fade out
    if (loadingElement) {
      loadingElement.style.opacity = '0';
      setTimeout(() => {
        loadingElement.style.display = 'none';
      }, 300);
    }
    
    console.log('Speak-2-Me v2.0 initialized successfully');
  } catch (error) {
    console.error('Failed to initialize application:', error);
    
    // Show error state
    const loadingElement = document.getElementById('loading');
    if (loadingElement) {
      loadingElement.innerHTML = `
        <div class="text-center">
          <div class="text-red-600 text-6xl mb-4">
            <i class="fas fa-exclamation-triangle"></i>
          </div>
          <h2 class="text-xl font-semibold text-red-800 mb-2">Initialization Failed</h2>
          <p class="text-red-600 mb-4">${error instanceof Error ? error.message : 'Unknown error occurred'}</p>
          <button onclick="location.reload()" class="btn-primary">
            <i class="fas fa-refresh mr-2"></i>
            Retry
          </button>
        </div>
      `;
    }
  }
});

// Global error handling
window.addEventListener('error', (event) => {
  console.error('Global error:', event.error);
});

window.addEventListener('unhandledrejection', (event) => {
  console.error('Unhandled promise rejection:', event.reason);
});

// Service worker registration for PWA functionality
if ('serviceWorker' in navigator) {
  window.addEventListener('load', async () => {
    try {
      const registration = await navigator.serviceWorker.register('/sw.js');
      console.log('Service Worker registered:', registration);
    } catch (error) {
      console.log('Service Worker registration failed:', error);
    }
  });
}

// Export for development/debugging
if (import.meta.env.DEV) {
  (window as any).SpeakToMe = { SpeakToMeApp };
}
