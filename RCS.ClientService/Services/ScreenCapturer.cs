using System.Drawing;
using System.Drawing.Imaging;
using RCS.Shared.Utils;

namespace RCS.ClientService.Services;

/// <summary>
/// Ekran görüntüsü yakalama servisi
/// </summary>
public class ScreenCapturer : IDisposable
{
    private bool _disposed = false;

    public ScreenCapturer()
    {
    }

    /// <summary>
    /// Tüm ekranı yakalar ve Bitmap döndürür
    /// </summary>
    public Bitmap CaptureScreen()
    {
        var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }
        
        return bitmap;
    }

    /// <summary>
    /// Belirli bir ekranı yakalar (çoklu monitör desteği için)
    /// </summary>
    public Bitmap CaptureScreen(int screenIndex)
    {
        var screens = Screen.AllScreens;
        if (screenIndex < 0 || screenIndex >= screens.Length)
            screenIndex = 0;

        var screen = screens[screenIndex];
        var bounds = screen.Bounds;
        
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }
        
        return bitmap;
    }

    /// <summary>
    /// Ekranı yakalar ve JPEG byte array olarak döndürür
    /// </summary>
    public byte[] CaptureScreenAsJpeg(int quality = 75, int? maxWidth = null, int? maxHeight = null)
    {
        using var bitmap = CaptureScreen();
        
        Bitmap? processedBitmap = null;
        try
        {
            if (maxWidth.HasValue && maxHeight.HasValue)
            {
                processedBitmap = ImageUtils.ResizeBitmap(bitmap, maxWidth.Value, maxHeight.Value);
                return ImageUtils.BitmapToJpeg(processedBitmap, quality);
            }
            
            return ImageUtils.BitmapToJpeg(bitmap, quality);
        }
        finally
        {
            processedBitmap?.Dispose();
        }
    }

    /// <summary>
    /// Ekran çözünürlüğünü döndürür
    /// </summary>
    public (int Width, int Height) GetScreenResolution()
    {
        var screen = Screen.PrimaryScreen ?? Screen.AllScreens.FirstOrDefault();
        if (screen == null)
            return (1920, 1080);
            
        return (screen.Bounds.Width, screen.Bounds.Height);
    }

    /// <summary>
    /// Tüm ekran çözünürlüklerini döndürür
    /// </summary>
    public string[] GetAllScreenResolutions()
    {
        return Screen.AllScreens
            .Select(s => $"{s.Bounds.Width}x{s.Bounds.Height}")
            .ToArray();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}

