using System.IO;
using UnityEngine;

namespace Delivery3
{
    public class ReplicationManagerClient : MonoBehaviour
    {
        private int m_lastSequence = -1;
        private LinkingContext m_linking; // Gestor de IDs en el cliente
        private GameObject[] m_prefabs;   // Tabla de prefabs asignados en el Inspector

        public void Init(LinkingContext linking, GameObject[] prefabs)
        {
            m_linking = linking;
            m_prefabs = prefabs;
        }

        public void ReadPacket(BinaryReader reader, int myObjectId)
        {
            int sequence = reader.ReadInt32();
            if (sequence <= m_lastSequence) return;
            m_lastSequence = sequence;

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                int id = reader.ReadInt32();
                ReplicationAction action = (ReplicationAction)reader.ReadByte();

                GameObject go = m_linking != null ? m_linking.Find(id) : null;

                if (action == ReplicationAction.Destroy)
                {
                    if (go != null)
                    {
                        Destroy(go);
                        if (m_linking != null) m_linking.Unregister(id);
                    }
                    continue;
                }

                byte type = reader.ReadByte();

                if (go == null)
                {
                    if (m_prefabs != null && type < m_prefabs.Length && m_prefabs[type] != null)
                    {
                        go = Instantiate(m_prefabs[type]);
                        NetObject netObjComponent = go.GetComponent<NetObject>();
                        if (netObjComponent != null)
                        {
                            netObjComponent.id = id;
                            netObjComponent.type = type;
                        }
                        if (m_linking != null) m_linking.Register(go, id);
                    }
                    else
                    {
                        Debug.LogWarning("[CLIENT] Prefab type " + type + " not found or out of bounds!");
                        DummyReadObject(reader);
                        continue;
                    }
                }
                if (id == myObjectId)
                {
                    DummyReadObject(reader);
                    continue;
                }

                NetObject targetNetObj = go.GetComponent<NetObject>();
                if (targetNetObj != null)
                {
                    targetNetObj.Read(reader);
                }
                else
                {
                    DummyReadObject(reader);
                }
            }
        }

        private void DummyReadObject(BinaryReader reader)
        {
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();

            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                try
                {
               
                }
                catch { }
            }
        }
    }
}