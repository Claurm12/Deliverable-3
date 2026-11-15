using System.IO;

namespace Delivery3
{
    public class NetPlayer : NetObject
    {
        public int health = 100;
        public string playerRole = "Chased";

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(health);
            writer.Write(playerRole ?? "");
        }

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            health = reader.ReadInt32();
            playerRole = reader.ReadString();
        }
    }
}