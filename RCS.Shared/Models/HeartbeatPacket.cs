namespace RCS.Shared.Models;

/// <summary>
/// Bağlantı kalp atışı paketi (keep-alive)
/// </summary>
public class HeartbeatPacket
{
    public string PacketType => "HEARTBEAT";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ClientId { get; set; } = string.Empty;
}

