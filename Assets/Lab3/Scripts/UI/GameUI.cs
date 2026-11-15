using System;
using System.Collections.Generic;
using UnityEngine;

namespace Delivery3
{
    public class GameUI : MonoBehaviour
    {
        string m_chatInput = "";

        private GameObject m_myPlayerObject;
        private float m_sendTimer;
        private int m_myObjectId;
        private bool m_moveW, m_moveS, m_moveA, m_moveD;
        private bool m_jumpRequested = false;
        private bool m_shootRequested = false;
        private bool m_isTypingInChat = false;

        // Salto
        private float m_verticalVelocity = 0f;
        private const float m_gravity = -20f;
        private const float m_jumpForce = 8f;

        void Start()
        {
            if (LobbyGUI.NeedsSession()) return;
            LobbySession session = LobbySession.Instance;
            if (session.Client == null) return;

            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("MainCamera");
                Camera cam = camObj.AddComponent<Camera>();
                cam.tag = "MainCamera";
                camObj.transform.position = new Vector3(0, 5, -10);
                camObj.transform.eulerAngles = new Vector3(25, 0, 0);
            }

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
            if (linking != null)
            {
                m_myPlayerObject = linking.Find(m_myObjectId);
            }

            if (m_myPlayerObject == null && session.Client.networkPrefabs != null && session.Client.networkPrefabs.Length > 0)
            {
                GameObject prefabToSpawn = session.Client.networkPrefabs[0];
                if (prefabToSpawn != null)
                {
                    m_myPlayerObject = Instantiate(prefabToSpawn);
                    m_myPlayerObject.name = "Player_" + myName;
                    float startX = UnityEngine.Random.Range(-3f, 3f);
                    m_myPlayerObject.transform.position = new Vector3(startX, 0.5f, 0f);

                    NetObject netObj = m_myPlayerObject.GetComponent<NetObject>();
                    if (netObj == null) netObj = m_myPlayerObject.AddComponent<NetPlayer>();
                    netObj.id = m_myObjectId;
                    netObj.type = 0;

                    if (linking != null)
                    {
                        linking.Register(m_myPlayerObject, m_myObjectId);
                    }
                }
            }

            if (m_myPlayerObject != null)
            {
                ApplyPlayerColor(m_myPlayerObject, myName);
            }

            string rolActual = "Chased";
            PlayerDTO2 data;
            if (session.Client.Teams.TryGetValue(myName, out data) && !string.IsNullOrEmpty(data.playerRole))
            {
                rolActual = data.playerRole;
            }

            string mensajeRol = (rolActual == "Hunter")
                ? "Eres el CAZADOR"
                : "ESCAPA del cazador";

            session.Client.Chat.Add(mensajeRol);
        }

        // Color según el rol asignado
        private void ApplyPlayerColor(GameObject playerObj, string playerName)
        {
            if (playerObj == null) return;

            LobbySession session = LobbySession.Instance;
            string rolActual = "Chased";
            PlayerDTO2 data;

            if (session.Client != null && session.Client.Teams.TryGetValue(playerName, out data))
            {
                if (!string.IsNullOrEmpty(data.playerRole))
                {
                    rolActual = data.playerRole;
                }
            }

            Renderer rend = playerObj.GetComponent<Renderer>();
            if (rend != null)
            {
                UnityEngine.Color targetColor;
                switch (rolActual)
                {
                    case "Hunter":
                        targetColor = UnityEngine.Color.red;   // Rojo cazador
                        break;

                    case "Chased":
                        targetColor = UnityEngine.Color.blue; // Azul los que escapan
                        break;

                    default:
                        targetColor = UnityEngine.Color.gray;
                        break;
                }
                rend.material = CreatePrimitiveMaterial(targetColor);
            }
        }

        private Material CreatePrimitiveMaterial(UnityEngine.Color baseColor)
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

