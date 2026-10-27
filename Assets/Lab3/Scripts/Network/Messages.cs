using System;
using System.IO;
using UnityEngine;

namespace Delivery3
{
    public static class Messages
    {
        public static byte[] Text(string text)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((byte)MsgType.Text);
            writer.Write(text);
            return stream.ToArray();
        }

        public static byte[] Player(PlayerDTO2 dto)
        {
            byte[] body = SerializerSolution.Serialize(dto);
            byte[] message = new byte[1 + body.Length];
            message[0] = (byte)MsgType.Player;
            body.CopyTo(message, 1);
            return message;
        }

        // Reads the type byte. 'text' or 'player' is filled depending on it.
        // Throws on a message that is too short or malformed: the caller drops it.
        public static MsgType Read(byte[] data, out string text, out PlayerDTO2 player)
        {
            text = null;
            player = new PlayerDTO2();
            if (data == null || data.Length < 1) throw new InvalidDataException("empty message");

            MsgType type = (MsgType)data[0];
            switch (type)
            {
                case MsgType.Text:
                    BinaryReader reader = new BinaryReader(new MemoryStream(data, 1, data.Length - 1));
                    text = reader.ReadString();
                    break;

                case MsgType.Player:
                    byte[] body = new byte[data.Length - 1];
                    Array.Copy(data, 1, body, 0, body.Length);
                    player = SerializerSolution.DeserializePlayerDTO2(body);
                    break;

                default:
                    throw new InvalidDataException("unknown message type " + data[0]);
            }
            return type;
        }


        public static MsgType Read(byte[] data, out string text, out PlayerDTO2 player, out int objectId, out Vector3 pos, out Vector3 dir)
        {
            text = null;
            player = new PlayerDTO2();
            objectId = 0;
            pos = Vector3.zero;
            dir = Vector3.zero;

            if (data == null || data.Length < 1) throw new InvalidDataException("empty message");

            MsgType type = (MsgType)data[0];
            switch (type)
            {
                case MsgType.Text:
                    BinaryReader readerText = new BinaryReader(new MemoryStream(data, 1, data.Length - 1));
                    text = readerText.ReadString();
                    break;

                case MsgType.Player:
                    byte[] bodyPlayer = new byte[data.Length - 1];
                    Array.Copy(data, 1, bodyPlayer, 0, bodyPlayer.Length);
                    player = SerializerSolution.DeserializePlayerDTO2(bodyPlayer);
                    break;

                case MsgType.Transform:
                    byte[] bodyTransform = new byte[data.Length - 1];
                    Array.Copy(data, 1, bodyTransform, 0, bodyTransform.Length);
                    SerializerSolution.DeserializeTransform(bodyTransform, out objectId, out pos);
                    break;

                case MsgType.Shoot:
                    byte[] bodyShoot = new byte[data.Length - 1];
                    Array.Copy(data, 1, bodyShoot, 0, bodyShoot.Length);
                    SerializerSolution.DeserializeShoot(bodyShoot, out objectId, out pos, out dir);
                    break;

                default:
                    throw new InvalidDataException("unknown message type " + data[0]);
            }
            return type;
        }

     
        public static byte[] Transform(int objectId, Vector3 pos)
        {
            byte[] body = SerializerSolution.SerializeTransform(objectId, pos);
            byte[] message = new byte[1 + body.Length];
            message[0] = (byte)MsgType.Transform;
            body.CopyTo(message, 1);
            return message;
        }

        public static byte[] Shoot(int objectId, Vector3 pos, Vector3 dir)
        {
            byte[] body = SerializerSolution.SerializeShoot(objectId, pos, dir);
            byte[] message = new byte[1 + body.Length];
            message[0] = (byte)MsgType.Shoot;
            body.CopyTo(message, 1);
            return message;
        }
    }
}

