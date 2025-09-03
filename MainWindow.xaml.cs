using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using CarelessWhisperV2.Services.Orchestration;
using CarelessWhisperV2.Views;

namespace CarelessWhisperV2;

public partial class MainWindow : Window
{
    private readonly TranscriptionOrchestrator _orchestrator;
    private readonly ILogger<MainWindow> _logger;
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(TranscriptionOrchestrator orchestrator, ILogger<MainWindow> logger, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _orchestrator = orchestrator;
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        // Configure for tray-only operation initially
        this.WindowState = WindowState.Minimized;
        this.ShowInTaskbar = false;
        this.Hide();
        
        // Subscribe to orchestrator events
        _orchestrator.TranscriptionCompleted += OnTranscriptionCompleted;
        _orchestrator.TranscriptionError += OnTranscriptionError;
        
        // Initialize the orchestrator
        _ = Task.Run(InitializeOrchestratorAsync);
    }

    private async Task InitializeOrchestratorAsync()
    {
        try
        {
            await _orchestrator.InitializeAsync();
            
            // Update UI on main thread
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = "Ready - Press F1 to record";
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize orchestrator");
            
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = "Error during initialization";
                MessageBox.Show($"Failed to initialize application: {ex.Message}", 
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }

    private void OnTranscriptionCompleted(object? sender, TranscriptionCompletedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                StatusText.Text = $"Last transcription: \"{e.TranscriptionResult.FullText.Substring(0, Math.Min(50, e.TranscriptionResult.FullText.Length))}...\"";
            }
            catch
            {
                // Fallback if UI elements aren't ready
                _logger.LogInformation("Transcription completed: {Text}", e.TranscriptionResult.FullText);
            }
        });
    }

    private void OnTranscriptionError(object? sender, TranscriptionErrorEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                StatusText.Text = $"Error: {e.Message}";
            }
            catch
            {
                // Fallback if UI elements aren't ready
                _logger.LogError("Transcription error: {Message}", e.Message);
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
        // Clean up resources
        _orchestrator?.Dispose();
        base.OnClosed(e);
    }
}
