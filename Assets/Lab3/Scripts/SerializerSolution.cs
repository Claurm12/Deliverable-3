using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class SerializerSolution
{
    public static byte[] Serialize(PlayerDTO dto)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        writer.Write(dto.playerName);
        writer.Write(dto.playerRole ?? "");

        return stream.ToArray();
    }

    public static PlayerDTO DeserializePlayerDTO(byte[] data)
    {
        BinaryReader reader = new BinaryReader(new MemoryStream(data));
        PlayerDTO dto = new PlayerDTO();

        dto.playerName = reader.ReadString();
        dto.playerRole = reader.ReadString();

        return dto;
    }

    public static byte[] Serialize(PlayerDTO2 dto)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        writer.Write(dto.playerName);
        writer.Write(dto.playerRole ?? "");

        return stream.ToArray();
    }

    public static PlayerDTO2 DeserializePlayerDTO2(byte[] data)
    {
        BinaryReader reader = new BinaryReader(new MemoryStream(data));
        PlayerDTO2 dto = new PlayerDTO2();

        dto.playerName = reader.ReadString();
        dto.playerRole = reader.ReadString();

        return dto;
    }

    public static byte[] ToJson(PlayerDTO2 dto)
    {
        return Encoding.UTF8.GetBytes(JsonUtility.ToJson(dto));
    }

    public static PlayerDTO2 FromJson(byte[] data)
    {
        return JsonUtility.FromJson<PlayerDTO2>(Encoding.UTF8.GetString(data));
    }

    public static string ToHex(byte[] data)
    {
        if (data == null) return "(null)";
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < data.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(data[i].ToString("X2"));
        }
        return sb.ToString();
    }

    
    public static byte[] SerializeTransform(int objectId, Vector3 pos)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        writer.Write(objectId);
        writer.Write(pos.x);
        writer.Write(pos.y);
        writer.Write(pos.z);

        return stream.ToArray();
    }

    public static void DeserializeTransform(byte[] data, out int objectId, out Vector3 pos)
    {
        BinaryReader reader = new BinaryReader(new MemoryStream(data));
        objectId = reader.ReadInt32();
        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        float z = reader.ReadSingle();
        pos = new Vector3(x, y, z);
    }

    public static byte[] SerializeShoot(int objectId, Vector3 pos, Vector3 dir)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        writer.Write(objectId);
        writer.Write(pos.x);
        writer.Write(pos.y);
        writer.Write(pos.z);
        writer.Write(dir.x);
        writer.Write(dir.y);
        writer.Write(dir.z);

        return stream.ToArray();
    }

    public static void DeserializeShoot(byte[] data, out int objectId, out Vector3 pos, out Vector3 dir)
    {
        BinaryReader reader = new BinaryReader(new MemoryStream(data));
        objectId = reader.ReadInt32();
        float px = reader.ReadSingle();
        float py = reader.ReadSingle();
        float pz = reader.ReadSingle();
        float dx = reader.ReadSingle();
        float dy = reader.ReadSingle();
        float dz = reader.ReadSingle();
        pos = new Vector3(px, py, pz);
        dir = new Vector3(dx, dy, dz);
    }
}
