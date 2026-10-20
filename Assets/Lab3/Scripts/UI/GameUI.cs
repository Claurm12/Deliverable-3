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
        private bool m_isTypingInChat = false;

        //salto
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
            }

            string myName = session.Client.userName;
            m_myObjectId = myName.GetHashCode();

            // Cubo
            m_myPlayerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_myPlayerObject.name = "Player_" + myName;
            float startX = UnityEngine.Random.Range(-3f, 3f);
            m_myPlayerObject.transform.position = new Vector3(startX, 0.5f, 0f);
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

                // Gravedad vertical
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

                // Se actualiza la posicion del UDP
                m_sendTimer += Time.deltaTime;
                if (m_sendTimer >= 0.05f)
                {
                    m_sendTimer = 0f;
                    byte[] packet = Messages.Transform(m_myObjectId, m_myPlayerObject.transform.position);

                    if (session.IsHost && session.Server is UDPLobbyServer udpServer)
                    {
                        udpServer.BroadcastCustomBytes(packet);
                    }
                    else if (!session.IsHost && client is UDPLobbyClient udpClient)
                    {
                        udpClient.SendCustomBytes(packet);
                    }
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

            // Recibe los paquetes de los otros "cubos"
            ProcessIncomingTransformPackets(client);
        }

        void ProcessIncomingTransformPackets(LobbyClient client)
        {
            if (client == null) return;
        }

        void OnGUI()
        {
            m_isTypingInChat = (GUI.GetNameOfFocusedControl() == "chat");

            Event e = Event.current;
            if (!m_isTypingInChat && e != null && (e.type == EventType.KeyDown || e.type == EventType.KeyUp))
            {
                bool isDown = (e.type == EventType.KeyDown);
                if (e.keyCode == KeyCode.W || e.keyCode == KeyCode.UpArrow) m_moveW = isDown;
                if (e.keyCode == KeyCode.S || e.keyCode == KeyCode.DownArrow) m_moveS = isDown;
                if (e.keyCode == KeyCode.A || e.keyCode == KeyCode.LeftArrow) m_moveA = isDown;
                if (e.keyCode == KeyCode.D || e.keyCode == KeyCode.RightArrow) m_moveD = isDown;
                if (e.keyCode == KeyCode.Space && isDown) m_jumpRequested = true;
            }
            else if (m_isTypingInChat)
            {
                m_moveW = false;
                m_moveS = false;
                m_moveA = false;
                m_moveD = false;
                m_jumpRequested = false;
            }

            if (LobbyGUI.NeedsSession()) return;
            LobbySession session = LobbySession.Instance;
            LobbyClient client = session.Client;

            LobbyGUI.Begin("GAME  (" + session.protocol + ")   " + client.RoomInfo);
            GUILayout.Label("Move: W, A, S, D Jump: Space");
            GUILayout.Label("Players: " + string.Join(", ", client.Players) +
                            "    ping " + (client.PingMs < 0 ? "-" : client.PingMs + " ms"));

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

                remotePlayer = new RemotePlayer { gameObject = newObj, lastReceiveTime = Time.realtimeSinceStartup };
                m_remotePlayers[objectId] = remotePlayer;
            }

            remotePlayer.gameObject.transform.position = pos;
            remotePlayer.lastReceiveTime = Time.realtimeSinceStartup;
        }
    }
}