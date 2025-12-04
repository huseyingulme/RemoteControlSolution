namespace RCS.Shared.Models;

/// <summary>
/// Client bilgilerini tutan model
/// </summary>
public class ClientInfo
{
    public string ClientId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string OSVersion { get; set; } = string.Empty;
    public string[] ScreenResolutions { get; set; } = Array.Empty<string>();
    public string IpAddress { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
}

