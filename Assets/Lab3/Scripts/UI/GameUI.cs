using UnityEngine;

namespace Delivery3
{
    public class GameUI : MonoBehaviour
    {
        string m_chatInput = "";

        public static bool MoveW { get; private set; }
        public static bool MoveS { get; private set; }
        public static bool MoveA { get; private set; }
        public static bool MoveD { get; private set; }
        public static bool JumpRequested { get; set; }
        public static bool ShootRequested { get; set; }
        public static bool IsTypingInChat { get; private set; }
        public static bool IsRotatingCamera { get; private set; }

        static Vector2 s_cameraDrag;

        public static Vector2 ConsumeCameraDrag()
        {
            Vector2 drag = s_cameraDrag;
            s_cameraDrag = Vector2.zero;
            return drag;
        }

        private GUIStyle m_hunterCardStyle;
        private GUIStyle m_chasedCardStyle;
        private GUIStyle m_panelStyle;
        private GUIStyle m_roleTitleStyle;
        private GUIStyle m_objectiveStyle;
        private GUIStyle m_infoLabelStyle;
        private GUIStyle m_controlsStyle;
        private GUIStyle m_chatLineStyle;
        private GUIStyle m_inputStyle;
        private GUIStyle m_leaveBtnStyle;
        private GUIStyle m_sendBtnStyle;
        private Texture2D m_shadowTex;

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
            if (m_hunterCardStyle != null) return;

            Color gold = new Color(1.00f, 0.82f, 0.20f, 1f);
            Color hunterRed = new Color(1.00f, 0.38f, 0.20f, 1f);
            Color chasedGreen = new Color(0.36f, 0.86f, 0.28f, 1f);
            Color cyan = new Color(0.42f, 0.78f, 0.88f, 0.85f);

            m_shadowTex = new Texture2D(2, 2);
            Color sc = new Color(0.05f, 0.04f, 0.08f, 0.55f);
            m_shadowTex.SetPixels(new[] { sc, sc, sc, sc });
            m_shadowTex.Apply();

