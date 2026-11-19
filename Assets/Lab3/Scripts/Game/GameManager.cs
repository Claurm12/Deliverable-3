using System.Collections.Generic;
using UnityEngine;

namespace Delivery3
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private int m_myObjectId;
        private HashSet<int> m_coloredPlayers = new HashSet<int>();

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
            m_myObjectId = myName.GetHashCode();
            FindObjectOfType<ReplicationManagerClient>().SetLocalId(m_myObjectId);

            LinkingContext linking = FindObjectOfType<LinkingContext>();
            GameObject myPlayerObject = (linking != null) ? linking.Find(m_myObjectId) : null;

            string rolActual = "Chased";
            if (session.Client.Teams.TryGetValue(myName, out PlayerDTO2 data) && !string.IsNullOrEmpty(data.playerRole))
            {
                rolActual = data.playerRole;
            }

            int prefabIndex = (rolActual == "Hunter") ? 0 : 1;

            if (myPlayerObject == null && session.Client.networkPrefabs != null && session.Client.networkPrefabs.Length > prefabIndex)
            {
                GameObject prefabToSpawn = session.Client.networkPrefabs[prefabIndex];
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
                ApplyPlayerColor(myPlayerObject, myName);

                // Añadimos el controlador solo a nuestro jugador local
                PlayerController controller = myPlayerObject.AddComponent<PlayerController>();
                controller.objectId = m_myObjectId;

                // Asignamos este jugador a la cámara
                CameraControl camControl = FindObjectOfType<CameraControl>();
                if (camControl != null)
                {
                    camControl.target = myPlayerObject.transform;
                }
            }

            string mensajeRol = (rolActual == "Hunter")
                ? "Eres el CAZADOR"
                : "ESCAPA del cazador";

            session.Client.Chat.Add(mensajeRol);
        }

        void Update()
        {
            if (LobbyGUI.NeedsSession()) return;
            LobbyClient client = LobbySession.Instance.Client;
            LinkingContext linking = FindObjectOfType<LinkingContext>();

            if (linking != null && client != null)
            {
                foreach (int id in linking.Ids)
                {
                    if (id == m_myObjectId || m_coloredPlayers.Contains(id)) continue;

                    GameObject remoteObj = linking.Find(id);
                    if (remoteObj != null)
                    {
                        foreach (string name in client.Players)
                        {
                            if (name.GetHashCode() == id)
                            {
                                ApplyPlayerColor(remoteObj, name);
                                m_coloredPlayers.Add(id);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void ApplyPlayerColor(GameObject playerObj, string playerName)
        {
            if (playerObj == null) return;

            LobbySession session = LobbySession.Instance;
            string rolActual = "Chased";

            if (session.Client != null && session.Client.Teams.TryGetValue(playerName, out PlayerDTO2 data))
            {
                if (!string.IsNullOrEmpty(data.playerRole))
                {
                    rolActual = data.playerRole;
                }
            }

            Renderer rend = playerObj.GetComponent<Renderer>();
            if (rend != null)
            {
                Color targetColor;
                switch (rolActual)
                {
                    case "Hunter":
                        targetColor = Color.red;
                        break;
                    case "Chased":
                        targetColor = Color.blue;
                        break;
                    default:
                        targetColor = Color.gray;
                        break;
                }
                rend.material = CreatePrimitiveMaterial(targetColor);
            }
        }

        private Material CreatePrimitiveMaterial(Color baseColor)
        {
            Material baseMat = Resources.Load<Material>("MaterialJugadores");
            Material mat = (baseMat != null) ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Lit"));

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", baseColor);
            }
            else
            {
                mat.color = baseColor;
            }

            return mat;
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
            Renderer rend = bullet.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = CreatePrimitiveMaterial(Color.yellow);
            }
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