using Mirror;
using Unity.Mathematics;

public static class ZombieNetConfig
{
    public const float WorldMinX = -500f;
    public const float WorldMinY = -100f;
    public const float WorldMinZ = -500f;
    public const float WorldSizeXZ = 1000f;
    public const float WorldSizeY = 300f;

    public const int BytesPerZombie = 9;
    public const int MaxZombiesPerMessage = 100;

    public static ushort Quantize(float value, float min, float size)
    {
        float t = (value - min) / size;
        t = math.clamp(t, 0f, 1f);
        return (ushort)(t * 65535f);
    }

    public static float Dequantize(ushort quantized, float min, float size)
    {
        return min + (quantized / 65535f) * size;
    }

    public static byte QuantizeYaw(float yawDegrees)
    {
        float normalized = math.frac(yawDegrees / 360f);
        if (normalized < 0f) normalized += 1f;
        return (byte)math.clamp(normalized * 255f, 0f, 255f);
    }

    public static float DequantizeYaw(byte quantized)
    {
        return (quantized / 255f) * 360f;
    }

    public static void Pack(byte[] buffer, int offset, ushort netId, float3 position, float yawDegrees)
    {
        ushort qx = Quantize(position.x, WorldMinX, WorldSizeXZ);
        ushort qy = Quantize(position.y, WorldMinY, WorldSizeY);
        ushort qz = Quantize(position.z, WorldMinZ, WorldSizeXZ);
        byte qyaw = QuantizeYaw(yawDegrees);

        buffer[offset + 0] = (byte)(netId & 0xFF);
        buffer[offset + 1] = (byte)(netId >> 8);
        buffer[offset + 2] = (byte)(qx & 0xFF);
        buffer[offset + 3] = (byte)(qx >> 8);
        buffer[offset + 4] = (byte)(qy & 0xFF);
        buffer[offset + 5] = (byte)(qy >> 8);
        buffer[offset + 6] = (byte)(qz & 0xFF);
        buffer[offset + 7] = (byte)(qz >> 8);
        buffer[offset + 8] = qyaw;
    }

    public static void Unpack(byte[] buffer, int offset, out ushort netId, out float3 position, out float yawDegrees)
    {
        netId = (ushort)(buffer[offset + 0] | (buffer[offset + 1] << 8));
        ushort qx = (ushort)(buffer[offset + 2] | (buffer[offset + 3] << 8));
        ushort qy = (ushort)(buffer[offset + 4] | (buffer[offset + 5] << 8));
        ushort qz = (ushort)(buffer[offset + 6] | (buffer[offset + 7] << 8));
        byte qyaw = buffer[offset + 8];

        position = new float3(
            Dequantize(qx, WorldMinX, WorldSizeXZ),
            Dequantize(qy, WorldMinY, WorldSizeY),
            Dequantize(qz, WorldMinZ, WorldSizeXZ));

        yawDegrees = DequantizeYaw(qyaw);
    }
}

public struct ZombieSnapshotMessage : NetworkMessage
{
    public byte[] Data;
    public ushort Count;
}

public struct ZombieDespawnMessage : NetworkMessage
{
    public ushort[] NetIds;
}

public struct ZombieDeathMessage : NetworkMessage
{
    public ushort[] NetIds;
}

public struct ZombieDamageRequestMessage : NetworkMessage
{
    public ushort NetId;
    public int Amount;
}