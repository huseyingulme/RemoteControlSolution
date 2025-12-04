namespace RCS.Shared.Models;

/// <summary>
/// Sistem komutu paketi (restart, shutdown, lock, vb.)
/// </summary>
public class SystemCommandPacket
{
    public string PacketType => "SYSTEM_COMMAND";
    public SystemCommand Command { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum SystemCommand
{
    Restart,
    Shutdown,
    Lock,
    Logoff,
    Hibernate,
    Sleep
}

