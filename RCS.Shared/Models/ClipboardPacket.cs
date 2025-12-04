namespace RCS.Shared.Models;

/// <summary>
/// Clipboard paylaşım paketi
/// </summary>
public class ClipboardPacket
{
    public string PacketType => "CLIPBOARD";
    public ClipboardAction Action { get; set; }
    public string? Text { get; set; }
    public byte[]? ImageData { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum ClipboardAction
{
    Get,      // Server'dan clipboard al
    Set,      // Server'a clipboard gönder
    Sync      // Otomatik senkronizasyon
}

