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

        private class RemotePlayer
        {
            public GameObject gameObject;
            public float lastReceiveTime;
        }

        //Guardamos los "cubos" que se relacionan con la id del obj
        private readonly Dictionary<int, RemotePlayer> m_remotePlayers = new Dictionary<int, RemotePlayer>();

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

            if (session.Client != null)
            {
                session.Client.TransformReceived += OnRemoteTransformReceived;
                session.Client.ShootReceived += OnRemoteShootReceived;
            }

            string myName = session.Client.userName;
            m_myObjectId = myName.GetHashCode();

            //Cubo local
            m_myPlayerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_myPlayerObject.name = "Player_" + myName;
            float startX = UnityEngine.Random.Range(-3f, 3f);
            m_myPlayerObject.transform.position = new Vector3(startX, 0.5f, 0f);

          
            ApplyPlayerColor(m_myPlayerObject, myName);

           
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

        // Color segun el rol
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

                // Gestion del salto
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

                // Enviar posicion por UDP
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

                //Disparar
                if (m_shootRequested)
                {
                    m_shootRequested = false;
                    Vector3 spawnPos = m_myPlayerObject.transform.position + Vector3.forward * 0.8f;
                    Vector3 shootDir = Vector3.forward;

                    SpawnAndSendBullet(session, client, m_myObjectId, spawnPos, shootDir);
                }
            }

            // Limpiar jugadores desconectados
            if (client != null && client.Players != null)
            {
                List<int> idsToRemove = new List<int>();
                float currentTime = Time.realtimeSinceStartup;

                foreach (KeyValuePair<int, RemotePlayer> pair in m_remotePlayers)
                {
                    int objectId = pair.Key;
                    bool playerStillInRoom = false;

                    foreach (string playerName in client.Players)
                    {
                        if (playerName.GetHashCode() == objectId)
                        {
                            playerStillInRoom = true;
                            break;
                        }
                    }

                    if (!playerStillInRoom || (currentTime - pair.Value.lastReceiveTime > 3.0f))
                    {
                        idsToRemove.Add(objectId);
                    }
                }

                foreach (int id in idsToRemove)
                {
                    if (m_remotePlayers.TryGetValue(id, out RemotePlayer remotePlayer))
                    {
                        if (remotePlayer.gameObject != null) Destroy(remotePlayer.gameObject);
                        m_remotePlayers.Remove(id);
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

        void OnRemoteTransformReceived(int objectId, Vector3 pos)
        {
            if (objectId == m_myObjectId) return;

            RemotePlayer remotePlayer;
            if (!m_remotePlayers.TryGetValue(objectId, out remotePlayer))
            {
                GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                newObj.name = "Player_Remote_" + objectId;

                string remotePlayerName = "";
                foreach (string name in LobbySession.Instance.Client.Players)
                {
                    if (name.GetHashCode() == objectId)
                    {
                        remotePlayerName = name;
                        break;
                    }
                }

                remotePlayer = new RemotePlayer { gameObject = newObj, lastReceiveTime = Time.realtimeSinceStartup };
                m_remotePlayers[objectId] = remotePlayer;

                ApplyPlayerColor(newObj, remotePlayerName);
            }

            remotePlayer.gameObject.transform.position = pos;
            remotePlayer.lastReceiveTime = Time.realtimeSinceStartup;
        }

        void OnRemoteShootReceived(int objectId, Vector3 pos, Vector3 dir)
        {
            if (objectId == m_myObjectId) return;
            CreateBulletGameObject(pos, dir);
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