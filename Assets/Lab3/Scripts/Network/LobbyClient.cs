using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Delivery3
{
    public abstract class LobbyClient : MonoBehaviour
    {
        public string serverIp = "127.0.0.1";
        public int port = 9050;
        public PlayerDTO2 myData;
        public float pingSeconds = 1f;

        public string userName
        {
            get { return myData.playerName; }
        }

        public readonly List<string> Players = new List<string>();
        public readonly Dictionary<string, PlayerDTO2> Teams =
            new Dictionary<string, PlayerDTO2>();
        public readonly List<string> Chat = new List<string>();

        public string RoomInfo = "";
        public int PingMs = -1;
        public bool InRoom { get; private set; }

        public event Action Joined;
        public event Action GameStarted;
        public event Action<string> Closed;

        protected readonly ConcurrentQueue<byte[]> m_inbox =
            new ConcurrentQueue<byte[]>();

        protected volatile string m_endReason =
            "Connection to the server lost";

        protected float m_lastHeard;
        float m_lastPing;
        bool m_closed;

        protected ReplicationManagerClient m_replicationClient;
        protected LinkingContext m_linkingContext;

        [Header("Prefabs de Red (0: Hunter, 1..N: Chased)")]
        public GameObject[] networkPrefabs;

        public event Action<int, Vector3, float> TransformReceived;
        public event Action<int, Vector3, Vector3> ShootReceived;

        public abstract void Connect();
        protected abstract void SendBytes(byte[] message);
        protected abstract void CloseSocket();

        protected void SendText(string text)
        {
            SendBytes(Messages.Text(text));
        }

        protected byte[] JoinMessage()
        {
            return Messages.Player(myData);
        }

        public void SendChat(string line)
        {
            line = line.Trim();
            if (line.Length > 0)
                SendText("CHAT:" + line);
        }

        public void UpdateData(PlayerDTO2 data)
        {
            data.playerName = userName;
            myData = data;
            SendBytes(JoinMessage());
        }

        public void Leave()
        {
            SendText("LEAVE:");
            Close("You left the room");
        }

        protected virtual void Awake()
        {
            EnsureReplicationComponents();
        }

        protected void EnsureReplicationComponents()
        {
            if (m_linkingContext == null)
            {
                m_linkingContext = gameObject.GetComponent<LinkingContext>();
                if (m_linkingContext == null)
                    m_linkingContext = gameObject.AddComponent<LinkingContext>();
            }

            if (m_replicationClient == null)
            {
                m_replicationClient = gameObject.GetComponent<ReplicationManagerClient>();
                if (m_replicationClient == null)
                    m_replicationClient = gameObject.AddComponent<ReplicationManagerClient>();
            }

            GameObject[] prefabsToUse = GetEffectivePrefabs();
            m_replicationClient.Init(
                m_linkingContext,
                prefabsToUse != null ? new List<GameObject>(prefabsToUse) : null);
        }

        GameObject[] GetEffectivePrefabs()
        {
            if (networkPrefabs != null && networkPrefabs.Length > 0)
                return networkPrefabs;

            if (LobbySession.Instance != null &&
                LobbySession.Instance.networkPrefabs != null &&
                LobbySession.Instance.networkPrefabs.Length > 0)
            {
                return LobbySession.Instance.networkPrefabs;
            }

            return null;
        }

        protected virtual void Update()
        {
            byte[] data;

            while (!m_closed && m_inbox.TryDequeue(out data))
            {
                if (data == null)
                {
                    Close(m_endReason);
                    break;
                }

                m_lastHeard = Time.realtimeSinceStartup;

                if (data.Length > 0 && (MsgType)data[0] == MsgType.Replication)
                {
                    if (m_replicationClient != null)
                    {
                        using (BinaryReader reader = new BinaryReader(
                            new MemoryStream(data, 1, data.Length - 1)))
                        {
                            m_replicationClient.ReadPacket(reader);
                        }
                    }

                    continue;
                }

                string text;
                PlayerDTO2 dto;
                int objectId;
                Vector3 pos;
                float rotY;
                Vector3 dir;
                MsgType type;

                try
                {
                    type = Messages.Read(
                        data,
                        out text,
                        out dto,
                        out objectId,
                        out pos,
                        out rotY,
                        out dir);
                }
                catch (Exception e)
                {
                    Log("[CLIENT " + userName + "] Paquete corrupto descartado: " + e.Message);
                    continue;
                }

                if (type == MsgType.Player)
                {
                    OnPlayerData(dto);
                }
                else if (type == MsgType.Transform)
                {
                    if (TransformReceived != null)
                        TransformReceived(objectId, pos, rotY);
                }
                else if (type == MsgType.Shoot)
                {
                    if (ShootReceived != null)
                        ShootReceived(objectId, pos, dir);
                }
                else
                {
                    OnText(text);
                }
            }

            if (!m_closed &&
                InRoom &&
                Time.realtimeSinceStartup - m_lastPing >= pingSeconds)
            {
                m_lastPing = Time.realtimeSinceStartup;
                SendText("PING:" + Environment.TickCount + "," + PingMs);
            }
        }

        void OnPlayerData(PlayerDTO2 dto)
        {
            PlayerDTO2 old;
            bool changed = !Teams.TryGetValue(dto.playerName, out old);
            Teams[dto.playerName] = dto;

            if (changed)
                Log("[CLIENT " + userName + "] Datos de " + dto.playerName);
        }

        void OnText(string text)
        {
            int colon = text.IndexOf(':');
            string type = colon < 0 ? text : text.Substring(0, colon);
            string content = colon < 0 ? "" : text.Substring(colon + 1);

            switch (type)
            {
                case "PLAYERS":
                    Players.Clear();
                    if (content.Length > 0)
                        Players.AddRange(content.Split(','));

                    foreach (string name in new List<string>(Teams.Keys))
                    {
                        if (!Players.Contains(name))
                            Teams.Remove(name);
                    }

                    Log("[CLIENT " + userName + "] Jugadores (" + Players.Count + "): " + string.Join(", ", Players));

                    if (!InRoom)
                    {
                        InRoom = true;
                        if (Joined != null) Joined();
                    }
                    break;

                case "ROOM":
                    RoomInfo = content;
                    break;

                case "CHAT":
                    Chat.Add(content);
                    if (Chat.Count > 50)
                        Chat.RemoveAt(0);
                    Log("[CLIENT " + userName + "] Chat: " + content);
                    break;

                case "PONG":
                    int sent;
                    if (int.TryParse(content, out sent))
                        PingMs = Environment.TickCount - sent;
                    break;

                case "START":
                    Log("[CLIENT " + userName + "] El host ha iniciado el juego");
                    if (GameStarted != null)
                        GameStarted();
                    break;

                case "KICKED":
                    Close("El host te ha expulsado de la sala");
                    break;

                case "ERROR":
                    Close(content);
                    break;
            }
        }

        protected void Close(string reason)
        {
            if (m_closed) return;

            m_closed = true;
            InRoom = false;
            CloseSocket();

            Log("[CLIENT " + userName + "] Cerrado: " + reason);

            if (Closed != null)
                Closed(reason);
        }

        void OnDestroy()
        {
            if (!m_closed)
            {
                m_closed = true;
                CloseSocket();
            }
        }

        protected void Log(string message)
        {
            LobbySession.Log(message);
        }

        public virtual void SendCustomBytes(byte[] payload)
        {
            SendBytes(payload);
        }

        public void InitReplication()
        {
            EnsureReplicationComponents();
        }
    }
}