        void Update()
        {
            if (LobbyGUI.NeedsSession()) return;
            LobbySession session = LobbySession.Instance;
            LobbyClient client = session.Client;

            LinkingContext linking = FindObjectOfType<LinkingContext>();
            if (linking != null && client != null)
            {
                foreach (int id in linking.Ids)
                {
                    if (id == m_myObjectId) continue;

                    GameObject remoteObj = linking.Find(id);
                    if (remoteObj != null)
                    {
                        string remoteName = "";
                        foreach (string name in client.Players)
                        {
                            if (name.GetHashCode() == id)
                            {
                                remoteName = name;
                                break;
                            }
                        }
                        ApplyPlayerColor(remoteObj, remoteName);
                    }
                }
            }

            // Si se escribe en el chat no se mueve el jugador
            if (!m_isTypingInChat && m_myPlayerObject != null)
            {
                float moveSpeed = 6f;
                float h = 0f;
                float v = 0f;

                if (m_moveW) v += 1f;
                if (m_moveS) v -= 1f;
                if (m_moveA) h -= 1f;
                if (m_moveD) h += 1f;

                Vector3 direction = new Vector3(h, 0, v);
                Vector3 currentPos = m_myPlayerObject.transform.position;

                // Gestión del salto
                if (m_jumpRequested && currentPos.y <= 0.51f)
                {
                    m_verticalVelocity = m_jumpForce;
                    m_jumpRequested = false;
                }

                if (currentPos.y > 0.5f || m_verticalVelocity != 0f)
                {
                    m_verticalVelocity += m_gravity * Time.deltaTime;
                    currentPos.y += m_verticalVelocity * Time.deltaTime;

                    if (currentPos.y <= 0.5f)
                    {
                        currentPos.y = 0.5f;
                        m_verticalVelocity = 0f;
                    }
                }

                if (direction.magnitude > 0.1f)
                {
                    currentPos += direction.normalized * moveSpeed * Time.deltaTime;
                }

                m_myPlayerObject.transform.position = currentPos;

                m_sendTimer += Time.deltaTime;
                if (m_sendTimer >= 0.05f)
                {
                    m_sendTimer = 0f;
                    byte[] packet = Messages.Transform(m_myObjectId, m_myPlayerObject.transform.position);

                    if (session.IsHost)
                    {
                        session.Server.BroadcastCustomBytes(packet);
                    }
                    else
                    {
                        client.SendCustomBytes(packet);
                    }
                }

                if (m_shootRequested)
                {
                    m_shootRequested = false;

                    string myName = client.userName;
                    string currentRole = "Chased";
                    PlayerDTO2 data;
                    if (client.Teams.TryGetValue(myName, out data) && !string.IsNullOrEmpty(data.playerRole))
                    {
                        currentRole = data.playerRole;
                    }

                    if (currentRole == "Hunter")
                    {
                        Vector3 spawnPos = m_myPlayerObject.transform.position + Vector3.forward * 0.8f;
                        Vector3 shootDir = Vector3.forward;

                        SpawnAndSendBullet(session, client, m_myObjectId, spawnPos, shootDir);
                    }
                    else
                    {
                        Debug.Log("No puedes disparar porque eres el que escapa.");
                    }
                }
            }
        }

        void SpawnAndSendBullet(LobbySession session, LobbyClient client, int objectId, Vector3 pos, Vector3 dir)
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

        void CreateBulletGameObject(Vector3 pos, Vector3 dir)
        {
            GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.transform.position = pos;
            bullet.transform.localScale = Vector3.one * 0.4f;
            bullet.AddComponent<BulletMover>();
            Renderer rend = bullet.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = CreatePrimitiveMaterial(UnityEngine.Color.yellow);
            }
        }

        void OnGUI()
        {
            m_isTypingInChat = (GUI.GetNameOfFocusedControl() == "chat");

            Event e = Event.current;
            if (!m_isTypingInChat && e != null)
            {
                if (e.type == EventType.KeyDown || e.type == EventType.KeyUp)
                {
                    bool isDown = (e.type == EventType.KeyDown);
                    if (e.keyCode == KeyCode.W || e.keyCode == KeyCode.UpArrow) m_moveW = isDown;
                    if (e.keyCode == KeyCode.S || e.keyCode == KeyCode.DownArrow) m_moveS = isDown;
                    if (e.keyCode == KeyCode.A || e.keyCode == KeyCode.LeftArrow) m_moveA = isDown;
                    if (e.keyCode == KeyCode.D || e.keyCode == KeyCode.RightArrow) m_moveD = isDown;
                    if (e.keyCode == KeyCode.Space && isDown) m_jumpRequested = true;
                    if (e.keyCode == KeyCode.F && isDown) m_shootRequested = true;
                }
            }
            else if (m_isTypingInChat)
            {
                m_moveW = false;
                m_moveS = false;
                m_moveA = false;
                m_moveD = false;
                m_jumpRequested = false;
                m_shootRequested = false;
            }

            if (LobbyGUI.NeedsSession()) return;
            LobbySession session = LobbySession.Instance;
            LobbyClient client = session.Client;

            LobbyGUI.Begin("<color=black>GAME  (" + session.protocol + ")   " + client.RoomInfo);
            GUILayout.Label("<color=black>Move: forward: W / ↑ Left: A / ← Backward: S / ↓ Right: D / →  Jump: Space  Shoot: F");
            GUILayout.Label("<color=black>Players: " + string.Join(", ", client.Players) +
                            "<color=black>    ping " + (client.PingMs < 0 ? "-" : client.PingMs + " ms"));

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(400));
            m_chatInput = LobbyGUI.Chat(client, m_chatInput, 220);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            if (GUILayout.Button(session.IsHost ? "End game" : "Leave", GUILayout.Width(160), GUILayout.Height(40)))
                client.Leave();
            LobbyGUI.End();
        }
    }

    public class BulletMover : MonoBehaviour
    {
        private Vector3 m_direction = Vector3.forward;
        private float m_speed = 12f;

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