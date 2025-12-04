using RCS.Shared.Models;
using RCS.ServerApp.Services;

namespace RCS.ServerApp.Services;

/// <summary>
/// Kontrol komutu gönderen servis
/// </summary>
public class ControlSender
{
    private readonly ListenerService _listenerService;

    public ControlSender(ListenerService listenerService)
    {
        _listenerService = listenerService;
    }

    /// <summary>
    /// Belirli bir client'a kontrol komutu gönderir
    /// </summary>
    public async Task SendControlAsync(string clientId, ControlPacket packet)
    {
        var connection = _listenerService.GetConnection(clientId);
        if (connection != null && connection.IsConnected)
        {
            await connection.SendControlPacketAsync(packet);
        }
    }

    /// <summary>
    /// Mouse hareketi gönderir
    /// </summary>
    public async Task SendMouseMoveAsync(string clientId, int x, int y)
    {
        var packet = new ControlPacket
        {
            Type = ControlType.MouseMove,
            X = x,
            Y = y
        };
        await SendControlAsync(clientId, packet);
    }

    /// <summary>
    /// Mouse tıklama gönderir
    /// </summary>
    public async Task SendMouseClickAsync(string clientId, int x, int y, MouseButton button)
    {
        var packet = new ControlPacket
        {
            Type = ControlType.MouseClick,
            X = x,
            Y = y,
            Button = button
        };
        await SendControlAsync(clientId, packet);
    }

    /// <summary>
    /// Mouse down gönderir
    /// </summary>
    public async Task SendMouseDownAsync(string clientId, int x, int y, MouseButton button)
    {
        var packet = new ControlPacket
        {
            Type = ControlType.MouseDown,
            X = x,
            Y = y,
            Button = button
        };
        await SendControlAsync(clientId, packet);
    }

    /// <summary>
    /// Mouse up gönderir
    /// </summary>
    public async Task SendMouseUpAsync(string clientId, int x, int y, MouseButton button)
    {
        var packet = new ControlPacket
        {
            Type = ControlType.MouseUp,
            X = x,
            Y = y,
            Button = button
        };
        await SendControlAsync(clientId, packet);
    }

    /// <summary>
    /// Mouse scroll gönderir
    /// </summary>
    public async Task SendMouseWheelAsync(string clientId, int delta)
    {
        var packet = new ControlPacket
        {
            Type = ControlType.MouseWheel,
            Y = delta
        };
        await SendControlAsync(clientId, packet);
    }

    /// <summary>
    /// Tuş basma gönderir
    /// </summary>
    public async Task SendKeyPressAsync(string clientId, int keyCode)
    {
        var packet = new ControlPacket
        {
            Type = ControlType.KeyPress,
            KeyCode = keyCode
        };
        await SendControlAsync(clientId, packet);
    }

    /// <summary>
    /// Metin gönderir
    /// </summary>
    public async Task SendTextAsync(string clientId, string text)
    {
        var packet = new ControlPacket
        {
            Type = ControlType.TextInput,
            Text = text
        };
        await SendControlAsync(clientId, packet);
    }
}

