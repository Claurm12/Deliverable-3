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
        public GameObject[] networkPrefabs;
 

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

        protected ReplicationManagerServer m_replicationServer;
        protected LinkingContext m_linkingContext;
        private float m_tickTimer;
        private const float ServerTickRate = 0.05f;

        

        public virtual bool StartServer()
        {
            m_linkingContext = gameObject.GetComponent<LinkingContext>();
            if (m_linkingContext == null)
                m_linkingContext = gameObject.AddComponent<LinkingContext>();

            m_replicationServer = new ReplicationManagerServer(m_linkingContext);
            return true;
        }

        public abstract void StopServer();
        protected abstract void Send(object connection, byte[] message);
        protected abstract void Drop(object connection);
        protected virtual string Describe(object connection) { return connection.ToString(); }

        protected void SendText(object connection, string text) { Send(connection, Messages.Text(text)); }

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
                if (m_linkingContext.Find(netId) != null)
                    m_replicationServer.MarkUpdate(netId);
            }

            byte[] replicationPacket = m_replicationServer.WritePacket();
            Debug.Log("[SERVER TICK] jugadores " + m_players.Count + " paquete " + (replicationPacket == null ? 0 : replicationPacket.Length) + " bytes");
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

            int netId = name.GetHashCode();
            EnsureServerObject(netId);

            if (m_replicationServer != null)
            {
                m_replicationServer.MarkCreate(netId);
            }

            BroadcastSystem(name + " joined");
            SendRoomState();
        }

        GameObject EnsureServerObject(int id)
        {
            GameObject go = m_linkingContext.Find(id);
            if (go != null) return go;
            if (networkPrefabs == null || networkPrefabs.Length == 0 || networkPrefabs[0] == null) return null;

            go = Instantiate(networkPrefabs[0]);
            go.name = "ServerPlayer_" + id;

            NetObject n = go.GetComponent<NetObject>();
            if (n == null) n = go.AddComponent<NetPlayer>();
            n.id = id;
            n.type = 0;

            m_linkingContext.Register(go, id);
            return go;
        }
        void OnTransform(object from, Player player, int objectId, Vector3 pos)
        {
            if (player == null) return;
            GameObject go = EnsureServerObject(objectId);
            if (go != null) go.transform.position = pos;
            Debug.Log("[SERVER] Transform id " + objectId + " pos " + pos + " prefabs: " + (networkPrefabs == null ? "null" : networkPrefabs.Length.ToString()));
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

        public void Kick(Player player)
        {
            object connection = player.connection;
            SendText(connection, "KICKED;");
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

                    p.data.playerRole = (rolDesignado == 0) ? "Hunter" : "Chased";

                    int netId = p.name.GetHashCode();
                    GameObject go = m_linkingContext.Find(netId);
                    if (go != null)
                    {
                        NetObject netObj = go.GetComponent<NetObject>();
                        if (netObj != null)
                        {
                            netObj.type = (byte)rolDesignado;
                        }

                        NetPlayer netPlayerComp = go.GetComponent<NetPlayer>();
                        if (netPlayerComp != null)
                        {
                            netPlayerComp.playerRole = p.data.playerRole;
                            netPlayerComp.type = (byte)rolDesignado;
                        }

                        if (m_replicationServer != null)
                        {
                            m_replicationServer.MarkCreate(netId);
                        }
                    }

                    if (rolDesignado == 0)
                        SendText(p.connection, "Eres el CAZADOR");
                    else
                        SendText(p.connection, "Escapa del cazador");
                }

                SendRoomState();
            }

            Started = true;
            Broadcast("START:");
        }

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

        String Names(string separator)
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