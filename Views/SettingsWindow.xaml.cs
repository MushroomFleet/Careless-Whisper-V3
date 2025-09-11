using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using CarelessWhisperV2.Models;
using CarelessWhisperV2.Services.Settings;
using CarelessWhisperV2.Services.Audio;
using CarelessWhisperV2.Services.Orchestration;
using CarelessWhisperV2.Services.OpenRouter;
using CarelessWhisperV2.Services.Ollama;
using CarelessWhisperV2.Services.Environment;
using CarelessWhisperV2.Services.AudioNotification;
using CarelessWhisperV2.Services.Network;

using CarelessWhisperV2.Services.TTS;


using SharpHook.Native;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;

namespace CarelessWhisperV2.Views;

public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly IAudioService _audioService;
    private readonly TranscriptionOrchestrator _orchestrator;
    private readonly IOpenRouterService _openRouterService;
    private readonly IOllamaService _ollamaService;
    private readonly IEnvironmentService _environmentService;
    private readonly IAudioNotificationService _audioNotificationService;

    private readonly ITTSService _ttsService;
    private readonly ILogger<SettingsWindow> _logger;
    private ApplicationSettings _settings;
    private string _capturedHotkey = "";
    private string _capturedLlmHotkey = "";
    private List<OpenRouterModel> _availableModels = new();
    private List<OllamaModel> _availableOllamaModels = new();
    private List<OpenRouterModel> _availableVisionModels = new();

    public SettingsWindow(
        ISettingsService settingsService, 
        IAudioService audioService,
        TranscriptionOrchestrator orchestrator,
        IOpenRouterService openRouterService,
        IOllamaService ollamaService,
        IEnvironmentService environmentService,
        IAudioNotificationService audioNotificationService,

        ITTSService ttsService,
        ILogger<SettingsWindow> logger)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _audioService = audioService;
        _orchestrator = orchestrator;
        _openRouterService = openRouterService;
        _ollamaService = ollamaService;
        _environmentService = environmentService;
        _audioNotificationService = audioNotificationService;

        _ttsService = ttsService;

        _logger = logger;
        _settings = new ApplicationSettings();

        Loaded += SettingsWindow_Loaded;
    }

    private async void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await LoadCurrentSettings();
            LoadAudioDevices();
            UpdateModelInfo();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings window");
            MessageBox.Show($"Failed to load settings: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadCurrentSettings()
    {
        _settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
        
        // General tab
        AutoStartCheckBox.IsChecked = _settings.AutoStartWithWindows;
        ThemeComboBox.SelectedItem = ThemeComboBox.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => item.Content.ToString() == _settings.Theme);
        EnableLoggingCheckBox.IsChecked = _settings.Logging.EnableTranscriptionLogging;
        SaveAudioFilesCheckBox.IsChecked = _settings.Logging.SaveAudioFiles;
        RetentionDaysTextBox.Text = _settings.Logging.LogRetentionDays.ToString();
        
        // Hotkeys tab
        HotkeyTextBox.Text = _settings.Hotkeys.PushToTalkKey;
        LlmHotkeyTextBox.Text = _settings.Hotkeys.LlmPromptKey;
        RequireModifiersCheckBox.IsChecked = _settings.Hotkeys.RequireModifiers;
        
        // Audio tab
        SampleRateComboBox.SelectedItem = SampleRateComboBox.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => item.Content.ToString()?.StartsWith(_settings.Audio.SampleRate.ToString()) == true);
        BufferSizeComboBox.SelectedItem = BufferSizeComboBox.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => item.Content.ToString() == _settings.Audio.BufferSize.ToString());
        
        // Whisper tab
        ModelSizeComboBox.SelectedItem = ModelSizeComboBox.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => item.Tag?.ToString() == _settings.Whisper.ModelSize);
        LanguageComboBox.SelectedItem = LanguageComboBox.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => item.Tag?.ToString() == _settings.Whisper.Language);
        EnableGpuCheckBox.IsChecked = _settings.Whisper.EnableGpuAcceleration;
        
        // Load OpenRouter, Ollama, Audio Notification, TTS, and Vision settings
        await LoadOpenRouterSettings();
        await LoadOllamaSettings();
        LoadAudioNotificationSettings();
        await LoadTtsSettings();
        await LoadVisionSettings();
    }

    private async Task LoadOpenRouterSettings()
    {
        try
        {
            // Load API key from environment service
            var apiKey = await _environmentService.GetApiKeyAsync();
            if (!string.IsNullOrEmpty(apiKey))
            {
                ApiKeyPasswordBox.Password = apiKey;
            }

            // Load other OpenRouter settings
            SystemPromptTextBox.Text = _settings.OpenRouter.SystemPrompt;
            TemperatureSlider.Value = _settings.OpenRouter.Temperature;
            MaxTokensTextBox.Text = _settings.OpenRouter.MaxTokens.ToString();
            EnableStreamingCheckBox.IsChecked = _settings.OpenRouter.EnableStreaming;

            // Load available models and set selected model
            await LoadAvailableModels();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load OpenRouter settings");
        }
    }

    private void LoadAudioNotificationSettings()
    {
        try
        {
            EnableNotificationsCheckBox.IsChecked = _settings.AudioNotification.EnableNotifications;
            PlayOnSpeechToTextCheckBox.IsChecked = _settings.AudioNotification.PlayOnSpeechToText;
            PlayOnLlmResponseCheckBox.IsChecked = _settings.AudioNotification.PlayOnLlmResponse;
            AudioFilePathTextBox.Text = _settings.AudioNotification.AudioFilePath;
            VolumeSlider.Value = _settings.AudioNotification.Volume;
            
            // Update volume display
            VolumeValueTextBlock.Text = $"{_settings.AudioNotification.Volume * 100:F0}%";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load audio notification settings");
        }
    }

    private async Task LoadAvailableModels(bool forceRefresh = false)
    {
        ApiKeyStatusTextBlock.Text = "Loading models...";
        ModelComboBox.Items.Clear();
        
        var apiKey = ApiKeyPasswordBox.Password;
        if (string.IsNullOrEmpty(apiKey))
        {
            LoadFallbackModel();
            SelectStoredModel();
            return;
        }

        try
        {
            // Direct HTTP call to OpenRouter
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            
            var response = await client.GetAsync("https://openrouter.ai/api/v1/models");
            var jsonString = await response.Content.ReadAsStringAsync();
            
            ApiKeyStatusTextBlock.Text = $"Got {jsonString.Length} chars from API";
            
            // Simple JSON parsing
            var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonString);
            var data = jsonDoc.RootElement.GetProperty("data");
            
            _availableModels = new List<OpenRouterModel>();
            
            foreach (var modelElement in data.EnumerateArray())
            {
                var model = new OpenRouterModel
                {
                    Id = modelElement.GetProperty("id").GetString() ?? "",
                    Name = modelElement.TryGetProperty("name", out var nameProperty) ? 
                           nameProperty.GetString() ?? modelElement.GetProperty("id").GetString() ?? "" : 
                           modelElement.GetProperty("id").GetString() ?? "",
                    Description = modelElement.TryGetProperty("description", out var descProperty) ? 
                                 descProperty.GetString() ?? "" : "",
                    PricePerMToken = 0.001m, // Default price
                    ContextLength = 4096,
                    SupportsStreaming = true
                };
                
                if (!string.IsNullOrEmpty(model.Id))
                {
                    _availableModels.Add(model);
                }
            }
            
            // Populate dropdown
            foreach (var model in _availableModels)
            {
                var item = new ComboBoxItem
                {
                    Content = $"{model.Name}",
                    Tag = model.Id
                };
                ModelComboBox.Items.Add(item);
            }
            
            // Select the stored model or default to first available
            if (!SelectStoredModel() && ModelComboBox.Items.Count > 0)
            {
                ModelComboBox.SelectedIndex = 0;
                // Update settings with the newly selected model
                if (ModelComboBox.SelectedItem is ComboBoxItem firstItem && firstItem.Tag != null)
                {
                    _settings.OpenRouter.SelectedModel = firstItem.Tag.ToString() ?? "";
                }
            }
            
            ApiKeyStatusTextBlock.Text = $"✓ Loaded {_availableModels.Count} models";
            ApiKeyStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load models from OpenRouter API");
            ApiKeyStatusTextBlock.Text = $"Error: {ex.Message}";
            LoadFallbackModel();
            SelectStoredModel();
        }
    }

    private void LoadFallbackModel()
    {
        _logger.LogInformation("Loading fallback model in UI");
        ModelComboBox.Items.Clear();
        
        var fallbackModel = new OpenRouterModel
        {
            Id = "anthropic/claude-sonnet-4",
            Name = "Claude Sonnet 4 (Fallback)",
            Description = "Fallback model when API models cannot be loaded",
            PricePerMToken = 0.015m,
            ContextLength = 200000,
            SupportsStreaming = true
        };
        
        _availableModels = new List<OpenRouterModel> { fallbackModel };
        
        var item = new ComboBoxItem
        {
            Content = $"{fallbackModel.Name} - ${fallbackModel.PricePerMToken}/1M tokens",
            Tag = fallbackModel.Id
        };
        ModelComboBox.Items.Add(item);
        ModelComboBox.SelectedItem = item;
        
        // Update model information display
        ModelNameTextBlock.Text = fallbackModel.Name;
        ModelDescriptionTextBlock.Text = fallbackModel.Description;
        ModelPricingTextBlock.Text = $"${fallbackModel.PricePerMToken}/1M tokens";
        ModelContextTextBlock.Text = $"Context: {fallbackModel.ContextLength:N0} tokens";
    }

    private bool SelectStoredModel()
    {
        try
        {
            var storedModelId = _settings.OpenRouter.SelectedModel;
            if (string.IsNullOrEmpty(storedModelId))
            {
                _logger.LogDebug("No stored model ID found");
                return false;
            }

            // Find the stored model in the dropdown
            foreach (ComboBoxItem item in ModelComboBox.Items)
            {
                if (item.Tag?.ToString() == storedModelId)
                {
                    ModelComboBox.SelectedItem = item;
                    _logger.LogInformation("Successfully selected stored model: {ModelId}", storedModelId);
                    return true;
                }
            }

            // If stored model not found in available models, validate if it exists in our available models list
            var storedModel = _availableModels.FirstOrDefault(m => m.Id == storedModelId);
            if (storedModel != null)
            {
                // Add the model to dropdown if it exists but wasn't loaded
                var item = new ComboBoxItem
                {
                    Content = $"{storedModel.Name}",
                    Tag = storedModel.Id
                };
                ModelComboBox.Items.Add(item);
                ModelComboBox.SelectedItem = item;
                _logger.LogInformation("Added and selected stored model that was missing from dropdown: {ModelId}", storedModelId);
                return true;
            }

            _logger.LogWarning("Stored model not found in available models: {ModelId}. Will use fallback.", storedModelId);
            
            // Reset to fallback model in settings since the stored one is invalid
            _settings.OpenRouter.SelectedModel = "anthropic/claude-sonnet-4";
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting stored model");
            return false;
        }
    }

    // OpenRouter Event Handlers
    private async void TestApiKey_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var apiKey = ApiKeyPasswordBox.Password;
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Please enter an API key first.", "API Key Required", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TestApiKeyButton.IsEnabled = false;
            ApiKeyStatusTextBlock.Text = "Testing API key...";
            ApiKeyStatusTextBlock.Foreground = System.Windows.Media.Brushes.Blue;

            // Test API key by fetching models
            var isValid = await _openRouterService.ValidateApiKeyAsync(apiKey);
            
            if (isValid)
            {
                ApiKeyStatusTextBlock.Text = "✓ API key is valid";
                ApiKeyStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                
                // Save API key to environment
                await _environmentService.SaveApiKeyAsync(apiKey);
                
                // Refresh models
                await LoadAvailableModels();
            }
            else
            {
                ApiKeyStatusTextBlock.Text = "✗ Invalid API key";
                ApiKeyStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API key test failed");
            ApiKeyStatusTextBlock.Text = $"✗ Test failed: {ex.Message}";
            ApiKeyStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
        }
        finally
        {
            TestApiKeyButton.IsEnabled = true;
        }
    }

    private async void RefreshModels_Click(object sender, RoutedEventArgs e)
    {
        await LoadAvailableModels(forceRefresh: true);
    }

    private async void DiagnoseConnection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DiagnoseConnectionButton.IsEnabled = false;
            ApiKeyStatusTextBlock.Text = "Running network diagnostics...";
            ApiKeyStatusTextBlock.Foreground = System.Windows.Media.Brushes.Blue;

            // Create and run network diagnostics
            var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());
            var diagnosticsLogger = loggerFactory.CreateLogger<NetworkDiagnosticsService>();
            var diagnosticsService = new NetworkDiagnosticsService(diagnosticsLogger);
            var result = await diagnosticsService.RunDiagnosticsAsync();

            // Display results
            var message = $"Network Diagnostics Results:\n\n{result.Summary}\n\n";
            
            if (result.Issues.Count > 0)
            {
                message += "Issues Found:\n";
                foreach (var issue in result.Issues)
                {
                    message += $"• {issue}\n";
                }
                message += "\n";
            }
            
            if (result.Suggestions.Count > 0)
            {
                message += "Suggestions:\n";
                foreach (var suggestion in result.Suggestions)
                {
                    message += $"• {suggestion}\n";
                }
                message += "\n";
            }
            
            if (result.ProxyInfo.ProxyDetected)
            {
                message += $"Proxy Information:\n";
                message += $"• Type: {result.ProxyInfo.ProxyType}\n";
                message += $"• Address: {result.ProxyInfo.ProxyAddress}\n";
                message += $"• Authentication Required: {result.ProxyInfo.AuthenticationRequired}\n";
            }

            MessageBox.Show(message, "Network Diagnostics Results", 
                MessageBoxButton.OK, 
                result.OpenRouterApiAccessible ? MessageBoxImage.Information : MessageBoxImage.Warning);

            // Update status text based on results
            ApiKeyStatusTextBlock.Text = result.Summary;
            ApiKeyStatusTextBlock.Foreground = result.OpenRouterApiAccessible 
                ? System.Windows.Media.Brushes.Green 
                : System.Windows.Media.Brushes.Red;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Network diagnostics failed");
            ApiKeyStatusTextBlock.Text = $"✗ Diagnostics failed: {ex.Message}";
            ApiKeyStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            
            MessageBox.Show($"Network diagnostics failed: {ex.Message}", "Diagnostics Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            DiagnoseConnectionButton.IsEnabled = true;
        }
    }

    private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModelComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
        {
            var selectedModelId = selectedItem.Tag.ToString();
            var selectedModel = _availableModels.FirstOrDefault(m => m.Id == selectedModelId);
            
            if (selectedModel != null)
            {
                ModelNameTextBlock.Text = selectedModel.Name;
                ModelDescriptionTextBlock.Text = selectedModel.Description;
                ModelPricingTextBlock.Text = $"${selectedModel.PricePerMToken}/1M tokens";
                ModelContextTextBlock.Text = $"Context: {selectedModel.ContextLength:N0} tokens";
            }
        }
    }

    // Ollama Event Handlers
    private async void TestOllamaConnection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var serverUrl = OllamaServerUrlTextBox.Text;
            if (string.IsNullOrEmpty(serverUrl))
            {
                MessageBox.Show("Please enter a server URL first.", "Server URL Required", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TestOllamaConnectionButton.IsEnabled = false;
            OllamaConnectionStatusTextBlock.Text = "Testing connection...";
            OllamaConnectionStatusTextBlock.Foreground = System.Windows.Media.Brushes.Blue;

            // Test connection to Ollama server
            var isConnected = await _ollamaService.ValidateConnectionAsync(serverUrl);
            
            if (isConnected)
            {
                OllamaConnectionStatusTextBlock.Text = "✓ Connected to Ollama server";
                OllamaConnectionStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                
                // Refresh models after successful connection
                await LoadAvailableOllamaModels();
            }
            else
            {
                OllamaConnectionStatusTextBlock.Text = "✗ Cannot connect to Ollama server";
                OllamaConnectionStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama connection test failed");
            OllamaConnectionStatusTextBlock.Text = $"✗ Test failed: {ex.Message}";
            OllamaConnectionStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
        }
        finally
        {
            TestOllamaConnectionButton.IsEnabled = true;
        }
    }

    private async void RefreshOllamaModels_Click(object sender, RoutedEventArgs e)
    {
        await LoadAvailableOllamaModels(forceRefresh: true);
    }

    private async Task LoadAvailableOllamaModels(bool forceRefresh = false)
    {
        OllamaConnectionStatusTextBlock.Text = "Loading models...";
        OllamaModelComboBox.Items.Clear();
        
        try
        {
            _availableOllamaModels = await _ollamaService.GetAvailableModelsAsync(forceRefresh);
            
            if (_availableOllamaModels.Count == 0)
            {
                OllamaConnectionStatusTextBlock.Text = "No models found. Ensure Ollama is running and models are installed.";
                OllamaConnectionStatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
                return;
            }
            
            // Populate dropdown
            foreach (var model in _availableOllamaModels)
            {
                var item = new ComboBoxItem
                {
                    Content = $"{model.Name}",
                    Tag = model.Model
                };
                OllamaModelComboBox.Items.Add(item);
            }
            
            // Select the stored model or default to first available
            if (!SelectStoredOllamaModel() && OllamaModelComboBox.Items.Count > 0)
            {
                OllamaModelComboBox.SelectedIndex = 0;
                // Update settings with the newly selected model
                if (OllamaModelComboBox.SelectedItem is ComboBoxItem firstItem && firstItem.Tag != null)
                {
                    _settings.Ollama.SelectedModel = firstItem.Tag.ToString() ?? "";
                }
            }
            
            OllamaConnectionStatusTextBlock.Text = $"✓ Loaded {_availableOllamaModels.Count} models";
            OllamaConnectionStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load models from Ollama server");
            OllamaConnectionStatusTextBlock.Text = $"Error: {ex.Message}";
            OllamaConnectionStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    private bool SelectStoredOllamaModel()
    {
        try
        {
            var storedModelId = _settings.Ollama.SelectedModel;
            if (string.IsNullOrEmpty(storedModelId))
            {
                _logger.LogDebug("No stored Ollama model ID found");
                return false;
            }

            _logger.LogDebug("Attempting to select stored Ollama model: {ModelId}", storedModelId);

            // Find the stored model in the dropdown
            foreach (ComboBoxItem item in OllamaModelComboBox.Items)
            {
                if (item.Tag?.ToString() == storedModelId)
                {
                    OllamaModelComboBox.SelectedItem = item;
                    _logger.LogInformation("Successfully selected stored Ollama model: {ModelId}", storedModelId);
                    return true;
                }
            }
            
            // If exact match not found, try to find a partial match (for model variants)
            foreach (ComboBoxItem item in OllamaModelComboBox.Items)
            {
                var itemModel = item.Tag?.ToString() ?? "";
                if (itemModel.StartsWith(storedModelId.Split(':')[0], StringComparison.OrdinalIgnoreCase))
                {
                    OllamaModelComboBox.SelectedItem = item;
                    _logger.LogInformation("Selected similar Ollama model '{ModelId}' (originally '{StoredModel}')", itemModel, storedModelId);
                    return true;
                }
            }
            
            _logger.LogWarning("Stored Ollama model '{ModelId}' not found in available models. Available models: {AvailableModels}", 
                storedModelId, 
                string.Join(", ", OllamaModelComboBox.Items.Cast<ComboBoxItem>().Select(i => i.Tag?.ToString() ?? "null")));
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting stored Ollama model");
            return false;
        }
    }

    private void OllamaModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OllamaModelComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
        {
            var selectedModelId = selectedItem.Tag.ToString();
            var selectedModel = _availableOllamaModels.FirstOrDefault(m => m.Model == selectedModelId);
            
            if (selectedModel != null)
            {
                OllamaModelNameTextBlock.Text = selectedModel.Name;
                OllamaModelSizeTextBlock.Text = $"Size: {selectedModel.Size / (1024 * 1024 * 1024):F1} GB";
                OllamaModelFamilyTextBlock.Text = $"Family: {selectedModel.Details?.Family ?? "Unknown"}";
                OllamaModelModifiedTextBlock.Text = $"Modified: {selectedModel.ModifiedAt:yyyy-MM-dd}";
            }
        }
    }

    private async Task LoadOllamaSettings()
    {
        try
        {
            // Load Ollama settings from ApplicationSettings
            OllamaServerUrlTextBox.Text = _settings.Ollama.ServerUrl;
            OllamaSystemPromptTextBox.Text = _settings.Ollama.SystemPrompt;
            OllamaTemperatureSlider.Value = _settings.Ollama.Temperature;
            OllamaMaxTokensTextBox.Text = _settings.Ollama.MaxTokens.ToString();
            OllamaEnableStreamingCheckBox.IsChecked = _settings.Ollama.EnableStreaming;

            // Load available models if server URL is configured
            if (!string.IsNullOrEmpty(_settings.Ollama.ServerUrl))
            {
                await LoadAvailableOllamaModels();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Ollama settings");
        }
    }

    // Audio Notification Event Handlers
    private void BrowseAudioFile_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select Audio Notification File",
            Filter = "Audio Files (*.wav;*.mp3)|*.wav;*.mp3|WAV Files (*.wav)|*.wav|MP3 Files (*.mp3)|*.mp3|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            AudioFilePathTextBox.Text = openFileDialog.FileName;
        }
    }

    private async void TestAudio_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var audioFilePath = AudioFilePathTextBox.Text;
            if (string.IsNullOrEmpty(audioFilePath) || !File.Exists(audioFilePath))
            {
                MessageBox.Show("Please select a valid audio file first.", "Audio File Required", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TestAudioButton.IsEnabled = false;

            // Set volume and test the audio file
            _audioNotificationService.SetVolume(VolumeSlider.Value);
            var success = await _audioNotificationService.TestAudioFileAsync(audioFilePath);

            if (success)
            {
                MessageBox.Show("✓ Audio played successfully!", "Audio Test", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("✗ Audio test failed. Please check the file format and try again.", "Audio Test Failed", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audio test failed");
            MessageBox.Show($"✗ Audio test failed: {ex.Message}", "Audio Test Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            TestAudioButton.IsEnabled = true;
        }
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VolumeValueTextBlock != null)
        {
            VolumeValueTextBlock.Text = $"{e.NewValue * 100:F0}%";
        }
        
        // Update settings object immediately so changes are captured when saving
        if (_settings?.AudioNotification != null)
        {
            _settings.AudioNotification.Volume = e.NewValue;
            
            // Update the audio notification service with the new volume in real-time
            try
            {
                _audioNotificationService?.SetVolume(e.NewValue);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update audio notification volume in real-time");
            }
        }
    }

    private void LoadAudioDevices()
    {
        try
        {
            var devices = _audioService.GetAvailableMicrophones();
            MicrophoneComboBox.Items.Clear();
            
            foreach (var device in devices)
            {
                var item = new ComboBoxItem
                {
                    Content = device.IsDefault ? $"{device.Name} (Default)" : device.Name,
                    Tag = device.Id
                };
                
                MicrophoneComboBox.Items.Add(item);
                
                if (device.Id == _settings.Audio.PreferredDeviceId || 
                    (string.IsNullOrEmpty(_settings.Audio.PreferredDeviceId) && device.IsDefault))
                {
                    MicrophoneComboBox.SelectedItem = item;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load audio devices");
            MessageBox.Show("Failed to load audio devices. Please try refreshing.", "Audio Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        
        var modifiers = new List<string>();
        var key = "";
        
        // Capture modifier keys
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            modifiers.Add("Ctrl");
        if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            modifiers.Add("Alt");
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            modifiers.Add("Shift");
        if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
            modifiers.Add("Win");
            
        // Capture main key (ignore modifier keys themselves)
        if (e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl &&
            e.Key != Key.LeftAlt && e.Key != Key.RightAlt &&
            e.Key != Key.LeftShift && e.Key != Key.RightShift &&
            e.Key != Key.LWin && e.Key != Key.RWin)
        {
            key = e.Key.ToString();
        }
        
        if (!string.IsNullOrEmpty(key))
        {
            if (modifiers.Count > 0)
            {
                _capturedHotkey = string.Join("+", modifiers) + "+" + key;
            }
            else
            {
                _capturedHotkey = key;
            }
            
            HotkeyTextBox.Text = _capturedHotkey;
        }
    }

    private void LlmHotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        
        var modifiers = new List<string>();
        var key = "";
        
        // Capture modifier keys
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            modifiers.Add("Ctrl");
        if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            modifiers.Add("Alt");
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            modifiers.Add("Shift");
        if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
            modifiers.Add("Win");
            
        // Capture main key (ignore modifier keys themselves)
        if (e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl &&
            e.Key != Key.LeftAlt && e.Key != Key.RightAlt &&
            e.Key != Key.LeftShift && e.Key != Key.RightShift &&
            e.Key != Key.LWin && e.Key != Key.RWin)
        {
            key = e.Key.ToString();
        }
        
        if (!string.IsNullOrEmpty(key))
        {
            if (modifiers.Count > 0)
            {
                _capturedLlmHotkey = string.Join("+", modifiers) + "+" + key;
            }
            else
            {
                _capturedLlmHotkey = key;
            }
            
            LlmHotkeyTextBox.Text = _capturedLlmHotkey;
        }
    }

    private void ClearHotkey_Click(object sender, RoutedEventArgs e)
    {
        HotkeyTextBox.Text = "";
        _capturedHotkey = "";
    }

    private void ClearLlmHotkey_Click(object sender, RoutedEventArgs e)
    {
        LlmHotkeyTextBox.Text = "";
        _capturedLlmHotkey = "";
    }

    private void MicrophoneComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        TestResultTextBlock.Text = "";
    }

    private void RefreshDevices_Click(object sender, RoutedEventArgs e)
    {
        LoadAudioDevices();
        TestResultTextBlock.Text = "Devices refreshed";
        TestResultTextBlock.Foreground = System.Windows.Media.Brushes.Green;
    }

    private async void TestMicrophone_Click(object sender, RoutedEventArgs e)
    {
        if (MicrophoneComboBox.SelectedItem is not ComboBoxItem selectedItem)
        {
            TestResultTextBlock.Text = "Please select a microphone first";
            TestResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            return;
        }

        try
        {
            TestResultTextBlock.Text = "Testing microphone and transcription... Speak now!";
            TestResultTextBlock.Foreground = System.Windows.Media.Brushes.Blue;
            
            TestMicrophoneButton.IsEnabled = false;
            
            // Enhanced test - record for 5 seconds and test transcription
            var tempFile = Path.Combine(Path.GetTempPath(), $"mic_test_{DateTime.Now:yyyyMMdd_HHmmss_fff}.wav");
            
            // Step 1: Test audio recording
            TestResultTextBlock.Text = "Step 1: Testing audio recording...";
            await Task.Run(async () =>
            {
                await _audioService.StartRecordingAsync(tempFile);
                await Task.Delay(5000); // Record for 5 seconds
                await _audioService.StopRecordingAsync();
            });
            
            // Wait for file to be fully released
            TestResultTextBlock.Text = "Waiting for recording to finalize...";
            await Task.Delay(1000);
            
            if (!File.Exists(tempFile))
            {
                TestResultTextBlock.Text = "✗ Audio recording failed - no file created";
                TestResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }
            
            var fileInfo = new FileInfo(tempFile);
            if (fileInfo.Length < 1000)
            {
                TestResultTextBlock.Text = "⚠ No audio detected. Check microphone connection.";
                TestResultTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
                File.Delete(tempFile);
                return;
            }
            
            // Step 2: Test transcription
            TestResultTextBlock.Text = $"Step 2: Testing transcription... (Audio: {fileInfo.Length / 1024}KB)";
            
            try
            {
                var transcriptionService = (CarelessWhisperV2.Services.Transcription.WhisperTranscriptionService)_orchestrator.GetType()
                    .GetField("_transcriptionService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(_orchestrator);
                
                if (transcriptionService == null)
                {
                    TestResultTextBlock.Text = "✗ Transcription service not found";
                    TestResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }
                
                // Check if transcription service is initialized
                var isInitialized = transcriptionService.IsInitialized;
                if (!isInitialized)
                {
                    TestResultTextBlock.Text = "⚠ Initializing transcription service...";
                    await transcriptionService.InitializeAsync("Base");
                }
                
                // Attempt transcription
                var result = await transcriptionService.TranscribeAsync(tempFile);
                
                if (!string.IsNullOrWhiteSpace(result.FullText))
                {
                    TestResultTextBlock.Text = $"✓ Test successful! Transcribed: \"{result.FullText.Substring(0, Math.Min(100, result.FullText.Length))}...\"";
                    TestResultTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    TestResultTextBlock.Text = "⚠ Audio recorded but no speech detected. Try speaking louder or closer to microphone.";
                    TestResultTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
                }
            }
            catch (Exception transcriptionEx)
            {
                _logger.LogError(transcriptionEx, "Transcription test failed");
                TestResultTextBlock.Text = $"✗ Transcription failed: {transcriptionEx.Message}";
                TestResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
            
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Microphone test failed");
            TestResultTextBlock.Text = $"✗ Test failed: {ex.Message}";
            TestResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
        }
        finally
        {
            TestMicrophoneButton.IsEnabled = true;
        }
    }

    private void ModelSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateModelInfo();
    }

    private void UpdateModelInfo()
    {
        if (ModelSizeComboBox.SelectedItem is not ComboBoxItem selectedItem)
            return;
            
        var modelSize = selectedItem.Tag?.ToString() ?? "Base";
        
        switch (modelSize.ToLower())
        {
            case "tiny":
                ModelInfoTextBlock.Text = "Tiny Model - Fast processing, basic accuracy";
                ModelSizeTextBlock.Text = "Size: ~39M parameters, ~1GB RAM";
                ModelPerformanceTextBlock.Text = "Performance: Very fast, suitable for testing";
                break;
            case "base":
                ModelInfoTextBlock.Text = "Base Model - Balanced performance and accuracy";
                ModelSizeTextBlock.Text = "Size: ~74M parameters, ~1GB RAM";
                ModelPerformanceTextBlock.Text = "Performance: Good accuracy with reasonable speed";
                break;
            case "small":
                ModelInfoTextBlock.Text = "Small Model - Good accuracy";
                ModelSizeTextBlock.Text = "Size: ~244M parameters, ~2GB RAM";
                ModelPerformanceTextBlock.Text = "Performance: Better accuracy, slower processing";
                break;
            case "medium":
                ModelInfoTextBlock.Text = "Medium Model - High accuracy";
                ModelSizeTextBlock.Text = "Size: ~769M parameters, ~5GB RAM";
                ModelPerformanceTextBlock.Text = "Performance: High accuracy, requires more resources";
                break;
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validate inputs
            if (!ValidateInputs())
                return;
                
            // Update settings object
            UpdateSettingsFromUI();
            
            // Save settings
            await _orchestrator.UpdateSettingsAsync(_settings);
            
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool ValidateInputs()
    {
        // Validate retention days
        if (!int.TryParse(RetentionDaysTextBox.Text, out var retentionDays) || retentionDays < 1)
        {
            MessageBox.Show("Retention period must be a positive number.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        
        // Validate hotkey
        if (string.IsNullOrWhiteSpace(HotkeyTextBox.Text))
        {
            MessageBox.Show("Please set a hotkey for push-to-talk.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        
        // Validate OpenRouter settings
        if (!int.TryParse(MaxTokensTextBox.Text, out var maxTokens) || maxTokens < 1 || maxTokens > 4000)
        {
            MessageBox.Show("Max tokens must be between 1 and 4000.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        // Validate audio notification settings
        if (EnableNotificationsCheckBox.IsChecked == true)
        {
            var audioPath = AudioFilePathTextBox.Text;
            
            // Check if audio file path is provided when notifications are enabled
            if (string.IsNullOrWhiteSpace(audioPath))
            {
                MessageBox.Show("Please select an audio file when notifications are enabled.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            // Check if file exists
            if (!File.Exists(audioPath))
            {
                MessageBox.Show("Selected audio file does not exist. Please choose a valid audio file.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            // Validate file format
            var extension = Path.GetExtension(audioPath)?.ToLowerInvariant();
            if (extension != ".wav" && extension != ".mp3")
            {
                MessageBox.Show("Audio file must be in .wav or .mp3 format.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            // Validate that at least one notification type is enabled
            if (!(PlayOnSpeechToTextCheckBox.IsChecked == true || PlayOnLlmResponseCheckBox.IsChecked == true))
            {
                MessageBox.Show("Please enable at least one notification type (Speech-to-Text or LLM Response) when notifications are enabled.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }
        
        // Validate volume range
        if (VolumeSlider.Value < 0.0 || VolumeSlider.Value > 1.0)
        {
            MessageBox.Show("Volume must be between 0% and 100%.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        
        return true;
    }

    private async void UpdateSettingsFromUI()
    {
        // General
        _settings.AutoStartWithWindows = AutoStartCheckBox.IsChecked ?? false;
        _settings.Theme = ((ComboBoxItem)ThemeComboBox.SelectedItem)?.Content?.ToString() ?? "Dark";
        _settings.Logging.EnableTranscriptionLogging = EnableLoggingCheckBox.IsChecked ?? true;
        _settings.Logging.SaveAudioFiles = SaveAudioFilesCheckBox.IsChecked ?? false;
        _settings.Logging.LogRetentionDays = int.Parse(RetentionDaysTextBox.Text);
        
        // Hotkeys
        _settings.Hotkeys.PushToTalkKey = HotkeyTextBox.Text;
        _settings.Hotkeys.LlmPromptKey = LlmHotkeyTextBox.Text;
        _settings.Hotkeys.RequireModifiers = RequireModifiersCheckBox.IsChecked ?? false;
        
        // Audio
        _settings.Audio.PreferredDeviceId = ((ComboBoxItem)MicrophoneComboBox.SelectedItem)?.Tag?.ToString() ?? "";
        
        var sampleRateText = ((ComboBoxItem)SampleRateComboBox.SelectedItem)?.Content?.ToString() ?? "16000 Hz";
        _settings.Audio.SampleRate = int.Parse(sampleRateText.Split(' ')[0]);
        
        var bufferSizeText = ((ComboBoxItem)BufferSizeComboBox.SelectedItem)?.Content?.ToString() ?? "1024";
        _settings.Audio.BufferSize = int.Parse(bufferSizeText);
        
        // Whisper
        _settings.Whisper.ModelSize = ((ComboBoxItem)ModelSizeComboBox.SelectedItem)?.Tag?.ToString() ?? "Base";
        _settings.Whisper.Language = ((ComboBoxItem)LanguageComboBox.SelectedItem)?.Tag?.ToString() ?? "auto";
        _settings.Whisper.EnableGpuAcceleration = EnableGpuCheckBox.IsChecked ?? true;
        
        // OpenRouter V3.0 Settings
        _settings.OpenRouter.SystemPrompt = SystemPromptTextBox.Text;
        _settings.OpenRouter.Temperature = TemperatureSlider.Value;
        _settings.OpenRouter.MaxTokens = int.Parse(MaxTokensTextBox.Text);
        _settings.OpenRouter.EnableStreaming = EnableStreamingCheckBox.IsChecked ?? true;
        
        // Save selected model
        if (ModelComboBox.SelectedItem is ComboBoxItem selectedModelItem && selectedModelItem.Tag != null)
        {
            _settings.OpenRouter.SelectedModel = selectedModelItem.Tag.ToString();
        }
        
        // Save API key to environment service
        var apiKey = ApiKeyPasswordBox.Password;
        if (!string.IsNullOrEmpty(apiKey))
        {
            try
            {
                await _environmentService.SaveApiKeyAsync(apiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save API key");
                // Note: Don't throw here as we want to continue saving other settings
            }
        }
        
        // Ollama V3.0 Settings
        _settings.Ollama.ServerUrl = OllamaServerUrlTextBox.Text;
        _settings.Ollama.SystemPrompt = OllamaSystemPromptTextBox.Text;
        _settings.Ollama.Temperature = OllamaTemperatureSlider.Value;
        _settings.Ollama.MaxTokens = int.Parse(OllamaMaxTokensTextBox.Text);
        _settings.Ollama.EnableStreaming = OllamaEnableStreamingCheckBox.IsChecked ?? true;
        
        // Save selected Ollama model
        if (OllamaModelComboBox.SelectedItem is ComboBoxItem selectedOllamaModelItem && selectedOllamaModelItem.Tag != null)
        {
            _settings.Ollama.SelectedModel = selectedOllamaModelItem.Tag.ToString();
        }

        // Audio Notification V3.0 Settings
        _settings.AudioNotification.EnableNotifications = EnableNotificationsCheckBox.IsChecked ?? false;
        _settings.AudioNotification.PlayOnSpeechToText = PlayOnSpeechToTextCheckBox.IsChecked ?? false;
        _settings.AudioNotification.PlayOnLlmResponse = PlayOnLlmResponseCheckBox.IsChecked ?? false;
        _settings.AudioNotification.AudioFilePath = AudioFilePathTextBox.Text;
        _settings.AudioNotification.Volume = VolumeSlider.Value;

        // TTS V3.6.5 Settings
        _settings.Tts.EnableTts = EnableTtsCheckBox.IsChecked ?? true;
        _settings.Tts.SpeechSpeed = (float)TtsSpeechSpeedSlider.Value;
        _settings.Tts.Volume = (float)TtsVolumeSlider.Value;
        _settings.Tts.MaxTextLength = int.Parse(TtsMaxTextLengthTextBox.Text);
        _settings.Tts.UseFallbackSapi = TtsUseFallbackSapiCheckBox.IsChecked ?? true;
        
        // Save selected TTS voice
        if (TtsVoiceComboBox.SelectedItem is ComboBoxItem selectedTtsVoiceItem && selectedTtsVoiceItem.Tag != null)
        {
            _settings.Tts.SelectedVoice = selectedTtsVoiceItem.Tag.ToString() ?? "expr-voice-2-f";
        }

        // Vision V3.6.3 Settings
        _settings.Vision.EnableVisionCapture = EnableVisionCheckBox.IsChecked ?? true;
        _settings.Vision.SystemPrompt = VisionSystemPromptTextBox.Text;
        
        // Save image processing settings
        var imageSizeTag = ((ComboBoxItem)VisionImageSizeComboBox.SelectedItem)?.Tag?.ToString() ?? "1024";
        _settings.Vision.ImageQuality = int.Parse(imageSizeTag) >= 1536 ? 95 : 85; // Higher quality for larger images
        
        // Save selected Vision model
        if (VisionModelComboBox.SelectedItem is ComboBoxItem selectedVisionModelItem && selectedVisionModelItem.Tag != null)
        {
            _settings.Vision.SelectedVisionModel = selectedVisionModelItem.Tag.ToString() ?? "";
        }
    }

    // TTS Event Handlers
    private async Task LoadTtsSettings()
    {
        try
        {
            EnableTtsCheckBox.IsChecked = _settings.Tts.EnableTts;
            TtsSpeechSpeedSlider.Value = _settings.Tts.SpeechSpeed;
            TtsSpeechSpeedValueTextBlock.Text = $"{_settings.Tts.SpeechSpeed:F1}x";
            TtsVolumeSlider.Value = _settings.Tts.Volume;
            TtsVolumeValueTextBlock.Text = $"{_settings.Tts.Volume * 100:F0}%";
            TtsMaxTextLengthTextBox.Text = _settings.Tts.MaxTextLength.ToString();
            TtsUseFallbackSapiCheckBox.IsChecked = _settings.Tts.UseFallbackSapi;

            // Load available voices
            await LoadTtsVoices();
            
            // Update Python status - but don't trigger initialization during settings load
            await RefreshTtsStatusLazy();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load TTS settings");
        }
    }

    private async Task LoadTtsVoices()
    {
        try
        {
            TtsVoiceComboBox.Items.Clear();
            var voices = _ttsEngine.GetAvailableVoices();
            
            foreach (var voice in voices)
            {
                var item = new ComboBoxItem
                {
                    Content = voice.Description,
                    Tag = voice.Id
                };
                TtsVoiceComboBox.Items.Add(item);
                
                if (voice.Id == _settings.Tts.SelectedVoice)
                {
                    TtsVoiceComboBox.SelectedItem = item;
                }
            }
            
            // If no voice is selected, select the first one
            if (TtsVoiceComboBox.SelectedItem == null && TtsVoiceComboBox.Items.Count > 0)
            {
                TtsVoiceComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load TTS voices");
        }
    }

    private void TtsVoiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TtsVoiceComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
        {
            var voiceId = selectedItem.Tag.ToString();
            var voices = _ttsEngine.GetAvailableVoices();
            var selectedVoice = voices.FirstOrDefault(v => v.Id == voiceId);
            
            if (selectedVoice != null)
            {
                TtsVoiceNameTextBlock.Text = selectedVoice.Description;
                TtsVoiceDescriptionTextBlock.Text = $"Gender: {selectedVoice.Gender}, Language: {selectedVoice.Language}";
                TtsVoiceDetailsTextBlock.Text = "KittenTTS neural voice with expressive capabilities";
            }
        }
    }

    private void TtsSpeechSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TtsSpeechSpeedValueTextBlock != null)
        {
            TtsSpeechSpeedValueTextBlock.Text = $"{e.NewValue:F1}x";
        }
    }

    private void TtsVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TtsVolumeValueTextBlock != null)
        {
            TtsVolumeValueTextBlock.Text = $"{e.NewValue * 100:F0}%";
        }
    }

    private async void RefreshTtsStatus_Click(object sender, RoutedEventArgs e)
    {
        await RefreshTtsStatus();
    }

    private async Task RefreshTtsStatus()
    {
        try
        {
            RefreshTtsStatusButton.IsEnabled = false;
            TtsPythonStatusTextBlock.Text = "Checking Python environment...";
            
            // Check Python availability
            var pythonAvailable = await _pythonManager.InitializeAsync();
            if (pythonAvailable)
            {
                TtsPythonStatusTextBlock.Text = "Python environment: Available";
                TtsPythonStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                TtsPythonStatusTextBlock.Text = "Python environment: Not available";
                TtsPythonStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
            
            // Check KittenTTS availability
            var kittenTtsAvailable = await _ttsEngine.IsAvailableAsync();
            if (kittenTtsAvailable)
            {
                TtsKittenStatusTextBlock.Text = "KittenTTS: Available and ready";
                TtsKittenStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                TtsKittenStatusTextBlock.Text = "KittenTTS: Not available, will use SAPI fallback";
                TtsKittenStatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
            }
            
            // Update engine status
            TtsEngineStatusTextBlock.Text = $"Engine: {_ttsEngine.EngineInfo}";
            TtsEngineStatusTextBlock.Foreground = System.Windows.Media.Brushes.Blue;
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh TTS status");
            TtsPythonStatusTextBlock.Text = "Status check failed";
            TtsPythonStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
        }
        finally
        {
            RefreshTtsStatusButton.IsEnabled = true;
        }
    }

    private async Task RefreshTtsStatusLazy()
    {
        try
        {
            // Don't trigger Python initialization during settings load - just show status based on current state
            if (_pythonManager.IsInitialized)
            {
                TtsPythonStatusTextBlock.Text = "Python environment: Available";
                TtsPythonStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                TtsPythonStatusTextBlock.Text = "Python environment: Not initialized (click Refresh to check)";
                TtsPythonStatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
            }
            
            // Update engine status without triggering initialization
            TtsEngineStatusTextBlock.Text = $"Engine: {_ttsEngine.EngineInfo}";
            TtsEngineStatusTextBlock.Foreground = System.Windows.Media.Brushes.Blue;
            
            // Show generic status for KittenTTS without checking availability
            TtsKittenStatusTextBlock.Text = "KittenTTS: Status unknown (click Refresh to check)";
            TtsKittenStatusTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh TTS status lazily");
            TtsPythonStatusTextBlock.Text = "Status display failed";
            TtsPythonStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    private async void TestTts_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            TestTtsButton.IsEnabled = false;
            
            // Set clipboard with test text
            var testText = "This is a KittenTTS test. The text-to-speech system is working correctly.";
            await _clipboardService.SetTextAsync(testText);
            
            // Generate TTS with current settings
            var options = new TtsOptions
            {
                Speed = (float)TtsSpeechSpeedSlider.Value,
                OutputFormat = TtsOutputFormat.Wav
            };
            
            var selectedVoiceId = ((ComboBoxItem)TtsVoiceComboBox.SelectedItem)?.Tag?.ToString() ?? "expr-voice-2-f";
            var result = await _ttsEngine.GenerateAudioAsync(testText, options);
            
            if (result.Success)
            {
                // Play the audio
                var audioPlayback = _serviceProvider.GetRequiredService<IAudioPlaybackService>();
                audioPlayback.Volume = (float)TtsVolumeSlider.Value;
                await audioPlayback.PlayAudioAsync(result.AudioData, CancellationToken.None);
                
                MessageBox.Show("TTS test completed successfully!", "TTS Test", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"TTS test failed: {result.ErrorMessage}", "TTS Test Failed", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TTS test failed");
            MessageBox.Show($"TTS test failed: {ex.Message}", "TTS Test Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            TestTtsButton.IsEnabled = true;
        }
    }

    // Vision Event Handlers
    private async Task LoadVisionSettings()
    {
        try
        {
            EnableVisionCheckBox.IsChecked = _settings.Vision.EnableVisionCapture;
            VisionSystemPromptTextBox.Text = _settings.Vision.SystemPrompt;
            
            // Load image size based on quality setting
            var imageSizeTag = _settings.Vision.ImageQuality >= 95 ? "1536" : "1024";
            VisionImageSizeComboBox.SelectedItem = VisionImageSizeComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag?.ToString() == imageSizeTag);
            
            // Set quality setting
            VisionImageQualityComboBox.SelectedItem = VisionImageQualityComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag?.ToString() == (_settings.Vision.ImageQuality >= 95 ? "high" : "balanced"));
                
            VisionShowPreviewCheckBox.IsChecked = true; // Default to enabled
            VisionAutoClipboardCheckBox.IsChecked = true; // Default to enabled

            // Load available vision models
            await LoadVisionModels();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Vision settings");
        }
    }

    private async Task LoadVisionModels()
    {
        try
        {
            VisionModelComboBox.Items.Clear();
            
            // Get available vision models from OpenRouter
            _availableVisionModels = await _openRouterService.GetAvailableVisionModelsAsync();
            
            foreach (var model in _availableVisionModels)
            {
                var item = new ComboBoxItem
                {
                    Content = $"{model.Name}",
                    Tag = model.Id
                };
                VisionModelComboBox.Items.Add(item);
                
                if (model.Id == _settings.Vision.SelectedVisionModel)
                {
                    VisionModelComboBox.SelectedItem = item;
                }
            }
            
            // If no model is selected, try to select a default vision model
            if (VisionModelComboBox.SelectedItem == null && VisionModelComboBox.Items.Count > 0)
            {
                // Try to find Claude or GPT-4 Vision first
                var preferredModel = VisionModelComboBox.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(item => item.Tag?.ToString()?.Contains("claude") == true ||
                                          item.Tag?.ToString()?.Contains("gpt-4") == true ||
                                          item.Tag?.ToString()?.Contains("vision") == true);
                                          
                if (preferredModel != null)
                {
                    VisionModelComboBox.SelectedItem = preferredModel;
                }
                else
                {
                    VisionModelComboBox.SelectedIndex = 0;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Vision models");
        }
    }

    private void VisionModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (VisionModelComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
        {
            var modelId = selectedItem.Tag.ToString();
            var selectedModel = _availableVisionModels.FirstOrDefault(v => v.Id == modelId);
            
            if (selectedModel != null)
            {
                VisionModelNameTextBlock.Text = selectedModel.Name;
                VisionModelDescriptionTextBlock.Text = selectedModel.Description;
                VisionModelDetailsTextBlock.Text = $"Pricing: ${selectedModel.PricePerMToken}/1M tokens, Context: {selectedModel.ContextLength:N0} tokens";
            }
        }
    }

    private async void RefreshVisionModels_Click(object sender, RoutedEventArgs e)
    {
        await LoadVisionModels();
    }

    private async void TestVisionCapture_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            TestVisionCaptureButton.IsEnabled = false;
            
            MessageBox.Show("The screen will be captured for vision analysis in 3 seconds. Position any content you want analyzed on your screen.", 
                           "Vision Test", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Small delay to let user position content
            await Task.Delay(3000);
            
            // Test vision capture using the service
            var result = await _visionProcessingService.CaptureAndAnalyzeAsync(null);
            
            if (!string.IsNullOrEmpty(result) && result != "Vision analysis failed: No screen area selected.")
            {
                var message = $"Vision test completed successfully!\n\nAnalysis result:\n{result.Substring(0, Math.Min(500, result.Length))}";
                if (result.Length > 500)
                {
                    message += "...";
                }
                
                MessageBox.Show(message, "Vision Test Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Vision test failed or was cancelled. Please ensure a vision-capable model is selected in OpenRouter/Ollama settings.", 
                               "Vision Test Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vision test failed");
            MessageBox.Show($"Vision test failed: {ex.Message}", "Vision Test Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            TestVisionCaptureButton.IsEnabled = true;
        }
    }

    // TTS Event Handlers - NEW
    private List<VoiceInfo> _availableVoices = new();

    private async void RefreshVoices_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshVoicesButton.IsEnabled = false;
            await LoadAvailableVoices();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh voices");
            MessageBox.Show($"Failed to refresh voices: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            RefreshVoicesButton.IsEnabled = true;
        }
    }

    private async Task LoadAvailableVoices()
    {
        try
        {
            VoiceComboBox.Items.Clear();
            _availableVoices = (await _ttsService.GetAvailableVoicesAsync()).ToList();

            // Add default option
            var defaultItem = new ComboBoxItem
            {
                Content = "System Default",
                Tag = ""
            };
            VoiceComboBox.Items.Add(defaultItem);

            // Add available voices
            foreach (var voice in _availableVoices)
            {
                var item = new ComboBoxItem
                {
                    Content = $"{voice.Name} ({voice.Gender}, {voice.Culture})",
                    Tag = voice.Name
                };
                VoiceComboBox.Items.Add(item);
            }

            // Select stored voice or default
            if (string.IsNullOrEmpty(_settings.TTS.SelectedVoice))
            {
                VoiceComboBox.SelectedItem = defaultItem;
            }
            else
            {
                var storedVoiceItem = VoiceComboBox.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(item => item.Tag?.ToString() == _settings.TTS.SelectedVoice);
                VoiceComboBox.SelectedItem = storedVoiceItem ?? defaultItem;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load available voices");
        }
    }

    private void VoiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Update settings immediately when voice changes
        if (_settings?.TTS != null && VoiceComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            _settings.TTS.SelectedVoice = selectedItem.Tag?.ToString() ?? "";
        }
    }

    private void SpeechRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Update settings immediately when rate changes
        if (_settings?.TTS != null)
        {
            _settings.TTS.Rate = (int)e.NewValue;
        }
    }

    private void SpeechVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SpeechVolumeValueTextBlock != null)
        {
            SpeechVolumeValueTextBlock.Text = $"{e.NewValue:F0}%";
        }

        // Update settings immediately when volume changes
        if (_settings?.TTS != null)
        {
            _settings.TTS.Volume = (int)e.NewValue;
        }
    }

    private async void TestVoice_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            TestVoiceButton.IsEnabled = false;
            
            // Configure TTS with current settings
            var selectedVoice = "";
            if (VoiceComboBox.SelectedItem is ComboBoxItem voiceItem && voiceItem.Tag != null)
            {
                selectedVoice = voiceItem.Tag.ToString() ?? "";
            }

            if (!string.IsNullOrEmpty(selectedVoice))
            {
                await _ttsService.SetVoiceAsync(selectedVoice);
            }
            await _ttsService.SetRateAsync((int)SpeechRateSlider.Value);
            await _ttsService.SetVolumeAsync((int)SpeechVolumeSlider.Value);

            // Test with a sample phrase
            await _ttsService.SpeakTextAsync("Hello, this is a test of the selected voice settings.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voice test failed");
            MessageBox.Show($"Voice test failed: {ex.Message}", "TTS Test Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            TestVoiceButton.IsEnabled = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
