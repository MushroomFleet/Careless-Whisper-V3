using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace CarelessWhisperV2.Services.ScreenCapture;

public class BitBltScreenCaptureService : IScreenCaptureService
{
    private readonly ILogger<BitBltScreenCaptureService> _logger;

    public BitBltScreenCaptureService(ILogger<BitBltScreenCaptureService> logger)
    {
        _logger = logger;
    }

    public async Task<Bitmap> CaptureScreenAsync(Rectangle bounds)
    {
        return await Task.Run(() => CaptureScreen(bounds));
    }

    public async Task<Bitmap> CaptureFullScreenAsync()
    {
        var bounds = GetVirtualScreenBounds();
        return await CaptureScreenAsync(bounds);
    }

    public Rectangle GetVirtualScreenBounds()
    {
        return new Rectangle(
            (int)SystemParameters.VirtualScreenLeft,
            (int)SystemParameters.VirtualScreenTop,
            (int)SystemParameters.VirtualScreenWidth,
            (int)SystemParameters.VirtualScreenHeight);
    }

    public bool IsModernCaptureSupported()
    {
        // BitBlt fallback doesn't use modern capture
        return false;
    }

    private Bitmap CaptureScreen(Rectangle bounds)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Validate bounds
            var screenBounds = GetVirtualScreenBounds();
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                _logger.LogWarning("Invalid capture bounds: {Bounds}", bounds);
                bounds = screenBounds;
            }

            // Ensure bounds are within screen limits
            bounds = Rectangle.Intersect(bounds, screenBounds);
            if (bounds.IsEmpty)
            {
                _logger.LogWarning("Capture bounds outside screen area, using full screen");
                bounds = screenBounds;
            }

            var bitmap = new Bitmap(bounds.Width, bounds.Height);
            
            using var graphics = Graphics.FromImage(bitmap);
            
            // Get device contexts
            var hdcDest = graphics.GetHdc();
            var hdcSrc = GetDC(IntPtr.Zero);

            try
            {
                // Perform BitBlt operation
                var success = BitBlt(
                    hdcDest, 0, 0, bounds.Width, bounds.Height,
                    hdcSrc, bounds.X, bounds.Y, SRCCOPY);

                if (!success)
                {
                    var error = GetLastError();
                    _logger.LogError("BitBlt failed with error code: {ErrorCode}", error);
                    throw new InvalidOperationException($"Screen capture failed with error: {error}");
                }

                var duration = DateTime.UtcNow - startTime;
                _logger.LogDebug("Screen captured successfully in {Duration}ms using BitBlt. Bounds: {Bounds}", 
                    duration.TotalMilliseconds, bounds);

                return bitmap;
            }
            finally
            {
                graphics.ReleaseHdc(hdcDest);
                ReleaseDC(IntPtr.Zero, hdcSrc);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture screen using BitBlt. Bounds: {Bounds}", bounds);
            throw;
        }
    }

    #region Win32 API

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight,
        IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

    [DllImport("kernel32.dll")]
    private static extern uint GetLastError();

    private const int SRCCOPY = 0x00CC0020;

    #endregion
}
