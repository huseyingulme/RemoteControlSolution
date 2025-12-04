namespace RCS.Shared.Models;

/// <summary>
/// Ekran görüntüsü paketi
/// </summary>
public class ScreenPacket
{
    public string PacketType => "SCREEN";
    public long Sequence { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int Width { get; set; }
    public int Height { get; set; }
    public string Format { get; set; } = "jpeg"; // jpeg, webp
    public int ImageLength { get; set; }
    // ImageBytes ayrı binary olarak gönderilecek
}

