using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Delivery3
{
    public abstract class LobbyServer : MonoBehaviour
    {
        public string serverName = "MyRoom";
        public int port = 9050;
        public int maxPlayers = 4;

        public class Player
        {
            public object connection;
            public PlayerDTO2 data;
            public int pingMs = -1;
            public float lastSeen;
            public string name { get { return data.playerName; } }
        }

        protected struct NetEvent { public object from; public byte[] data; }
        protected readonly ConcurrentQueue<NetEvent> m_inbox = new ConcurrentQueue<NetEvent>();

        readonly List<Player> m_players = new List<Player>();

        public IList<Player> Players { get { return m_players.AsReadOnly(); } }
        public bool Started { get; private set; }
        public abstract bool IsRunning { get; }

        // Gestores para la replicación del mundo (Delivery 4)
        protected ReplicationManagerServer m_replicationServer;
        protected LinkingContext m_linkingContext;
        private float m_tickTimer;
        private const float ServerTickRate = 0.05f; // 20 ticks por segundo

        // ------------------------------------------------------------------ 

        public virtual bool StartServer()
        {
            m_linkingContext = gameObject.GetComponent<LinkingContext>();
            if (m_linkingContext == null)
            {
                m_linkingContext = gameObject.AddComponent<LinkingContext>();
            }

            m_replicationServer = gameObject.GetComponent<ReplicationManagerServer>();
            if (m_replicationServer == null)
            {
                m_replicationServer = gameObject.AddComponent<ReplicationManagerServer>();
            }
            m_replicationServer.Init(m_linkingContext);

            return true;
        }

        public abstract void StopServer();
        protected abstract void Send(object connection, byte[] message);
        protected abstract void Drop(object connection);
        protected virtual string Describe(object connection) { return connection.ToString(); }

        protected void SendText(object connection, string text) { Send(connection, Messages.Text(text)); }

        // -----------------------------------------------------------------------------
        protected virtual void Update()
        {
            NetEvent e;
            while (m_inbox.TryDequeue(out e))
            {
                if (e.data == null) { OnConnectionLost(e.from); continue; }

                string text;
                PlayerDTO2 dto;
                int objectId;
                Vector3 pos;
                Vector3 dir;
                MsgType type;

                try
                {
                    type = Messages.Read(e.data, out text, out dto, out objectId, out pos, out dir);
                }
                catch (Exception ex)
                {
                    Log("[SERVER] Mensaje erróneo descartado de " + Describe(e.from) + ": " + ex.Message);
                    continue;
                }

                Player player = Find(e.from);
                if (player != null) player.lastSeen = Time.realtimeSinceStartup;

                if (type == MsgType.Player) OnPlayerData(e.from, player, dto);
                else if (type == MsgType.Transform) OnTransform(e.from, player, objectId, pos);
                else if (type == MsgType.Shoot) OnShoot(e.from, player, objectId, pos, dir);
                else OnText(e.from, player, text);
            }

            if (Started)
            {
                m_tickTimer += Time.deltaTime;
                if (m_tickTimer >= ServerTickRate)
                {
                    m_tickTimer = 0f;
                    ServerTick();
                }
            }
        }

        protected virtual void ServerTick()
        {
            if (!Started || m_replicationServer == null) return;

            foreach (Player p in m_players)
            {
                int netId = p.name.GetHashCode();
                m_replicationServer.MarkUpdate(netId);
            }

            byte[] replicationPacket = m_replicationServer.WritePacket();
            if (replicationPacket != null && replicationPacket.Length > 0)
            {
                BroadcastCustomBytes(replicationPacket);
            }
        }

        void OnText(object from, Player player, string text)
        {
            int colon = text.IndexOf(':');
            string type = colon < 0 ? text : text.Substring(0, colon);
            string content = colon < 0 ? "" : text.Substring(colon + 1);

            switch (type)
            {
                case "CHAT":
                    if (player == null) break;
                    Log("[CHAT] " + player.name + ": " + content);
                    Broadcast("CHAT:" + player.name + ": " + content);
                    break;

                case "PING":
                    if (player == null) break;
                    string[] parts = content.Split(',');
                    SendText(from, "PONG:" + parts[0]);
                    int ping;
                    if (parts.Length > 1 && int.TryParse(parts[1], out ping)) player.pingMs = ping;
                    break;

                case "LEAVE":
                    if (player != null) RemovePlayer(player, "left");
                    Drop(from);
                    break;
            }
        }

        void OnPlayerData(object from, Player player, PlayerDTO2 dto)
        {
            string name = (dto.playerName ?? "").Replace(",", "").Replace(":", "").Trim();
            if (name.Length == 0) name = "Player";
            dto.playerName = name;

            if (player != null)
            {
                if (dto.playerName != player.name) { SendRoomStateTo(from); return; }
                player.data = dto;
                SendRoomState();
                return;
            }

            if (Started) { Refuse(from, "The game has already started"); return; }
            if (m_players.Count >= maxPlayers) { Refuse(from, "The room is full (" + maxPlayers + "/" + maxPlayers + ")"); return; }
            if (FindByName(name) != null) { Refuse(from, "The name " + name + " is already taken"); return; }

            m_players.Add(new Player { connection = from, data = dto, lastSeen = Time.realtimeSinceStartup });

            if (m_replicationServer != null)
            {
                m_replicationServer.MarkCreate(name.GetHashCode());
            }

            BroadcastSystem(name + " joined");
            SendRoomState();
        }

        void OnTransform(object from, Player player, int objectId, Vector3 pos)
        {
            if (player == null) return;
            byte[] transformPacket = Messages.Transform(objectId, pos);
            foreach (Player p in m_players)
            {
                Send(p.connection, transformPacket);
            }
        }

        void OnShoot(object from, Player player, int objectId, Vector3 pos, Vector3 dir)
        {
            if (player == null) return;
            byte[] shootPacket = Messages.Shoot(objectId, pos, dir);
            foreach (Player p in m_players)
            {
                Send(p.connection, shootPacket);
            }
        }

        void Refuse(object to, string reason)
        {
            Log("[SERVER] Refused " + Describe(to) + ": " + reason);
            SendText(to, "ERROR:" + reason);
            Drop(to);
        }

        void OnConnectionLost(object connection)
        {
            Player player = Find(connection);
            if (player != null) RemovePlayer(player, "disconnected");
        }

        protected void RemovePlayer(Player player, string reason)
        {
            if (!m_players.Remove(player)) return;

            if (m_replicationServer != null)
            {
                m_replicationServer.MarkDestroy(player.name.GetHashCode());
            }

            Log("[SERVER] " + player.name + " " + reason);
            BroadcastSystem(player.name + " " + reason);
            SendRoomState();
        }

        // -------------------------------------------------------------------------

        public void Kick(Player player)
        {
            object connection = player.connection;
            SendText(connection, "KICKED:");
            RemovePlayer(player, "was kicked");
            Drop(connection);
        }

        public void StartGame()
        {
            if (Started) return;

            if (m_players.Count > 0)
            {
                int hunterIndex = UnityEngine.Random.Range(0, m_players.Count);

                for (int i = 0; i < m_players.Count; i++)
                {
                    Player p = m_players[i];
                    int rolDesignado = (i == hunterIndex) ? 0 : 1;

                    switch (rolDesignado)
                    {
                        case 0:
                            p.data.playerRole = "Hunter";
                            SendText(p.connection, "Eres el CAZADOR");
                            break;
                        case 1:
                            p.data.playerRole = "Chased";
                            SendText(p.connection, "Escapa del cazador");
                            break;
                        default:
                            p.data.playerRole = "Chased";
                            SendText(p.connection, "Escapa del cazador");
                            break;
                    }
                }

                SendRoomState();
            }

            Started = true;
            Broadcast("START:");
        }

        // ------------------------------------------------------------------------------

        void SendRoomState()
        {
            Log("[SERVER] Players (" + m_players.Count + "/" + maxPlayers + "): " + Names(", "));
            foreach (Player p in m_players.ToArray()) SendRoomStateTo(p.connection);
        }

        void SendRoomStateTo(object connection)
        {
            foreach (Player p in m_players.ToArray()) Send(connection, Messages.Player(p.data));
            SendText(connection, "PLAYERS:" + Names(","));
            SendText(connection, "ROOM:" + serverName + "   " + m_players.Count + "/" + maxPlayers + " players");
        }

        void BroadcastSystem(string text) { Broadcast("CHAT:* " + text); }

        protected void Broadcast(string text)
        {
            byte[] message = Messages.Text(text);
            foreach (Player p in m_players.ToArray()) Send(p.connection, message);
        }

        public string Names(string separator)
        {
            List<string> names = new List<string>();
            foreach (Player p in m_players) names.Add(p.name);
            return string.Join(separator, names);
        }

        protected Player Find(object connection)
        {
            foreach (Player p in m_players) if (p.connection.Equals(connection)) return p;
            return null;
        }

        Player FindByName(string name)
        {
            foreach (Player p in m_players) if (p.name == name) return p;
            return null;
        }

        protected void ClearPlayers() { m_players.Clear(); Started = false; }

        protected void Log(string message) { LobbySession.Log(message); }

        public virtual void BroadcastCustomBytes(byte[] payload)
        {
            foreach (Player p in m_players)
            {
                Send(p.connection, payload);
            }
        }
    }
}