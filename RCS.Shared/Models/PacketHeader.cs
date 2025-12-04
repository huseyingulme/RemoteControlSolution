namespace RCS.Shared.Models;

/// <summary>
/// Paket başlığı - length-prefixed mesajlar için
/// </summary>
public class PacketHeader
{
    public string PacketType { get; set; } = string.Empty;
    public int DataLength { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

