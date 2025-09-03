using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace CarelessWhisperV2.Services.Clipboard;

public class ClipboardService : IClipboardService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<string> GetTextAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await Task.Run(() =>
            {
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    throw new InvalidOperationException("Clipboard operations require STA thread");
                }

                return System.Windows.Clipboard.ContainsText() ? System.Windows.Clipboard.GetText() : string.Empty;
            });
        }
        catch (ExternalException ex)
        {
            // Handle clipboard access failures (common when other apps lock clipboard)
            throw new ClipboardException($"Failed to access clipboard: {ex.Message}", ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SetTextAsync(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        await _semaphore.WaitAsync();
        try
        {
            await Task.Run(() =>
            {
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    throw new InvalidOperationException("Clipboard operations require STA thread");
                }

                // Retry logic for clipboard access failures
                var attempts = 0;
                while (attempts < 3)
                {
                    try
                    {
                        System.Windows.Clipboard.SetText(text);
                        return;
                    }
                    catch (ExternalException) when (attempts < 2)
                    {
                        attempts++;
                        Thread.Sleep(100); // Brief delay before retry
                    }
                }
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> ContainsTextAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await Task.Run(() => System.Windows.Clipboard.ContainsText());
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
