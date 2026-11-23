using UnityEngine;
using UnityEngine.SceneManagement;

namespace Delivery3
{
    public class MenuUI : MonoBehaviour
    {
        private GUIStyle m_cardStyle;
        private GUIStyle m_titleStyle;
        private GUIStyle m_subtitleStyle;
        private GUIStyle m_labelStyle;
        private GUIStyle m_inputStyle;
        private GUIStyle m_primaryBtnStyle;
        private GUIStyle m_secondaryBtnStyle;
        private GUIStyle m_toolbarStyle;

        void Start()
        {
            LobbySession.Instance.EndSession(); // coming back here always closes the old session
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
            m_cardStyle.normal.background = MakeTex(2, 2, new Color(0.09f, 0.11f, 0.15f, 0.92f));
            m_cardStyle.padding = new RectOffset(35, 35, 30, 30);

            m_titleStyle = new GUIStyle(GUI.skin.label);
            m_titleStyle.fontSize = 32;
            m_titleStyle.fontStyle = FontStyle.Bold;
            m_titleStyle.alignment = TextAnchor.MiddleCenter;
            m_titleStyle.normal.textColor = new Color(0.95f, 0.96f, 0.98f);

            m_subtitleStyle = new GUIStyle(GUI.skin.label);
            m_subtitleStyle.fontSize = 14;
            m_subtitleStyle.alignment = TextAnchor.MiddleCenter;
            m_subtitleStyle.normal.textColor = new Color(0.60f, 0.68f, 0.78f);

            m_labelStyle = new GUIStyle(GUI.skin.label);
            m_labelStyle.fontSize = 16;
            m_labelStyle.fontStyle = FontStyle.Bold;
            m_labelStyle.normal.textColor = new Color(0.82f, 0.86f, 0.92f);

            m_inputStyle = new GUIStyle(GUI.skin.textField);
            m_inputStyle.fontSize = 18;
            m_inputStyle.alignment = TextAnchor.MiddleLeft;
            m_inputStyle.padding = new RectOffset(12, 12, 8, 8);
            m_inputStyle.normal.textColor = Color.white;
            m_inputStyle.normal.background = MakeTex(2, 2, new Color(0.16f, 0.19f, 0.25f, 1f));
            m_inputStyle.focused.background = MakeTex(2, 2, new Color(0.20f, 0.25f, 0.34f, 1f));

            m_toolbarStyle = new GUIStyle(GUI.skin.button);
            m_toolbarStyle.fontSize = 16;
            m_toolbarStyle.fontStyle = FontStyle.Bold;
            m_toolbarStyle.normal.textColor = new Color(0.75f, 0.80f, 0.88f);
            m_toolbarStyle.normal.background = MakeTex(2, 2, new Color(0.16f, 0.19f, 0.25f, 1f));
            m_toolbarStyle.onNormal.textColor = Color.white;
            m_toolbarStyle.onNormal.background = MakeTex(2, 2, new Color(0.20f, 0.52f, 0.88f, 1f));

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
            m_secondaryBtnStyle.normal.background = MakeTex(2, 2, new Color(0.18f, 0.48f, 0.82f, 1f));
            m_secondaryBtnStyle.hover.background = MakeTex(2, 2, new Color(0.24f, 0.56f, 0.92f, 1f));
            m_secondaryBtnStyle.active.background = MakeTex(2, 2, new Color(0.14f, 0.38f, 0.68f, 1f));
        }

        void OnGUI()
        {
            InitStyles();
            LobbySession session = LobbySession.Instance;

            float cardWidth = 440f;
            float cardHeight = 410f;
            Rect cardRect = new Rect(
                (Screen.width - cardWidth) * 0.5f,
                (Screen.height - cardHeight) * 0.5f,
                cardWidth,
                cardHeight
            );

            GUILayout.BeginArea(cardRect, m_cardStyle);

            GUILayout.Label("HUNTER vs CHASED", m_titleStyle);
            GUILayout.Label("MULTIPLAYER LOBBY", m_subtitleStyle);
            GUILayout.Space(22);

            GUILayout.Label("PLAYER NAME", m_labelStyle);
            GUILayout.Space(4);
            session.playerName = GUILayout.TextField(session.playerName, 20, m_inputStyle, GUILayout.Height(40));

            GUILayout.Space(16);

            GUILayout.Label("NETWORK PROTOCOL", m_labelStyle);
            GUILayout.Space(4);
            session.protocol = (LobbyProtocol)GUILayout.Toolbar(
                (int)session.protocol,
                new[] { "TCP", "UDP" },
                m_toolbarStyle,
                GUILayout.Height(36)
            );

            GUILayout.Space(24);

            bool hasName = session.playerName.Trim().Length > 0;
            GUI.enabled = hasName;

            if (GUILayout.Button("CREATE GAME", m_primaryBtnStyle, GUILayout.Height(44)))
                SceneManager.LoadScene(LobbySession.CreateScene);

            GUILayout.Space(10);

            if (GUILayout.Button("JOIN GAME", m_secondaryBtnStyle, GUILayout.Height(44)))
                SceneManager.LoadScene(LobbySession.JoinScene);

            GUI.enabled = true;

            if (!hasName)
            {
                GUILayout.Space(8);
                GUILayout.Label("Enter your name to continue", m_subtitleStyle);
            }

            LobbyGUI.Message(session.message);
            GUILayout.EndArea();
        }
    }
}