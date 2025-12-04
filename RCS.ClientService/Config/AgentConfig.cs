namespace RCS.ClientService.Config;

/// <summary>
/// Agent konfigürasyonu
/// </summary>
public class AgentConfig
{
    public string ServerIp { get; set; } = "127.0.0.1";
    public int ServerPort { get; set; } = 9999;
    public int CaptureIntervalMs { get; set; } = 100; // 10 FPS için 100ms
    public int JpegQuality { get; set; } = 75;
    public int? MaxWidth { get; set; } = null; // null = original size
    public int? MaxHeight { get; set; } = null;
}

