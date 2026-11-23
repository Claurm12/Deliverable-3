using UnityEngine;
using UnityEngine.SceneManagement;

namespace Delivery3
{
    public class CreateGameUI : MonoBehaviour
    {
        private string m_roomName = "MyRoom";
        private string m_port = "9050";
        private int m_maxPlayers = 4;
        private bool m_creating;
        private string m_localIp;

        private GUIStyle m_cardStyle;
        private GUIStyle m_titleStyle;
        private GUIStyle m_subtitleStyle;
        private GUIStyle m_labelStyle;
        private GUIStyle m_valueStyle;
        private GUIStyle m_inputStyle;
        private GUIStyle m_primaryBtnStyle;
        private GUIStyle m_secondaryBtnStyle;
        private GUIStyle m_stepBtnStyle;
        private GUIStyle m_ipBoxStyle;

        void Start()
        {
            m_localIp = LobbyServer.GetLocalHostIP();
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
            m_cardStyle.padding = new RectOffset(35, 35, 30, 30);

            m_titleStyle = new GUIStyle(GUI.skin.label);
            m_titleStyle.fontSize = 30;
            m_titleStyle.fontStyle = FontStyle.Bold;
            m_titleStyle.alignment = TextAnchor.MiddleCenter;
            m_titleStyle.normal.textColor = new Color(0.95f, 0.96f, 0.98f);

            m_subtitleStyle = new GUIStyle(GUI.skin.label);
            m_subtitleStyle.fontSize = 14;
            m_subtitleStyle.alignment = TextAnchor.MiddleCenter;
            m_subtitleStyle.normal.textColor = new Color(0.60f, 0.68f, 0.78f);

            m_labelStyle = new GUIStyle(GUI.skin.label);
            m_labelStyle.fontSize = 15;
            m_labelStyle.fontStyle = FontStyle.Bold;
            m_labelStyle.normal.textColor = new Color(0.82f, 0.86f, 0.92f);

            m_valueStyle = new GUIStyle(GUI.skin.label);
            m_valueStyle.fontSize = 20;
            m_valueStyle.fontStyle = FontStyle.Bold;
            m_valueStyle.alignment = TextAnchor.MiddleCenter;
            m_valueStyle.normal.textColor = Color.white;

            m_inputStyle = new GUIStyle(GUI.skin.textField);
            m_inputStyle.fontSize = 18;
            m_inputStyle.alignment = TextAnchor.MiddleLeft;
            m_inputStyle.padding = new RectOffset(12, 12, 8, 8);
            m_inputStyle.normal.textColor = Color.white;
            m_inputStyle.normal.background = MakeTex(2, 2, new Color(0.16f, 0.19f, 0.25f, 1f));
            m_inputStyle.focused.background = MakeTex(2, 2, new Color(0.20f, 0.25f, 0.34f, 1f));

            m_ipBoxStyle = new GUIStyle(GUI.skin.box);
            m_ipBoxStyle.fontSize = 15;
            m_ipBoxStyle.fontStyle = FontStyle.Bold;
            m_ipBoxStyle.alignment = TextAnchor.MiddleCenter;
            m_ipBoxStyle.padding = new RectOffset(10, 10, 10, 10);
            m_ipBoxStyle.normal.textColor = new Color(0.45f, 0.78f, 1f);
            m_ipBoxStyle.normal.background = MakeTex(2, 2, new Color(0.13f, 0.17f, 0.24f, 1f));

            m_stepBtnStyle = new GUIStyle(GUI.skin.button);
            m_stepBtnStyle.fontSize = 20;
            m_stepBtnStyle.fontStyle = FontStyle.Bold;
            m_stepBtnStyle.normal.textColor = Color.white;
            m_stepBtnStyle.normal.background = MakeTex(2, 2, new Color(0.18f, 0.48f, 0.82f, 1f));
            m_stepBtnStyle.hover.background = MakeTex(2, 2, new Color(0.24f, 0.56f, 0.92f, 1f));

            m_primaryBtnStyle = new GUIStyle(GUI.skin.button);
            m_primaryBtnStyle.fontSize = 18;
            m_primaryBtnStyle.fontStyle = FontStyle.Bold;
            m_primaryBtnStyle.normal.textColor = Color.white;
            m_primaryBtnStyle.normal.background = MakeTex(2, 2, new Color(0.85f, 0.26f, 0.22f, 1f));
            m_primaryBtnStyle.hover.background = MakeTex(2, 2, new Color(0.95f, 0.32f, 0.28f, 1f));
            m_primaryBtnStyle.active.background = MakeTex(2, 2, new Color(0.70f, 0.18f, 0.15f, 1f));

            m_secondaryBtnStyle = new GUIStyle(GUI.skin.button);
            m_secondaryBtnStyle.fontSize = 18;
            m_secondaryBtnStyle.fontStyle = FontStyle.Bold;
            m_secondaryBtnStyle.normal.textColor = Color.white;
            m_secondaryBtnStyle.normal.background = MakeTex(2, 2, new Color(0.25f, 0.30f, 0.38f, 1f));
            m_secondaryBtnStyle.hover.background = MakeTex(2, 2, new Color(0.32f, 0.38f, 0.48f, 1f));
            m_secondaryBtnStyle.active.background = MakeTex(2, 2, new Color(0.18f, 0.22f, 0.28f, 1f));
        }

