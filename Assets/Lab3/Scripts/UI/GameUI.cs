using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lab3Solution
{
    public class GameUI : MonoBehaviour
    {
        string m_chatInput = "";

        private GameObject m_myPlayerObject;
        private float m_sendTimer;
        private int m_myObjectId;
        private bool m_moveW, m_moveS, m_moveA, m_moveD;

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

            //Movimiento
            if (m_myPlayerObject != null)
            {
                float moveSpeed = 6f;
                float h = 0f;
                float v = 0f;

                if (m_moveW) v += 1f;
                if (m_moveS) v -= 1f;
                if (m_moveA) h -= 1f;
                if (m_moveD) h += 1f;

                Vector3 direction = new Vector3(h, 0, v);
                if (direction.magnitude > 0.1f)
                {
                    m_myPlayerObject.transform.position += direction.normalized * moveSpeed * Time.deltaTime;
                }

                //Posición
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
        }

        void OnGUI()
        {
            
            Event e = Event.current;
            if (e != null && (e.type == EventType.KeyDown || e.type == EventType.KeyUp))
            {
                bool isDown = (e.type == EventType.KeyDown);
                if (e.keyCode == KeyCode.W || e.keyCode == KeyCode.UpArrow) m_moveW = isDown;
                if (e.keyCode == KeyCode.S || e.keyCode == KeyCode.DownArrow) m_moveS = isDown;
                if (e.keyCode == KeyCode.A || e.keyCode == KeyCode.LeftArrow) m_moveA = isDown;
                if (e.keyCode == KeyCode.D || e.keyCode == KeyCode.RightArrow) m_moveD = isDown;
            }

            if (LobbyGUI.NeedsSession()) return;
            LobbySession session = LobbySession.Instance;
            LobbyClient client = session.Client;

            LobbyGUI.Begin("GAME  (" + session.protocol + ")   " + client.RoomInfo);
            GUILayout.Label("Usa W, A, S, D para mover tu cubo en red.");
            GUILayout.Label("Players: " + string.Join(", ", client.Players) +
                            "    ping " + (client.PingMs < 0 ? "-" : client.PingMs + " ms"));

            m_chatInput = LobbyGUI.Chat(client, m_chatInput, Screen.height - 250);

            if (GUILayout.Button(session.IsHost ? "End game" : "Leave", GUILayout.Width(160), GUILayout.Height(40)))
                client.Leave();
            LobbyGUI.End();
        }
    }
}