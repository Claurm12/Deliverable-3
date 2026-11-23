using UnityEngine;
using UnityEngine.SceneManagement;

namespace Delivery3
{
    public class JoinGameUI : MonoBehaviour
    {
        private string m_ip = "127.0.0.1";
        private string m_port = "9050";

        private GUIStyle m_cardStyle;
        private GUIStyle m_titleStyle;
        private GUIStyle m_subtitleStyle;
        private GUIStyle m_labelStyle;
        private GUIStyle m_inputStyle;
        private GUIStyle m_primaryBtnStyle;
        private GUIStyle m_secondaryBtnStyle;
        private GUIStyle m_smallBtnStyle;

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

            m_inputStyle = new GUIStyle(GUI.skin.textField);
            m_inputStyle.fontSize = 18;
            m_inputStyle.alignment = TextAnchor.MiddleLeft;
            m_inputStyle.padding = new RectOffset(12, 12, 8, 8);
            m_inputStyle.normal.textColor = Color.white;
            m_inputStyle.normal.background = MakeTex(2, 2, new Color(0.16f, 0.19f, 0.25f, 1f));
            m_inputStyle.focused.background = MakeTex(2, 2, new Color(0.20f, 0.25f, 0.34f, 1f));

            m_primaryBtnStyle = new GUIStyle(GUI.skin.button);
            m_primaryBtnStyle.fontSize = 18;
            m_primaryBtnStyle.fontStyle = FontStyle.Bold;
            m_primaryBtnStyle.normal.textColor = Color.white;
            m_primaryBtnStyle.normal.background = MakeTex(2, 2, new Color(0.18f, 0.48f, 0.82f, 1f));
            m_primaryBtnStyle.hover.background = MakeTex(2, 2, new Color(0.24f, 0.56f, 0.92f, 1f));
            m_primaryBtnStyle.active.background = MakeTex(2, 2, new Color(0.14f, 0.38f, 0.68f, 1f));

            m_secondaryBtnStyle = new GUIStyle(GUI.skin.button);
            m_secondaryBtnStyle.fontSize = 18;
            m_secondaryBtnStyle.fontStyle = FontStyle.Bold;
            m_secondaryBtnStyle.normal.textColor = Color.white;
            m_secondaryBtnStyle.normal.background = MakeTex(2, 2, new Color(0.25f, 0.30f, 0.38f, 1f));
            m_secondaryBtnStyle.hover.background = MakeTex(2, 2, new Color(0.32f, 0.38f, 0.48f, 1f));
            m_secondaryBtnStyle.active.background = MakeTex(2, 2, new Color(0.18f, 0.22f, 0.28f, 1f));

            m_smallBtnStyle = new GUIStyle(GUI.skin.button);
            m_smallBtnStyle.fontSize = 13;
            m_smallBtnStyle.fontStyle = FontStyle.Bold;
            m_smallBtnStyle.normal.textColor = Color.white;
            m_smallBtnStyle.normal.background = MakeTex(2, 2, new Color(0.22f, 0.27f, 0.36f, 1f));
            m_smallBtnStyle.hover.background = MakeTex(2, 2, new Color(0.28f, 0.35f, 0.46f, 1f));
        }

        void OnGUI()
        {
            InitStyles();
            LobbySession session = LobbySession.Instance;
            bool joining = session.Client != null;

            float cardWidth = 440f;
            float cardHeight = 400f;
            Rect cardRect = new Rect(
                (Screen.width - cardWidth) * 0.5f,
                (Screen.height - cardHeight) * 0.5f,
                cardWidth,
                cardHeight
            );

            GUILayout.BeginArea(cardRect, m_cardStyle);

            GUILayout.Label("JOIN GAME", m_titleStyle);
            GUILayout.Label("PROTOCOL: " + session.protocol, m_subtitleStyle);
            GUILayout.Space(22);

            GUI.enabled = !joining;


            GUILayout.Label("SERVER IP", m_labelStyle);
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            m_ip = GUILayout.TextField(m_ip, 45, m_inputStyle, GUILayout.Height(40));
            GUILayout.Space(8);
            if (GUILayout.Button("PASTE", m_smallBtnStyle, GUILayout.Width(72), GUILayout.Height(40)))
            {
                string clip = GUIUtility.systemCopyBuffer;
                if (!string.IsNullOrEmpty(clip)) m_ip = clip.Trim();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(16);


            GUILayout.Label("PORT", m_labelStyle);
            GUILayout.Space(4);
            m_port = GUILayout.TextField(m_port, 6, m_inputStyle, GUILayout.Height(40));

            GUILayout.Space(26);


            int port;
            bool canJoin = !joining && int.TryParse(m_port, out port) && m_ip.Trim().Length > 0;

            GUILayout.BeginHorizontal();
            GUI.enabled = true;
            if (GUILayout.Button(joining ? "CANCEL" : "BACK", m_secondaryBtnStyle, GUILayout.Width(140), GUILayout.Height(44)))
            {
                session.EndSession();
                SceneManager.LoadScene(LobbySession.MenuScene);
            }

            GUILayout.Space(12);

            GUI.enabled = canJoin;
            if (GUILayout.Button(joining ? "JOINING..." : "JOIN", m_primaryBtnStyle, GUILayout.Height(44)))
            {
                session.Join(m_ip.Trim(), int.Parse(m_port));
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            LobbyGUI.Message(session.message);
            GUILayout.EndArea();
        }
    }
}