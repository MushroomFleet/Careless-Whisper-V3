using Microsoft.Extensions.Logging;
using CarelessWhisperV2.Models;
using System.IO;
using System.Diagnostics;

namespace CarelessWhisperV2.Services.Tts;

public class WindowsSapiEngine : ITtsEngine, IDisposable
{
    private readonly ILogger<WindowsSapiEngine> _logger;
    private bool _disposed;
    private List<TtsVoice>? _cachedVoices;

    public string EngineInfo => "Windows SAPI - Built-in text-to-speech fallback";

    public WindowsSapiEngine(ILogger<WindowsSapiEngine> logger)
    {
        _logger = logger;
    }

    public async Task<TtsResult> GenerateAudioAsync(string text, TtsOptions options)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WindowsSapiEngine));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate input text
            if (string.IsNullOrWhiteSpace(text))
            {
                return new TtsResult
                {
                    Success = false,
                    ErrorMessage = "Text cannot be empty"
                };
            }

            // Limit text length
            if (text.Length > 1000)
            {
                text = text.Substring(0, 1000);
                _logger.LogWarning("Text truncated to 1,000 characters for SAPI TTS fallback");
            }

            // Use PowerShell to invoke Windows SAPI for TTS
            var tempAudioFile = Path.GetTempFileName() + ".wav";
            
            try
            {
                // Escape text for PowerShell
                var escapedText = text.Replace("'", "''").Replace("`", "``");
                
                // PowerShell command to use Windows SAPI
                var powershellScript = $@"
Add-Type -AssemblyName System.Speech
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$synth.SetOutputToWaveFile('{tempAudioFile}')
$synth.Rate = {Math.Max(-10, Math.Min(10, (int)((options.Speed - 1.0f) * 10)))}
$synth.Speak('{escapedText}')
$synth.Dispose()
";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{powershellScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return new TtsResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to start PowerShell process",
                        GenerationTime = stopwatch.Elapsed
                    };
                }

                await process.WaitForExitAsync();
                var errorOutput = await process.StandardError.ReadToEndAsync();
                
                stopwatch.Stop();

                if (process.ExitCode != 0)
                {
                    return new TtsResult
                    {
                        Success = false,
                        ErrorMessage = $"PowerShell SAPI failed: {errorOutput}",
                        GenerationTime = stopwatch.Elapsed
                    };
                }

                if (File.Exists(tempAudioFile))
                {
                    var audioData = await File.ReadAllBytesAsync(tempAudioFile);
                    File.Delete(tempAudioFile);

                    _logger.LogInformation("SAPI TTS generation completed in {Duration}ms, output size: {Size} bytes", 
                        stopwatch.Elapsed.TotalMilliseconds, audioData.Length);

                    return new TtsResult
                    {
                        Success = true,
                        AudioData = audioData,
                        GenerationTime = stopwatch.Elapsed
                    };
                }
                else
                {
                    return new TtsResult
                    {
                        Success = false,
                        ErrorMessage = "SAPI audio file not created",
                        GenerationTime = stopwatch.Elapsed
                    };
                }
            }
            finally
            {
                // Cleanup temp file if it exists
                try
                {
                    if (File.Exists(tempAudioFile))
                        File.Delete(tempAudioFile);
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "SAPI TTS generation failed");
            
            return new TtsResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                GenerationTime = stopwatch.Elapsed
            };
        }
    }

    public Task<bool> IsAvailableAsync()
    {
        try
        {
            // Check if PowerShell and SAPI are available
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-Command \"Add-Type -AssemblyName System.Speech; Write-Output 'SAPI Available'\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit(5000);
            
            var available = process?.ExitCode == 0;
            _logger.LogDebug("SAPI availability check via PowerShell: {Available}", available);
            return Task.FromResult(available);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check SAPI availability");
            return Task.FromResult(false);
        }
    }

    public IEnumerable<TtsVoice> GetAvailableVoices()
    {
        if (_cachedVoices != null)
        {
            return _cachedVoices;
        }

        try
        {
            // Return basic fallback voices for SAPI
            _cachedVoices = new List<TtsVoice>
            {
                new TtsVoice { Id = "sapi-default", Description = "Windows Default Voice", Gender = "Unknown", Language = "en" },
                new TtsVoice { Id = "sapi-male", Description = "Windows Male Voice", Gender = "Male", Language = "en" },
                new TtsVoice { Id = "sapi-female", Description = "Windows Female Voice", Gender = "Female", Language = "en" }
            };

            _logger.LogInformation("Loaded {Count} SAPI fallback voices", _cachedVoices.Count);
            return _cachedVoices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SAPI voices");
            return new List<TtsVoice>();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cachedVoices = null;
            _disposed = true;
            _logger.LogDebug("WindowsSapiEngine disposed");
        }
    }
}
