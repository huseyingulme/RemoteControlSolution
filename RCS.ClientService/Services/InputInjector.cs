using System.Runtime.InteropServices;
using RCS.Shared.Models;

namespace RCS.ClientService.Services;

/// <summary>
/// Windows API kullanarak mouse ve keyboard girişlerini simüle eder
/// </summary>
public class InputInjector
{
    private const int MOUSEEVENTF_LEFTDOWN = 0x02;
    private const int MOUSEEVENTF_LEFTUP = 0x04;
    private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
    private const int MOUSEEVENTF_RIGHTUP = 0x10;
    private const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
    private const int MOUSEEVENTF_MIDDLEUP = 0x40;
    private const int MOUSEEVENTF_MOVE = 0x0001;
    private const int MOUSEEVENTF_ABSOLUTE = 0x8000;
    private const int MOUSEEVENTF_WHEEL = 0x0800;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;

    /// <summary>
    /// Mouse'u belirtilen koordinatlara taşır
    /// </summary>
    public void MoveMouse(int x, int y)
    {
        // Mutlak koordinat modunda (0-65535 arası normalize edilmiş)
        var screenWidth = System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Width ?? 1920;
        var screenHeight = System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Height ?? 1080;
        
        int normalizedX = (int)((double)x * 65535.0 / screenWidth);
        int normalizedY = (int)((double)y * 65535.0 / screenHeight);
        
        var inputs = new INPUT[1];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].U.mi.dx = normalizedX;
        inputs[0].U.mi.dy = normalizedY;
        inputs[0].U.mi.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE;
        inputs[0].U.mi.time = 0;
        inputs[0].U.mi.dwExtraInfo = IntPtr.Zero;
        
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// Mouse butonuna basar
    /// </summary>
    public void MouseDown(MouseButton button)
    {
        uint flags = button switch
        {
            MouseButton.Left => MOUSEEVENTF_LEFTDOWN,
            MouseButton.Right => MOUSEEVENTF_RIGHTDOWN,
            MouseButton.Middle => MOUSEEVENTF_MIDDLEDOWN,
            _ => MOUSEEVENTF_LEFTDOWN
        };

        var inputs = new INPUT[1];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].U.mi.dwFlags = flags;
        inputs[0].U.mi.time = 0;
        inputs[0].U.mi.dwExtraInfo = IntPtr.Zero;
        
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// Mouse butonunu bırakır
    /// </summary>
    public void MouseUp(MouseButton button)
    {
        uint flags = button switch
        {
            MouseButton.Left => MOUSEEVENTF_LEFTUP,
            MouseButton.Right => MOUSEEVENTF_RIGHTUP,
            MouseButton.Middle => MOUSEEVENTF_MIDDLEUP,
            _ => MOUSEEVENTF_LEFTUP
        };

        var inputs = new INPUT[1];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].U.mi.dwFlags = flags;
        inputs[0].U.mi.time = 0;
        inputs[0].U.mi.dwExtraInfo = IntPtr.Zero;
        
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// Mouse tıklama yapar (down + up)
    /// </summary>
    public void MouseClick(MouseButton button)
    {
        MouseDown(button);
        Thread.Sleep(10);
        MouseUp(button);
    }

    /// <summary>
    /// Mouse scroll yapar
    /// </summary>
    public void MouseWheel(int delta)
    {
        var inputs = new INPUT[1];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].U.mi.dwFlags = MOUSEEVENTF_WHEEL;
        inputs[0].U.mi.mouseData = (uint)delta;
        inputs[0].U.mi.time = 0;
        inputs[0].U.mi.dwExtraInfo = IntPtr.Zero;
        
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// Tuşa basar
    /// </summary>
    public void KeyDown(int virtualKeyCode)
    {
        var inputs = new INPUT[1];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].U.ki.wVk = (ushort)virtualKeyCode;
        inputs[0].U.ki.wScan = 0;
        inputs[0].U.ki.dwFlags = 0;
        inputs[0].U.ki.time = 0;
        inputs[0].U.ki.dwExtraInfo = IntPtr.Zero;
        
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// Tuşu bırakır
    /// </summary>
    public void KeyUp(int virtualKeyCode)
    {
        var inputs = new INPUT[1];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].U.ki.wVk = (ushort)virtualKeyCode;
        inputs[0].U.ki.wScan = 0;
        inputs[0].U.ki.dwFlags = KEYEVENTF_KEYUP;
        inputs[0].U.ki.time = 0;
        inputs[0].U.ki.dwExtraInfo = IntPtr.Zero;
        
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// Tuş basma işlemi (down + up)
    /// </summary>
    public void KeyPress(int virtualKeyCode)
    {
        KeyDown(virtualKeyCode);
        Thread.Sleep(10);
        KeyUp(virtualKeyCode);
    }

    /// <summary>
    /// Metin gönderir (Unicode karakterler için)
    /// </summary>
    public void SendText(string text)
    {
        foreach (char c in text)
        {
            var inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki.wVk = 0;
            inputs[0].U.ki.wScan = c;
            inputs[0].U.ki.dwFlags = KEYEVENTF_UNICODE;
            inputs[0].U.ki.time = 0;
            inputs[0].U.ki.dwExtraInfo = IntPtr.Zero;
            
            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
            
            inputs[0].U.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }

    /// <summary>
    /// ControlPacket'e göre işlem yapar
    /// </summary>
    public void ProcessControlPacket(ControlPacket packet)
    {
        switch (packet.Type)
        {
            case ControlType.MouseMove:
                MoveMouse(packet.X, packet.Y);
                break;
                
            case ControlType.MouseDown:
                MoveMouse(packet.X, packet.Y);
                MouseDown(packet.Button);
                break;
                
            case ControlType.MouseUp:
                MoveMouse(packet.X, packet.Y);
                MouseUp(packet.Button);
                break;
                
            case ControlType.MouseClick:
                MoveMouse(packet.X, packet.Y);
                MouseClick(packet.Button);
                break;
                
            case ControlType.MouseWheel:
                MouseWheel(packet.Y); // delta olarak Y kullanılıyor
                break;
                
            case ControlType.KeyDown:
                KeyDown(packet.KeyCode);
                break;
                
            case ControlType.KeyUp:
                KeyUp(packet.KeyCode);
                break;
                
            case ControlType.KeyPress:
                KeyPress(packet.KeyCode);
                break;
                
            case ControlType.TextInput:
                if (!string.IsNullOrEmpty(packet.Text))
                    SendText(packet.Text);
                break;
        }
    }
}

