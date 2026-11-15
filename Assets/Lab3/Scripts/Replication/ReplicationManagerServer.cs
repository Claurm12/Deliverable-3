using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Delivery3
{
    public enum ReplicationAction : byte { Create = 1, Update = 2, Destroy = 3 }

    public class ReplicationManagerServer : MonoBehaviour
    {
        private uint m_sequence = 0;
        private readonly Dictionary<int, ReplicationAction> m_pending = new Dictionary<int, ReplicationAction>();

        private readonly Dictionary<int, (ReplicationAction action, float until)> m_repeat = new Dictionary<int, (ReplicationAction, float)>();

        private LinkingContext m_linking;

        public void Init(LinkingContext linking)
        {
            m_linking = linking;
        }

        public void MarkCreate(int id)
        {
            m_pending[id] = ReplicationAction.Create;
            m_repeat[id] = (ReplicationAction.Create, Time.time + 1.0f);
        }

        public void MarkDestroy(int id)
        {
            m_pending[id] = ReplicationAction.Destroy;
            m_repeat[id] = (ReplicationAction.Destroy, Time.time + 1.0f);
        }

        public void MarkUpdate(int id)
        {
            if (!m_pending.ContainsKey(id))
            {
                m_pending[id] = ReplicationAction.Update;
            }
        }

        public byte[] WritePacket()
        {
            foreach (int id in new List<int>(m_repeat.Keys))
            {
                if (Time.time > m_repeat[id].until)
                {
                    m_repeat.Remove(id);
                }
                else if (!m_pending.ContainsKey(id))
                {
                    m_pending[id] = m_repeat[id].action;
                }
            }

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write((byte)MsgType.Replication);
            writer.Write(m_sequence++);
            writer.Write(m_pending.Count);

            foreach (KeyValuePair<int, ReplicationAction> entry in m_pending)
            {
                writer.Write(entry.Key);
                writer.Write((byte)entry.Value);

                if (entry.Value == ReplicationAction.Destroy) continue;

                GameObject objGo = m_linking.Find(entry.Key);
                if (objGo != null)
                {
                    NetObject netObj = objGo.GetComponent<NetObject>();
                    if (netObj != null)
                    {
                        writer.Write(netObj.type);
                        netObj.Write(writer);
                    }
                }
            }

            m_pending.Clear();
            return stream.ToArray();
        }
    }
}