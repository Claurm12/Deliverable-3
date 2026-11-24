using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Delivery3
{
    public class ReplicationManagerClient : MonoBehaviour
    {
        [Header("0 = Hunter | 1..N = Chased Prefabs (same order as LobbyServer)")]
        [SerializeField] List<GameObject> m_prefabs = new List<GameObject>();

        LinkingContext m_linking;
        int m_lastSequence = -1;
        int m_localId;

        public void ResetState()
        {
            m_lastSequence = -1;
            m_localId = 0;
        }

        public void SetLocalId(int id)
        {
            m_localId = id;

            if (m_linking != null)
            {
                GameObject localGo = m_linking.Find(m_localId);

                NetPlayer localNp =
                    localGo != null
                        ? localGo.GetComponent<NetPlayer>()
                        : null;

                if (localNp != null)
                    localNp.SetIsLocalPlayer(true);
            }
        }

        public void Init(
            LinkingContext linking,
            List<GameObject> prefabs = null)
        {
            m_linking = linking;

            if (prefabs != null && prefabs.Count > 0)
                m_prefabs = prefabs;
        }

        public void ReadPacket(BinaryReader reader)
        {
            int sequence = reader.ReadInt32();

            if (sequence <= m_lastSequence)
                return;

            m_lastSequence = sequence;

            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                int id = reader.ReadInt32();

                ReplicationAction action =
                    (ReplicationAction)reader.ReadByte();

                GameObject go = m_linking.Find(id);

                if (action == ReplicationAction.Destroy)
                {
                    if (go != null)
                    {
                        Destroy(go);
                        m_linking.Unregister(id);
                    }

                    continue;
                }

                byte type = reader.ReadByte();

                if (go != null)
                {
                    NetObject existingNetObj = go.GetComponent<NetObject>();

                    if (existingNetObj != null &&
                        existingNetObj.type != type)
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
                    go = InstantiatePrefab(id, type);

                    if (go == null)
                        continue;
                }

                NetObject target = go.GetComponent<NetObject>();

                if (target == null)
                    continue;

                Vector3 keepPos = go.transform.position;
                Quaternion keepRot = go.transform.rotation;

                NetPlayer netPlayer = target as NetPlayer;

                if (netPlayer != null)
                    netPlayer.SetIsLocalPlayer(id == m_localId);

                bool keepIsMoving =
                    netPlayer != null && netPlayer.isMoving;

                int keepShotCount =
                    netPlayer != null ? netPlayer.shotCount : 0;

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
            if (type == TaskMatch.TypeId)
            {
                TaskMatch tasks = FindObjectOfType<TaskMatch>();

                if (tasks == null)
                {
                    GameObject state = new GameObject("TaskMatch");
                    tasks = state.AddComponent<TaskMatch>();
                }

                tasks.id = id;
                tasks.type = type;
                m_linking.Register(tasks.gameObject, id);
                return tasks.gameObject;
            }

            if (type == NetBullet.TypeId)
            {
                NetBullet bullet =
                    NetBullet.CreateVisual(Vector3.zero, 0f, false);

                bullet.id = id;
                bullet.replicaLinking = m_linking;
                m_linking.Register(bullet.gameObject, id);
                return bullet.gameObject;
            }

            if (m_prefabs == null || m_prefabs.Count == 0)
            {
                Debug.LogError("[REPL CLIENT] No prefabs configured.");
                return null;
            }

            int index =
                type < m_prefabs.Count && m_prefabs[type] != null
                    ? type
                    : 0;

            GameObject go = Instantiate(m_prefabs[index]);
            NetObject no = go.GetComponent<NetObject>();

            if (no == null)
                no = go.AddComponent<NetPlayer>();

            no.id = id;
            no.type = (byte)index;

            NetPlayer np = no as NetPlayer;

            if (np != null)
                np.SetIsLocalPlayer(id == m_localId);

            if (id != m_localId)
            {
                PlayerController controller =
                    go.GetComponent<PlayerController>();

                if (controller != null)
                    controller.enabled = false;

                Rigidbody body = go.GetComponent<Rigidbody>();

                if (body != null)
                    body.isKinematic = true;

                Animator animator = go.GetComponentInChildren<Animator>();

                if (animator != null)
                    animator.applyRootMotion = false;
            }

            m_linking.Register(go, id);
            return go;
        }
    }
}