            m_hunterCardStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeGradTex(64, 64,
                    new Color(0.28f, 0.16f, 0.16f, 0.94f),
                    new Color(0.16f, 0.11f, 0.13f, 0.96f), hunterRed, 3) },
                padding = new RectOffset(18, 18, 10, 10)
            };

            m_chasedCardStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeGradTex(64, 64,
                    new Color(0.16f, 0.26f, 0.20f, 0.94f),
                    new Color(0.12f, 0.17f, 0.15f, 0.96f), chasedGreen, 3) },
                padding = new RectOffset(18, 18, 10, 10)
            };

            m_panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeGradTex(64, 64,
                    new Color(0.22f, 0.23f, 0.28f, 0.92f),
                    new Color(0.14f, 0.15f, 0.19f, 0.95f), gold, 2) },
                padding = new RectOffset(14, 14, 12, 12)
            };

            m_roleTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = { textColor = Color.white }
            };

            m_objectiveStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = { textColor = new Color(0.95f, 0.96f, 0.92f) }
            };

            m_controlsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = { textColor = new Color(0.75f, 0.92f, 1f) }
            };

            m_infoLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                richText = true,
                normal = { textColor = Color.white }
            };

            m_chatLineStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                richText = true,
                normal = { textColor = new Color(0.90f, 0.92f, 0.95f) }
            };

            m_inputStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 4, 4),
                normal = { textColor = Color.white, background = MakeGradTex(16, 16,
                    new Color(0.11f, 0.12f, 0.15f, 1f), new Color(0.15f, 0.16f, 0.20f, 1f), cyan, 2) },
                focused = { textColor = gold, background = MakeGradTex(16, 16,
                    new Color(0.14f, 0.15f, 0.19f, 1f), new Color(0.19f, 0.20f, 0.25f, 1f), gold, 2) }
            };

            m_leaveBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = MakeGradTex(16, 32,
                    new Color(1.00f, 0.45f, 0.18f, 1f), new Color(0.86f, 0.22f, 0.18f, 1f), new Color(1.00f, 0.88f, 0.48f, 0.95f), 2) },
                hover = { textColor = Color.white, background = MakeGradTex(16, 32,
                    new Color(1.00f, 0.56f, 0.26f, 1f), new Color(0.96f, 0.30f, 0.24f, 1f), Color.white, 2) }
            };

            m_sendBtnStyle = new GUIStyle(GUI.skin.button)
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
            IsTypingInChat = (GUI.GetNameOfFocusedControl() == "chat");

            Event e = Event.current;
            if (e != null && e.isMouse && e.button == 1)
            {
                if (e.type == EventType.MouseDown) IsRotatingCamera = true;
                else if (e.type == EventType.MouseUp) IsRotatingCamera = false;
                else if (e.type == EventType.MouseDrag && IsRotatingCamera) s_cameraDrag += e.delta;
            }

            if (!IsTypingInChat && e != null)
            {
                if (e.type == EventType.KeyDown || e.type == EventType.KeyUp)
                {
                    bool isDown = (e.type == EventType.KeyDown);
                    if (e.keyCode == KeyCode.W || e.keyCode == KeyCode.UpArrow) MoveW = isDown;
                    if (e.keyCode == KeyCode.S || e.keyCode == KeyCode.DownArrow) MoveS = isDown;
                    if (e.keyCode == KeyCode.A || e.keyCode == KeyCode.LeftArrow) MoveA = isDown;
                    if (e.keyCode == KeyCode.D || e.keyCode == KeyCode.RightArrow) MoveD = isDown;
                    if (e.keyCode == KeyCode.Space && isDown) JumpRequested = true;
                    if (e.keyCode == KeyCode.F && isDown) ShootRequested = true;
                }
            }
            else if (IsTypingInChat)
            {
                MoveW = false;
                MoveS = false;
                MoveA = false;
                MoveD = false;
                JumpRequested = false;
                ShootRequested = false;
            }

            if (LobbyGUI.NeedsSession()) return;
            InitStyles();

            LobbySession session = LobbySession.Instance;
            LobbyClient client = session.Client;

            string role = "Chased";
            if (client != null && !string.IsNullOrEmpty(client.userName) &&
                client.Teams.TryGetValue(client.userName, out PlayerDTO2 dto) &&
                !string.IsNullOrEmpty(dto.playerRole))
            {
                role = dto.playerRole;
            }

            bool isHunter = (role == "Hunter");
            int lives = isHunter ? 3 : 1;
            bool isDead = false;

            int myId = NetObject.ComputeId(client.userName);
            LinkingContext linking = client.GetComponent<LinkingContext>();
            if (linking == null) linking = FindObjectOfType<LinkingContext>();
            if (linking != null)
            {
                GameObject myGo = linking.Find(myId);
                if (myGo != null)
                {
                    NetPlayer np = myGo.GetComponent<NetPlayer>();
                    if (np != null)
                    {
                        lives = np.health;
                        isDead = np.isDead;
                    }
                }
            }

            float roleW = 540f;
            float roleH = 96f;
            Rect roleRect = new Rect((Screen.width - roleW) * 0.5f, 14f, roleW, roleH);

            GUI.DrawTexture(new Rect(roleRect.x + 6f, roleRect.y + 8f, roleRect.width, roleRect.height), m_shadowTex);
            GUILayout.BeginArea(roleRect, isHunter ? m_hunterCardStyle : m_chasedCardStyle);

            if (isHunter)
            {
                string statusText = isDead
                    ? "<color=#FF5252>DEAD</color>"
                    : ("<color=#FFD166>Lives: " + lives + " / 3</color>");

                GUILayout.Label("<color=#FF6B35>ROLE: HUNTER</color>   |   " + statusText, m_roleTitleStyle);
                GUILayout.Label("Eliminate the runners. Careful: shooting an NPC costs 1 life!", m_objectiveStyle);
                GUILayout.Label("Move: W/A/S/D or Arrows  •  Jump: Space  •  <color=#FFD166>Shoot: F</color>  •  Camera: hold right click", m_controlsStyle);
            }
            else
            {
                string statusText = isDead
                    ? "<color=#FF5252>DEAD</color>"
                    : ("<color=#70FF5C>Lives: " + lives + "</color>");

                GUILayout.Label("<color=#58D646>ROLE: CHASED</color>   |   " + statusText, m_roleTitleStyle);
                GUILayout.Label("Stand inside the red circles until they turn green to complete tasks!", m_objectiveStyle);
                GUILayout.Label("Move: W/A/S/D or Arrows  •  Jump: Space  •  Camera: hold right click", m_controlsStyle);
            }

            GUILayout.EndArea();

            float infoW = 310f;
            float infoH = 128f;
            Rect infoRect = new Rect(16f, 14f, infoW, infoH);

            GUI.DrawTexture(new Rect(infoRect.x + 6f, infoRect.y + 8f, infoRect.width, infoRect.height), m_shadowTex);
            GUILayout.BeginArea(infoRect, m_panelStyle);

            GUILayout.Label("<color=#FFD166>GAME (" + session.protocol + ")</color>  " + client.RoomInfo, m_infoLabelStyle);
            string pingStr = client.PingMs < 0 ? "-" : client.PingMs + " ms";
            GUILayout.Label("Players: " + string.Join(", ", client.Players) + "  |  " + pingStr, m_chatLineStyle);
            GUILayout.Space(8);
            if (GUILayout.Button(session.IsHost ? "End game" : "Leave", m_leaveBtnStyle, GUILayout.Height(34)))
                client.Leave();

            GUILayout.EndArea();

            float chatW = 370f;
            float chatH = 200f;
            Rect chatRect = new Rect(16f, Screen.height - chatH - 16f, chatW, chatH);

            GUI.DrawTexture(new Rect(chatRect.x + 6f, chatRect.y + 8f, chatRect.width, chatRect.height), m_shadowTex);
            GUILayout.BeginArea(chatRect, m_panelStyle);

            GUILayout.Label("<color=#FFD166>Chat</color>", m_infoLabelStyle);
            GUILayout.Space(4);

            int maxLines = 5;
            int first = Mathf.Max(0, client.Chat.Count - maxLines);
            for (int i = first; i < client.Chat.Count; i++)
                GUILayout.Label(client.Chat[i], m_chatLineStyle);

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            bool enter = Event.current.type == EventType.KeyDown &&
                         (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);

            GUI.SetNextControlName("chat");
            m_chatInput = GUILayout.TextField(m_chatInput, 200, m_inputStyle, GUILayout.Height(30));
            GUILayout.Space(6);
            if ((GUILayout.Button("Send", m_sendBtnStyle, GUILayout.Width(68), GUILayout.Height(30)) ||
                (enter && GUI.GetNameOfFocusedControl() == "chat")) && m_chatInput.Trim().Length > 0)
            {
                client.SendChat(m_chatInput);
                m_chatInput = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }
    }
}