        void OnGUI()
        {
            if (string.IsNullOrEmpty(m_localIp))
                m_localIp = LobbyServer.GetLocalHostIP();

            InitStyles();
            LobbySession session = LobbySession.Instance;

            float cardWidth = 460f;
            float cardHeight = 490f;
            Rect cardRect = new Rect(
                (Screen.width - cardWidth) * 0.5f,
                (Screen.height - cardHeight) * 0.5f,
                cardWidth,
                cardHeight
            );

            GUILayout.BeginArea(cardRect, m_cardStyle);

            GUILayout.Label("CREATE GAME", m_titleStyle);
            GUILayout.Label("PROTOCOL: " + session.protocol, m_subtitleStyle);
            GUILayout.Space(20);

            GUI.enabled = !m_creating;

            GUILayout.Label("ROOM NAME", m_labelStyle);
            GUILayout.Space(4);
            m_roomName = GUILayout.TextField(m_roomName, 24, m_inputStyle, GUILayout.Height(38));

            GUILayout.Space(14);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(170));
            GUILayout.Label("PORT", m_labelStyle);
            GUILayout.Space(4);
            m_port = GUILayout.TextField(m_port, 6, m_inputStyle, GUILayout.Height(38));
            GUILayout.EndVertical();

            GUILayout.Space(20);

            GUILayout.BeginVertical();
            GUILayout.Label("MAX PLAYERS", m_labelStyle);
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", m_stepBtnStyle, GUILayout.Width(42), GUILayout.Height(38))) m_maxPlayers--;
            GUILayout.Label(m_maxPlayers.ToString(), m_valueStyle, GUILayout.Height(38));
            if (GUILayout.Button("+", m_stepBtnStyle, GUILayout.Width(42), GUILayout.Height(38))) m_maxPlayers++;
            m_maxPlayers = Mathf.Clamp(m_maxPlayers, 2, 16);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(18);

            GUILayout.Box("SERVER IP: " + m_localIp + "   |   PORT: " + m_port, m_ipBoxStyle, GUILayout.Height(40));

            GUILayout.Space(22);

            int port;
            bool validPort = int.TryParse(m_port, out port);

            GUILayout.BeginHorizontal();
            GUI.enabled = true;
            if (GUILayout.Button("BACK", m_secondaryBtnStyle, GUILayout.Width(140), GUILayout.Height(44)))
            {
                session.EndSession();
                SceneManager.LoadScene(LobbySession.MenuScene);
            }

            GUILayout.Space(12);

            GUI.enabled = !m_creating && validPort && m_roomName.Trim().Length > 0;
            if (GUILayout.Button(m_creating ? "CREATING..." : "CREATE", m_primaryBtnStyle, GUILayout.Height(44)))
            {
                m_creating = session.Host(m_roomName.Trim(), port, m_maxPlayers);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            LobbyGUI.Message(session.message);
            GUILayout.EndArea();
        }
    }
}