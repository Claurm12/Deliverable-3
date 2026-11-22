using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Delivery3
{
    public class TaskMatch : NetObject
    {
        public const byte TypeId = 201;

        const float MatchSeconds = 420f;
        const float CompleteDisplaySeconds = 1f;
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

        void OnGUI()
        {
            if (!m_ready)
                return;

            int completed = 0;

            foreach (TaskData task in m_tasks)
            {
                if (task.complete)
                    completed++;
            }

            int seconds = Mathf.CeilToInt(Mathf.Max(0f, m_remaining));

            string text =
                "Tasks: " + completed + "/" + m_tasks.Count +
                "\nTime: " + (seconds / 60).ToString("00") +
                ":" + (seconds % 60).ToString("00");

            if (!string.IsNullOrEmpty(m_result))
                text += "\n" + m_result;

            GUI.Box(new Rect(Screen.width - 290, 15, 270, 80), text);
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