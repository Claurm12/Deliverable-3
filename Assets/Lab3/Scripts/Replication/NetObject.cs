using System.IO;
using UnityEngine;

namespace Delivery3
{
    public class NetObject : MonoBehaviour
    {
        public int id;
        public byte type;       //prefab index

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
        public static int ComputeId(string value)
        {
            unchecked
            {
                const int fnvPrime = 16777619;
                int hash = (int)2166136261;
                if (value == null) return hash;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= fnvPrime;
                }
                return hash;
            }
        }
    }

}