using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using CarelessWhisperV2.Services.Orchestration;
using CarelessWhisperV2.Services.Settings;
using CarelessWhisperV2.Services.OpenRouter;
using CarelessWhisperV2.Services.Ollama;
using CarelessWhisperV2.Models;
using CarelessWhisperV2.Views;

namespace CarelessWhisperV2;

public partial class MainWindow : Window
{
    private readonly TranscriptionOrchestrator _orchestrator;
    private readonly ILogger<MainWindow> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISettingsService _settingsService;
    private readonly IOpenRouterService _openRouterService;
    private readonly IOllamaService _ollamaService;

    public MainWindow(TranscriptionOrchestrator orchestrator, ILogger<MainWindow> logger, IServiceProvider serviceProvider,
                     ISettingsService settingsService, IOpenRouterService openRouterService, IOllamaService ollamaService)
    {
        WriteDebugLine("STARTUP: MainWindow constructor started");
        
        try
        {
            WriteDebugLine("STARTUP: Calling InitializeComponent...");
            InitializeComponent();
            WriteDebugLine("STARTUP: InitializeComponent completed");
            
            // Store dependencies
            _orchestrator = orchestrator;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _settingsService = settingsService;
            _openRouterService = openRouterService;
            _ollamaService = ollamaService;
            
            WriteDebugLine("STARTUP: Dependencies assigned");
            
            _logger.LogInformation("MainWindow constructor started - FIXED MODE");
            
            // Configure for tray-only operation initially
            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Hide();
            
            WriteDebugLine("STARTUP: Window state configured");
            
            // Subscribe to orchestrator events
            _orchestrator.TranscriptionCompleted += OnTranscriptionCompleted;
            _orchestrator.TranscriptionError += OnTranscriptionError;
            
            WriteDebugLine("STARTUP: Event subscriptions completed");
            
            // Subscribe to Loaded event for safe async initialization
            this.Loaded += MainWindow_Loaded;
            
            WriteDebugLine("STARTUP: Loaded event subscribed");
            _logger.LogInformation("MainWindow constructor completed - FIXED MODE");
        }
        catch (Exception ex)
        {
            WriteDebugLine($"STARTUP ERROR: MainWindow constructor failed: {ex.Message}");
            WriteDebugLine($"STARTUP ERROR: Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            WriteDebugLine("STARTUP: MainWindow_Loaded event triggered");
            _logger.LogInformation("MainWindow loaded successfully");
            
            // Initialize the orchestrator
            try
            {
                WriteDebugLine("STARTUP: Initializing orchestrator...");
                await _orchestrator.InitializeAsync();
                _logger.LogInformation("Orchestrator initialized successfully");
                WriteDebugLine("STARTUP: Orchestrator initialization completed");
                
                // Update status to show system is ready
                Dispatcher.Invoke(() =>
                {
                    var statusText = FindName("StatusText") as TextBlock;
                    if (statusText != null)
                    {
                        statusText.Text = "Ready - F1: Speech, Shift+F2: Prompt, Ctrl+F2: Copy+Prompt";
                    }
                });
                _logger.LogInformation("Orchestrator ready - F1: Speech, Shift+F2: Prompt, Ctrl+F2: Copy+Prompt");
                WriteDebugLine("STARTUP: Orchestrator ready - F1: Speech, Shift+F2: Prompt, Ctrl+F2: Copy+Prompt");
            }
            catch (Exception orchEx)
            {
                _logger.LogError(orchEx, "Failed to initialize orchestrator");
                WriteDebugLine($"STARTUP: Orchestrator initialization failed: {orchEx.Message}");
            }
            
            // Load provider settings
            try
            {
                WriteDebugLine("STARTUP: Loading provider settings...");
                LoadProviderSettings(); // Call without await since it's async void
                _logger.LogInformation("Provider settings loading started");
                WriteDebugLine("STARTUP: Provider settings loading started");
            }
            catch (Exception settingsEx)
            {
                _logger.LogError(settingsEx, "Failed to start provider settings load");
                WriteDebugLine($"STARTUP: Provider settings load failed: {settingsEx.Message}");
            }
        }
        catch (Exception ex)
        {
            WriteDebugLine($"STARTUP ERROR: MainWindow_Loaded failed: {ex.Message}");
            _logger.LogError(ex, "MainWindow_Loaded failed");
        }
    }

    private void OnTranscriptionCompleted(object? sender, TranscriptionCompletedEventArgs e)
    {
        _logger.LogInformation("Transcription completed: {Text}", e.TranscriptionResult.FullText);
        
        // Update UI with completion status and preview
        Dispatcher.Invoke(() =>
        {
            var statusText = FindName("StatusText") as TextBlock;
            if (statusText != null)
            {
                var previewText = e.TranscriptionResult.FullText;
                if (previewText.Length > 100)
                {
                    previewText = previewText.Substring(0, 100) + "...";
                }
                statusText.Text = $"Completed: {previewText}";
                
                // Clear status after 10 seconds
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(10)
                };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    var statusTextInner = FindName("StatusText") as TextBlock;
                    if (statusTextInner != null)
                    {
                        statusTextInner.Text = "Ready - F1: Speech, Shift+F2: Prompt, Ctrl+F2: Copy+Prompt";
                    }
                };
                timer.Start();
            }
        });
    }

    private void OnTranscriptionError(object? sender, TranscriptionErrorEventArgs e)
    {
        _logger.LogError("Transcription error: {Message}", e.Message);
        
        // Update UI with error status
        Dispatcher.Invoke(() =>
        {
            var statusText = FindName("StatusText") as TextBlock;
            if (statusText != null)
            {
                statusText.Text = $"Error: {e.Message}";
                
                // Clear error status after 15 seconds
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(15)
                };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    var statusTextInner = FindName("StatusText") as TextBlock;
                    if (statusTextInner != null)
                    {
                        statusTextInner.Text = "Ready - F1: Speech, Shift+F2: Prompt, Ctrl+F2: Copy+Prompt";
                    }
                };
                timer.Start();
            }
        });
    }

    private void ShowApplication_Click(object sender, RoutedEventArgs e)
    {
        Show();
        this.WindowState = WindowState.Normal;
        this.ShowInTaskbar = true;
        this.Activate();
        _logger.LogDebug("Application window shown");
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
            _logger.LogDebug("Settings window opened");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open settings window");
            MessageBox.Show($"Failed to open settings window: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StartTest_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Press and hold F1 to test recording functionality", "Test Recording", 
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ViewHistory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var historyWindow = _serviceProvider.GetRequiredService<TranscriptionHistoryWindow>();
            historyWindow.Owner = this;
            historyWindow.ShowDialog();
            _logger.LogDebug("Transcription history window opened");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open transcription history window");
            MessageBox.Show($"Failed to open transcription history window: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Application exit requested");
        Application.Current.Shutdown();
    }

    private void Hide_Click(object sender, RoutedEventArgs e)
    {
        this.Hide();
        this.ShowInTaskbar = false;
        _logger.LogDebug("Application window hidden to tray");
    }

    private void TrayIcon_LeftMouseUp(object sender, RoutedEventArgs e)
    {
        // Show window on left click of tray icon
        ShowApplication_Click(sender, e);
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            this.Hide();
            this.ShowInTaskbar = false;
        }
        base.OnStateChanged(e);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Prevent actual closing - minimize to tray instead
        e.Cancel = true;
        this.Hide();
        this.ShowInTaskbar = false;
        _logger.LogDebug("Close prevented, minimized to tray");
    }

    protected override void OnClosed(EventArgs e)
    {
        // Clean up resources - orchestrator removed for minimal testing
        base.OnClosed(e);
    }

    private async void LoadProviderSettings()
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            _logger.LogInformation("Provider settings loaded: {Provider}", settings.SelectedLlmProvider);
            
            // Update radio buttons to match saved settings
            Dispatcher.Invoke(() =>
            {
                var openRouterRadio = FindName("OpenRouterRadioButton") as RadioButton;
                var ollamaRadio = FindName("OllamaRadioButton") as RadioButton;
                
                if (openRouterRadio != null && ollamaRadio != null)
                {
                    if (settings.SelectedLlmProvider == LlmProvider.OpenRouter)
                    {
                        openRouterRadio.IsChecked = true;
                        ollamaRadio.IsChecked = false;
                    }
                    else
                    {
                        openRouterRadio.IsChecked = false;
                        ollamaRadio.IsChecked = true;
                    }
                    
                    _logger.LogInformation("Radio buttons updated to reflect {Provider} provider", settings.SelectedLlmProvider);
                }
            });
            
            // Update provider status display
            await UpdateProviderStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load provider settings");
        }
    }

    private async void LlmProvider_Changed(object sender, RoutedEventArgs e)
    {
        // Guard against early event firing during InitializeComponent
        if (_logger == null || _settingsService == null) return;
        
        try
        {
            var radioButton = sender as RadioButton;
            if (radioButton?.IsChecked == true)
            {
                var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
                
                // Determine which provider was selected
                LlmProvider selectedProvider;
                if (radioButton.Name == "OpenRouterRadioButton")
                {
                    selectedProvider = LlmProvider.OpenRouter;
                }
                else if (radioButton.Name == "OllamaRadioButton")
                {
                    selectedProvider = LlmProvider.Ollama;
                }
                else
                {
                    _logger.LogWarning("Unknown radio button clicked: {Name}", radioButton.Name);
                    return;
                }
                
                // Only update if the provider actually changed
                if (settings.SelectedLlmProvider != selectedProvider)
                {
                    _logger.LogInformation("LLM provider changed from {Old} to {New}", 
                        settings.SelectedLlmProvider, selectedProvider);
                    
                    settings.SelectedLlmProvider = selectedProvider;
                    await _settingsService.SaveSettingsAsync(settings);
                    
                    // Update the orchestrator with new settings
                    await _orchestrator.UpdateSettingsAsync(settings);
                    
                    // Update provider status display
                    await UpdateProviderStatusAsync();
                    
                    _logger.LogInformation("Successfully switched to {Provider} provider", selectedProvider);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change LLM provider");
            
            // Show error to user and revert radio button selection
            Dispatcher.Invoke(() =>
            {
                var statusText = FindName("StatusText") as TextBlock;
                if (statusText != null)
                {
                    statusText.Text = $"Error switching provider: {ex.Message}";
                }
            });
        }
    }

    private async Task UpdateProviderStatusAsync()
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            
            string providerName = "";
            
            if (settings.SelectedLlmProvider == LlmProvider.OpenRouter)
            {
                providerName = "OpenRouter";
                _logger.LogInformation("OpenRouter provider selected and enabled");
            }
            else
            {
                providerName = "Ollama";
                _logger.LogInformation("Ollama provider selected and enabled");
            }
            
            // Update UI on the UI thread - always show enabled status in green for selected provider
            Dispatcher.Invoke(() =>
            {
                var providerStatus = FindName("ProviderStatusTextBlock") as TextBlock;
                if (providerStatus != null)
                {
                    providerStatus.Text = $"✓ {providerName} Enabled";
                    providerStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update provider status");
            
            // Show error status
            Dispatcher.Invoke(() =>
            {
                var providerStatus = FindName("ProviderStatusTextBlock") as TextBlock;
                if (providerStatus != null)
                {
                    providerStatus.Text = "✗ Status Error";
                    providerStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
            });
        }
    }

    private async Task<bool> TestOpenRouterConnectionAsync()
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            
            if (string.IsNullOrWhiteSpace(settings.OpenRouter.ApiKey))
            {
                return false;
            }

            // Test connection by checking available models
            var models = await _openRouterService.GetAvailableModelsAsync();
            return models?.Any() == true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "OpenRouter connection test failed");
            return false;
        }
    }

    private async Task<bool> TestOllamaConnectionAsync()
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            
            if (string.IsNullOrWhiteSpace(settings.Ollama.ServerUrl))
            {
                return false;
            }

            // Test connection by checking if server is reachable
            var models = await _ollamaService.GetAvailableModelsAsync();
            return models?.Any() == true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ollama connection test failed");
            return false;
        }
    }

    private static void WriteDebugLine(string message)
    {
        // Only write to console if debugger is attached or debug flag is present
        if (System.Diagnostics.Debugger.IsAttached || System.Environment.GetCommandLineArgs().Contains("--debug"))
        {
            try
            {
                Console.WriteLine(message);
            }
            catch
            {
                // Ignore console write errors in production
            }
        }
    }
}
