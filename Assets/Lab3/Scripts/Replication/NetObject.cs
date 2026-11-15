using System.IO;
using UnityEngine;

namespace Delivery3
{
    public class NetObject : MonoBehaviour
    {
        public int id;
        public byte type;       // Índice del prefab en la lista del cliente

        public virtual void Write(BinaryWriter writer)
        {
            writer.Write(transform.position.x);
            writer.Write(transform.position.y);
            writer.Write(transform.position.z);
            writer.Write(transform.eulerAngles.y);
        }

        public virtual void Read(BinaryReader reader)
        {
            transform.position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            transform.rotation = Quaternion.Euler(0f, reader.ReadSingle(), 0f);
        }
    }
}