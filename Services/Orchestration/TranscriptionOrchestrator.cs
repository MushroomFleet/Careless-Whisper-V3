using Microsoft.Extensions.Logging;
using CarelessWhisperV2.Services.Hotkeys;
using CarelessWhisperV2.Services.Audio;
using CarelessWhisperV2.Services.Transcription;
using CarelessWhisperV2.Services.Clipboard;
using CarelessWhisperV2.Services.Logging;
using CarelessWhisperV2.Services.Settings;
using CarelessWhisperV2.Services.OpenRouter;
using CarelessWhisperV2.Services.Ollama;
using CarelessWhisperV2.Services.AudioNotification;
using CarelessWhisperV2.Services.Vision;
using CarelessWhisperV2.Services.Tts;
using CarelessWhisperV2.Models;
using System.IO;
using System.Windows;

namespace CarelessWhisperV2.Services.Orchestration;

public class TranscriptionOrchestrator : IDisposable
{
    private readonly PushToTalkManager _hotkeyManager;
    private readonly IAudioService _audioService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly IClipboardService _clipboardService;
    private readonly ITranscriptionLogger _transcriptionLogger;
    private readonly ISettingsService _settingsService;
    private readonly IOpenRouterService _openRouterService; // NEW
    private readonly IOllamaService _ollamaService; // NEW
    private readonly IAudioNotificationService _audioNotificationService; // NEW
    private readonly IVisionProcessingService _visionProcessingService; // NEW - Vision capture
    private readonly TtsHotkeyHandler _ttsHotkeyHandler; // NEW V3.6.5 - TTS handler
    private readonly ILogger<TranscriptionOrchestrator> _logger;
    
    private string _currentRecordingPath = "";
    private string _capturedClipboardContent = ""; // Store clipboard content for copy-prompt mode
    private bool _disposed;
    private ApplicationSettings _settings = new();

    public event EventHandler<TranscriptionCompletedEventArgs>? TranscriptionCompleted;
    public event EventHandler<TranscriptionErrorEventArgs>? TranscriptionError;

    public TranscriptionOrchestrator(
        PushToTalkManager hotkeyManager,
        IAudioService audioService,
        ITranscriptionService transcriptionService,
        IClipboardService clipboardService,
        ITranscriptionLogger transcriptionLogger,
        ISettingsService settingsService,
        IOpenRouterService openRouterService, // NEW
        IOllamaService ollamaService, // NEW
        IAudioNotificationService audioNotificationService, // NEW
        IVisionProcessingService visionProcessingService, // NEW - Vision capture
        TtsHotkeyHandler ttsHotkeyHandler, // NEW V3.6.5
        ILogger<TranscriptionOrchestrator> logger)
    {
        _hotkeyManager = hotkeyManager;
        _audioService = audioService;
        _transcriptionService = transcriptionService;
        _clipboardService = clipboardService;
        _transcriptionLogger = transcriptionLogger;
        _settingsService = settingsService;
        _openRouterService = openRouterService; // NEW
        _ollamaService = ollamaService; // NEW
        _audioNotificationService = audioNotificationService; // NEW
        _visionProcessingService = visionProcessingService; // NEW - Vision capture
        _ttsHotkeyHandler = ttsHotkeyHandler; // NEW V3.6.5 - TTS handler
        _logger = logger;

        _hotkeyManager.TransmissionStarted += OnTransmissionStarted;
        _hotkeyManager.TransmissionEnded += OnTransmissionEnded;
        _hotkeyManager.LlmTransmissionStarted += OnLlmTransmissionStarted; // NEW
        _hotkeyManager.LlmTransmissionEnded += OnLlmTransmissionEnded; // NEW
        _hotkeyManager.CopyPromptTransmissionStarted += OnCopyPromptTransmissionStarted; // NEW for Ctrl+F2
        _hotkeyManager.CopyPromptTransmissionEnded += OnCopyPromptTransmissionEnded; // NEW for Ctrl+F2
        _hotkeyManager.VisionCaptureStarted += OnVisionCaptureStarted; // NEW for Shift+F3
        _hotkeyManager.VisionCaptureWithPromptStarted += OnVisionPttTransmissionStarted; // NEW for Ctrl+F3 PTT
        _hotkeyManager.VisionCaptureWithPromptEnded += OnVisionPttTransmissionEnded; // NEW for Ctrl+F3 PTT
        _hotkeyManager.TtsTriggered += OnTtsTriggered; // NEW for Ctrl+F1 TTS
        
        // Load settings
        _ = Task.Run(LoadSettingsAsync);
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing TranscriptionOrchestrator");
            
            // Initialize transcription service with current model
            await _transcriptionService.InitializeAsync(_settings.Whisper.ModelSize);
            
            _logger.LogInformation("TranscriptionOrchestrator initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize TranscriptionOrchestrator");
            throw;
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            _settings = await _settingsService.LoadSettingsAsync<ApplicationSettings>();
            _logger.LogDebug("Settings loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            _settings = new ApplicationSettings();
        }
    }

