using System.Drawing;
using System.Drawing.Imaging;

namespace RCS.Shared.Utils;

/// <summary>
/// Görüntü işleme yardımcı fonksiyonları
/// </summary>
public static class ImageUtils
{
    /// <summary>
    /// Bitmap'i JPEG byte array'e çevirir
    /// </summary>
    public static byte[] BitmapToJpeg(Bitmap bitmap, int quality = 75)
    {
        if (bitmap == null)
            throw new ArgumentNullException(nameof(bitmap));
        
        if (quality < 1 || quality > 100)
            quality = 75; // Default quality
        
        using var ms = new MemoryStream();
        
        var encoder = ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid)
            ?? throw new InvalidOperationException("JPEG encoder not found");

        using var encoderParams = new EncoderParameters(1)
        {
            Param = { [0] = new EncoderParameter(Encoder.Quality, quality) }
        };

        bitmap.Save(ms, encoder, encoderParams);
        return ms.ToArray();
    }

    /// <summary>
    /// Byte array'den Bitmap oluşturur
    /// </summary>
    public static Bitmap BytesToBitmap(byte[] imageBytes)
    {
        using var ms = new MemoryStream(imageBytes);
        return new Bitmap(ms);
    }

    /// <summary>
    /// Bitmap'i belirtilen boyuta yeniden boyutlandırır
    /// </summary>
    public static Bitmap ResizeBitmap(Bitmap original, int maxWidth, int maxHeight)
    {
        if (original == null)
            throw new ArgumentNullException(nameof(original));
        
        if (maxWidth < 1 || maxHeight < 1)
            throw new ArgumentException("MaxWidth and MaxHeight must be greater than 0");
        
        if (original.Width <= maxWidth && original.Height <= maxHeight)
            return new Bitmap(original);

        double ratio = Math.Min((double)maxWidth / original.Width, (double)maxHeight / original.Height);
        int newWidth = Math.Max(1, (int)(original.Width * ratio));
        int newHeight = Math.Max(1, (int)(original.Height * ratio));

        var resized = new Bitmap(newWidth, newHeight);
        using (var g = Graphics.FromImage(resized))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.DrawImage(original, 0, 0, newWidth, newHeight);
        }

        return resized;
    }

    /// <summary>
    /// Bitmap'i byte array'e çevirir (PNG format)
    /// </summary>
    public static byte[] BitmapToBytes(Bitmap bitmap, ImageFormat format)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, format);
        return ms.ToArray();
    }
}

