using System.Collections.Generic;
using UnityEngine;

namespace Delivery3
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private int m_myObjectId;
        private LobbyClient m_subscribedClient;

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

            SubscribeClientEvents(session.Client);

            string myName = session.Client.userName;
            m_myObjectId = NetObject.ComputeId(myName);

            LinkingContext linking = GetLinkingContext(session.Client);
            ReplicationManagerClient replClient =
                GetReplicationClient(session.Client);

            if (replClient != null)
            {
                replClient.SetLocalId(m_myObjectId);

                GameObject[] prefabs = GetPrefabs(session);

                if (prefabs != null && prefabs.Length > 0)
                    replClient.Init(linking, new List<GameObject>(prefabs));
            }

            GameObject myPlayerObject =
                linking != null ? linking.Find(m_myObjectId) : null;

            string rolActual = GetRoleFromClient(session.Client, myName);
            int prefabIndex = rolActual == "Hunter" ? 0 : 1;

            GameObject[] currentPrefabs = GetPrefabs(session);

            if (myPlayerObject == null &&
                currentPrefabs != null &&
                currentPrefabs.Length > 0)
            {
                if (prefabIndex >= currentPrefabs.Length ||
                    currentPrefabs[prefabIndex] == null)
                {
                    prefabIndex = 0;
                }

                GameObject prefabToSpawn = currentPrefabs[prefabIndex];

                if (prefabToSpawn != null)
                {
                    myPlayerObject = Instantiate(prefabToSpawn);
                    myPlayerObject.name = "Player_" + myName;

                    float startX = UnityEngine.Random.Range(-3f, 3f);
                    myPlayerObject.transform.position =
                        new Vector3(startX, 0.5f, 0f);

                    NetObject netObj =
                        myPlayerObject.GetComponent<NetObject>();

                    if (netObj == null)
                        netObj = myPlayerObject.AddComponent<NetPlayer>();

                    netObj.id = m_myObjectId;
                    netObj.type = (byte)prefabIndex;

                    if (linking != null)
                        linking.Register(myPlayerObject, m_myObjectId);
                }
            }

            if (myPlayerObject != null)
                EnsureLocalPlayerSetup(myPlayerObject);

            if (session.IsHost)
            {
                NpcCrowd crowd = GetComponent<NpcCrowd>();

                if (crowd == null)
                    Debug.LogError("[NPC] Hey, add NpcCrowd to GameManager!");
                else
                    session.Server.SpawnNpcs(crowd);
            }
        }

        void OnDestroy()
        {
            if (m_subscribedClient != null)
            {
                m_subscribedClient.TransformReceived -= OnRemoteTransformReceived;
                m_subscribedClient = null;
            }
        }

        private void SubscribeClientEvents(LobbyClient client)
        {
            if (client == null || m_subscribedClient == client)
                return;

            if (m_subscribedClient != null)
                m_subscribedClient.TransformReceived -= OnRemoteTransformReceived;

            m_subscribedClient = client;
            m_subscribedClient.TransformReceived += OnRemoteTransformReceived;
        }

        private void OnRemoteTransformReceived(
            int objectId,
            Vector3 pos,
            float rotY)
        {
            if (objectId == m_myObjectId)
                return;

            LobbyClient client =
                LobbySession.Instance != null
                    ? LobbySession.Instance.Client
                    : null;

            LinkingContext linking = GetLinkingContext(client);

            if (linking == null)
                return;

            GameObject obj = linking.Find(objectId);

            if (obj != null)
            {
                obj.transform.position = pos;
                obj.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            }
        }

        private LinkingContext GetLinkingContext(LobbyClient client)
        {
            if (client != null)
            {
                LinkingContext lc = client.GetComponent<LinkingContext>();
                if (lc != null) return lc;
            }

            return FindObjectOfType<LinkingContext>();
        }

        private ReplicationManagerClient GetReplicationClient(
            LobbyClient client)
        {
            if (client != null)
            {
                ReplicationManagerClient rc =
                    client.GetComponent<ReplicationManagerClient>();

                if (rc != null) return rc;
            }

            return FindObjectOfType<ReplicationManagerClient>();
        }

        void Update()
        {
            if (LobbyGUI.NeedsSession()) return;

            LobbyClient client = LobbySession.Instance.Client;
            LinkingContext linking = GetLinkingContext(client);

            if (linking == null || client == null)
                return;

            GameObject myObj = linking.Find(m_myObjectId);

            if (myObj != null)
                EnsureLocalPlayerSetup(myObj);

            foreach (int id in linking.Ids)
            {
                if (id == m_myObjectId)
                    continue;

                GameObject otherObj = linking.Find(id);

                if (otherObj == null)
                    continue;

                PlayerController otherCtrl =
                    otherObj.GetComponent<PlayerController>();

                if (otherCtrl != null)
                    otherCtrl.enabled = false;
            }
        }

        private void EnsureLocalPlayerSetup(GameObject playerObj)
        {
            PlayerController ctrl = playerObj.GetComponent<PlayerController>();

            if (ctrl == null)
                ctrl = playerObj.AddComponent<PlayerController>();

            ctrl.enabled = true;
            ctrl.objectId = m_myObjectId;

            NetPlayer netPlayer = playerObj.GetComponent<NetPlayer>();

            if (netPlayer != null)
                netPlayer.SetIsLocalPlayer(true);

            Rigidbody rb = playerObj.GetComponent<Rigidbody>();

            if (rb != null)
                rb.isKinematic = false;

            CameraControl cam = FindObjectOfType<CameraControl>();

            if (cam != null && cam.target != playerObj.transform)
                cam.target = playerObj.transform;
        }

        private string GetRoleFromClient(
            LobbyClient client,
            string playerName)
        {
            if (client != null &&
                !string.IsNullOrEmpty(playerName) &&
                client.Teams.TryGetValue(playerName, out PlayerDTO2 data) &&
                !string.IsNullOrEmpty(data.playerRole))
            {
                return data.playerRole;
            }

            return "Chased";
        }

        private GameObject[] GetPrefabs(LobbySession session)
        {
            if (session != null &&
                session.Client != null &&
                session.Client.networkPrefabs != null &&
                session.Client.networkPrefabs.Length > 0)
            {
                return session.Client.networkPrefabs;
            }

            if (session != null &&
                session.networkPrefabs != null &&
                session.networkPrefabs.Length > 0)
            {
                return session.networkPrefabs;
            }

            return null;
        }

        public void SpawnAndSendBullet(
            LobbySession session,
            LobbyClient client,
            int objectId,
            Vector3 pos,
            Vector3 dir)
        {
            if (session.IsHost)
                session.Server.ShootFromHost(objectId, pos, dir);
            else
                client.SendCustomBytes(Messages.Shoot(objectId, pos, dir));
        }
    }
}