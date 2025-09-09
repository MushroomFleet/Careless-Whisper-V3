using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Logging;
using System.IO;

namespace CarelessWhisperV2.Services.Vision;

public class ImageProcessingService : IImageProcessingService
{
    private readonly ILogger<ImageProcessingService> _logger;
    private readonly ImageOptimizationSettings _settings;

    public ImageProcessingService(ILogger<ImageProcessingService> logger)
    {
        _logger = logger;
        _settings = new ImageOptimizationSettings();
    }

    public byte[] OptimizeForVisionAPI(Bitmap image, int maxTokens = 1000, ImageFormat? preferredFormat = null)
    {
        if (image == null) throw new ArgumentNullException(nameof(image));

        var startTime = DateTime.UtcNow;

        try
        {
            // Determine optimal format based on content
            var format = preferredFormat ?? (IsUIContent(image) ? ImageFormat.Png : ImageFormat.Jpeg);
            
            // Estimate current token usage
            var currentTokens = EstimateTokenCount(image.Width, image.Height);
            _logger.LogDebug("Image optimization starting. Original size: {Width}x{Height}, Estimated tokens: {Tokens}, Target: {MaxTokens}", 
                image.Width, image.Height, currentTokens, maxTokens);

            Bitmap processedImage = image;
            
            // Resize if necessary to stay within token limits
            if (currentTokens > maxTokens)
            {
                var scaleFactor = Math.Sqrt((double)maxTokens / currentTokens);
                var newWidth = Math.Max(1, (int)(image.Width * scaleFactor));
                var newHeight = Math.Max(1, (int)(image.Height * scaleFactor));
                
                // Ensure we don't exceed maximum dimensions
                if (newWidth > _settings.MaxWidth || newHeight > _settings.MaxHeight)
                {
                    newWidth = Math.Min(newWidth, _settings.MaxWidth);
                    newHeight = Math.Min(newHeight, _settings.MaxHeight);
                }
                
                _logger.LogDebug("Resizing image from {OriginalWidth}x{OriginalHeight} to {NewWidth}x{NewHeight}", 
                    image.Width, image.Height, newWidth, newHeight);
                
                processedImage = ResizeImage(image, newWidth, newHeight);
            }

            // Store dimensions before potential disposal
            var finalWidth = processedImage.Width;
            var finalHeight = processedImage.Height;
            
            // Convert to bytes with compression
            var imageBytes = CompressToTargetSize(processedImage, _settings.MaxFileSizeBytes, format);
            
            // Clean up resized image if it's different from original
            if (processedImage != image)
            {
                processedImage.Dispose();
            }

            var duration = DateTime.UtcNow - startTime;
            var finalTokens = EstimateTokenCount(finalWidth, finalHeight);
            
            _logger.LogInformation("Image optimized in {Duration}ms. Final size: {Bytes} bytes, Estimated tokens: {Tokens}, Format: {Format}", 
                duration.TotalMilliseconds, imageBytes.Length, finalTokens, format.ToString());

            return imageBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize image for vision API");
            throw;
        }
    }

    public string ConvertToBase64(byte[] imageBytes, ImageFormat format)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            throw new ArgumentException("Image bytes cannot be null or empty", nameof(imageBytes));

        var mimeType = GetMimeType(format);
        var base64String = Convert.ToBase64String(imageBytes);
        
