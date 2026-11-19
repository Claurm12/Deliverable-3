using System.Collections.Generic;
using UnityEngine;

namespace Delivery3
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private int m_myObjectId;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            if (LobbyGUI.NeedsSession()) return;
            LobbySession session = LobbySession.Instance;
            if (session.Client == null) return;

            if (FindObjectOfType<Light>() == null)
            {
                GameObject lightObj = new GameObject("DirectionalLight");
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                lightObj.transform.eulerAngles = new Vector3(50, -30, 0);
            }

            string myName = session.Client.userName;
            m_myObjectId = NetObject.ComputeId(myName);

            LinkingContext linking = FindObjectOfType<LinkingContext>();
            ReplicationManagerClient replClient = FindObjectOfType<ReplicationManagerClient>();

            if (replClient != null)
            {
                replClient.SetLocalId(m_myObjectId);
                GameObject[] prefabs = GetPrefabs(session);
                if (prefabs != null && prefabs.Length > 0)
                {
                    replClient.Init(linking, new List<GameObject>(prefabs));
                }
            }

            GameObject myPlayerObject = (linking != null) ? linking.Find(m_myObjectId) : null;
            string rolActual = GetRoleFromClient(session.Client, myName);
            int prefabIndex = (rolActual == "Hunter") ? 0 : 1;

            GameObject[] currentPrefabs = GetPrefabs(session);
            if (myPlayerObject == null && currentPrefabs != null && currentPrefabs.Length > 0)
            {
                if (prefabIndex >= currentPrefabs.Length || currentPrefabs[prefabIndex] == null)
                {
                    prefabIndex = 0;
                }

                GameObject prefabToSpawn = currentPrefabs[prefabIndex];
                if (prefabToSpawn != null)
                {
                    myPlayerObject = Instantiate(prefabToSpawn);
                    myPlayerObject.name = "Player_" + myName;
                    float startX = UnityEngine.Random.Range(-3f, 3f);
                    myPlayerObject.transform.position = new Vector3(startX, 0.5f, 0f);

                    NetObject netObj = myPlayerObject.GetComponent<NetObject>();
                    if (netObj == null) netObj = myPlayerObject.AddComponent<NetPlayer>();
                    netObj.id = m_myObjectId;
                    netObj.type = (byte)prefabIndex;

                    if (linking != null)
                    {
                        linking.Register(myPlayerObject, m_myObjectId);
                    }
                }
            }

            if (myPlayerObject != null)
            {
                EnsureLocalPlayerSetup(myPlayerObject);
            }
        }

        void Update()
        {
            if (LobbyGUI.NeedsSession()) return;
            LobbyClient client = LobbySession.Instance.Client;
            LinkingContext linking = FindObjectOfType<LinkingContext>();

            if (linking == null || client == null) return;

            GameObject myObj = linking.Find(m_myObjectId);
            if (myObj != null)
            {
                EnsureLocalPlayerSetup(myObj);
            }

            foreach (int id in linking.Ids)
            {
                if (id == m_myObjectId) continue;
                GameObject otherObj = linking.Find(id);
                if (otherObj == null) continue;

                PlayerController otherCtrl = otherObj.GetComponent<PlayerController>();
                if (otherCtrl != null)
                {
                    otherCtrl.enabled = false;
                }
            }
        }

        private void EnsureLocalPlayerSetup(GameObject playerObj)
        {
            PlayerController ctrl = playerObj.GetComponent<PlayerController>();
            if (ctrl == null)
            {
                ctrl = playerObj.AddComponent<PlayerController>();
            }
            ctrl.enabled = true;
            ctrl.objectId = m_myObjectId;

            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            CameraControl cam = FindObjectOfType<CameraControl>();
            if (cam != null && cam.target != playerObj.transform)
            {
                cam.target = playerObj.transform;
            }
        }

        private string GetRoleFromClient(LobbyClient client, string playerName)
        {
            if (client != null && !string.IsNullOrEmpty(playerName) &&
                client.Teams.TryGetValue(playerName, out PlayerDTO2 data) &&
                !string.IsNullOrEmpty(data.playerRole))
            {
                return data.playerRole;
            }
            return "Chased";
        }

        private GameObject[] GetPrefabs(LobbySession session)
        {
            if (session != null && session.Client != null && session.Client.networkPrefabs != null && session.Client.networkPrefabs.Length > 0)
            {
                return session.Client.networkPrefabs;
            }
            if (session != null && session.networkPrefabs != null && session.networkPrefabs.Length > 0)
            {
                return session.networkPrefabs;
            }
            return null;
        }

        public void SpawnAndSendBullet(LobbySession session, LobbyClient client, int objectId, Vector3 pos, Vector3 dir)
        {
            CreateBulletGameObject(pos, dir);

            byte[] packet = Messages.Shoot(objectId, pos, dir);
            if (session.IsHost)
            {
                session.Server.BroadcastCustomBytes(packet);
            }
            else
            {
                client.SendCustomBytes(packet);
            }
        }

        public void CreateBulletGameObject(Vector3 pos, Vector3 dir)
        {
            GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.transform.position = pos;
            bullet.transform.localScale = Vector3.one * 0.4f;
            BulletMover mover = bullet.AddComponent<BulletMover>();
            mover.SetDirection(dir);
        }
    }

    public class BulletMover : MonoBehaviour
    {
        private Vector3 m_direction = Vector3.forward;
        private float m_speed = 12f;

        public void SetDirection(Vector3 dir)
        {
            if (dir.sqrMagnitude > 0.001f)
            {
                m_direction = dir.normalized;
            }
        }

        void Start()
        {
            Destroy(gameObject, 3.0f);
        }

        void Update()
        {
            transform.position += m_direction * m_speed * Time.deltaTime;
        }
    }
}
