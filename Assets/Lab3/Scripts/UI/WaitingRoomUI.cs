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

        void Start()
        {
            m_localIP = GetLocalIPAddress();
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void InitStyles()
        {
            if (m_cardStyle != null) return;

            m_cardStyle = new GUIStyle(GUI.skin.box);
            m_cardStyle.normal.background = MakeTex(2, 2, new Color(0.09f, 0.11f, 0.15f, 0.94f));
            m_cardStyle.padding = new RectOffset(30, 30, 25, 25);

            m_panelStyle = new GUIStyle(GUI.skin.box);
            m_panelStyle.normal.background = MakeTex(2, 2, new Color(0.13f, 0.16f, 0.22f, 0.95f));
            m_panelStyle.padding = new RectOffset(15, 15, 15, 15);

            m_titleStyle = new GUIStyle(GUI.skin.label);
            m_titleStyle.fontSize = 28;
            m_titleStyle.fontStyle = FontStyle.Bold;
            m_titleStyle.normal.textColor = new Color(0.95f, 0.96f, 0.98f);

            m_subtitleStyle = new GUIStyle(GUI.skin.label);
            m_subtitleStyle.fontSize = 14;
            m_subtitleStyle.normal.textColor = new Color(0.60f, 0.68f, 0.78f);

            m_labelStyle = new GUIStyle(GUI.skin.label);
            m_labelStyle.fontSize = 15;
            m_labelStyle.fontStyle = FontStyle.Bold;
            m_labelStyle.normal.textColor = new Color(0.82f, 0.86f, 0.92f);

            m_playerItemStyle = new GUIStyle(GUI.skin.label);
            m_playerItemStyle.fontSize = 16;
            m_playerItemStyle.normal.textColor = Color.white;
            m_playerItemStyle.padding = new RectOffset(5, 5, 4, 4);

            m_inputStyle = new GUIStyle(GUI.skin.textField);
            m_inputStyle.fontSize = 16;
            m_inputStyle.alignment = TextAnchor.MiddleLeft;
            m_inputStyle.padding = new RectOffset(10, 10, 6, 6);
            m_inputStyle.normal.textColor = Color.white;
            m_inputStyle.normal.background = MakeTex(2, 2, new Color(0.18f, 0.22f, 0.29f, 1f));
            m_inputStyle.focused.background = MakeTex(2, 2, new Color(0.22f, 0.27f, 0.36f, 1f));

            m_primaryBtnStyle = new GUIStyle(GUI.skin.button);
            m_primaryBtnStyle.fontSize = 17;
            m_primaryBtnStyle.fontStyle = FontStyle.Bold;
            m_primaryBtnStyle.normal.textColor = Color.white;
            m_primaryBtnStyle.normal.background = MakeTex(2, 2, new Color(0.85f, 0.26f, 0.22f, 1f));
            m_primaryBtnStyle.hover.background = MakeTex(2, 2, new Color(0.95f, 0.32f, 0.28f, 1f));
            m_primaryBtnStyle.active.background = MakeTex(2, 2, new Color(0.70f, 0.18f, 0.15f, 1f));

            m_secondaryBtnStyle = new GUIStyle(GUI.skin.button);
            m_secondaryBtnStyle.fontSize = 17;
            m_secondaryBtnStyle.fontStyle = FontStyle.Bold;
            m_secondaryBtnStyle.normal.textColor = Color.white;
            m_secondaryBtnStyle.normal.background = MakeTex(2, 2, new Color(0.25f, 0.30f, 0.38f, 1f));
            m_secondaryBtnStyle.hover.background = MakeTex(2, 2, new Color(0.32f, 0.38f, 0.48f, 1f));
            m_secondaryBtnStyle.active.background = MakeTex(2, 2, new Color(0.18f, 0.22f, 0.28f, 1f));

            m_smallBtnStyle = new GUIStyle(GUI.skin.button);
            m_smallBtnStyle.fontSize = 13;
            m_smallBtnStyle.fontStyle = FontStyle.Bold;
            m_smallBtnStyle.normal.textColor = Color.white;
            m_smallBtnStyle.normal.background = MakeTex(2, 2, new Color(0.18f, 0.48f, 0.82f, 1f));
            m_smallBtnStyle.hover.background = MakeTex(2, 2, new Color(0.24f, 0.56f, 0.92f, 1f));
        }

        void OnGUI()
        {
            if (LobbyGUI.NeedsSession()) return;
            InitStyles();

            LobbySession session = LobbySession.Instance;
            LobbyClient client = session.Client;

            float cardWidth = Mathf.Min(860f, Screen.width - 40f);
            float cardHeight = Mathf.Min(560f, Screen.height - 40f);
            Rect cardRect = new Rect(
                (Screen.width - cardWidth) * 0.5f,
                (Screen.height - cardHeight) * 0.5f,
                cardWidth,
                cardHeight
            );

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
                if (GUILayout.Button("COPY IP", m_smallBtnStyle, GUILayout.Width(75), GUILayout.Height(24)))
                {
                    GUIUtility.systemCopyBuffer = m_localIP;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(16);

            float innerHeight = cardHeight - 175f;
            GUILayout.BeginHorizontal(GUILayout.Height(innerHeight));
            DrawPlayers(session, client, innerHeight);
            GUILayout.Space(16);
            DrawStyledChat(client, innerHeight);
            GUILayout.EndHorizontal();

            GUILayout.Space(16);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(session.IsHost ? "CLOSE ROOM" : "LEAVE", m_secondaryBtnStyle, GUILayout.Width(160), GUILayout.Height(42)))
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