        return $"data:{mimeType};base64,{base64String}";
    }

    public Bitmap ResizeImage(Bitmap image, int maxWidth, int maxHeight)
    {
        if (image == null) throw new ArgumentNullException(nameof(image));
        
        // Calculate the scaling factor while maintaining aspect ratio
        var ratioX = (double)maxWidth / image.Width;
        var ratioY = (double)maxHeight / image.Height;
        var ratio = Math.Min(ratioX, ratioY);
        
        var newWidth = (int)(image.Width * ratio);
        var newHeight = (int)(image.Height * ratio);
        
        if (newWidth == image.Width && newHeight == image.Height)
        {
            // No resize needed, return copy
            return new Bitmap(image);
        }

        var resizedImage = new Bitmap(newWidth, newHeight);
        
        using (var graphics = Graphics.FromImage(resizedImage))
        {
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            
            graphics.DrawImage(image, 0, 0, newWidth, newHeight);
        }
        
        _logger.LogDebug("Image resized from {OriginalWidth}x{OriginalHeight} to {NewWidth}x{NewHeight}", 
            image.Width, image.Height, newWidth, newHeight);
        
        return resizedImage;
    }

    public int EstimateTokenCount(int width, int height)
    {
        // Rough approximation: 1 token per ~100 pixels
        // This is a conservative estimate and may vary by model
        return (width * height) / 100;
    }

    public byte[] CompressToTargetSize(Bitmap image, long maxSizeBytes, ImageFormat format)
    {
        if (image == null) throw new ArgumentNullException(nameof(image));

        if (format.Equals(ImageFormat.Png))
        {
            // PNG compression is lossless, just save normally
            using var stream = new MemoryStream();
            image.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
        
        // For JPEG, use quality compression
        var quality = _settings.JpegQuality;
        byte[] result;
        
        do
        {
            using var stream = new MemoryStream();
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            
            var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
            image.Save(stream, jpegEncoder, encoderParams);
            result = stream.ToArray();
            
            if (result.Length <= maxSizeBytes || quality <= 20)
                break;
                
            quality -= 10; // Reduce quality and try again
            
        } while (quality > 10);
        
        _logger.LogDebug("Image compressed to {Size} bytes with quality {Quality}", result.Length, quality);
        return result;
    }

    public bool IsUIContent(Bitmap image)
    {
        if (image == null) return false;

        try
        {
            // Sample pixels to detect UI characteristics
            // UI content typically has more uniform colors, sharp edges, and limited color palette
            var sampleSize = Math.Min(100, Math.Min(image.Width, image.Height));
            var uniqueColors = new HashSet<Color>();
            var sharpEdges = 0;
            var totalSamples = 0;

            for (int x = 0; x < image.Width; x += image.Width / sampleSize)
            {
                for (int y = 0; y < image.Height; y += image.Height / sampleSize)
                {
                    if (x < image.Width && y < image.Height)
                    {
                        var pixel = image.GetPixel(x, y);
                        uniqueColors.Add(pixel);
                        
                        // Check for sharp edges (high contrast with neighbors)
                        if (x > 0 && y > 0 && x < image.Width - 1 && y < image.Height - 1)
                        {
                            var neighbor = image.GetPixel(x + 1, y);
                            var contrast = Math.Abs(pixel.R - neighbor.R) + Math.Abs(pixel.G - neighbor.G) + Math.Abs(pixel.B - neighbor.B);
                            if (contrast > 100) sharpEdges++;
                        }
                        
                        totalSamples++;
                    }
                }
            }

            // UI content characteristics:
            // - Limited color palette (unique colors / total samples < 0.3)
            // - Many sharp edges (sharp edges / total samples > 0.2)
            var colorRatio = (double)uniqueColors.Count / totalSamples;
            var edgeRatio = (double)sharpEdges / totalSamples;
            
            var isUI = colorRatio < 0.3 && edgeRatio > 0.2;
            
            _logger.LogDebug("Content analysis: {UniqueColors} colors, {SharpEdges} sharp edges out of {TotalSamples} samples. Color ratio: {ColorRatio:F3}, Edge ratio: {EdgeRatio:F3}, IsUI: {IsUI}", 
                uniqueColors.Count, sharpEdges, totalSamples, colorRatio, edgeRatio, isUI);
            
            return isUI;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze image content, defaulting to UI content");
            return true; // Default to PNG for safety
        }
    }

    private string GetMimeType(ImageFormat format)
    {
        if (format.Equals(ImageFormat.Png)) return "image/png";
        if (format.Equals(ImageFormat.Jpeg)) return "image/jpeg";
        if (format.Equals(ImageFormat.Gif)) return "image/gif";
        if (format.Equals(ImageFormat.Bmp)) return "image/bmp";
        return "image/png"; // Default fallback
    }

    private ImageCodecInfo? GetEncoder(ImageFormat format)
    {
        var codecs = ImageCodecInfo.GetImageDecoders();
        return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid);
    }
}
