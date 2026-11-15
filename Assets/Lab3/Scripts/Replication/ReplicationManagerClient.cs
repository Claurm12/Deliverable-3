
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Delivery3
{
    public class ReplicationManagerClient : MonoBehaviour
    {
        [SerializeField] List<GameObject> m_prefabs = new List<GameObject>();   // index = type
        LinkingContext m_linking;
        int m_lastSequence = -1;
        int m_localId;
        public void SetLocalId(int id) { m_localId = id; }

        public void Init(LinkingContext linking, List<GameObject> prefabs = null)
        {
            m_linking = linking;
            if (prefabs != null) m_prefabs = prefabs;
        }

        public void ReadPacket(BinaryReader reader)
        {
            int sequence = reader.ReadInt32();
            if (sequence <= m_lastSequence) return;
            m_lastSequence = sequence;

            int count = reader.ReadInt32();
            Debug.Log("[REPL CLIENT] seq " + sequence + " entradas " + count);

            for (int i = 0; i < count; i++)
            {
                int id = reader.ReadInt32();
                ReplicationAction action = (ReplicationAction)reader.ReadByte();
                GameObject go = m_linking.Find(id);

                if (action == ReplicationAction.Destroy)
                {
                    if (go != null) { Destroy(go); m_linking.Unregister(id); }
                    continue;
                }

                byte type = reader.ReadByte();
                if (go == null)
                {
                    Debug.Log("[REPL CLIENT] creando id " + id + " tipo " + type);
                    go = Instantiate(m_prefabs[type]);
                    NetObject no = go.GetComponent<NetObject>();
                    if (no == null) no = go.AddComponent<NetPlayer>();
                    no.id = id;
                    m_linking.Register(go, id);
                }
                NetObject target = go.GetComponent<NetObject>();
                Vector3 keepPos = go.transform.position;
                target.Read(reader);
                if (id == m_localId) go.transform.position = keepPos;
            }
        }
    }
}
