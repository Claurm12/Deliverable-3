using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Delivery3
{
    public class TaskMatch : NetObject
    {
        public const byte TypeId = 201;

        const float MatchSeconds = 420f;
        const float CompleteDisplaySeconds = 1f;
        const float ResultDisplaySeconds = 4f;
        const int Segments = 48;

        class TaskData
        {
            public Vector3 position;
            public float radius;
            public float required;
            public float progress;
            public bool complete;
            public float completedAt;
            public LineRenderer circle;
        }

        readonly List<TaskData> m_tasks = new List<TaskData>();

        LobbyServer m_server;
        LinkingContext m_linking;
        Shader m_shader;
        bool m_shaderMissing;

        float m_remaining;
        string m_result = "";
        bool m_ready;

        float m_resultShownAt = -1f;
        bool m_returningToLobby;

        private GUIStyle m_boxStyle;
        private GUIStyle m_textStyle;
        private GUIStyle m_winCardStyle;
        private GUIStyle m_loseCardStyle;
        private GUIStyle m_endTitleStyle;
        private GUIStyle m_endSubStyle;
        private Texture2D m_shadowTex;

        void Start()
        {
            if (LobbyGUI.NeedsSession())
                return;

            LobbySession session = LobbySession.Instance;

            if (session.IsHost)
                session.Server.SetupTasks(this);
        }

        public void Setup(LobbyServer server, LinkingContext linking)
        {
            m_server = server;
            m_linking = linking;
            m_remaining = MatchSeconds;

            int chased = 0;

            foreach (LobbyServer.Player player in server.Players)
            {
                if (player.data.playerRole == "Chased")
                    chased++;
            }

            int needed = 3 + 2 * (chased - 1);
            TaskPoint[] points = FindObjectsOfType<TaskPoint>();

            if (chased < 1 || points.Length < needed)
            {
                m_ready = true;

                string problem =
                    "[TASKS] Need " + needed +
                    " task points, found " + points.Length + ".";

                Debug.LogError(problem);

                server.EndMatch(
                    "Not enough task points. Match cancelled.");

                m_result = server.MatchResult;
                return;
            }

            for (int i = points.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                TaskPoint temp = points[i];
                points[i] = points[j];
                points[j] = temp;
            }

            for (int i = 0; i < needed; i++)
            {
                float duration = Random.Range(8, 16);

                if (chased % 2 == 1 && i == 0)
                    duration = 8f;

                if (chased % 2 == 1 && i == 1)
                    duration = 15f;

                m_tasks.Add(new TaskData
                {
                    position = points[i].transform.position,
                    radius = Mathf.Max(0.5f, points[i].radius),
                    required = duration
                });
            }

            m_ready = true;

            Debug.Log(
                "[TASKS] Picked " + needed +
                " spots for " + chased + " chased.");
        }

        void Update()
        {
            UpdateCircles();

            if (!string.IsNullOrEmpty(m_result) && m_resultShownAt < 0f)
                m_resultShownAt = Time.time;

            if (!m_returningToLobby && m_resultShownAt >= 0f && Time.time - m_resultShownAt >= ResultDisplaySeconds)
            {
                m_returningToLobby = true;
                SceneManager.LoadScene(LobbySession.RoomScene);
            }
        }

        public void ServerUpdate()
        {
            if (m_server == null || !m_ready)
                return;

            if (m_server.MatchEnded)
                m_result = m_server.MatchResult;
            else
                UpdateServer();
        }

        void UpdateServer()
        {
            if (m_remaining <= Time.deltaTime)
            {
                m_remaining = 0f;
                m_server.EndMatch("Time's up. Hunter wins!");
                m_result = m_server.MatchResult;
                return;
            }

            m_remaining -= Time.deltaTime;

            bool[] occupied = new bool[m_tasks.Count];

            foreach (LobbyServer.Player player in m_server.Players)
            {
                if (player.data.playerRole != "Chased")
                    continue;

                GameObject go = m_linking.Find(
                    NetObject.ComputeId(player.name));

                NetPlayer netPlayer =
                    go != null ? go.GetComponent<NetPlayer>() : null;

                if (netPlayer == null || netPlayer.isDead)
                    continue;

                int nearest = -1;
                float nearestDistance = float.PositiveInfinity;

                for (int i = 0; i < m_tasks.Count; i++)
                {
                    TaskData task = m_tasks[i];

                    if (task.complete)
                        continue;

                    Vector3 delta = go.transform.position - task.position;

                    if (Mathf.Abs(delta.y) > 1.2f)
                        continue;

                    float distance =
                        delta.x * delta.x + delta.z * delta.z;

                    if (distance <= task.radius * task.radius &&
                        distance < nearestDistance)
                    {
                        nearest = i;
                        nearestDistance = distance;
                    }
                }

                if (nearest >= 0)
                    occupied[nearest] = true;
            }

            int completed = 0;

            for (int i = 0; i < m_tasks.Count; i++)
            {
                TaskData task = m_tasks[i];

                if (!task.complete && occupied[i])
                {
                    task.progress = Mathf.Min(
                        task.required,
                        task.progress + Time.deltaTime);

                    if (task.progress >= task.required)
                    {
                        task.complete = true;
                        task.completedAt = Time.time;
                    }
                }

                if (task.complete)
                    completed++;
            }

            if (m_tasks.Count > 0 && completed == m_tasks.Count)
            {
                m_server.EndMatch("All tasks done. Chased win!");
                m_result = m_server.MatchResult;
            }
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(m_ready);
            writer.Write(m_remaining);
            writer.Write(m_result);
            writer.Write(m_tasks.Count);

            foreach (TaskData task in m_tasks)
            {
                writer.Write(task.position.x);
                writer.Write(task.position.y);
                writer.Write(task.position.z);
                writer.Write(task.radius);
                writer.Write(task.required);
                writer.Write(task.progress);
                writer.Write(task.complete);
            }
        }

        public override void Read(BinaryReader reader)
        {
            m_ready = reader.ReadBoolean();
            m_remaining = reader.ReadSingle();
            m_result = reader.ReadString();

            int count = reader.ReadInt32();

            if (count < 0 || count > 31)
                throw new InvalidDataException("Bad task count: " + count);

            while (m_tasks.Count < count)
                m_tasks.Add(new TaskData());

            while (m_tasks.Count > count)
            {
                int last = m_tasks.Count - 1;

                if (m_tasks[last].circle != null)
                {
                    Destroy(m_tasks[last].circle.sharedMaterial);
                    Destroy(m_tasks[last].circle.gameObject);
                }

                m_tasks.RemoveAt(last);
            }

            for (int i = 0; i < count; i++)
            {
                TaskData task = m_tasks[i];

                task.position = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle());

                task.radius = reader.ReadSingle();
                task.required = reader.ReadSingle();
                task.progress = reader.ReadSingle();

                bool complete = reader.ReadBoolean();

                if (complete && !task.complete)
                    task.completedAt = Time.time;

                task.complete = complete;
            }
        }

        void UpdateCircles()
        {
            LobbyClient client = LobbySession.Instance.Client;
            PlayerDTO2 player;

            bool show =
                client != null &&
                client.Teams.TryGetValue(client.userName, out player) &&
                player.playerRole == "Chased";

            foreach (TaskData task in m_tasks)
            {
                bool visible =
                    show &&
                    m_ready &&
                    (!task.complete ||
                     Time.time - task.completedAt < CompleteDisplaySeconds);

                if (!visible)
                {
                    if (task.circle != null)
                        task.circle.gameObject.SetActive(false);

                    continue;
                }

                if (task.circle == null)
                    task.circle = CreateCircle();

                if (task.circle == null)
                    continue;

                task.circle.gameObject.SetActive(true);

                task.circle.transform.position =
                    task.position + Vector3.up * 0.06f;

                for (int i = 0; i < Segments; i++)
                {
                    float angle = 2f * Mathf.PI * i / Segments;

                    Vector3 point = new Vector3(
                        Mathf.Cos(angle),
                        0f,
                        Mathf.Sin(angle));

                    task.circle.SetPosition(i, point * task.radius);
                }

                Color color = task.complete
                    ? Color.green
                    : Color.Lerp(
                        Color.red,
                        Color.green,
                        Mathf.Clamp01(task.progress / task.required));

                Material material = task.circle.sharedMaterial;

                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);

                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", color);
            }
        }

        LineRenderer CreateCircle()
        {
            if (m_shader == null && !m_shaderMissing)
            {
                m_shader = Shader.Find("Universal Render Pipeline/Unlit");

                if (m_shader == null)
                    m_shader = Shader.Find("Sprites/Default");

                if (m_shader == null)
                {
                    Debug.LogError(
                        "[TASKS] Can't find a shader for the circles!");

                    m_shaderMissing = true;
                }
            }

            if (m_shader == null)
                return null;

            GameObject go = new GameObject("Task circle");
            go.transform.SetParent(transform, false);

            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = Segments;
            line.startWidth = 0.15f;
            line.endWidth = 0.15f;
            line.sharedMaterial = new Material(m_shader);

            return line;
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
            if (m_boxStyle != null) return;

            Color gold = new Color(1.00f, 0.82f, 0.20f, 1f);
            Color winBorder = new Color(0.44f, 0.86f, 0.24f, 1f);
            Color loseBorder = new Color(1.00f, 0.45f, 0.18f, 1f);

            m_shadowTex = new Texture2D(2, 2);
            Color sc = new Color(0.05f, 0.04f, 0.08f, 0.55f);
            m_shadowTex.SetPixels(new[] { sc, sc, sc, sc });
            m_shadowTex.Apply();

            m_boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeGradTex(64, 64,
                    new Color(0.22f, 0.23f, 0.28f, 0.94f),
                    new Color(0.14f, 0.15f, 0.19f, 0.96f), gold, 3) },
                padding = new RectOffset(14, 14, 10, 10)
            };

            m_textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            m_winCardStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeGradTex(64, 64,
                    new Color(0.16f, 0.28f, 0.20f, 0.96f),
                    new Color(0.11f, 0.17f, 0.14f, 0.98f), winBorder, 3) },
                padding = new RectOffset(28, 28, 22, 22)
            };

            m_loseCardStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeGradTex(64, 64,
                    new Color(0.28f, 0.16f, 0.16f, 0.96f),
                    new Color(0.16f, 0.11f, 0.13f, 0.98f), loseBorder, 3) },
                padding = new RectOffset(28, 28, 22, 22)
            };

            m_endTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = { textColor = gold }
            };

            m_endSubStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = { textColor = new Color(0.95f, 0.96f, 0.92f) }
            };
        }

        void OnGUI()
        {
            if (!m_ready)
                return;

            InitStyles();

            int completed = 0;

            foreach (TaskData task in m_tasks)
            {
                if (task.complete)
                    completed++;
            }

            int seconds = Mathf.CeilToInt(Mathf.Max(0f, m_remaining));

            string text =
                "<color=#FFD166>Tasks:</color> <color=#70FF5C>" + completed + "/" + m_tasks.Count + "</color>" +
                "\n<color=#FFD166>Time:</color> " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");

            float boxW = 260f;
            float boxH = 74f;
            Rect boxRect = new Rect(Screen.width - boxW - 16f, 14f, boxW, boxH);

            GUI.DrawTexture(new Rect(boxRect.x + 6f, boxRect.y + 8f, boxRect.width, boxRect.height), m_shadowTex);
            GUI.Box(boxRect, "", m_boxStyle);
            GUI.Label(boxRect, text, m_textStyle);

            if (!string.IsNullOrEmpty(m_result))
            {
                string role = "Chased";
                LobbyClient client = LobbySession.Instance.Client;
                if (client != null && !string.IsNullOrEmpty(client.userName) &&
                    client.Teams.TryGetValue(client.userName, out PlayerDTO2 dto) &&
                    !string.IsNullOrEmpty(dto.playerRole))
                {
                    role = dto.playerRole;
                }

                bool hasWinner = m_result.Contains("Hunter") || m_result.Contains("Chased");
                bool won = (m_result.Contains("Hunter") && role == "Hunter") ||
                           (m_result.Contains("Chased") && role == "Chased");

                string headline = hasWinner
                    ? (won ? "<color=#70FF5C>YOU WIN</color>" : "<color=#FF6B35>YOU LOSE</color>")
                    : "<color=#FFD166>MATCH ENDED</color>";

                float leftSec = Mathf.Max(0f, ResultDisplaySeconds - (m_resultShownAt >= 0f ? Time.time - m_resultShownAt : 0f));

                float endW = 460f;
                float endH = 156f;
                Rect endRect = new Rect((Screen.width - endW) * 0.5f, (Screen.height - endH) * 0.5f, endW, endH);

                GUI.DrawTexture(new Rect(endRect.x + 8f, endRect.y + 10f, endRect.width, endRect.height), m_shadowTex);
                GUILayout.BeginArea(endRect, won || !hasWinner ? m_winCardStyle : m_loseCardStyle);
                GUILayout.Label(headline, m_endTitleStyle);
                GUILayout.Space(4);
                GUILayout.Label(m_result, m_endSubStyle);
                GUILayout.Space(6);
                GUILayout.Label("<color=#FFD166>Returning to lobby in " + Mathf.CeilToInt(leftSec) + "s...</color>", m_endSubStyle);
                GUILayout.EndArea();
            }
        }

        void OnDestroy()
        {
            foreach (TaskData task in m_tasks)
            {
                if (task.circle != null &&
                    task.circle.sharedMaterial != null)
                {
                    Destroy(task.circle.sharedMaterial);
                }
            }
        }
    }
}