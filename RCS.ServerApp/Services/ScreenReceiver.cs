using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using RCS.Shared.Models;
using RCS.Shared.Utils;

namespace RCS.ServerApp.Services;

/// <summary>
/// Ekran görüntüsü alıcı servisi - byte array'leri BitmapImage'e çevirir
/// </summary>
public class ScreenReceiver
{
    /// <summary>
    /// ScreenPacket + ImageBytes'i BitmapImage'e çevirir
    /// </summary>
    public BitmapImage? ConvertToBitmapImage(ScreenPacket packet, byte[] imageBytes)
    {
        try
        {
            using var ms = new MemoryStream(imageBytes);
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = ms;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // Thread-safe için
            
            return bitmapImage;
        }
        catch (Exception ex)
        {
            // Logger eklenebilir
            return null;
        }
    }

    /// <summary>
    /// Byte array'i doğrudan BitmapImage'e çevirir
    /// </summary>
    public BitmapImage? ConvertBytesToBitmapImage(byte[] imageBytes)
    {
        try
        {
            using var ms = new MemoryStream(imageBytes);
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = ms;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            
            return bitmapImage;
        }
        catch
        {
            return null;
        }
    }
}

