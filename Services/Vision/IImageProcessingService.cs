using System.Drawing;
using System.Drawing.Imaging;

namespace CarelessWhisperV2.Services.Vision;

public interface IImageProcessingService
{
    /// <summary>
    /// Optimizes an image for vision API processing
    /// </summary>
    /// <param name="image">The source image</param>
    /// <param name="maxTokens">Maximum estimated tokens for the target model</param>
    /// <param name="preferredFormat">Preferred output format (PNG for UI, JPEG for photos)</param>
    /// <returns>Optimized image bytes</returns>
    byte[] OptimizeForVisionAPI(Bitmap image, int maxTokens = 1000, ImageFormat? preferredFormat = null);
    
    /// <summary>
    /// Converts image to base64 string for API requests
    /// </summary>
    /// <param name="imageBytes">Image bytes</param>
    /// <param name="format">Image format for MIME type</param>
    /// <returns>Base64 encoded image string</returns>
    string ConvertToBase64(byte[] imageBytes, ImageFormat format);
    
    /// <summary>
    /// Resizes image maintaining aspect ratio
    /// </summary>
    /// <param name="image">Source image</param>
    /// <param name="maxWidth">Maximum width</param>
    /// <param name="maxHeight">Maximum height</param>
    /// <returns>Resized image</returns>
    Bitmap ResizeImage(Bitmap image, int maxWidth, int maxHeight);
    
    /// <summary>
    /// Estimates token count for an image (rough approximation)
    /// </summary>
    /// <param name="width">Image width</param>
    /// <param name="height">Image height</param>
    /// <returns>Estimated token count</returns>
    int EstimateTokenCount(int width, int height);
    
    /// <summary>
    /// Compresses image to target file size
    /// </summary>
    /// <param name="image">Source image</param>
    /// <param name="maxSizeBytes">Maximum file size in bytes</param>
    /// <param name="format">Target format</param>
    /// <returns>Compressed image bytes</returns>
    byte[] CompressToTargetSize(Bitmap image, long maxSizeBytes, ImageFormat format);
    
    /// <summary>
    /// Detects if image contains primarily UI elements or photographic content
    /// </summary>
    /// <param name="image">Image to analyze</param>
    /// <returns>True if UI content (use PNG), False if photographic (use JPEG)</returns>
    bool IsUIContent(Bitmap image);
}

public class ImageOptimizationSettings
{
    public int MaxWidth { get; set; } = 2048;
    public int MaxHeight { get; set; } = 2048;
    public int MaxTokens { get; set; } = 1000;
    public long MaxFileSizeBytes { get; set; } = 2 * 1024 * 1024; // 2MB
    public ImageFormat PreferredFormat { get; set; } = ImageFormat.Png;
    public int JpegQuality { get; set; } = 85;
}
