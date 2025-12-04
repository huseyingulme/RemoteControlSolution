namespace RCS.Shared.Models;

/// <summary>
/// Kontrol komutu paketi (mouse/keyboard)
/// </summary>
public class ControlPacket
{
    public string PacketType => "CONTROL";
    public ControlType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public MouseButton Button { get; set; }
    public int KeyCode { get; set; }
    public bool IsKeyDown { get; set; }
    public string? Text { get; set; } // Metin gönderimi için
}

/// <summary>
/// Kontrol tipi
/// </summary>
public enum ControlType
{
    MouseMove,
    MouseDown,
    MouseUp,
    MouseClick,
    MouseWheel,
    KeyDown,
    KeyUp,
    KeyPress,
    TextInput
}

/// <summary>
/// Mouse buton tipi
/// </summary>
public enum MouseButton
{
    Left,
    Right,
    Middle,
    XButton1,
    XButton2
}

