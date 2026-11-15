using System;
using System.IO;
using UnityEngine;

namespace Delivery3
{
    public class NetObject : MonoBehaviour
    {
        [Header("Network Identification")]
        public int id;          // ID de red único 
        public byte type;       // Tipo de prefab 

        // Escribe los datos de posición y rotación 
        public virtual void Write(BinaryWriter writer)
        {
            writer.Write(transform.position.x);
            writer.Write(transform.position.y);
            writer.Write(transform.position.z);
            writer.Write(transform.eulerAngles.y);
        }

        // Lee los datos y actualiza la transformación del objeto en el cliente
        public virtual void Read(BinaryReader reader)
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            float rotY = reader.ReadSingle();

            transform.position = new Vector3(x, y, z);
            transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        }
    }
}