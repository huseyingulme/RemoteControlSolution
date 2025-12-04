namespace RCS.Shared.Models;

/// <summary>
/// İstatistik paketi - FPS, latency, bandwidth bilgileri
/// </summary>
public class StatisticsPacket
{
    public string PacketType => "STATISTICS";
    public string ClientId { get; set; } = string.Empty;
    public double Fps { get; set; }
    public long LatencyMs { get; set; }
    public long BytesPerSecond { get; set; }
    public long TotalBytesReceived { get; set; }
    public long TotalBytesSent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

