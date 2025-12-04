namespace RCS.ServerApp.Config;

/// <summary>
/// Server konfigürasyonu
/// </summary>
public class ServerConfig
{
    public int Port { get; set; } = 9999;
    public bool AutoStart { get; set; } = false;
    public int HeartbeatTimeoutSeconds { get; set; } = 15;
    public int MaxClients { get; set; } = 100;
    public string LogDirectory { get; set; } = "Logs";
}