    private async void OnTransmissionStarted()
    {
        try
        {
            _currentRecordingPath = GenerateRecordingPath();
            await _audioService.StartRecordingAsync(_currentRecordingPath);
            _logger.LogInformation("Recording started: {Path}", _currentRecordingPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start recording");
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = "Failed to start recording"
            });
        }
    }

    private async void OnTransmissionEnded()
    {
        try
        {
            await _audioService.StopRecordingAsync();
            _logger.LogInformation("Recording stopped: {Path}", _currentRecordingPath);

            // Wait for file to be fully released - same fix as settings test
            await Task.Delay(1000);

            if (File.Exists(_currentRecordingPath))
            {
                var fileInfo = new FileInfo(_currentRecordingPath);
                _logger.LogInformation("Audio file created: {Path}, Size: {Size} bytes", _currentRecordingPath, fileInfo.Length);
                
                // Process transcription in background
                _ = Task.Run(async () => await ProcessTranscriptionAsync(_currentRecordingPath));
            }
            else
            {
                _logger.LogWarning("Audio file not found after recording: {Path}", _currentRecordingPath);
                TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
                {
                    Message = "Audio file not found after recording"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop recording");
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = "Failed to stop recording"
            });
        }
    }

    // NEW LLM event handlers
    private async void OnLlmTransmissionStarted()
    {
        try
        {
            _currentRecordingPath = GenerateRecordingPath();
            await _audioService.StartRecordingAsync(_currentRecordingPath);
            _logger.LogInformation("LLM recording started: {Path}", _currentRecordingPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start LLM recording");
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = "Failed to start LLM recording"
            });
        }
    }

    private async void OnLlmTransmissionEnded()
    {
        try
        {
            await _audioService.StopRecordingAsync();
            _logger.LogInformation("LLM recording stopped: {Path}", _currentRecordingPath);

            // Wait for file to be fully released
            await Task.Delay(1000);

            if (File.Exists(_currentRecordingPath))
            {
                var fileInfo = new FileInfo(_currentRecordingPath);
                _logger.LogInformation("LLM audio file created: {Path}, Size: {Size} bytes", _currentRecordingPath, fileInfo.Length);
                
                // Process LLM transcription in background
                _ = Task.Run(async () => await ProcessLlmTranscriptionAsync(_currentRecordingPath));
            }
            else
            {
                _logger.LogWarning("LLM audio file not found after recording: {Path}", _currentRecordingPath);
                TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
                {
                    Message = "LLM audio file not found after recording"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop LLM recording");
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = "Failed to stop LLM recording"
            });
        }
    }

    // NEW Copy-Prompt event handlers for Ctrl+F2
    private async void OnCopyPromptTransmissionStarted()
    {
        try
        {
            // CAPTURE CLIPBOARD CONTENT FIRST - before anything else that might overwrite it!
            _logger.LogInformation("CLIPBOARD DEBUG - Starting capture process. Thread: {ThreadId}, STA: {IsSTAThread}", 
                System.Threading.Thread.CurrentThread.ManagedThreadId, 
                System.Threading.Thread.CurrentThread.GetApartmentState() == System.Threading.ApartmentState.STA);
            
            try
            {
                // Capture clipboard content on the UI thread to ensure STA access
                _capturedClipboardContent = "";
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        _logger.LogInformation("CLIPBOARD DEBUG - Now on UI thread: {ThreadId}, STA: {IsSTAThread}", 
                            System.Threading.Thread.CurrentThread.ManagedThreadId, 
                            System.Threading.Thread.CurrentThread.GetApartmentState() == System.Threading.ApartmentState.STA);
                        
                        // First check if clipboard contains text
                        bool hasText = System.Windows.Clipboard.ContainsText();
                        _logger.LogInformation("CLIPBOARD DEBUG - ContainsText result: {HasText}", hasText);
                        
                        if (!hasText)
                        {
                            _logger.LogWarning("CLIPBOARD DEBUG - Clipboard reports no text content available!");
                            _capturedClipboardContent = "";
                            return;
                        }
                        
                        // Capture the clipboard content directly using WPF clipboard
                        _capturedClipboardContent = System.Windows.Clipboard.GetText();
                        
                        var contentLength = _capturedClipboardContent?.Length ?? 0;
                        var contentPreview = !string.IsNullOrWhiteSpace(_capturedClipboardContent) && _capturedClipboardContent.Length > 0 
                            ? _capturedClipboardContent.Substring(0, Math.Min(200, _capturedClipboardContent.Length))
                            : "[EMPTY]";
                        
                        _logger.LogInformation("CLIPBOARD CAPTURE - Length: {ContentLength}, Preview: '{ContentPreview}'", 
                            contentLength, contentPreview);
                        
                        if (string.IsNullOrWhiteSpace(_capturedClipboardContent))
                        {
                            _logger.LogWarning("CLIPBOARD CAPTURE - WARNING: Captured clipboard content is empty or null!");
                        }
                        else
                        {
                            _logger.LogInformation("CLIPBOARD CAPTURE - SUCCESS: Captured {Length} characters on UI thread", 
                                _capturedClipboardContent.Length);
                        }
                    }
                    catch (Exception uiThreadEx)
                    {
                        _logger.LogError(uiThreadEx, "CLIPBOARD CAPTURE - UI thread access failed: {Error}", uiThreadEx.Message);
                        _capturedClipboardContent = "";
                    }
                });
            }
            catch (Exception clipboardEx)
            {
                _logger.LogError(clipboardEx, "CLIPBOARD CAPTURE - Dispatcher invoke failed: {Error}", clipboardEx.Message);
                _capturedClipboardContent = "";
            }

            // Now start recording
            _currentRecordingPath = GenerateRecordingPath();
            await _audioService.StartRecordingAsync(_currentRecordingPath);
            _logger.LogInformation("Copy-prompt recording started: {Path}", _currentRecordingPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start copy-prompt recording");
            // Clear captured content on error to prevent stale data
            _capturedClipboardContent = "";
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = "Failed to start copy-prompt recording"
            });
        }
    }

    private async void OnCopyPromptTransmissionEnded()
    {
        try
        {
            await _audioService.StopRecordingAsync();
            _logger.LogInformation("Copy-prompt recording stopped: {Path}", _currentRecordingPath);

            // Wait for file to be fully released
            await Task.Delay(1000);

            if (File.Exists(_currentRecordingPath))
            {
                var fileInfo = new FileInfo(_currentRecordingPath);
                _logger.LogInformation("Copy-prompt audio file created: {Path}, Size: {Size} bytes", _currentRecordingPath, fileInfo.Length);
                
                // Process copy-prompt transcription in background
                _ = Task.Run(async () => await ProcessCopyPromptTranscriptionAsync(_currentRecordingPath));
            }
            else
            {
                _logger.LogWarning("Copy-prompt audio file not found after recording: {Path}", _currentRecordingPath);
                TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
                {
                    Message = "Copy-prompt audio file not found after recording"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop copy-prompt recording");
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = "Failed to stop copy-prompt recording"
            });
        }
    }

    private async Task ProcessTranscriptionAsync(string audioFilePath)
    {
        var startTime = DateTime.Now;
        
        try
        {
            _logger.LogInformation("Starting transcription: {Path}", audioFilePath);
            
            var result = await _transcriptionService.TranscribeAsync(audioFilePath);
            
            if (!string.IsNullOrWhiteSpace(result.FullText))
            {
                // Copy to clipboard on UI thread (required for STA thread)
                _logger.LogInformation("Attempting to copy text to clipboard: {Text}", result.FullText.Substring(0, Math.Min(50, result.FullText.Length)));
                
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Use direct WPF clipboard - safe and synchronous
                        System.Windows.Clipboard.SetText(result.FullText);
                        _logger.LogInformation("Successfully copied text to clipboard");
                    });

                    // Play audio notification after successful clipboard operation
                    if (_settings.AudioNotification.EnableNotifications && 
                        _settings.AudioNotification.PlayOnSpeechToText &&
                        !string.IsNullOrWhiteSpace(_settings.AudioNotification.AudioFilePath))
                    {
                        try
                        {
                            _audioNotificationService.SetVolume(_settings.AudioNotification.Volume);
                            await _audioNotificationService.PlayNotificationAsync(NotificationType.SpeechToText);
                            _logger.LogDebug("Audio notification played successfully");
                        }
                        catch (Exception audioEx)
                        {
                            _logger.LogWarning(audioEx, "Failed to play audio notification: {Error}", audioEx.Message);
                            // Don't throw - audio notification failure shouldn't break transcription
                        }
                    }
                }
                catch (Exception clipboardEx)
                {
                    _logger.LogError(clipboardEx, "Failed to copy text to clipboard: {Error}", clipboardEx.Message);
                    // Don't throw - continue with logging and event notification
                }
                
                // Log to file if enabled
                if (_settings.Logging.EnableTranscriptionLogging)
                {
                    var transcriptionEntry = new TranscriptionEntry
                    {
                        Timestamp = startTime,
                        FullText = result.FullText,
                        Segments = result.Segments,
                        Language = result.Language,
                        Duration = DateTime.Now - startTime,
                        ModelUsed = _settings.Whisper.ModelSize,
                        AudioFilePath = _settings.Logging.SaveAudioFiles ? audioFilePath : null
                    };
                    
                    await _transcriptionLogger.LogTranscriptionAsync(transcriptionEntry);
                }
                
                _logger.LogInformation("Transcription completed: {Text}", result.FullText);
                
                TranscriptionCompleted?.Invoke(this, new TranscriptionCompletedEventArgs
                {
                    TranscriptionResult = result,
                    ProcessingTime = DateTime.Now - startTime
                });
            }
            else
            {
                _logger.LogWarning("Transcription returned empty result");
                TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
                {
                    Message = "No speech detected in audio"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed: {Path}. Error details: {ErrorMessage}", audioFilePath, ex.Message);
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = $"Transcription processing failed: {ex.Message}"
            });
        }
        finally
        {
            // Clean up temporary audio file (unless settings say to keep it)
            if (!_settings.Logging.SaveAudioFiles)
            {
                try
                {
                    if (File.Exists(audioFilePath))
                    {
                        File.Delete(audioFilePath);
                        _logger.LogDebug("Deleted temporary audio file: {Path}", audioFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary file: {Path}", audioFilePath);
                }
            }
        }
    }

    // NEW LLM processing method
    private async Task ProcessLlmTranscriptionAsync(string audioFilePath)
    {
        var startTime = DateTime.Now;
        TranscriptionEntry? transcriptionEntry = null;
        
        try
        {
            _logger.LogInformation("Starting LLM transcription: {Path}", audioFilePath);
            
            // First, transcribe the audio
            var transcriptionResult = await _transcriptionService.TranscribeAsync(audioFilePath);
            
            if (string.IsNullOrWhiteSpace(transcriptionResult.FullText))
            {
                _logger.LogWarning("LLM transcription returned empty result");
                TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
                {
                    Message = "No speech detected in audio for LLM processing"
                });
                return;
            }

            _logger.LogInformation("LLM transcription completed, processing with {Provider}: {Text}", 
                _settings.SelectedLlmProvider,
                transcriptionResult.FullText.Substring(0, Math.Min(50, transcriptionResult.FullText.Length)));

            // Create base transcription entry - this will always be logged regardless of LLM success
            transcriptionEntry = new TranscriptionEntry
            {
                Timestamp = startTime,
                FullText = transcriptionResult.FullText,
                Segments = transcriptionResult.Segments,
                Language = transcriptionResult.Language,
                Duration = DateTime.Now - startTime,
                ModelUsed = $"Whisper:{_settings.Whisper.ModelSize}",
                AudioFilePath = _settings.Logging.SaveAudioFiles ? audioFilePath : null
            };

            // Process with selected LLM provider
            string llmResponse = "";
            string llmError = "";
            
            try
            {
                if (_settings.SelectedLlmProvider == LlmProvider.OpenRouter)
                {
                    if (await _openRouterService.IsConfiguredAsync())
                    {
                        llmResponse = await _openRouterService.ProcessPromptAsync(
                            transcriptionResult.FullText,
                            _settings.OpenRouter.SystemPrompt,
                            _settings.OpenRouter.SelectedModel);
                        transcriptionEntry.ModelUsed = $"Whisper:{_settings.Whisper.ModelSize} + OpenRouter:{_settings.OpenRouter.SelectedModel}";
                    }
                    else
                    {
                        llmError = "OpenRouter API not configured. Please check settings.";
                        _logger.LogError("OpenRouter service not configured for LLM processing");
                    }
                }
                else if (_settings.SelectedLlmProvider == LlmProvider.Ollama)
                {
                    if (await _ollamaService.IsConfiguredAsync())
                    {
                        llmResponse = await _ollamaService.ProcessPromptAsync(
                            transcriptionResult.FullText,
                            _settings.Ollama.SystemPrompt,
                            _settings.Ollama.SelectedModel);
                        transcriptionEntry.ModelUsed = $"Whisper:{_settings.Whisper.ModelSize} + Ollama:{_settings.Ollama.SelectedModel}";
                    }
                    else
                    {
                        llmError = "Ollama server not configured or not running. Please check settings.";
                        _logger.LogError("Ollama service not configured for LLM processing");
                    }
                }
            }
            catch (Exception llmEx)
            {
                llmError = $"{_settings.SelectedLlmProvider} processing failed: {llmEx.Message}";
                _logger.LogError(llmEx, "{Provider} processing failed: {Error}", _settings.SelectedLlmProvider, llmEx.Message);
            }

            // Update transcription entry based on LLM result
            if (!string.IsNullOrWhiteSpace(llmResponse))
            {
                // LLM processing succeeded
                transcriptionEntry.FullText = $"INPUT: {transcriptionResult.FullText}\n\nLLM RESPONSE: {llmResponse}";
                transcriptionEntry.Duration = DateTime.Now - startTime;

                // Copy LLM response to clipboard
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.Clipboard.SetText(llmResponse);
                        _logger.LogInformation("Successfully copied LLM response to clipboard");
                    });

                    // Play audio notification after successful LLM clipboard operation
                    if (_settings.AudioNotification.EnableNotifications && 
                        _settings.AudioNotification.PlayOnLlmResponse &&
                        !string.IsNullOrWhiteSpace(_settings.AudioNotification.AudioFilePath))
                    {
                        try
                        {
                            _audioNotificationService.SetVolume(_settings.AudioNotification.Volume);
                            await _audioNotificationService.PlayNotificationAsync(NotificationType.LlmResponse);
                            _logger.LogDebug("LLM audio notification played successfully");
                        }
                        catch (Exception audioEx)
                        {
                            _logger.LogWarning(audioEx, "Failed to play LLM audio notification: {Error}", audioEx.Message);
                            // Don't throw - audio notification failure shouldn't break transcription
                        }
                    }

                    _logger.LogInformation("LLM transcription completed with {Provider}: {Response}", 
                        _settings.SelectedLlmProvider,
                        llmResponse.Substring(0, Math.Min(100, llmResponse.Length)));

                    TranscriptionCompleted?.Invoke(this, new TranscriptionCompletedEventArgs
                    {
                        TranscriptionResult = new TranscriptionResult 
                        { 
                            FullText = llmResponse,
                            Language = transcriptionResult.Language,
                            Segments = transcriptionResult.Segments
                        },
                        ProcessingTime = DateTime.Now - startTime
                    });
                }
                catch (Exception clipboardEx)
                {
                    _logger.LogError(clipboardEx, "Failed to copy LLM response to clipboard: {Error}", clipboardEx.Message);
                    // Add error to transcription entry but still log it
                    transcriptionEntry.FullText += $"\n\nCLIPBOARD ERROR: {clipboardEx.Message}";
                }
            }
            else
            {
                // LLM processing failed or returned empty - still log the original transcription with error info
                if (!string.IsNullOrWhiteSpace(llmError))
                {
                    transcriptionEntry.FullText = $"INPUT: {transcriptionResult.FullText}\n\nLLM ERROR: {llmError}";
                }
                
                transcriptionEntry.Duration = DateTime.Now - startTime;

                _logger.LogWarning("{Provider} returned empty response or failed: {Error}", _settings.SelectedLlmProvider, llmError);
                TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
                {
                    Message = !string.IsNullOrWhiteSpace(llmError) ? llmError : $"{_settings.SelectedLlmProvider} processing returned empty response"
                });
            }

            // Always log the transcription entry if logging is enabled, regardless of LLM success/failure
            if (_settings.Logging.EnableTranscriptionLogging && transcriptionEntry != null)
            {
                await _transcriptionLogger.LogTranscriptionAsync(transcriptionEntry);
                _logger.LogDebug("Transcription entry logged to history: {Provider}", _settings.SelectedLlmProvider);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM transcription processing failed: {Path}. Error: {ErrorMessage}", audioFilePath, ex.Message);
            
            // Even on complete failure, try to log what we have if transcriptionEntry was created
            if (_settings.Logging.EnableTranscriptionLogging && transcriptionEntry != null)
            {
                transcriptionEntry.FullText += $"\n\nPROCESSING ERROR: {ex.Message}";
                transcriptionEntry.Duration = DateTime.Now - startTime;
                try
                {
                    await _transcriptionLogger.LogTranscriptionAsync(transcriptionEntry);
                    _logger.LogDebug("Error transcription entry logged to history");
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to log error transcription entry");
                }
            }
            
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = $"LLM processing failed: {ex.Message}"
            });
        }
        finally
        {
            // Clean up temporary audio file (unless settings say to keep it)
            if (!_settings.Logging.SaveAudioFiles)
            {
                try
                {
                    if (File.Exists(audioFilePath))
                    {
                        File.Delete(audioFilePath);
                        _logger.LogDebug("Deleted temporary LLM audio file: {Path}", audioFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary LLM file: {Path}", audioFilePath);
                }
            }
        }
    }

    // NEW Copy-Prompt processing method for Ctrl+F2
    private async Task ProcessCopyPromptTranscriptionAsync(string audioFilePath)
    {
        var startTime = DateTime.Now;
        TranscriptionEntry? transcriptionEntry = null;
        
        try
        {
            _logger.LogInformation("Starting copy-prompt transcription: {Path}", audioFilePath);
            
            // First, transcribe the audio
            var transcriptionResult = await _transcriptionService.TranscribeAsync(audioFilePath);
            
            if (string.IsNullOrWhiteSpace(transcriptionResult.FullText))
            {
                _logger.LogWarning("Copy-prompt transcription returned empty result");
                TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
                {
                    Message = "No speech detected in audio for copy-prompt processing"
                });
                return;
            }

            // Use the clipboard content we captured earlier when Ctrl+F2 was pressed
            string clipboardContent = _capturedClipboardContent ?? "";
            var clipboardPreview = !string.IsNullOrWhiteSpace(clipboardContent) && clipboardContent.Length > 0 
                ? clipboardContent.Substring(0, Math.Min(200, clipboardContent.Length))
                : "[EMPTY]";
            
            _logger.LogInformation("CLIPBOARD RETRIEVAL - Length: {ContentLength}, Preview: '{ClipboardPreview}'", 
                clipboardContent.Length, clipboardPreview);

            // Clear the captured content for next use
            _capturedClipboardContent = "";

            // Log speech transcription details
            var speechPreview = transcriptionResult.FullText.Length > 0 
                ? transcriptionResult.FullText.Substring(0, Math.Min(200, transcriptionResult.FullText.Length))
                : "[EMPTY]";
            _logger.LogInformation("SPEECH TRANSCRIPTION - Length: {SpeechLength}, Preview: '{SpeechPreview}'", 
                transcriptionResult.FullText.Length, speechPreview);

            // Create the combined prompt using the template: "{speech-transcription}, {copy-buffer text}"
            string combinedPrompt;
            if (!string.IsNullOrWhiteSpace(clipboardContent))
            {
                combinedPrompt = $"{transcriptionResult.FullText}, {clipboardContent}";
                _logger.LogInformation("PROMPT COMBINATION - Speech({SpeechLength}) + Clipboard({ClipboardLength}) = Total({TotalLength})", 
                    transcriptionResult.FullText.Length, clipboardContent.Length, combinedPrompt.Length);
            }
            else
            {
                combinedPrompt = transcriptionResult.FullText;
                _logger.LogWarning("PROMPT COMBINATION - No clipboard content found, using speech transcription only");
            }

            // Log the complete combined prompt being sent to LLM
            var combinedPreview = combinedPrompt.Length > 0 
                ? combinedPrompt.Substring(0, Math.Min(500, combinedPrompt.Length))
                : "[EMPTY]";
            _logger.LogInformation("COMBINED PROMPT TO LLM - Length: {TotalLength}, Preview: '{CombinedPreview}'", 
                combinedPrompt.Length, combinedPreview);

            // Create base transcription entry - this will always be logged regardless of LLM success
            transcriptionEntry = new TranscriptionEntry
            {
                Timestamp = startTime,
                FullText = $"SPEECH: {transcriptionResult.FullText}\nCLIPBOARD: {clipboardContent}\nCOMBINED: {combinedPrompt}",
                Segments = transcriptionResult.Segments,
                Language = transcriptionResult.Language,
                Duration = DateTime.Now - startTime,
                ModelUsed = $"Whisper:{_settings.Whisper.ModelSize}",
                AudioFilePath = _settings.Logging.SaveAudioFiles ? audioFilePath : null
            };

            // Process with selected LLM provider
            string llmResponse = "";
            string llmError = "";
            
            try
            {
                if (_settings.SelectedLlmProvider == LlmProvider.OpenRouter)
                {
                    if (await _openRouterService.IsConfiguredAsync())
                    {
                        llmResponse = await _openRouterService.ProcessPromptAsync(
                            combinedPrompt,
                            _settings.OpenRouter.SystemPrompt,
                            _settings.OpenRouter.SelectedModel);
                        transcriptionEntry.ModelUsed = $"Whisper:{_settings.Whisper.ModelSize} + OpenRouter:{_settings.OpenRouter.SelectedModel}";
                    }
                    else
                    {
                        llmError = "OpenRouter API not configured. Please check settings.";
                        _logger.LogError("OpenRouter service not configured for copy-prompt processing");
                    }
                }
                else if (_settings.SelectedLlmProvider == LlmProvider.Ollama)
                {
                    if (await _ollamaService.IsConfiguredAsync())
                    {
                        llmResponse = await _ollamaService.ProcessPromptAsync(
                            combinedPrompt,
                            _settings.Ollama.SystemPrompt,
                            _settings.Ollama.SelectedModel);
                        transcriptionEntry.ModelUsed = $"Whisper:{_settings.Whisper.ModelSize} + Ollama:{_settings.Ollama.SelectedModel}";
                    }
                    else
                    {
                        llmError = "Ollama server not configured or not running. Please check settings.";
                        _logger.LogError("Ollama service not configured for copy-prompt processing");
                    }
                }
            }
            catch (Exception llmEx)
            {
                llmError = $"{_settings.SelectedLlmProvider} processing failed: {llmEx.Message}";
                _logger.LogError(llmEx, "{Provider} copy-prompt processing failed: {Error}", _settings.SelectedLlmProvider, llmEx.Message);
            }

            // Update transcription entry based on LLM result
            if (!string.IsNullOrWhiteSpace(llmResponse))
            {
                // LLM processing succeeded
                transcriptionEntry.FullText += $"\n\nLLM RESPONSE: {llmResponse}";
                transcriptionEntry.Duration = DateTime.Now - startTime;

                // Copy LLM response to clipboard
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.Clipboard.SetText(llmResponse);
                        _logger.LogInformation("Successfully copied copy-prompt LLM response to clipboard");
                    });

                    // Play audio notification after successful copy-prompt clipboard operation
                    if (_settings.AudioNotification.EnableNotifications && 
                        _settings.AudioNotification.PlayOnLlmResponse &&
                        !string.IsNullOrWhiteSpace(_settings.AudioNotification.AudioFilePath))
                    {
                        try
                        {
                            _audioNotificationService.SetVolume(_settings.AudioNotification.Volume);
                            await _audioNotificationService.PlayNotificationAsync(NotificationType.LlmResponse);
                            _logger.LogDebug("Copy-prompt audio notification played successfully");
                        }
                        catch (Exception audioEx)
                        {
                            _logger.LogWarning(audioEx, "Failed to play copy-prompt audio notification: {Error}", audioEx.Message);
                            // Don't throw - audio notification failure shouldn't break transcription
                        }
                    }

                    _logger.LogInformation("Copy-prompt transcription completed with {Provider}: {Response}", 
                        _settings.SelectedLlmProvider,
                        llmResponse.Substring(0, Math.Min(100, llmResponse.Length)));

                    TranscriptionCompleted?.Invoke(this, new TranscriptionCompletedEventArgs
                    {
                        TranscriptionResult = new TranscriptionResult 
                        { 
                            FullText = llmResponse,
                            Language = transcriptionResult.Language,
                            Segments = transcriptionResult.Segments
                        },
                        ProcessingTime = DateTime.Now - startTime
                    });
                }
                catch (Exception clipboardEx)
                {
                    _logger.LogError(clipboardEx, "Failed to copy copy-prompt LLM response to clipboard: {Error}", clipboardEx.Message);
                    // Add error to transcription entry but still log it
                    transcriptionEntry.FullText += $"\n\nCLIPBOARD ERROR: {clipboardEx.Message}";
                }
            }
            else
            {
                // LLM processing failed or returned empty - still log the original transcription with error info
                if (!string.IsNullOrWhiteSpace(llmError))
                {
                    transcriptionEntry.FullText += $"\n\nLLM ERROR: {llmError}";
                }
                
                transcriptionEntry.Duration = DateTime.Now - startTime;

                _logger.LogWarning("{Provider} copy-prompt returned empty response or failed: {Error}", _settings.SelectedLlmProvider, llmError);
                TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
                {
                    Message = !string.IsNullOrWhiteSpace(llmError) ? llmError : $"{_settings.SelectedLlmProvider} copy-prompt processing returned empty response"
                });
            }

            // Always log the transcription entry if logging is enabled, regardless of LLM success/failure
            if (_settings.Logging.EnableTranscriptionLogging && transcriptionEntry != null)
            {
                await _transcriptionLogger.LogTranscriptionAsync(transcriptionEntry);
                _logger.LogDebug("Copy-prompt transcription entry logged to history: {Provider}", _settings.SelectedLlmProvider);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy-prompt transcription processing failed: {Path}. Error: {ErrorMessage}", audioFilePath, ex.Message);
            
            // Even on complete failure, try to log what we have if transcriptionEntry was created
            if (_settings.Logging.EnableTranscriptionLogging && transcriptionEntry != null)
            {
                transcriptionEntry.FullText += $"\n\nPROCESSING ERROR: {ex.Message}";
                transcriptionEntry.Duration = DateTime.Now - startTime;
                try
                {
                    await _transcriptionLogger.LogTranscriptionAsync(transcriptionEntry);
                    _logger.LogDebug("Error copy-prompt transcription entry logged to history");
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to log error copy-prompt transcription entry");
                }
            }
            
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = $"Copy-prompt processing failed: {ex.Message}"
            });
        }
        finally
        {
            // Clean up temporary audio file (unless settings say to keep it)
            if (!_settings.Logging.SaveAudioFiles)
            {
                try
                {
                    if (File.Exists(audioFilePath))
                    {
                        File.Delete(audioFilePath);
                        _logger.LogDebug("Deleted temporary copy-prompt audio file: {Path}", audioFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary copy-prompt file: {Path}", audioFilePath);
                }
            }
        }
    }

    // NEW Vision event handlers for F3 hotkeys
    private async void OnVisionCaptureStarted()
    {
        try
        {
            _logger.LogInformation("Vision capture started (Shift+F3)");
            
            // Process vision capture in background to avoid blocking
            _ = Task.Run(async () => await ProcessVisionCaptureAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start vision capture");
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = "Failed to start vision capture"
            });
        }
    }

    private async void OnVisionCaptureWithPromptStarted()
    {
        try
        {
            _logger.LogInformation("Vision capture with prompt started (Ctrl+F3)");
            
            // Process vision capture with prompt in background
            _ = Task.Run(async () => await ProcessVisionCaptureWithPromptAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start vision capture with prompt");
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = "Failed to start vision capture with prompt"
            });
        }
    }

    private async Task ProcessVisionCaptureAsync()
    {
        var startTime = DateTime.Now;
        
        try
        {
            _logger.LogInformation("Processing vision capture workflow (Shift+F3)");
            
            // Use the vision processing service to capture and analyze
            var result = await _visionProcessingService.CaptureAndAnalyzeAsync();
            
            if (!string.IsNullOrWhiteSpace(result))
            {
                _logger.LogInformation("Vision analysis completed: {Preview}", 
                    result.Substring(0, Math.Min(100, result.Length)));
                
                // Fire completion event
                TranscriptionCompleted?.Invoke(this, new TranscriptionCompletedEventArgs
                {
                    TranscriptionResult = new TranscriptionResult 
                    { 
                        FullText = result,
                        Language = "en", // Default for vision analysis
                        Segments = new List<TranscriptionSegment>()
                    },
                    ProcessingTime = DateTime.Now - startTime
                });
            }
            else
            {
                _logger.LogWarning("Vision capture returned empty result");
                TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
                {
                    Message = "Vision capture was cancelled or returned empty result"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vision capture processing failed: {Error}", ex.Message);
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = $"Vision capture failed: {ex.Message}"
            });
        }
    }

    private async Task ProcessVisionCaptureWithPromptAsync()
    {
        // This method is currently not used because Ctrl+F3 needs PTT behavior
        // We'll implement the PTT+Vision workflow in new methods
        _logger.LogInformation("ProcessVisionCaptureWithPromptAsync called - this should not happen with new PTT implementation");
        
        TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
        {
            Message = "Ctrl+F3 should use PTT behavior, not immediate trigger"
        });
    }

    // NEW Vision PTT event handlers for Ctrl+F3
    private async void OnVisionPttTransmissionStarted()
    {
        try
        {
            _logger.LogInformation("Vision PTT recording started (Ctrl+F3 hold)");
            _currentRecordingPath = GenerateRecordingPath();
            await _audioService.StartRecordingAsync(_currentRecordingPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start vision PTT recording");
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = "Failed to start vision PTT recording"
            });
        }
    }

    private async void OnVisionPttTransmissionEnded()
    {
        try
        {
            await _audioService.StopRecordingAsync();
            _logger.LogInformation("Vision PTT recording stopped: {Path}", _currentRecordingPath);

            // Wait for file to be fully released
            await Task.Delay(1000);

            if (File.Exists(_currentRecordingPath))
            {
                var fileInfo = new FileInfo(_currentRecordingPath);
                _logger.LogInformation("Vision PTT audio file created: {Path}, Size: {Size} bytes", _currentRecordingPath, fileInfo.Length);
                
                // Process speech+vision workflow in background
                _ = Task.Run(async () => await ProcessSpeechPlusVisionAsync(_currentRecordingPath));
            }
            else
            {
                _logger.LogWarning("Vision PTT audio file not found after recording: {Path}", _currentRecordingPath);
                TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
                {
                    Message = "Vision PTT audio file not found after recording"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop vision PTT recording");
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = "Failed to stop vision PTT recording"
            });
        }
    }

    // NEW TTS event handler for Ctrl+F1
    private async void OnTtsTriggered()
    {
        try
        {
            _logger.LogInformation("TTS triggered (Ctrl+F1)");
            
            // Process TTS in background to avoid blocking
            _ = Task.Run(async () => await ProcessTtsAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger TTS");
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = "Failed to trigger TTS"
            });
        }
    }

    private async Task ProcessTtsAsync()
    {
        try
        {
            _logger.LogInformation("Processing TTS workflow (Ctrl+F1) - delegating to TtsHotkeyHandler");
            
            // Delegate TTS processing to the dedicated TTS handler
            await _ttsHotkeyHandler.HandleCtrlF1Async();
            
            // Fire completion event to indicate TTS was triggered
            TranscriptionCompleted?.Invoke(this, new TranscriptionCompletedEventArgs
            {
                TranscriptionResult = new TranscriptionResult 
                { 
                    FullText = "🐱 KittenTTS clipboard reading initiated",
                    Language = "en",
                    Segments = new List<TranscriptionSegment>()
                },
                ProcessingTime = TimeSpan.FromMilliseconds(50)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TTS processing failed: {Error}", ex.Message);
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = $"TTS processing failed: {ex.Message}"
            });
        }
    }

    // NEW Speech+Vision combined processing for Ctrl+F3
    private async Task ProcessSpeechPlusVisionAsync(string audioFilePath)
    {
        var startTime = DateTime.Now;
        TranscriptionEntry? transcriptionEntry = null;
        
        try
        {
            _logger.LogInformation("SPEECH+VISION: Starting speech+vision processing workflow");
            
            // First, transcribe the speech
            var transcriptionResult = await _transcriptionService.TranscribeAsync(audioFilePath);
            
            if (string.IsNullOrWhiteSpace(transcriptionResult.FullText))
            {
                _logger.LogWarning("SPEECH+VISION: Speech transcription returned empty result");
                TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
                {
                    Message = "No speech detected in audio for speech+vision processing"
                });
                return;
            }

            _logger.LogInformation("SPEECH+VISION: Speech transcribed: '{Speech}'", 
                transcriptionResult.FullText.Substring(0, Math.Min(100, transcriptionResult.FullText.Length)));

            // Now capture the screen area
            _logger.LogInformation("SPEECH+VISION: Showing screen capture overlay for user selection");
            var selectedArea = await _visionProcessingService.CaptureAndAnalyzeAsync();
            
            if (string.IsNullOrWhiteSpace(selectedArea) || selectedArea.StartsWith("Screen capture was cancelled"))
            {
                _logger.LogWarning("SPEECH+VISION: Screen capture was cancelled by user");
                TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
                {
                    Message = "Screen capture was cancelled"
                });
                return;
            }

            // Combine speech transcription with vision analysis
            var combinedResult = $"SPEECH: {transcriptionResult.FullText}\n\nVISION: {selectedArea}";
            
            _logger.LogInformation("SPEECH+VISION: Combined processing completed. Speech: {SpeechLength} chars, Vision: {VisionLength} chars", 
                transcriptionResult.FullText.Length, selectedArea.Length);

            // Create transcription entry
            transcriptionEntry = new TranscriptionEntry
            {
                Timestamp = startTime,
                FullText = combinedResult,
                Segments = transcriptionResult.Segments,
                Language = transcriptionResult.Language,
                Duration = DateTime.Now - startTime,
                ModelUsed = $"Whisper:{_settings.Whisper.ModelSize} + Vision:{_settings.SelectedLlmProvider}",
                AudioFilePath = _settings.Logging.SaveAudioFiles ? audioFilePath : null
            };

            // Copy combined result to clipboard
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.Clipboard.SetText(combinedResult);
                _logger.LogInformation("SPEECH+VISION: Combined result copied to clipboard");
            });

            // Log transcription if enabled
            if (_settings.Logging.EnableTranscriptionLogging)
            {
                await _transcriptionLogger.LogTranscriptionAsync(transcriptionEntry);
            }

            // Fire completion event
            TranscriptionCompleted?.Invoke(this, new TranscriptionCompletedEventArgs
            {
                TranscriptionResult = new TranscriptionResult 
                { 
                    FullText = combinedResult,
                    Language = transcriptionResult.Language,
                    Segments = transcriptionResult.Segments
                },
                ProcessingTime = DateTime.Now - startTime
            });

            _logger.LogInformation("SPEECH+VISION: Workflow completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SPEECH+VISION: Processing failed: {Error}", ex.Message);
            
            // Try to log what we have if transcriptionEntry was created
            if (_settings.Logging.EnableTranscriptionLogging && transcriptionEntry != null)
            {
                transcriptionEntry.FullText += $"\n\nPROCESSING ERROR: {ex.Message}";
                transcriptionEntry.Duration = DateTime.Now - startTime;
                try
                {
                    await _transcriptionLogger.LogTranscriptionAsync(transcriptionEntry);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to log error speech+vision entry");
                }
            }
            
            TranscriptionError?.Invoke(this, new TranscriptionErrorEventArgs
            {
                Exception = ex,
                Message = $"Speech+vision processing failed: {ex.Message}"
            });
        }
        finally
        {
            // Clean up temporary audio file
            if (!_settings.Logging.SaveAudioFiles)
            {
                try
                {
                    if (File.Exists(audioFilePath))
                    {
                        File.Delete(audioFilePath);
                        _logger.LogDebug("Deleted temporary speech+vision audio file: {Path}", audioFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary speech+vision file: {Path}", audioFilePath);
                }
            }
        }
    }

    private string GenerateRecordingPath()
    {
        var tempPath = Path.GetTempPath();
        var fileName = $"whisper_recording_{DateTime.Now:yyyyMMdd_HHmmss_fff}.wav";
        return Path.Combine(tempPath, fileName);
    }

    public bool IsRecording => _audioService.IsRecording;

    public bool IsTransmitting => _hotkeyManager.IsTransmitting;

    public async Task UpdateSettingsAsync(ApplicationSettings newSettings)
    {
        try
        {
            await _settingsService.SaveSettingsAsync(newSettings);
            _settings = newSettings;
            
            // Reinitialize transcription service if model changed
            if (!_transcriptionService.IsInitialized || _settings.Whisper.ModelSize != newSettings.Whisper.ModelSize)
            {
                await _transcriptionService.InitializeAsync(newSettings.Whisper.ModelSize);
            }
            
            _logger.LogInformation("Settings updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update settings");
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _hotkeyManager?.Dispose();
            _audioService?.Dispose();
            _transcriptionService?.Dispose();
            
            // Clear any captured clipboard content
            _capturedClipboardContent = "";
            
            _disposed = true;
            _logger.LogInformation("TranscriptionOrchestrator disposed");
        }
    }
}

public class TranscriptionCompletedEventArgs : EventArgs
{
    public TranscriptionResult TranscriptionResult { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
}

public class TranscriptionErrorEventArgs : EventArgs
{
    public string Message { get; set; } = "";
    public Exception? Exception { get; set; }
}
