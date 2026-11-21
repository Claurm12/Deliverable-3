using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Delivery3
{
    public class ReplicationManagerClient : MonoBehaviour
    {
        [SerializeField] List<GameObject> m_prefabs = new List<GameObject>();   // 0 = Hunter, 1 = Chased
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

                if (go != null)
                {
                    NetObject existingNetObj = go.GetComponent<NetObject>();
                    if (existingNetObj != null && existingNetObj.type != type)
                    {
                        Vector3 oldPos = go.transform.position;
                        Quaternion oldRot = go.transform.rotation;

                        Destroy(go);
                        m_linking.Unregister(id);
                        go = InstantiatePrefab(id, type);
                        if (go != null)
                        {
                            go.transform.position = oldPos;
                            go.transform.rotation = oldRot;
                        }
                    }
                }

                if (go == null)
                {
                    Debug.Log("[REPL CLIENT] creando id " + id + " tipo " + type);
                    go = InstantiatePrefab(id, type);
                    if (go == null) continue;
                }

                NetObject target = go.GetComponent<NetObject>();
                Vector3 keepPos = go.transform.position;
                Quaternion keepRot = go.transform.rotation;

                NetPlayer netPlayer = target as NetPlayer;
                bool keepIsMoving = netPlayer != null && netPlayer.isMoving;
                int keepShotCount = netPlayer != null ? netPlayer.shotCount : 0;

                target.Read(reader);

                if (id == m_localId)
                {
                    go.transform.position = keepPos;
                    go.transform.rotation = keepRot;

                    if (netPlayer != null)
                    {
                        netPlayer.isMoving = keepIsMoving;
                        netPlayer.shotCount = keepShotCount;
                        netPlayer.ApplyAnimationState();
                    }
                }
            }
        }

        private GameObject InstantiatePrefab(int id, byte type)
        {
            // creacion balas
            if (type == NetBullet.TypeId)
            {
                NetBullet bullet = NetBullet.CreateVisual(Vector3.zero, 0f);
                bullet.id = id;
                bullet.replicaLinking = m_linking;
                m_linking.Register(bullet.gameObject, id);
                return bullet.gameObject;
            }

            if (m_prefabs == null || m_prefabs.Count == 0)
            {
                Debug.LogError("[REPL CLIENT] No hay prefabs configurados.");
                return null;
            }

            int index = (type < m_prefabs.Count && m_prefabs[type] != null) ? type : 0;
            GameObject go = Instantiate(m_prefabs[index]);
            NetObject no = go.GetComponent<NetObject>();
            if (no == null) no = go.AddComponent<NetPlayer>();
            no.id = id;
            no.type = (byte)index;
            m_linking.Register(go, id);
            return go;
        }
    }
}