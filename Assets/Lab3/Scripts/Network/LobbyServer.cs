using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Delivery3
{
    public abstract class LobbyServer : MonoBehaviour
    {
        public string serverName = "MyRoom";
        public int port = 9050;
        public int maxPlayers = 4;

        [Header("0 = Hunter | 1..N = Prefabs de Chased")]
        public GameObject[] networkPrefabs;

        public string LocalHostIP { get; private set; } = "127.0.0.1";

        public class Player
        {
            public object connection;
            public PlayerDTO2 data;
            public int pingMs = -1;
            public float lastSeen;
            public float nextShotTime;
            public int assignedPrefabType = 1;

            public string name
            {
                get { return data.playerName; }
            }
        }

        protected struct NetEvent
        {
            public object from;
            public byte[] data;
        }

        protected readonly ConcurrentQueue<NetEvent> m_inbox =
            new ConcurrentQueue<NetEvent>();

        readonly List<Player> m_players = new List<Player>();

        public IList<Player> Players
        {
            get { return m_players.AsReadOnly(); }
        }

        public bool Started { get; private set; }
        public bool MatchEnded { get; private set; }
        public abstract bool IsRunning { get; }

        protected ReplicationManagerServer m_replicationServer;
        protected LinkingContext m_linkingContext;

        NpcCrowd m_npcCrowd;
        private float m_tickTimer;
        private const float ServerTickRate = 0.05f;

        class ServerBullet
        {
            public int id;
            public string owner;
            public Vector3 velocity;
            public float dieAt;
        }

        readonly List<ServerBullet> m_bullets =
            new List<ServerBullet>();

        public static string GetLocalHostIP()
        {
            try
            {
                using (Socket socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Dgram,
                    ProtocolType.Udp))
                {
                    socket.Connect("8.8.8.8", 65530);

                    IPEndPoint endPoint =
                        socket.LocalEndPoint as IPEndPoint;

                    if (endPoint != null &&
                        !IPAddress.IsLoopback(endPoint.Address))
                    {
                        return endPoint.Address.ToString();
                    }
                }
            }
            catch { }

            try
            {
                string hostName = Dns.GetHostName();
                IPHostEntry hostEntry = Dns.GetHostEntry(hostName);

                foreach (IPAddress ip in hostEntry.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }

            return "127.0.0.1";
        }

        public virtual bool StartServer()
        {
            LocalHostIP = GetLocalHostIP();
            Log("[SERVER] IP Local: " + LocalHostIP + ":" + port);

            m_linkingContext = gameObject.GetComponent<LinkingContext>();

            if (m_linkingContext == null)
                m_linkingContext = gameObject.AddComponent<LinkingContext>();

            m_replicationServer =
                new ReplicationManagerServer(m_linkingContext);

            return true;
        }

        public abstract void StopServer();
        protected abstract void Send(object connection, byte[] message);
        protected abstract void Drop(object connection);

        protected virtual string Describe(object connection)
        {
            return connection.ToString();
        }

        protected void SendText(object connection, string text)
        {
            Send(connection, Messages.Text(text));
        }

        protected virtual void Update()
        {
            NetEvent e;

            while (m_inbox.TryDequeue(out e))
            {
                if (e.data == null)
                {
                    OnConnectionLost(e.from);
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
                        e.data,
                        out text,
                        out dto,
                        out objectId,
                        out pos,
                        out rotY,
                        out dir);
                }
                catch (Exception ex)
                {
                    Log(
                        "[SERVER] Mensaje erróneo descartado de " +
                        Describe(e.from) + ": " + ex.Message);
                    continue;
                }

                Player player = Find(e.from);

                if (player != null)
                    player.lastSeen = Time.realtimeSinceStartup;

                if (type == MsgType.Player)
                    OnPlayerData(e.from, player, dto);
                else if (type == MsgType.Transform)
                    OnTransform(e.from, player, objectId, pos, rotY);
                else if (type == MsgType.Shoot)
                    OnShoot(e.from, player, objectId, pos, dir);
                else
                    OnText(e.from, player, text);
            }

            if (Started)
            {
                if (!MatchEnded)
                    UpdateBullets();

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
            if (!Started || m_replicationServer == null)
                return;

            foreach (Player p in m_players)
            {
                int netId = NetObject.ComputeId(p.name);

                if (m_linkingContext.Find(netId) != null)
                    m_replicationServer.MarkUpdate(netId);
            }

            foreach (ServerBullet b in m_bullets)
                m_replicationServer.MarkUpdate(b.id);

            if (m_npcCrowd != null)
            {
                foreach (NetPlayer npc in m_npcCrowd.Players)
                    m_replicationServer.MarkUpdate(npc.id);
            }

            byte[] replicationPacket = m_replicationServer.WritePacket();

            if (replicationPacket != null && replicationPacket.Length > 0)
                BroadcastCustomBytes(replicationPacket);
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

                    if (parts.Length > 1 &&
                        int.TryParse(parts[1], out ping))
                    {
                        player.pingMs = ping;
                    }

                    break;

                case "LEAVE":
                    if (player != null)
                        RemovePlayer(player, "left");

                    Drop(from);
                    break;
            }
        }

        void OnPlayerData(object from, Player player, PlayerDTO2 dto)
        {
            string name = (dto.playerName ?? "")
                .Replace(",", "")
                .Replace(":", "")
                .Trim();

            if (name.Length == 0)
                name = "Player";

            dto.playerName = name;

            if (player != null)
            {
                if (dto.playerName != player.name)
                {
                    SendRoomStateTo(from);
                    return;
                }

                if (Started)
                    dto.playerRole = player.data.playerRole;

                player.data = dto;
                SendRoomState();
                return;
            }

            if (Started)
            {
                Refuse(from, "The game has already started");
                return;
            }

            if (m_players.Count >= maxPlayers)
            {
                Refuse(
                    from,
                    "The room is full (" +
                    maxPlayers + "/" + maxPlayers + ")");
                return;
            }

            if (FindByName(name) != null)
            {
                Refuse(from, "The name " + name + " is already taken");
                return;
            }

            Player newPlayer = new Player
            {
                connection = from,
                data = dto,
                lastSeen = Time.realtimeSinceStartup,
                assignedPrefabType = 0
            };

            m_players.Add(newPlayer);

            int netId = NetObject.ComputeId(name);
            EnsureServerObject(netId, 0);

            if (m_replicationServer != null)
                m_replicationServer.MarkCreate(netId);

            BroadcastSystem(name + " joined");
            SendRoomState();
        }

        int GetRandomChasedPrefabType()
        {
            if (networkPrefabs == null || networkPrefabs.Length <= 1)
                return 0;

            return UnityEngine.Random.Range(1, networkPrefabs.Length);
        }

        GameObject EnsureServerObject(int id, int prefabType)
        {
            if (networkPrefabs == null || networkPrefabs.Length == 0)
                return null;

            int validType =
                prefabType >= 0 &&
                prefabType < networkPrefabs.Length &&
                networkPrefabs[prefabType] != null
                    ? prefabType
                    : 0;

            if (networkPrefabs[validType] == null)
                return null;

            GameObject go = m_linkingContext.Find(id);
            Vector3 oldPos = Vector3.zero;
            Quaternion oldRot = Quaternion.identity;

            if (go != null)
            {
                NetObject existing = go.GetComponent<NetObject>();

                if (existing != null && existing.type == validType)
                    return go;

                oldPos = go.transform.position;
                oldRot = go.transform.rotation;

                Destroy(go);
                m_linkingContext.Unregister(id);
            }

            go = Instantiate(networkPrefabs[validType], oldPos, oldRot);
            go.name = "ServerPlayer_" + id + "_Type" + validType;

            NetObject n = go.GetComponent<NetObject>();

            if (n == null)
                n = go.AddComponent<NetPlayer>();

            n.id = id;
            n.type = (byte)validType;

            Rigidbody body = go.GetComponent<Rigidbody>();
            if (body != null) body.isKinematic = true;

            Animator animator = go.GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;

            m_linkingContext.Register(go, id);
            return go;
        }

        void OnTransform(
            object from,
            Player player,
            int objectId,
            Vector3 pos,
            float rotY)
        {
            if (player == null ||
                MatchEnded ||
                objectId != NetObject.ComputeId(player.name))
            {
                return;
            }

            int prefabType =
                player.data.playerRole == "Hunter"
                    ? 0
                    : player.assignedPrefabType;

            GameObject go = EnsureServerObject(objectId, prefabType);

            if (go != null)
            {
                NetPlayer np = go.GetComponent<NetPlayer>();

                if (np != null)
                {
                    if (np.isDead) return;

                    np.isMoving =
                        (go.transform.position - pos).sqrMagnitude > 0.0001f;

                    np.ApplyAnimationState();
                }

                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            }

            if (m_replicationServer != null)
                m_replicationServer.MarkUpdate(objectId);
        }

        public void SpawnNpcs(NpcCrowd crowd)
        {
            if (!Started || m_npcCrowd != null || crowd == null)
                return;

            m_npcCrowd = crowd;

            crowd.Spawn(
                this,
                m_linkingContext,
                networkPrefabs,
                m_replicationServer);
        }

        void OnShoot(
            object from,
            Player player,
            int objectId,
            Vector3 pos,
            Vector3 dir)
        {
            if (player == null || MatchEnded)
                return;

            TrySpawnBullet(player, objectId, pos, dir);
        }

        public void ShootFromHost(int objectId, Vector3 pos, Vector3 dir)
        {
            if (MatchEnded) return;

            foreach (Player p in m_players)
            {
                if (NetObject.ComputeId(p.name) == objectId)
                {
                    TrySpawnBullet(p, objectId, pos, dir);
                    return;
                }
            }
        }

        bool TrySpawnBullet(
            Player player,
            int objectId,
            Vector3 pos,
            Vector3 dir)
        {
            if (!Started || MatchEnded || m_replicationServer == null)
                return false;

            if (objectId != NetObject.ComputeId(player.name))
                return false;

            if (player.data.playerRole != "Hunter")
                return false;

            GameObject shooter = m_linkingContext.Find(objectId);

            NetPlayer shooterNetPlayer =
                shooter != null ? shooter.GetComponent<NetPlayer>() : null;

            if (shooterNetPlayer != null && shooterNetPlayer.isDead)
                return false;

            float now = Time.time;

            if (now < player.nextShotTime)
                return false;

            int alive = 0;

            foreach (ServerBullet b in m_bullets)
            {
                if (b.owner == player.name)
                    alive++;
            }

            if (alive >= NetBullet.MaxAlivePerPlayer)
                return false;

            dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f)
                return false;

            dir.Normalize();

            if (shooter != null)
                pos = shooter.transform.position + dir * 0.8f;

            player.nextShotTime = now + NetBullet.Cooldown * 0.8f;

            NetBullet bullet = NetBullet.CreateVisual(
                pos,
                Quaternion.LookRotation(dir).eulerAngles.y);

            int id = m_linkingContext.Register(bullet.gameObject);
            bullet.id = id;

            m_bullets.Add(new ServerBullet
            {
                id = id,
                owner = player.name,
                velocity = dir * NetBullet.Speed,
                dieAt = now + NetBullet.Lifetime
            });

            m_replicationServer.MarkCreate(id);

            if (shooterNetPlayer != null)
            {
                shooterNetPlayer.shotCount++;
                shooterNetPlayer.ApplyAnimationState();
                m_replicationServer.MarkUpdate(objectId);
            }

            return true;
        }

        public void OnHunterHitNpc(string hunterName)
        {
            if (!Started || MatchEnded)
                return;

            Player hunterPlayer = FindByName(hunterName);

            if (hunterPlayer == null ||
                hunterPlayer.data.playerRole != "Hunter")
            {
                return;
            }

            int hunterId = NetObject.ComputeId(hunterPlayer.name);
            GameObject go = m_linkingContext.Find(hunterId);

            if (go == null)
                return;

            NetPlayer np = go.GetComponent<NetPlayer>();

            if (np == null || np.isDead)
                return;

            np.health = Mathf.Max(0, np.health - 1);

            if (np.health <= 0)
                SetPlayerDead(hunterId, true);
            else if (m_replicationServer != null)
                m_replicationServer.MarkUpdate(hunterId);
        }

        public void SetPlayerDead(int netId, bool dead)
        {
            GameObject go = m_linkingContext.Find(netId);

            if (go == null)
                return;

            NetPlayer np = go.GetComponent<NetPlayer>();

            if (np != null && np.isDead != dead)
            {
                np.isDead = dead;

                if (dead)
                    np.isMoving = false;

                np.ApplyAnimationState();

                if (m_replicationServer != null)
                    m_replicationServer.MarkUpdate(netId);

                CheckWinLoseCondition();
            }
        }

        void CheckWinLoseCondition()
        {
            if (!Started || MatchEnded)
                return;

            int totalChased = 0;
            int aliveChased = 0;
            bool anyHunterAlive = false;
            bool anyHunterExists = false;

            foreach (Player p in m_players)
            {
                int netId = NetObject.ComputeId(p.name);
                GameObject go = m_linkingContext.Find(netId);

                NetPlayer np =
                    go != null ? go.GetComponent<NetPlayer>() : null;

                bool isDead = np != null && np.isDead;

                if (p.data.playerRole == "Hunter")
                {
                    anyHunterExists = true;

                    if (!isDead)
                        anyHunterAlive = true;
                }
                else if (p.data.playerRole == "Chased")
                {
                    totalChased++;

                    if (!isDead)
                        aliveChased++;
                }
            }

            if (anyHunterExists && !anyHunterAlive)
            {
                MatchEnded = true;
                Log("[GAME] El cazador se ha quedado sin vidas. Ganan los Chased.");
                Broadcast(
                    "CHAT:* Fin de partida: el cazador ha perdido. " +
                    "¡Ganan los que escapan!");
            }
            else if (totalChased > 0 && aliveChased == 0)
            {
                MatchEnded = true;
                Log("[GAME] Todos los Chased eliminados. Gana el Hunter.");
                Broadcast("CHAT:* Fin de partida: ¡gana el cazador!");
            }
        }

        void UpdateBullets()
        {
            float now = Time.time;
            float dt = Time.deltaTime;

            for (int i = m_bullets.Count - 1; i >= 0; i--)
            {
                ServerBullet b = m_bullets[i];

                if (now >= b.dieAt)
                {
                    DestroyBullet(b.id);
                    m_bullets.RemoveAt(i);
                    continue;
                }

                GameObject bulletGo = m_linkingContext.Find(b.id);

                if (bulletGo == null)
                {
                    m_bullets.RemoveAt(i);
                    continue;
                }

                Vector3 previousPos = bulletGo.transform.position;
                bulletGo.transform.position += b.velocity * dt;
                Vector3 bulletPos = bulletGo.transform.position;

                NetPlayer closestPlayer = null;
                bool hitNpc = false;
                float closestHit = float.PositiveInfinity;

                foreach (Player targetPlayer in m_players)
                {
                    if (targetPlayer.name == b.owner)
                        continue;

                    if (targetPlayer.data.playerRole != "Chased")
                        continue;

                    int targetId = NetObject.ComputeId(targetPlayer.name);
                    GameObject targetGo = m_linkingContext.Find(targetId);

                    if (targetGo == null)
                        continue;

                    NetPlayer targetNetPlayer =
                        targetGo.GetComponent<NetPlayer>();

                    if (targetNetPlayer == null || targetNetPlayer.isDead)
                        continue;

                    float hit;

                    if (TryBulletHit(
                            previousPos,
                            bulletPos,
                            targetGo.transform.position,
                            out hit) &&
                        hit < closestHit)
                    {
                        closestHit = hit;
                        closestPlayer = targetNetPlayer;
                        hitNpc = false;
                    }
                }

                if (m_npcCrowd != null)
                {
                    foreach (NetPlayer npc in m_npcCrowd.Players)
                    {
                        float hit;

                        if (TryBulletHit(
                                previousPos,
                                bulletPos,
                                npc.transform.position,
                                out hit) &&
                            hit < closestHit)
                        {
                            closestHit = hit;
                            closestPlayer = npc;
                            hitNpc = true;
                        }
                    }
                }

                if (closestPlayer != null)
                {
                    if (hitNpc)
                    {
                        OnHunterHitNpc(b.owner);
                    }
                    else
                    {
                        closestPlayer.health =
                            Mathf.Max(0, closestPlayer.health - 1);

                        if (closestPlayer.health <= 0)
                            SetPlayerDead(closestPlayer.id, true);
                        else if (m_replicationServer != null)
                            m_replicationServer.MarkUpdate(closestPlayer.id);
                    }

                    DestroyBullet(b.id);
                    m_bullets.RemoveAt(i);

                    if (MatchEnded)
                        break;
                }
            }
        }

        static bool TryBulletHit(
            Vector3 start,
            Vector3 end,
            Vector3 target,
            out float distance)
        {
            Vector2 a = new Vector2(start.x, start.z);

            Vector2 segment = new Vector2(
                end.x - start.x,
                end.z - start.z);

            Vector2 toTarget = new Vector2(target.x, target.z) - a;

            float lengthSq = segment.sqrMagnitude;

            float projection = lengthSq > 0f
                ? Vector2.Dot(toTarget, segment) / lengthSq
                : 0f;

            float closest = Mathf.Clamp01(projection);

            if ((toTarget - segment * closest).sqrMagnitude > 1f)
            {
                distance = 0f;
                return false;
            }

            float perpendicularSq =
                (toTarget - segment * projection).sqrMagnitude;

            float entry = lengthSq > 0f
                ? projection - Mathf.Sqrt(
                    Mathf.Max(0f, 1f - perpendicularSq) / lengthSq)
                : 0f;

            distance = Mathf.Clamp01(entry);
            return true;
        }

        void DestroyBullet(int id)
        {
            GameObject go = m_linkingContext.Find(id);

            if (go != null)
                Destroy(go);

            m_linkingContext.Unregister(id);

            if (m_replicationServer != null)
                m_replicationServer.MarkDestroy(id);
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

            if (player != null)
                RemovePlayer(player, "disconnected");
        }

        protected void RemovePlayer(Player player, string reason)
        {
            if (!m_players.Remove(player))
                return;

            int netId = NetObject.ComputeId(player.name);
            GameObject go = m_linkingContext.Find(netId);

            if (go != null)
            {
                Destroy(go);
                m_linkingContext.Unregister(netId);
            }

            if (m_replicationServer != null)
                m_replicationServer.MarkDestroy(netId);

            Log("[SERVER] " + player.name + " " + reason);
            BroadcastSystem(player.name + " " + reason);
            SendRoomState();

            if (Started && !MatchEnded)
                CheckWinLoseCondition();
        }

        public void Kick(Player player)
        {
            object connection = player.connection;
            SendText(connection, "KICKED:");
            RemovePlayer(player, "was kicked");
            Drop(connection);
        }

        public void StartGame()
        {
            if (Started)
                return;

            MatchEnded = false;

            if (m_players.Count > 0)
            {
                int hunterIndex =
                    UnityEngine.Random.Range(0, m_players.Count);

                for (int i = 0; i < m_players.Count; i++)
                {
                    Player p = m_players[i];
                    bool isHunter = i == hunterIndex;
                    p.data.playerRole = isHunter ? "Hunter" : "Chased";

                    int prefabType =
                        isHunter ? 0 : GetRandomChasedPrefabType();

                    p.assignedPrefabType = prefabType;

                    int netId = NetObject.ComputeId(p.name);
                    GameObject go = EnsureServerObject(netId, prefabType);

                    if (go != null)
                    {
                        NetPlayer netPlayerComp =
                            go.GetComponent<NetPlayer>();

                        if (netPlayerComp != null)
                        {
                            netPlayerComp.playerRole = p.data.playerRole;
                            netPlayerComp.health = isHunter ? 3 : 1;
                            netPlayerComp.isDead = false;
                            netPlayerComp.isMoving = false;
                            netPlayerComp.ApplyAnimationState();
                        }

                        if (m_replicationServer != null)
                        {
                            m_replicationServer.MarkCreate(netId);
                            m_replicationServer.MarkUpdate(netId);
                        }
                    }

                    SendText(
                        p.connection,
                        isHunter
                            ? "CHAT:Eres el CAZADOR"
                            : "CHAT:Escapa del cazador");
                }

                SendRoomState();
            }

            Started = true;
            Broadcast("START:");
        }

        void SendRoomState()
        {
            Log(
                "[SERVER] Players (" +
                m_players.Count + "/" + maxPlayers + "): " +
                Names(", "));

            foreach (Player p in m_players.ToArray())
                SendRoomStateTo(p.connection);
        }

        void SendRoomStateTo(object connection)
        {
            foreach (Player p in m_players.ToArray())
                Send(connection, Messages.Player(p.data));

            SendText(connection, "PLAYERS:" + Names(","));

            SendText(
                connection,
                "ROOM:" + serverName + "   " +
                m_players.Count + "/" + maxPlayers + " players");
        }

        void BroadcastSystem(string text)
        {
            Broadcast("CHAT:* " + text);
        }

        protected void Broadcast(string text)
        {
            byte[] message = Messages.Text(text);

            foreach (Player p in m_players.ToArray())
                Send(p.connection, message);
        }

        string Names(string separator)
        {
            List<string> names = new List<string>();

            foreach (Player p in m_players)
                names.Add(p.name);

            return string.Join(separator, names);
        }

        protected Player Find(object connection)
        {
            foreach (Player p in m_players)
            {
                if (p.connection.Equals(connection))
                    return p;
            }

            return null;
        }

        Player FindByName(string name)
        {
            foreach (Player p in m_players)
            {
                if (p.name == name)
                    return p;
            }

            return null;
        }

        protected void ClearPlayers()
        {
            foreach (ServerBullet b in m_bullets)
                DestroyBullet(b.id);

            m_bullets.Clear();
            m_players.Clear();
            m_npcCrowd = null;
            Started = false;
            MatchEnded = false;
        }

        protected void Log(string message)
        {
            LobbySession.Log(message);
        }

        public virtual void BroadcastCustomBytes(byte[] payload)
        {
            foreach (Player p in m_players)
                Send(p.connection, payload);
        }
    }
}