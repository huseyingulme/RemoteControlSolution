using System.Text;
using System.Text.Json;
using RCS.Shared.Models;

namespace RCS.Shared.Protocol;

/// <summary>
/// Paket serileştirme/seri hale getirme işlemleri
/// Length-prefixed format: [4 byte length][JSON header][binary payload?]
/// </summary>
public static class PacketSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// JSON paketini byte array'e çevirir
    /// </summary>
    public static byte[] Serialize<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj, JsonOptions);
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Byte array'i JSON paketine çevirir
    /// </summary>
    public static T? Deserialize<T>(byte[] bytes)
    {
        var json = Encoding.UTF8.GetString(bytes);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    /// <summary>
    /// Length-prefixed paket gönderir (4 byte length + payload)
    /// </summary>
    public static byte[] CreateLengthPrefixedPacket(byte[] payload)
    {
        var lengthBytes = BitConverter.GetBytes(payload.Length);
        
        // Big-endian format (network byte order)
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(lengthBytes);
        }

        var packet = new byte[4 + payload.Length];
        Array.Copy(lengthBytes, 0, packet, 0, 4);
        Array.Copy(payload, 0, packet, 4, payload.Length);
        
        return packet;
    }

    /// <summary>
    /// Length-prefixed paketi parse eder (ilk 4 byte length, sonra payload)
    /// </summary>
    public static (int length, byte[] lengthBytes) ParsePacketLength(byte[] lengthBytes)
    {
        if (lengthBytes.Length < 4)
            throw new ArgumentException("Length bytes must be at least 4 bytes");

        // Big-endian'dan int'e çevir
        var bytes = new byte[4];
        Array.Copy(lengthBytes, bytes, 4);
        
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        int length = BitConverter.ToInt32(bytes, 0);
        return (length, bytes);
    }

    /// <summary>
    /// ScreenPacket + ImageBytes'i birleştirir (JSON header + binary image)
    /// </summary>
    public static byte[] SerializeScreenPacket(ScreenPacket packet, byte[] imageBytes)
    {
        var headerBytes = Serialize(packet);
        var totalLength = headerBytes.Length + imageBytes.Length;
        
        var lengthBytes = BitConverter.GetBytes(totalLength);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(lengthBytes);
        }

        var result = new byte[4 + totalLength];
        Array.Copy(lengthBytes, 0, result, 0, 4);
        Array.Copy(headerBytes, 0, result, 4, headerBytes.Length);
        Array.Copy(imageBytes, 0, result, 4 + headerBytes.Length, imageBytes.Length);
        
        return result;
    }

    /// <summary>
    /// Binary paketten ScreenPacket header'ını ve image bytes'i ayırır
    /// Format: [4 byte length][JSON header][binary image]
    /// </summary>
    public static (ScreenPacket packet, byte[] imageBytes) DeserializeScreenPacket(byte[] data)
    {
        if (data.Length < 4)
            throw new ArgumentException("Data too short");

        // İlk 4 byte length'i atla (zaten parse edilmiş)
        int payloadStart = 4;
        
        // JSON header'ı bulmak için '{' karakterini ara
        int jsonStart = payloadStart;
        while (jsonStart < data.Length && data[jsonStart] != (byte)'{')
            jsonStart++;
        
        if (jsonStart >= data.Length)
            throw new InvalidOperationException("JSON start not found");

        // JSON'ın sonunu bul (balanced braces)
        int braceCount = 0;
        int jsonEnd = jsonStart;
        for (int i = jsonStart; i < data.Length; i++)
        {
            if (data[i] == (byte)'{') braceCount++;
            else if (data[i] == (byte)'}') braceCount--;
            
            if (braceCount == 0)
            {
                jsonEnd = i + 1;
                break;
            }
        }
        
        if (braceCount != 0)
            throw new InvalidOperationException("Invalid JSON structure");

        // JSON header'ı parse et
        var jsonBytes = new byte[jsonEnd - jsonStart];
        Array.Copy(data, jsonStart, jsonBytes, 0, jsonBytes.Length);
        
        var packet = Deserialize<ScreenPacket>(jsonBytes) 
            ?? throw new InvalidOperationException("Failed to deserialize ScreenPacket");
        
        // Image bytes'i al
        int imageStart = jsonEnd;
        int imageLength = Math.Min(packet.ImageLength, data.Length - imageStart);
        
        if (imageLength <= 0)
            return (packet, Array.Empty<byte>());
        
        var imageBytes = new byte[imageLength];
        Array.Copy(data, imageStart, imageBytes, 0, imageLength);
        
        return (packet, imageBytes);
    }
}

