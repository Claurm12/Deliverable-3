using UnityEngine;
using UnityEngine.SceneManagement;

namespace Delivery3
{
    public class MenuUI : MonoBehaviour
    {
        private GUIStyle m_cardStyle;
        private GUIStyle m_titleStyle;
        private GUIStyle m_labelStyle;
        private GUIStyle m_inputStyle;
        private GUIStyle m_primaryBtnStyle;
        private GUIStyle m_secondaryBtnStyle;
        private GUIStyle m_toolbarStyle;
        private Texture2D m_shadowTex;

        void Start()
        {
            LobbySession.Instance.EndSession();   // coming back here always closes the old session
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

            m_shadowTex = new Texture2D(2, 2);
            Color sc = new Color(0.05f, 0.04f, 0.08f, 0.55f);
            m_shadowTex.SetPixels(new[] { sc, sc, sc, sc });
            m_shadowTex.Apply();

            m_cardStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeGradTex(64, 64,
                    new Color(0.22f, 0.23f, 0.28f, 0.95f),
                    new Color(0.14f, 0.15f, 0.19f, 0.97f), gold, 3) },
                padding = new RectOffset(35, 35, 28, 28)
            };

            m_titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = gold }
            };

            m_labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.96f, 0.92f) }
            };

            m_inputStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 12, 8, 8),
                normal = { textColor = Color.white, background = MakeGradTex(16, 16,
                    new Color(0.11f, 0.12f, 0.15f, 1f), new Color(0.15f, 0.16f, 0.20f, 1f), new Color(0.45f, 0.48f, 0.55f, 1f), 2) },
                focused = { textColor = gold, background = MakeGradTex(16, 16,
                    new Color(0.14f, 0.15f, 0.19f, 1f), new Color(0.19f, 0.20f, 0.25f, 1f), gold, 2) }
            };

            Texture2D cyanNormal = MakeGradTex(16, 32,
                new Color(0.20f, 0.62f, 0.82f, 1f), new Color(0.13f, 0.42f, 0.62f, 1f), new Color(0.60f, 0.88f, 0.98f, 0.9f), 2);
            Texture2D panelNormal = MakeGradTex(16, 32,
                new Color(0.26f, 0.28f, 0.34f, 1f), new Color(0.18f, 0.19f, 0.24f, 1f), new Color(0.45f, 0.50f, 0.58f, 0.8f), 2);

            m_toolbarStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = panelNormal },
                hover = { textColor = gold, background = cyanNormal },
                onNormal = { textColor = Color.white, background = cyanNormal },
                onHover = { textColor = Color.white, background = cyanNormal }
            };

            m_primaryBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = MakeGradTex(16, 32,
                    new Color(1.00f, 0.45f, 0.18f, 1f), new Color(0.86f, 0.22f, 0.18f, 1f), new Color(1.00f, 0.88f, 0.48f, 0.95f), 2) },
                hover = { textColor = Color.white, background = MakeGradTex(16, 32,
                    new Color(1.00f, 0.56f, 0.26f, 1f), new Color(0.96f, 0.30f, 0.24f, 1f), Color.white, 2) }
            };

            m_secondaryBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = MakeGradTex(16, 32,
                    new Color(0.44f, 0.86f, 0.24f, 1f), new Color(0.16f, 0.64f, 0.32f, 1f), new Color(0.82f, 1.00f, 0.65f, 0.95f), 2) },
                hover = { textColor = Color.white, background = MakeGradTex(16, 32,
                    new Color(0.54f, 0.94f, 0.32f, 1f), new Color(0.22f, 0.74f, 0.38f, 1f), Color.white, 2) }
            };
        }

        void OnGUI()
        {
            InitStyles();
            LobbySession session = LobbySession.Instance;

            float cardWidth = 440f;
            float cardHeight = 435f;
            Rect cardRect = new Rect((Screen.width - cardWidth) * 0.5f, (Screen.height - cardHeight) * 0.5f, cardWidth, cardHeight);

            GUI.DrawTexture(new Rect(cardRect.x + 8f, cardRect.y + 10f, cardRect.width, cardRect.height), m_shadowTex);
            GUILayout.BeginArea(cardRect, m_cardStyle);

            GUILayout.Label("GAME LOBBY  -  Lab 3", m_titleStyle);
            GUILayout.Space(22);

            GUILayout.Label("Your name", m_labelStyle);
            GUILayout.Space(4);
            session.playerName = GUILayout.TextField(session.playerName, m_inputStyle, GUILayout.Height(40));

            GUILayout.Space(16);

            GUILayout.Label("Protocol", m_labelStyle);
            GUILayout.Space(4);
            session.protocol = (LobbyProtocol)GUILayout.Toolbar(
                (int)session.protocol,
                new[] { "TCP", "UDP" },
                m_toolbarStyle,
                GUILayout.Height(36)
            );

            GUILayout.Space(24);

            GUI.enabled = session.playerName.Trim().Length > 0;

            if (GUILayout.Button("Create game", m_primaryBtnStyle, GUILayout.Height(44)))
                SceneManager.LoadScene(LobbySession.CreateScene);

            GUILayout.Space(10);

            if (GUILayout.Button("Join game", m_secondaryBtnStyle, GUILayout.Height(44)))
                SceneManager.LoadScene(LobbySession.JoinScene);

            GUI.enabled = true;

            LobbyGUI.Message(session.message);
            GUILayout.EndArea();
        }
    }
}