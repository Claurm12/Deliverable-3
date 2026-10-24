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
            public object connection;   // a Socket in TCP, an EndPoint in UDP
            public PlayerDTO2 data;     // what the client sent when joining
            public int pingMs = -1;     // reported by the client in every PING
            public float lastSeen;      // last time anything arrived from it (UDP timeout)
            public string name { get { return data.playerName; } }
        }

        // Network thread to main thread. data == null means "this connection is gone" (TCP only).
        protected struct NetEvent { public object from; public byte[] data; }
        protected readonly ConcurrentQueue<NetEvent> m_inbox = new ConcurrentQueue<NetEvent>();

        readonly List<Player> m_players = new List<Player>();   // main thread only

        public IList<Player> Players { get { return m_players.AsReadOnly(); } }
        public bool Started { get; private set; }
        public abstract bool IsRunning { get; }

        // ------------------------------------------------------------------ what TCP and UDP provide

        public abstract bool StartServer();                         // false if the port is taken
        public abstract void StopServer();
        protected abstract void Send(object connection, byte[] message);
        protected abstract void Drop(object connection);           // stop talking to it
        protected virtual string Describe(object connection) { return connection.ToString(); }

        protected void SendText(object connection, string text) { Send(connection, Messages.Text(text)); }

        // ----------------------------------------------------------------------------- main thread
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
                    Log("[SERVER] Dropped a bad message from " + Describe(e.from) + ": " + ex.Message);
                    continue;
                }

                Player player = Find(e.from);
                if (player != null) player.lastSeen = Time.realtimeSinceStartup;

                if (type == MsgType.Player) OnPlayerData(e.from, player, dto);
                else if (type == MsgType.Transform) OnTransform(e.from, player, objectId, pos);
                else if (type == MsgType.Shoot) OnShoot(e.from, player, objectId, pos, dir);
                else OnText(e.from, player, text);
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
                    if (player == null) break;                 // only players in the room may talk
                    Log("[CHAT] " + player.name + ": " + content);
                    Broadcast("CHAT:" + player.name + ": " + content);
                    break;

                case "PING":
                    if (player == null) break;
                    // content = "<clientMs>,<lastPingMs>". Echo the first part, keep the second.
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

        // The PlayerDTO2 is the join request. From somebody already in the room it is a repeat
        // (UDP resends until answered) or an update of its data.
        void OnPlayerData(object from, Player player, PlayerDTO2 dto)
        {
            // PLAYERS: is still a comma-separated text list, so names keep the 2.2 restrictions.
            string name = (dto.playerName ?? "").Replace(",", "").Replace(":", "").Trim();
            if (name.Length == 0) name = "Player";
            dto.playerName = name;
            if (dto.ownedPokemons == null) dto.ownedPokemons = new List<Pokemon>();

            if (player != null)
            {
                if (dto.playerName != player.name) { SendRoomStateTo(from); return; }   // no renames in the room
                if (Messages.Describe(dto) == Messages.Describe(player.data)) { SendRoomStateTo(from); return; }
                player.data = dto;
                Log("[SERVER] " + name + " updated: " + Messages.Describe(dto));
                SendRoomState();
                return;
            }

            if (Started) { Refuse(from, "The game has already started"); return; }
            if (m_players.Count >= maxPlayers) { Refuse(from, "The room is full (" + maxPlayers + "/" + maxPlayers + ")"); return; }
            if (FindByName(name) != null) { Refuse(from, "The name " + name + " is already taken"); return; }

            m_players.Add(new Player { connection = from, data = dto, lastSeen = Time.realtimeSinceStartup });
            Log("[SERVER] " + name + " joined (" + Describe(from) + "): " + Messages.Describe(dto));
            BroadcastSystem(name + " joined");
            SendRoomState();
        }

        void OnTransform(object from, Player player, int objectId, Vector3 pos)
        {
            if (player == null) return; // Solo para las jugadores que esten en sala

            byte[] transformPacket = Messages.Transform(objectId, pos);

            // Renvia la posicion de todos los jugadores
            foreach (Player p in m_players)
            {
                Send(p.connection, transformPacket);
            }
        }

        void OnShoot(object from, Player player, int objectId, Vector3 pos, Vector3 dir)
        {
            if (player == null) return;

            byte[] shootPacket = Messages.Shoot(objectId, pos, dir);

            // Reenvia el disparo para tdos los jugadores
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

        // TCP: the client thread saw Receive return 0 or fail.
        void OnConnectionLost(object connection)
        {
            Player player = Find(connection);
            if (player != null) RemovePlayer(player, "disconnected");
        }

        protected void RemovePlayer(Player player, string reason)
        {
            if (!m_players.Remove(player)) return;
            Log("[SERVER] " + player.name + " " + reason);
            BroadcastSystem(player.name + " " + reason);
            SendRoomState();
        }

        // ------------------------------------------------------------------------- host buttons

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

                    if (p.data.ownedPokemons == null) p.data.ownedPokemons = new List<Pokemon>();
                    else p.data.ownedPokemons.Clear();

                    int rolDesignado = (i == hunterIndex) ? 0 : 1;

                    switch (rolDesignado)
                    {
                        case 0:
                            p.data.ownedPokemons.Add(new Pokemon { name = "Hunter" });
                            SendText(p.connection, "Eres el CAZADOR");
                            break;

                        case 1:
                            p.data.ownedPokemons.Add(new Pokemon { name = "Chased" });
                            SendText(p.connection, "Escapa del cazador");
                            break;

                        default:
                            p.data.ownedPokemons.Add(new Pokemon { name = "Chased" });
                            SendText(p.connection, "Eres el CAZADOR");
                            break;
                    }
                }

                SendRoomState();
            }

            Started = true;
            Log("[SERVER] Game started with " + Names(", "));
            Broadcast("START:");
        }

        // ------------------------------------------------------------------------------ helpers

        // The server owns the list: on every change everybody gets all of it again. First every
        // player's data, then PLAYERS, so a client never shows a name it has no data for.
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

        // ToArray: Send can fail and remove a player while we are still looping.
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

        // Equals, not ==: two IPEndPoint objects with the same address and port are the same client.
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
    }
}