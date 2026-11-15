
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Delivery3
{

    public class ReplicationManagerServer
    {
        readonly LinkingContext m_linking;
        readonly Dictionary<int, ReplicationAction> m_pending = new Dictionary<int, ReplicationAction>();
        readonly Dictionary<int, (ReplicationAction action, float until)> m_repeat =
            new Dictionary<int, (ReplicationAction, float)>();
        int m_sequence;

        // The time in seconds, Time.time on slide 15. The test scene replaces it to replay exact
        // situations (a lost Create, a lost Destroy). In your game, leave it as it is.
        public Func<float> clock = () => Time.time;

        public ReplicationManagerServer(LinkingContext linking) { m_linking = linking; }

        public class TodoException : Exception
        {
            public int number;
            public TodoException(int number) : base("TODO " + number) { this.number = number; }
        }

        // ------------------------------------------------------------- given, slides 10 and 15 ---
        public void MarkCreate(int id)
        {
            m_pending[id] = ReplicationAction.Create;
            m_repeat[id] = (ReplicationAction.Create, clock() + 1f);       // repeated for 1 s
        }

        public void MarkDestroy(int id)
        {
            m_pending[id] = ReplicationAction.Destroy;
            m_repeat[id] = (ReplicationAction.Destroy, clock() + 1f);
        }

        public void MarkUpdate(int id)
        {
            if (!m_pending.ContainsKey(id))                                 // a Create or Destroy wins
                m_pending[id] = ReplicationAction.Update;
        }

        public byte[] WritePacket()                                         // once per tick
        {
            foreach (int id in m_repeat.Keys.ToList())
            {
                if (clock() > m_repeat[id].until) m_repeat.Remove(id);
                else m_pending[id] = m_repeat[id].action;
            }

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            List<KeyValuePair<int, ReplicationAction>> entries = new List<KeyValuePair<int, ReplicationAction>>();
            foreach (KeyValuePair<int, ReplicationAction> entry in m_pending)
            {
                if (entry.Value != ReplicationAction.Destroy)
                {
                    GameObject go = m_linking.Find(entry.Key);
                    if (go == null || go.GetComponent<NetObject>() == null) continue;
                }
                entries.Add(entry);
            }

            writer.Write((byte)MsgType.Replication);
            writer.Write(m_sequence++);
            writer.Write(entries.Count);

            foreach (KeyValuePair<int, ReplicationAction> entry in entries)
            {
                writer.Write(entry.Key);
                writer.Write((byte)entry.Value);
                if (entry.Value == ReplicationAction.Destroy) continue;
                NetObject obj = m_linking.Find(entry.Key).GetComponent<NetObject>();
                writer.Write(obj.type);
                obj.Write(writer);
            }

            m_pending.Clear();
            return stream.ToArray();
        }
    }
}
