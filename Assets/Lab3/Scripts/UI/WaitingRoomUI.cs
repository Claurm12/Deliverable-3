using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Delivery3
{
    public class WaitingRoomUI : MonoBehaviour
    {
        private string m_chatInput = "";
        private string m_localIP = "";

        private GUIStyle m_cardStyle;
        private GUIStyle m_panelStyle;
        private GUIStyle m_titleStyle;
        private GUIStyle m_subtitleStyle;
        private GUIStyle m_labelStyle;
        private GUIStyle m_playerItemStyle;
        private GUIStyle m_inputStyle;
        private GUIStyle m_primaryBtnStyle;
        private GUIStyle m_secondaryBtnStyle;
        private GUIStyle m_smallBtnStyle;
        private Texture2D m_shadowTex;

        void Start()
        {
            m_localIP = GetLocalIPAddress();
        }

        private Texture2D MakeGradTex(int w, int h, Color top, Color bottom, Color border, int borderSize)
        {
            Texture2D tex = new Texture2D(w, h);
            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color rowColor = Color.Lerp(bottom, top, t);
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = x < borderSize || x >= w - borderSize || y < borderSize || y >= h - borderSize;
                    pixels[y * w + x] = isBorder ? border : rowColor;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void InitStyles()
        {
            if (m_cardStyle != null) return;

            Color gold = new Color(1.00f, 0.82f, 0.20f, 1f);
            Color cyan = new Color(0.42f, 0.78f, 0.88f, 0.85f);

            m_shadowTex = new Texture2D(2, 2);
            Color sc = new Color(0.05f, 0.04f, 0.08f, 0.55f);
            m_shadowTex.SetPixels(new[] { sc, sc, sc, sc });
            m_shadowTex.Apply();

            m_cardStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeGradTex(64, 64,
                    new Color(0.22f, 0.23f, 0.28f, 0.95f),
                    new Color(0.14f, 0.15f, 0.19f, 0.97f), gold, 3) },
                padding = new RectOffset(30, 30, 26, 26)
            };

            m_panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeGradTex(32, 32,
                    new Color(0.26f, 0.28f, 0.34f, 0.95f),
                    new Color(0.18f, 0.19f, 0.24f, 0.96f), cyan, 2) },
                padding = new RectOffset(15, 15, 14, 14)
            };

            m_titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 27,
                fontStyle = FontStyle.Bold,
                normal = { textColor = gold }
            };

            m_subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.85f, 0.88f, 0.92f) }
            };

            m_labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = gold }
            };

            m_playerItemStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                padding = new RectOffset(4, 4, 4, 4)
            };

            m_inputStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 6, 6),
                normal = { textColor = Color.white, background = MakeGradTex(16, 16,
                    new Color(0.11f, 0.12f, 0.15f, 1f), new Color(0.15f, 0.16f, 0.20f, 1f), new Color(0.45f, 0.48f, 0.55f, 1f), 2) },
                focused = { textColor = gold, background = MakeGradTex(16, 16,
                    new Color(0.14f, 0.15f, 0.19f, 1f), new Color(0.19f, 0.20f, 0.25f, 1f), gold, 2) }
            };

            m_primaryBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = MakeGradTex(16, 32,
                    new Color(0.44f, 0.86f, 0.24f, 1f), new Color(0.16f, 0.64f, 0.32f, 1f), new Color(0.82f, 1.00f, 0.65f, 0.95f), 2) },
                hover = { textColor = Color.white, background = MakeGradTex(16, 32,
                    new Color(0.54f, 0.94f, 0.32f, 1f), new Color(0.22f, 0.74f, 0.38f, 1f), Color.white, 2) }
            };

            m_secondaryBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = MakeGradTex(16, 32,
                    new Color(1.00f, 0.45f, 0.18f, 1f), new Color(0.86f, 0.22f, 0.18f, 1f), new Color(1.00f, 0.88f, 0.48f, 0.95f), 2) },
                hover = { textColor = Color.white, background = MakeGradTex(16, 32,
                    new Color(1.00f, 0.56f, 0.26f, 1f), new Color(0.96f, 0.30f, 0.24f, 1f), Color.white, 2) }
            };

            m_smallBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = MakeGradTex(16, 32,
                    new Color(0.20f, 0.62f, 0.82f, 1f), new Color(0.13f, 0.42f, 0.62f, 1f), new Color(0.60f, 0.88f, 0.98f, 0.9f), 2) },
                hover = { textColor = Color.white, background = MakeGradTex(16, 32,
                    new Color(0.28f, 0.72f, 0.92f, 1f), new Color(0.18f, 0.52f, 0.74f, 1f), Color.white, 2) }
            };
        }

        void OnGUI()
        {
            if (LobbyGUI.NeedsSession()) return;
            InitStyles();

            LobbySession session = LobbySession.Instance;
            LobbyClient client = session.Client;

            float cardWidth = Mathf.Min(860f, Screen.width - 40f);
            float cardHeight = Mathf.Min(585f, Screen.height - 40f);
            Rect cardRect = new Rect(
                (Screen.width - cardWidth) * 0.5f,
                (Screen.height - cardHeight) * 0.5f,
                cardWidth,
                cardHeight
            );

            GUI.DrawTexture(new Rect(cardRect.x + 8f, cardRect.y + 10f, cardRect.width, cardRect.height), m_shadowTex);
            GUILayout.BeginArea(cardRect, m_cardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("WAITING ROOM  •  " + client.RoomInfo, m_titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(session.protocol.ToString(), m_labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            string pingStr = client.PingMs < 0 ? "-" : client.PingMs + " ms";
            GUILayout.Label("Player: " + client.userName + (session.IsHost ? " (Host)" : "") + "   |   Ping: " + pingStr, m_subtitleStyle);
            GUILayout.FlexibleSpace();

            if (session.IsHost)
            {
                GUILayout.Label("IP: " + m_localIP + " : " + session.Server.port, m_labelStyle);
                GUILayout.Space(8);
                if (GUILayout.Button("COPY IP", m_smallBtnStyle, GUILayout.Width(78), GUILayout.Height(26)))
                {
                    GUIUtility.systemCopyBuffer = m_localIP;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(16);

            // Extra vertical margin so bottom buttons stay clear of the card border
            float innerHeight = cardHeight - 200f;
            GUILayout.BeginHorizontal(GUILayout.Height(innerHeight));
            DrawPlayers(session, client, innerHeight);
            GUILayout.Space(16);
            DrawStyledChat(client, innerHeight);
            GUILayout.EndHorizontal();

            GUILayout.Space(18);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(session.IsHost ? "CLOSE ROOM" : "LEAVE", m_secondaryBtnStyle, GUILayout.Width(165), GUILayout.Height(42)))
            {
                client.Leave();
            }
            GUILayout.FlexibleSpace();
            if (session.IsHost && GUILayout.Button("START GAME", m_primaryBtnStyle, GUILayout.Width(200), GUILayout.Height(42)))
            {
                session.Server.StartGame();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawPlayers(LobbySession session, LobbyClient client, float height)
        {
            GUILayout.BeginVertical(m_panelStyle, GUILayout.Width(340), GUILayout.Height(height));
            string countText = "PLAYERS (" + client.Players.Count + (session.IsHost ? "/" + session.Server.maxPlayers : "") + ")";
            GUILayout.Label(countText, m_labelStyle);
            GUILayout.Space(8);

            foreach (string name in client.Players)
            {
                GUILayout.BeginHorizontal();
                string line = "• " + name + (name == client.userName ? " (you)" : "");
                LobbyServer.Player hosted = session.IsHost ? FindHosted(session, name) : null;
                if (hosted != null)
                {
                    line += "  [" + (hosted.pingMs < 0 ? "-" : hosted.pingMs + "ms") + "]";
                }
                GUILayout.Label(line, m_playerItemStyle);
                GUILayout.FlexibleSpace();

                bool kick = hosted != null && name != client.userName && GUILayout.Button("KICK", m_smallBtnStyle, GUILayout.Width(60), GUILayout.Height(26));
                GUILayout.EndHorizontal();

                if (kick)
                {
                    session.Server.Kick(hosted);
                    break;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        private void DrawStyledChat(LobbyClient client, float height)
        {
            GUILayout.BeginVertical(m_panelStyle, GUILayout.Height(height));
            GUILayout.Label("ROOM CHAT", m_labelStyle);
            GUILayout.Space(6);

            int maxLines = Mathf.Max(1, (int)((height - 95f) / 24f));
            int first = Mathf.Max(0, client.Chat.Count - maxLines);
            for (int i = first; i < client.Chat.Count; i++)
            {
                GUILayout.Label(client.Chat[i], m_subtitleStyle);
            }
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            bool enter = Event.current.type == EventType.KeyDown &&
                         (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);

            GUI.SetNextControlName("chat");
            m_chatInput = GUILayout.TextField(m_chatInput, 200, m_inputStyle, GUILayout.Height(34));

            GUILayout.Space(6);
            if ((GUILayout.Button("SEND", m_smallBtnStyle, GUILayout.Width(75), GUILayout.Height(34)) || (enter && GUI.GetNameOfFocusedControl() == "chat"))
                && m_chatInput.Trim().Length > 0)
            {
                client.SendChat(m_chatInput);
                m_chatInput = "";
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static LobbyServer.Player FindHosted(LobbySession session, string name)
        {
            foreach (LobbyServer.Player p in session.Server.Players) if (p.name == name) return p;
            return null;
        }

        public static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "127.0.0.1";
        }
    }
}