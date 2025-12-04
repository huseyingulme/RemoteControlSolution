namespace RCS.Shared.Models;

/// <summary>
/// İlk bağlantı paketi - client bilgilerini gönderir
/// </summary>
public class ConnectionPacket
{
    public string PacketType => "CONNECTION";
    public ClientInfo ClientInfo { get; set; } = new();
}

